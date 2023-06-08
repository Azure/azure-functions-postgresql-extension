// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    /// <typeparam name="T">A user-defined POCO that represents a row of the user's table</typeparam>
    internal class PostgreSqlAsyncCollector<T> : IAsyncCollector<T>, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly PostgreSqlAttribute _attribute;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlAsyncCollector{T}"/> class.
        /// </summary>
        /// <param name="configuration">
        /// Contains the function's configuration properties
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
            Console.WriteLine("AsyncCollector Constructor");
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            this._logger = logger;

            using (NpgsqlConnection connection = BuildConnection(attribute.ConnectionStringSetting, configuration))
            {
                connection.OpenAsync().GetAwaiter().GetResult();
                // check if conncetion is open
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    throw new InvalidOperationException("Connection is not open");
                }
                // log that connection is open
                Console.WriteLine("Connection is open");
            }
        }

        public static NpgsqlConnection BuildConnection(string connectionStringSetting, IConfiguration configuration)
        {
            return new NpgsqlConnection(GetConnectionString(connectionStringSetting, configuration));
        }

        public static string GetConnectionString(string connectionStringSetting, IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(connectionStringSetting))
            {
                throw new ArgumentException("Must specify ConnectionStringSetting, which should refer to the name of an app setting that " +
                    "contains a PostgreSQL connection string");
            }
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            string connectionString = configuration.GetConnectionStringOrSetting(connectionStringSetting);
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException(connectionString == null ? $"ConnectionStringSetting '{connectionStringSetting}' is missing in your function app settings, please add the setting with a valid PostgreSQL connection string." :
                $"ConnectionStringSetting '{connectionStringSetting}' is empty in your function app settings, please update the setting with a valid PostgreSQL connection string.");
            }
            return connectionString;
        }

        /// <summary>
        /// Adds an item to this collector that is processed in a batch along with all other items added via
        /// AddAsync when <see cref="FlushAsync"/> is called. Each item is interpreted as a row to be added to the SQL table
        /// specified in the SQL Binding.
        /// </summary>
        /// <param name="item"> The item to add to the collector </param>
        /// <param name="cancellationToken">The cancellationToken is not used in this method</param>
        /// <returns> A CompletedTask if executed successfully </returns>
        public async Task AddAsync(T item, CancellationToken cancellationToken = default)
        {
            //unimplemented
            await Task.Delay(0);
        }

        /// <summary>
        /// </summary>
        /// <param name="cancellationToken">The cancellationToken is not used in this method</param>
        /// <returns> A CompletedTask if executed successfully. If no rows were added, this is returned
        /// automatically. </returns>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {

            using (NpgsqlConnection connection = BuildConnection(_attribute.ConnectionStringSetting, _configuration))
            {
                connection.OpenAsync().GetAwaiter().GetResult();
                // check if conncetion is open
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    throw new InvalidOperationException("Connection is not open");
                }

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    Console.WriteLine($"Executing command: {command.CommandText}");
                    command.CommandText = $"SELECT 1;";
                    int res = await command.ExecuteNonQueryAsync();
                    Console.WriteLine($"Result: {res} rows affected");
                }
            }
        }
        public void Dispose()
        {
            return;
        }
    }
}