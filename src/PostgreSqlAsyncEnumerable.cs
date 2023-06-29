// <copyright file="PostgreSqlAsyncEnumerable.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Npgsql;
using static Microsoft.Azure.WebJobs.Extensions.PostgreSql.PostgreSqlBindingConstants;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    /// <summary>
    /// An IAsyncEnumerable that lazily reads rows from a PostgreSql database.
    /// </summary>
    /// <typeparam name="T">A user-defined POCO that represents a row of the user's table.</typeparam>
    internal class PostgreSqlAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly PostgreSqlAttribute attribute;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlAsyncEnumerable{T}"/> class.
        /// </summary>
        /// <param name="connection">The NpgsqlConnection to be used by the enumerator.</param>
        /// <param name="attribute">The attribute containing the query, parameters, and query type.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if either connection or attribute is null.
        /// </exception>
        public PostgreSqlAsyncEnumerable(NpgsqlConnection connection, PostgreSqlAttribute attribute)
        {
            this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            this.Connection.Open();
        }

        /// <summary>
        /// Gets the NpgsqlConnection to be used by the enumerator.
        /// </summary>
        public NpgsqlConnection Connection { get; private set; }

        /// <summary>
        /// Returns the enumerator associated with this enumerable. The enumerator will execute the query specified
        /// in attribute and "lazily" grab rows corresponding to the query result. It will only read a
        /// row into memory if <see cref="PostgreSqlAsyncEnumerator.MoveNextAsync"/> is called.
        /// </summary>
        /// <param name="cancellationToken">The cancellationToken is not used in this method.</param>
        /// <returns>The enumerator.</returns>
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new PostgreSqlAsyncEnumerator(this.Connection, this.attribute);
        }

        /// <summary>
        /// An IAsyncEnumerator that lazily reads rows from a PostgreSql database.
        /// </summary>
        private class PostgreSqlAsyncEnumerator : IAsyncEnumerator<T>
        {
            private readonly NpgsqlConnection connection;
            private readonly PostgreSqlAttribute attribute;
            private NpgsqlDataReader reader;

            /// <summary>
            /// Initializes a new instance of the <see cref="PostgreSqlAsyncEnumerator"/> class.
            /// </summary>
            /// <param name="connection">The NpgsqlConnection to be used by the enumerator.</param>
            /// <param name="attribute">The attribute containing the query, parameters, and query type.</param>
            /// <exception cref="ArgumentNullException">
            /// Thrown if either connection or attribute is null.
            /// </exception>
            public PostgreSqlAsyncEnumerator(NpgsqlConnection connection, PostgreSqlAttribute attribute)
            {
                this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
                this.attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            }

            /// <summary>
            /// Gets the current row of the query result that the enumerator is on. If Current is called before a call
            /// to <see cref="MoveNextAsync"/> is ever made, it will return null. If Current is called after
            /// <see cref="MoveNextAsync"/> has moved through all of the rows returned by the query, it will return
            /// the last row of the query.
            /// </summary>
            public T Current { get; private set; }

            /// <summary>
            /// Closes the NpgSQL connection and resources associated with reading the results of the query.
            /// </summary>
            /// <returns>A ValueTask.</returns>
            public ValueTask DisposeAsync()
            {
                // Doesn't seem like there's an async version of closing the reader/connection
                this.reader?.Close();
                this.connection.Close();
                return new ValueTask(Task.CompletedTask);
            }

            /// <summary>
            /// Moves the enumerator to the next row of the PostgreSQL query result.
            /// </summary>
            /// <returns>
            /// True if there is another row left in the query to process, or false if this was the last row.
            /// </returns>
            public ValueTask<bool> MoveNextAsync()
            {
                return new ValueTask<bool>(this.GetNextRowAsync());
            }

            /// <summary>
            /// Attempts to grab the next row of the PostgreSQL query result.
            /// </summary>
            /// <returns>
            /// True if there is another row left in the query to process, or false if this was the last row.
            /// </returns>
            private async Task<bool> GetNextRowAsync()
            {
                // check connection state before trying to access the reader
                // if DisposeAsync has already closed it due to the issue described here https://github.com/Azure/azure-functions-sql-extension/issues/350
                if (this.connection.State != System.Data.ConnectionState.Closed)
                {
                    if (this.reader == null)
                    {
                        using NpgsqlCommand command = PostgreSqlBindingUtilities.BuildCommand(this.attribute, this.connection);
                        this.reader = await command.ExecuteReaderAsync();
                    }

                    if (await this.reader.ReadAsync())
                    {
                        this.Current = Utils.JsonDeserializeObject<T>(this.SerializeRow());
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Serializes the reader's current PostgreSQL row into JSON.
            /// </summary>
            /// <returns>JSON string version of the PostgreSQL row.</returns>
            private string SerializeRow()
            {
                var jsonSerializerSettings = new JsonSerializerSettings()
                {
                    DateFormatString = ISO8061DATETIMEFORMAT,
                };
                return Utils.JsonSerializeObject(PostgreSqlBindingUtilities.BuildDictionaryFromSqlRow(this.reader), jsonSerializerSettings);
            }
        }
    }
}