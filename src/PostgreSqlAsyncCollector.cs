// <copyright file="PostgreSqlAsyncCollector.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoreLinq;
using Npgsql;
using static Microsoft.Azure.WebJobs.Extensions.PostgreSql.PostgreSqlConverters;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    /// <summary>
    /// Logic for the output binding.
    /// </summary>
    /// <typeparam name="T">User defined POCO type.</typeparam>
    internal class PostgreSqlAsyncCollector<T> : IAsyncCollector<T>, IDisposable
    {
        private const int BatchSize = 1000;

        private readonly IConfiguration configuration;
        private readonly PostgreSqlAttribute attribute;
        private readonly ILogger logger;
        private readonly SemaphoreSlim rowLock = new SemaphoreSlim(1, 1);
        private readonly TimeSpan columnRefreshInterval = TimeSpan.FromMinutes(5);
        private readonly List<T> rows = new List<T>();
        private readonly PropertyInfo[] genericProperties;
        private IEnumerable<Column> columns;

        private string[] primaryKeys;
        private DateTime lastRetrievedColumns = DateTime.MinValue;

        private BatchInsertCommandComponents commandComponents;

        private string fullBatchCommandText;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlAsyncCollector{T}"/> class.
        /// </summary>
        /// <param name="configuration">
        /// Contains the resolved PostgreSql binding context.
        /// </param>
        /// <param name="attribute">
        /// Contains as one of its attributes the PostgreSQL table that rows will be inserted into.
        /// </param>
        /// <param name="logger">
        /// Logger Factory for creating an ILogger.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if either configuration or attribute is null.
        /// </exception>
        public PostgreSqlAsyncCollector(IConfiguration configuration, PostgreSqlAttribute attribute, ILogger logger)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            this.logger = logger;

            using NpgsqlConnection connection = this.CreateConnection();
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred while trying to open the database connection.");
                throw;
            }

            // check if connection is open
            if (connection.State != System.Data.ConnectionState.Open)
            {
                this.logger.LogError("Failed to open the database connection.");
                throw new InvalidOperationException("Connection is not open");
            }

            // make sure that the table is sanitized, if not throw error
            VerifyCleanTableName(attribute.CommandText);

            this.genericProperties = typeof(T).GetProperties();
        }

        /// <summary>
        /// Adds an item to this collector that is processed in a batch along with all other items added via
        /// AddAsync when <see cref="FlushAsync"/> is called. Each item is interpreted as a row to be added to the PostgreSQL table
        /// specified in the PostgreSQL Binding.
        /// </summary>
        /// <param name="item"> The item to add to the collector. </param>
        /// <param name="cancellationToken">The cancellationToken is not used in this method.</param>
        /// <returns> A CompletedTask if executed successfully. </returns>
        public async Task AddAsync(T item, CancellationToken cancellationToken = default)
        {
            if (item != null)
            {
                await this.rowLock.WaitAsync(cancellationToken);
                try
                {
                    this.rows.Add(item);
                }
                finally
                {
                    this.rowLock.Release();
                }
            }
        }

        /// <summary>
        /// Responsible for outputing to the database.
        /// </summary>
        /// <param name="cancellationToken">The cancellationToken is not used in this method.</param>
        /// <returns> A CompletedTask if executed successfully. If no rows were added, this is returned
        /// automatically. </returns>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            await this.rowLock.WaitAsync(cancellationToken);
            try
            {
                if (this.rows.Count != 0)
                {
                    await this.SetColumnData();

                    // TODO make a validity check that we can upsert
                    this.RunValidityCheck();
                    await this.UpsertRowsAsync(this.rows, this.attribute, this.configuration);
                    this.rows.Clear();
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred while trying to flush the rows.");
                this.logger.LogError(ex.StackTrace);
                throw;
            }
            finally
            {
                this.rowLock.Release();
            }
        }

        /// <summary>
        /// Disposes the rowlock semaphore.
        /// </summary>
        public void Dispose()
        {
            this.rowLock.Dispose();
        }

        /// <summary>
        /// Verifies that the table name is valid to prevent SQL injection.
        /// </summary>
        /// <param name="tableName">Table name to be verified.</param>
        /// <exception cref="ArgumentException">Thrown if the table name contains invalid characters.</exception>
        private static void VerifyCleanTableName(string tableName)
        {
            // make sure only allowed characters are in the fully qualified table name
            // allowed characters are alphanumeric, underscores, and periods
            if (!Regex.IsMatch(tableName, @"^[a-zA-Z0-9_\.]+$"))
            {
                throw new ArgumentException("The table name contains invalid characters. Only alphanumeric, underscores, and periods are allowed.");
            }
        }

        /// <summary>
        /// Creates a NpgsqlConnection using the connection string specified in the PostgreSQL binding.
        /// </summary>
        /// <returns> A NpgsqlConnection. </returns>
        private NpgsqlConnection CreateConnection()
        {
            string connectionString = this.attribute.ConnectionStringSetting;
            return new NpgsqlConnection(connectionString);
        }

        /// <summary>
        /// Generates a BatchInsertCommandComponents object for batch operations.
        /// </summary>
        /// <param name="table">The target table name.</param>
        /// <param name="primaryKeys">The primary key columns for conflict resolution.</param>
        /// <param name="maxBatchSize">The maximum rows per batch insert.</param>
        /// <returns>A BatchInsertCommandComponents object.</returns>
        private BatchInsertCommandComponents CreateReusableCommandComponents(string table, string[] primaryKeys, int maxBatchSize)
        {
            BatchInsertCommandComponents components = new BatchInsertCommandComponents
            {
                // create insert clause: INSERT INTO table (column1, column2, ...) VALUES
                // create conflict clause: ON CONFLICT (primaryKey1, primaryKey2, ...) DO UPDATE SET
                InsertClause = $"INSERT INTO {table} ({string.Join(", ", this.genericProperties.Select(p => p.Name))}) VALUES ",
                ConflictClause = primaryKeys.Length == 0 ? string.Empty : $" ON CONFLICT ({string.Join(", ", primaryKeys)}) DO UPDATE SET ",
            };

            // add to conflict clause all columns that are not primary keys
            foreach (var property in this.genericProperties)
            {
                if (!primaryKeys.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                {
                    components.ConflictClause += $"{property.Name} = EXCLUDED.{property.Name}, ";
                }
            }

            components.ConflictClause = components.ConflictClause.TrimEnd(',', ' ');

            // now generate a max batch size values clause for reuse when handling batches
            components.ValuesClause = this.GenerateValuesClause(maxBatchSize);

            return components;
        }

        /// <summary>
        /// Generates a parameterized values clause for a SQL query.
        /// Format is as follows: (@columnA1, @columnB2, @columnC3), (@columnA4, @columnB5, @columnC6), ...
        /// </summary>
        /// <param name="len">Number of parameterized rows to generate.</param>
        /// <returns>Values clause as a string.</returns>
        private string GenerateValuesClause(int len)
        {
            StringBuilder valuesClause = new StringBuilder();

            for (int i = 0; i < len; i++)
            {
                valuesClause.Append('(');
                valuesClause.Append(string.Join(", ", this.genericProperties.Select(p => $"@{p.Name}{i}")));
                valuesClause.Append("), ");
            }

            return valuesClause.ToString().TrimEnd(',', ' ');
        }

        /// <summary>
        /// Creates a NpgsqlCommand for batch insert operations.
        /// </summary>
        /// <param name="batch">Batch of items to be inserted.</param>
        /// <param name="components">Command components for batch insert.</param>
        /// <returns>A NpgsqlCommand object.</returns>
        private NpgsqlCommand CreateBatchInsertCommand(IEnumerable<T> batch, BatchInsertCommandComponents components)
        {
            NpgsqlCommand command = new NpgsqlCommand();

            // if we have a full batch, we can use the full command text
            // this will be the case for all batches except the last one
            if (batch.Count() == BatchSize)
            {
                command.CommandText = this.fullBatchCommandText;
            }

            // otherwise, we need to generate a new command text using the batch size
            // this may be the case for the last batch only
            else
            {
                command.CommandText = $"{components.InsertClause} {this.GenerateValuesClause(batch.Count())} {components.ConflictClause}";
            }

            // now add the actual values as parameters
            command.Parameters.AddRange(this.CreateParameters(batch));

            return command;
        }

        /// <summary>
        /// Creates a list of parameters for a NpgsqlCommand.
        /// </summary>
        /// <param name="batch">Batch of items to create params for.</param>
        /// <returns>Array of NpgsqlParameters.</returns>
        private Array CreateParameters(IEnumerable<T> batch)
        {
            List<NpgsqlParameter> parameters = new List<NpgsqlParameter>();
            int i = 0;
            foreach (T item in batch)
            {
                foreach (var property in this.genericProperties)
                {
                    parameters.Add(new NpgsqlParameter($"@{property.Name}{i}", property.GetValue(item)));
                }

                i++;
            }

            return parameters.ToArray();
        }

        /// <summary>
        /// Checks the validity of table schema against the properties of T. Throws an exception if the table schema is invalid.
        /// </summary>
        private void RunValidityCheck()
        {
            var columnNames = this.columns.Select(c => c.ColumnName);

            // check that we can upsert
            // throw an exception if we can't

            // make sure that the properties of T match the columns in the table
            foreach (var property in this.genericProperties)
            {
                if (!columnNames.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"The property {property.Name} does not exist in the table {this.attribute.CommandText}.");
                }
            }
        }

        /// <summary>
        /// Retrieves column properties of a table and caches them for future use.
        /// </summary>
        /// <returns>A Task.</returns>
        private async Task SetColumnData()
        {
            // if we haven't retrieved the columns yet, or if column data is expired, get them again
            if (this.columns == null || this.lastRetrievedColumns.Add(this.columnRefreshInterval) < DateTime.Now)
            {
                Stopwatch stopwatch = new Stopwatch();
                this.columns = await this.GetColumnPropertiesAsync(this.attribute, this.configuration);
                this.primaryKeys = this.columns.Where(c => c.IsPrimaryKey == "true").Select(c => c.ColumnName).ToArray();
                this.lastRetrievedColumns = DateTime.Now;
                stopwatch.Stop();
                this.logger.LogInformation($"Cache Miss: Retrieved column data in {stopwatch.ElapsedMilliseconds} ms.");
            }
        }

        /// <summary>
        /// Retrieves metadata about each column in a table.
        /// </summary>
        /// <param name="attribute">PostgreSQL table attribute.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <returns>An Task of IEnumerable of Column objects.</returns>
        private async Task<IEnumerable<Column>> GetColumnPropertiesAsync(PostgreSqlAttribute attribute, IConfiguration configuration)
        {
            PostgreSqlGenericsConverter<Column> converter = new PostgreSqlGenericsConverter<Column>(this.configuration, this.logger);

            // sql query to retrieve column properties
            // gets column name, data type, and whether or not it's a primary key
            string columnPropertyQuery = $"SELECT a.attname AS \"ColumnName\", format_type(a.atttypid, a.atttypmod) AS \"DataType\", CASE WHEN a.attnum = ANY(i.indkey) THEN TRUE ELSE FALSE END AS \"IsPrimaryKey\" FROM pg_attribute a LEFT JOIN pg_index i ON a.attrelid = i.indrelid WHERE a.attnum > 0 AND NOT a.attisdropped AND a.attrelid = '{attribute.CommandText}'::regclass;";

            // TODO could query the database for a list of the tables and use that as a whitelist
            IEnumerable<Column> columns = await converter.ConvertAsync(columnPropertyQuery, this.attribute, CancellationToken.None);

            return columns;
        }

        /// <summary>
        /// Inserts or updates rows in a PostgreSQL table.
        /// </summary>
        /// <param name="rows">Rows to be upserted.</param>
        /// <param name="attribute">PostgreSQL table attribute.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <returns>A Task.</returns>
        private async Task UpsertRowsAsync(IList<T> rows, PostgreSqlAttribute attribute, IConfiguration configuration)
        {
            Stopwatch upsert_stopwatch = Stopwatch.StartNew();
            using NpgsqlConnection connection = this.CreateConnection();
            await connection.OpenAsync();

            // table name is the full command text
            string fullTableName = attribute.CommandText;

            // create the reusable command components if they don't exist
            this.commandComponents ??= this.CreateReusableCommandComponents(this.attribute.CommandText, this.primaryKeys, BatchSize);

            // create a command text for the full batch if it doesn't exist
            this.fullBatchCommandText ??= $"{this.commandComponents.InsertClause} {this.commandComponents.ValuesClause} {this.commandComponents.ConflictClause}";

            try
            {
                // Starting the transaction.
                using var transaction = await connection.BeginTransactionAsync();

                // Partition the rows into batches.
                foreach (IEnumerable<T> batch in rows.Batch(BatchSize))
                {
                    using NpgsqlCommand command = this.CreateBatchInsertCommand(batch, this.commandComponents);
                    command.Connection = connection;
                    await command.ExecuteNonQueryAsync();
                }

                // Commit the transaction.
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "An error occurred while upserting rows.");
                throw;
            }
            finally
            {
                upsert_stopwatch.Stop();
                this.logger.LogInformation($"Upserted {rows.Count} rows in {upsert_stopwatch.ElapsedMilliseconds} ms.");
            }
        }
    }
}