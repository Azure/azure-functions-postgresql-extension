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

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    internal class PostgreSqlAsyncCollector<T> : IAsyncCollector<T>, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly PostgreSqlAttribute _attribute;
        private readonly ILogger _logger;

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
            Console.WriteLine("AsyncCollector Constructor");
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            this._logger = logger;


            Console.WriteLine("AsyncCollector Constructor: " + this._attribute.ConnectionStringSetting);

            using (NpgsqlConnection connection = CreateConnection())
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
            // add the item right away
            Console.WriteLine("AsyncCollector AddAsync: " + item);
            using (NpgsqlConnection connection = this.CreateConnection())
            {
                connection.OpenAsync().GetAwaiter().GetResult();
                // check if conncetion is open
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    throw new InvalidOperationException("Connection is not open");
                }

                using (NpgsqlCommand command = createInsertCommand("inventory", item, connection))
                {
                    Console.WriteLine("Executing SQL command: " + command.CommandText);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        private NpgsqlConnection CreateConnection()
        {
            string connectionString = this._attribute.ConnectionStringSetting;
            return new NpgsqlConnection(connectionString);
        }

        private NpgsqlCommand createInsertCommand(string table, T item, NpgsqlConnection conn)
        {
            var properties = typeof(T).GetProperties();

            var sqlCommand = new StringBuilder($"INSERT INTO {table} (");
            var sqlCommandValues = new StringBuilder(" VALUES (");
            var parameters = new List<NpgsqlParameter>();

            foreach (var property in properties)
            {
                sqlCommand.Append($"{property.Name}, ");
                sqlCommandValues.Append($"@{property.Name}, ");

                var value = property.GetValue(item);
                if (value == null)
                {
                    throw new Exception($"No value associated with key {property.Name}");
                }

                parameters.Add(new NpgsqlParameter(property.Name, value));
            }

            sqlCommand.Length -= 2; // Remove trailing comma and space
            sqlCommandValues.Length -= 2; // Remove trailing comma and space

            sqlCommand.Append(")");
            sqlCommandValues.Append(")");

            var commandText = sqlCommand.ToString() + sqlCommandValues.ToString();
            var command = new NpgsqlCommand(commandText, conn);

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

            // dont care about this for now

            Console.WriteLine("AsyncCollector FlushAsync");



            await Task.Delay(0);
        }


        public void Dispose()
        {
            return;
        }
    }
}