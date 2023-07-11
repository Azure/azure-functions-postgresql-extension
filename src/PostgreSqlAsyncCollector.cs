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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        private IEnumerable<Column> columns;

        private string[] primaryKeys;
        private DateTime lastRetrievedColumns = DateTime.MinValue;

        private string fullCommandText;

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
        }

        /// <summary>
        /// Verifies that the table name is valid to prevent SQL injection.
        /// </summary>
        /// <param name="tableName">Table name to be verified.</param>
        /// <exception cref="ArgumentException">Thrown if the table name contains invalid characters.</exception>
        public static void VerifyCleanTableName(string tableName)
        {
            // make sure only allowed characters are in the fully qualified table name
            // allowed characters are alphanumeric, underscores, and periods
            if (tableName.Equals(string.Empty) || !Regex.IsMatch(tableName, @"^[a-zA-Z0-9_\.]+$"))
            {
                throw new ArgumentException("The table name contains invalid characters. Only alphanumeric, underscores, and periods are allowed.");
            }
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
        /// Creates a NpgsqlParameter for a batch of rows.
        /// </summary>
        /// <param name="batch">Batch of rows to be upserted.</param>
        /// <returns>A NpgsqlParameter.</returns>
        private static NpgsqlParameter CreateBatchValueParameter(IEnumerable<T> batch)
        {
            string batchJsonData = Utils.JsonSerializeObject(batch);

            Console.WriteLine("@jsonData Param Value:\n" + batchJsonData);

            return new NpgsqlParameter("@jsonData", batchJsonData);
        }

        /// <summary>
        /// Gets the column names from PropertyInfo when T is POCO
        /// and when T is JObject, parses the data to get column names.
        /// </summary>
        /// <param name="row"> Sample row used to get the column names when item is a JObject.</param>
        /// <returns>List of column names in the table.</returns>
        private static IEnumerable<string> GetColumnNamesFromItem(T row)
        {
            if (typeof(T) == typeof(JObject))
            {
                var jsonObj = JObject.Parse(row.ToString());
                Dictionary<string, string> dictObj = jsonObj.ToObject<Dictionary<string, string>>();
                return dictObj.Keys;
            }

            return typeof(T).GetProperties().Select(prop => prop.Name);
        }

        /// <summary>
        /// Creates a NpgsqlConnection using the connection string specified in the PostgreSQL binding.
        /// </summary>
        /// <returns> A NpgsqlConnection. </returns>
        private NpgsqlConnection CreateConnection()
        {
            return PostgreSqlBindingUtilities.BuildConnection(this.attribute.ConnectionStringSetting, this.configuration);
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

            // create a command text for the full batch if it doesn't exist
            this.fullCommandText ??= this.GenerateInsertText(fullTableName, this.columns, this.rows.First());

            Console.WriteLine("Full Command Text:\n" + this.fullCommandText);

            try
            {
                // Starting the transaction.
                using var transaction = await connection.BeginTransactionAsync();

                // Create the baseline command.
                NpgsqlCommand command = new NpgsqlCommand(this.fullCommandText, connection, transaction);

                // Partition the rows into batches.
                foreach (IEnumerable<T> batch in rows.Batch(BatchSize))
                {
                    // insert the parameters into the command and execute
                    command.Parameters.Clear();
                    command.Parameters.Add(CreateBatchValueParameter(batch));
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

        /// <summary>
        /// Creates the insert text for json to be inserted into a table.
        /// </summary>
        /// <param name="fullTableName">Full name of the table.</param>
        /// <param name="columns">Columns of the table.</param>
        /// <param name="row">Sample row used to get the column names of the actual data being sent.</param>
        /// <returns>Insert text.</returns>
        private string GenerateInsertText(string fullTableName, IEnumerable<Column> columns, T row)
        {
            IEnumerable<string> columnNamesFromItem = GetColumnNamesFromItem(row);
            IEnumerable<Column> filteredColumns = columns.Where(c => columnNamesFromItem.Contains(c.ColumnName));
            string csColumnNameTypes = string.Join(", ", filteredColumns.Select(c => $"\"{c.ColumnName}\" {c.DataType}"));
            string csColumnNames = string.Join(", ", filteredColumns.Select(c => $"\"{c.ColumnName}\""));
            string csPrimaryKeyColumns = string.Join(", ", this.primaryKeys.Select(c => $"\"{c}\""));
            Column[] nonPrimaryKeyColumns = filteredColumns.Where(c => !this.primaryKeys.Contains(c.ColumnName)).ToArray();
            string excludeStatement = string.Join(", ", nonPrimaryKeyColumns.Select(c => $"\"{c.ColumnName}\" = excluded.\"{c.ColumnName}\""));

            string insertText = $@"WITH cte AS (SELECT * FROM json_to_recordset(@jsonData::json) AS ({csColumnNameTypes})) INSERT INTO {fullTableName}({csColumnNames}) SELECT {csColumnNames} FROM cte ON CONFLICT ({csPrimaryKeyColumns}) DO UPDATE SET {excludeStatement};";

            return insertText;
        }
    }
}