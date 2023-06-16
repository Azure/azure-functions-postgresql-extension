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

        private NpgsqlCommand CreateBatchInsertCommand(string table, IEnumerable<T> batch, NpgsqlConnection conn)
        {
            var properties = typeof(T).GetProperties();
            var sqlCommandBatch = new StringBuilder();
            var parameters = new List<NpgsqlParameter>();
            int itemIndex = 0;

            // Generate primary keys clause
            var primaryKeys = _columns.Where(c => c.IsPrimaryKey == "true").Select(c => c.ColumnName).ToArray();
            var pkConflictClause = string.Join(", ", primaryKeys);

            foreach (T item in batch)
            {
                var sqlCommand = new StringBuilder($"INSERT INTO {table} (");
                var sqlCommandValues = new StringBuilder(" VALUES (");
                var sqlCommandConflict = new StringBuilder($" ON CONFLICT ({pkConflictClause}) DO UPDATE SET ");

                foreach (var property in properties)
                {
                    var value = property.GetValue(item);
                    if (value == null)
                    {
                        continue;
                    }

                    sqlCommand.Append($"{property.Name}, ");
                    sqlCommandValues.Append($"@{property.Name}{itemIndex}, ");

                    // Only append to conflict statement if it's not a primary key
                    if (!primaryKeys.Contains(property.Name))
                    {
                        sqlCommandConflict.Append($"{property.Name} = EXCLUDED.{property.Name}, ");
                    }

                    parameters.Add(new NpgsqlParameter($"{property.Name}{itemIndex}", value));
                }

                sqlCommand.Length -= 2; // Remove trailing comma and space
                sqlCommandValues.Length -= 2; // Remove trailing comma and space
                sqlCommandConflict.Length -= 2; // Remove trailing comma and space

                sqlCommand.Append(')');
                sqlCommandValues.Append(')');
                sqlCommand.Append(sqlCommandValues);

                if (primaryKeys.Length > 0) // Only append conflict clause if there are primary keys
                {
                    sqlCommand.Append(sqlCommandConflict);
                }

                sqlCommand.Append("; ");

                sqlCommandBatch.Append(sqlCommand);

                itemIndex++;
            }

            var command = new NpgsqlCommand(sqlCommandBatch.ToString(), conn);

            parameters.ForEach(p => command.Parameters.Add(p));

            return command;
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
                    _columns = await GetColumnPropertiesAsync(_attribute, _configuration); //TODO move to initialization
                    await UpsertRowsAsync(_rows, _attribute, _configuration);
                    _rows.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while trying to flush the rows.");
                throw;
            }
            finally
            {
                _rowLock.Release();
            }
        }

        private async Task<IEnumerable<Column>> GetColumnPropertiesAsync(PostgreSqlAttribute attribute, IConfiguration configuration)
        {
            PostgreSqlGenericsConverter<Column> converter = new PostgreSqlGenericsConverter<Column>(_configuration, _logger);
            string columnPropertyQuery = $"SELECT a.attname AS \"ColumnName\", format_type(a.atttypid, a.atttypmod) AS \"DataType\", CASE WHEN a.attnum = ANY(i.indkey) THEN TRUE ELSE FALSE END AS \"IsPrimaryKey\" FROM pg_attribute a LEFT JOIN pg_index i ON a.attrelid = i.indrelid WHERE a.attnum > 0 AND NOT a.attisdropped AND a.attrelid = '{attribute.CommandText}'::regclass;"; // TODO: use parameters instead of string interpolation for security
            // print the query
            IEnumerable<Column> columns = await converter.ConvertAsync(columnPropertyQuery, _attribute, new CancellationToken());

            return columns;
        }

        private async Task UpsertRowsAsync(IList<T> rows, PostgreSqlAttribute attribute, IConfiguration configuration)
        {
            using NpgsqlConnection connection = CreateConnection();
            await connection.OpenAsync();
            string fullTableName = attribute.CommandText;

            try
            {
                // Starting the transaction.
                using var transaction = await connection.BeginTransactionAsync();
                int batchSize = 1000;

                // Partition the rows into batches.
                foreach (IEnumerable<T> batch in rows.Batch(batchSize))
                {
                    using NpgsqlCommand command = CreateBatchInsertCommand(fullTableName, batch, connection);
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

}