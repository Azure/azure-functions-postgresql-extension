// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Npgsql;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using NpgsqlTypes;
using MoreLinq;
using static Microsoft.Azure.WebJobs.Extensions.PostgreSql.PostgreSqlConverters;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    internal class PostgreSqlAsyncCollector<T> : IAsyncCollector<T>, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly PostgreSqlAttribute _attribute;
        private readonly ILogger _logger;

        private readonly SemaphoreSlim _rowLock = new SemaphoreSlim(1, 1);

        private readonly List<T> _rows = new List<T>();

        private IEnumerable<Column> _columns;

        private string[] _primaryKeys;
        private DateTime _lastRetrievedColumns = DateTime.MinValue;
        private readonly TimeSpan _columnRefreshInterval = TimeSpan.FromMinutes(5);

        private const int _batchSize = 1000;

        private readonly PropertyInfo[] _generic_properties;

        private BatchInsertCommandComponents _commandComponents;

        private string _fullBatchCommandText;


        /// <summary>
        /// Initializes a new instance of the PostgreSqlAsyncCollector class.
        /// </summary>
        /// <param name="configuration">
        /// Contains the resolved PostgreSql binding context
        /// </param>
        /// <param name="attribute">
        /// Contains as one of its attributes the PostgreSQL table that rows will be inserted into
        /// </param>
        /// <param name="logger">
        /// Logger Factory for creating an ILogger
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if either configuration or attribute is null
        /// </exception>
        public PostgreSqlAsyncCollector(IConfiguration configuration, PostgreSqlAttribute attribute, ILogger logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            _logger = logger;

            using NpgsqlConnection connection = CreateConnection();
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to open the database connection.");
                throw;
            }

            // check if connection is open
            if (connection.State != System.Data.ConnectionState.Open)
            {
                _logger.LogError("Failed to open the database connection.");
                throw new InvalidOperationException("Connection is not open");
            }

            _generic_properties = typeof(T).GetProperties();
        }

        /// <summary>
        /// Adds an item to this collector that is processed in a batch along with all other items added via
        /// AddAsync when <see cref="FlushAsync"/> is called. Each item is interpreted as a row to be added to the PostgreSQL table
        /// specified in the PostgreSQL Binding.
        /// </summary>
        /// <param name="item"> The item to add to the collector </param>
        /// <param name="cancellationToken">The cancellationToken is not used in this method</param>
        /// <returns> A CompletedTask if executed successfully </returns>
        public async Task AddAsync(T item, CancellationToken cancellationToken = default)
        {
            if (item != null)
            {
                await _rowLock.WaitAsync(cancellationToken);
                try
                {
                    _rows.Add(item);
                }
                finally
                {
                    _rowLock.Release();
                }
            }

        }

        private NpgsqlConnection CreateConnection()
        {
            string connectionString = _attribute.ConnectionStringSetting;
            return new NpgsqlConnection(connectionString);
        }

        private BatchInsertCommandComponents CreateReusableCommandComponents(string table, string[] primaryKeys, int maxBatchSize)
        {
            BatchInsertCommandComponents components = new BatchInsertCommandComponents
            {
                // create insert clause
                InsertClause = $"INSERT INTO {table} ({string.Join(", ", _generic_properties.Select(p => p.Name))}) VALUES ",
                ConflictClause = primaryKeys.Length == 0 ? "" : $" ON CONFLICT ({string.Join(", ", primaryKeys)}) DO UPDATE SET "
            };
            foreach (var property in _generic_properties)
            {
                if (!primaryKeys.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                {
                    components.ConflictClause += $"{property.Name} = EXCLUDED.{property.Name}, ";
                }
            }
            components.ConflictClause = components.ConflictClause.TrimEnd(',', ' ');

            // now generate a max batch size values clause
            components.ValuesClause = GenerateValuesClause(maxBatchSize);

            return components;
        }

        private string GenerateValuesClause(int len)
        {
            StringBuilder valuesClause = new StringBuilder();

            for (int i = 0; i < len; i++)
            {
                valuesClause.Append('(');
                valuesClause.Append(string.Join(", ", _generic_properties.Select(p => $"@{p.Name}{i}")));
                valuesClause.Append("), ");
            }

            return valuesClause.ToString().TrimEnd(',', ' ');
        }

        private NpgsqlCommand CreateBatchInsertCommand(IEnumerable<T> batch, BatchInsertCommandComponents components)
        {

            NpgsqlCommand command = new NpgsqlCommand();

            if (batch.Count() == _batchSize)
            {
                command.CommandText = _fullBatchCommandText;
            }
            else
            {
                command.CommandText = $"{components.InsertClause} {GenerateValuesClause(batch.Count())} {components.ConflictClause}";
            }

            //now add parameters
            command.Parameters.AddRange(CreateParameters(batch));

            return command;
        }

        private Array CreateParameters(IEnumerable<T> batch)
        {
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();
            int i = 0;
            foreach (T item in batch)
            {
                foreach (var property in _generic_properties)
                {
                    parameters.Add(new NpgsqlParameter($"@{property.Name}{i}", property.GetValue(item)));
                }
                i++;
            }

            return parameters.ToArray();
        }


        /// <summary>
        /// </summary>
        /// <param name="cancellationToken">The cancellationToken is not used in this method</param>
        /// <returns> A CompletedTask if executed successfully. If no rows were added, this is returned
        /// automatically. </returns>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            await _rowLock.WaitAsync(cancellationToken);
            try
            {
                if (_rows.Count != 0)
                {
                    await SetColumnData();
                    // TODO make a validity check that we can upsert
                    RunValidityCheck();
                    await UpsertRowsAsync(_rows, _attribute, _configuration);
                    _rows.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to flush the rows.");
                _logger.LogError(ex.StackTrace);
                throw;
            }
            finally
            {
                _rowLock.Release();
            }
        }

        private void RunValidityCheck()
        {
            var columnNames = _columns.Select(c => c.ColumnName);

            // check that we can upsert
            // throw an exception if we can't

            // make sure that the properties of T match the columns in the table
            foreach (var property in _generic_properties)
            {
                if (!columnNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"The property {property.Name} does not exist in the table {_attribute.CommandText}.");
                }
            }


        }

        private async Task SetColumnData()
        {
            // if we haven't retrieved the columns yet, or if column data is expired, get them again
            if (_columns == null || _lastRetrievedColumns.Add(_columnRefreshInterval) < DateTime.Now)
            {
                Stopwatch stopwatch = new Stopwatch();
                _columns = await GetColumnPropertiesAsync(_attribute, _configuration);
                _primaryKeys = _columns.Where(c => c.IsPrimaryKey == "true").Select(c => c.ColumnName).ToArray();
                _lastRetrievedColumns = DateTime.Now;
                stopwatch.Stop();
                _logger.LogInformation($"Cache Miss: Retrieved column data in {stopwatch.ElapsedMilliseconds} ms.");
            }

        }

        private async Task<IEnumerable<Column>> GetColumnPropertiesAsync(PostgreSqlAttribute attribute, IConfiguration configuration)
        {
            PostgreSqlGenericsConverter<Column> converter = new PostgreSqlGenericsConverter<Column>(_configuration, _logger);
            string columnPropertyQuery = $"SELECT a.attname AS \"ColumnName\", format_type(a.atttypid, a.atttypmod) AS \"DataType\", CASE WHEN a.attnum = ANY(i.indkey) THEN TRUE ELSE FALSE END AS \"IsPrimaryKey\" FROM pg_attribute a LEFT JOIN pg_index i ON a.attrelid = i.indrelid WHERE a.attnum > 0 AND NOT a.attisdropped AND a.attrelid = '{attribute.CommandText}'::regclass;";
            // could query the database for a list of the tables and use that as a whitelist
            IEnumerable<Column> columns = await converter.ConvertAsync(columnPropertyQuery, _attribute, new CancellationToken());

            return columns;
        }

        private async Task UpsertRowsAsync(IList<T> rows, PostgreSqlAttribute attribute, IConfiguration configuration)
        {
            Stopwatch upsert_stopwatch = Stopwatch.StartNew();
            using NpgsqlConnection connection = CreateConnection();
            await connection.OpenAsync();
            string fullTableName = attribute.CommandText;

            // create the reusable command components if they don't exist
            _commandComponents ??= CreateReusableCommandComponents(_attribute.CommandText, _primaryKeys, _batchSize);

            _fullBatchCommandText ??= $"{_commandComponents.InsertClause} {_commandComponents.ValuesClause} {_commandComponents.ConflictClause}";


            try
            {
                // Starting the transaction.
                using var transaction = await connection.BeginTransactionAsync();


                // Partition the rows into batches.
                foreach (IEnumerable<T> batch in rows.Batch(_batchSize))
                {
                    using NpgsqlCommand command = CreateBatchInsertCommand(batch, _commandComponents);
                    command.Connection = connection;
                    await command.ExecuteNonQueryAsync();
                }

                // Commit the transaction.
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while upserting rows.");
                throw;
            }
            finally
            {
                upsert_stopwatch.Stop();
                _logger.LogInformation($"Upserted {rows.Count} rows in {upsert_stopwatch.ElapsedMilliseconds} ms.");
            }
        }

        public void Dispose()
        {
            _rowLock.Dispose();
        }
    }

    internal class Column
    {
        public string ColumnName { get; set; }
        public string DataType { get; set; }
        public string IsPrimaryKey { get; set; }

        public override string ToString()
        {
            return $"ColumnName: {ColumnName}, DataType: {DataType}, IsPrimaryKey: {IsPrimaryKey}";
        }
    }

    internal class BatchInsertCommandComponents
    {
        public string InsertClause { get; set; }
        public string ValuesClause { get; set; }
        public string ConflictClause { get; set; }

        public override string ToString()
        {
            return $"InsertClause: {InsertClause}, ValuesClause: {ValuesClause}, ConflictClause: {ConflictClause}";
        }

    }

}