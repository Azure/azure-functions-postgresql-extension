// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Npgsql;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    internal class PostgreSqlAsyncCollector : IAsyncCollector<string>, IDisposable
    {
        private readonly PostgreSqlBindingContext _context;
        private readonly PostgreSqlAttribute _attribute;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlAsyncCollector"/> class.
        /// </summary>
        /// <param name="context">
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
        public PostgreSqlAsyncCollector(PostgreSqlBindingContext context, PostgreSqlAttribute attribute, ILogger logger)
        {
            Console.WriteLine("AsyncCollector Constructor");
            this._context = context ?? throw new ArgumentNullException(nameof(context));
            this._attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            this._logger = logger;


            Console.WriteLine("AsyncCollector Constructor: " + this._attribute.ConnectionStringSetting);

            using (NpgsqlConnection connection = this._context.Connection)
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
        public async Task AddAsync(string item, CancellationToken cancellationToken = default)
        {
            Console.WriteLine("AsyncCollector AddAsync: " + item);
            await Task.Delay(0);
        }

        /// <summary>
        /// </summary>
        /// <param name="cancellationToken">The cancellationToken is not used in this method</param>
        /// <returns> A CompletedTask if executed successfully. If no rows were added, this is returned
        /// automatically. </returns>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {

            Console.WriteLine("AsyncCollector FlushAsync");

            using (NpgsqlConnection connection = this.CreateConnection())
            {
                connection.OpenAsync().GetAwaiter().GetResult();
                // check if conncetion is open
                if (connection.State != System.Data.ConnectionState.Open)
                {
                    throw new InvalidOperationException("Connection is not open");
                }

                using (NpgsqlCommand command = connection.CreateCommand())
                {
                    // Console.WriteLine($"Executing command: {command.CommandText}");
                    // command.CommandText = $"select * from inventory;";
                    await using (var cmd = new NpgsqlCommand("select * from inventory", connection))
                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Console.WriteLine(reader.GetString(1));
                        }
                    }
                }
            }

            await Task.Delay(0);
        }

        private NpgsqlConnection CreateConnection()
        {
            string connectionString = this._attribute.ConnectionStringSetting;
            return new NpgsqlConnection(connectionString);
        }
        public void Dispose()
        {
            return;
        }
    }
}