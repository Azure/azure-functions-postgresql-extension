// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Npgsql;
using static Microsoft.Azure.WebJobs.Extensions.PostgreSql.PostgreSqlBindingConstants;


namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    /// <typeparam name="T">A user-defined POCO that represents a row of the user's table</typeparam>
    internal class PostgreSqlAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public NpgsqlConnection Connection { get; private set; }
        private readonly PostgreSqlAttribute _attribute;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlAsyncEnumerable{T}"/> class.
        /// </summary>
        /// <param name="connection">The NpgsqlConnection to be used by the enumerator</param>
        /// <param name="attribute">The attribute containing the query, parameters, and query type</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if either connection or attribute is null
        /// </exception>
        public PostgreSqlAsyncEnumerable(NpgsqlConnection connection, PostgreSqlAttribute attribute)
        {
            Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            Connection.Open();
        }
        /// <summary>
        /// Returns the enumerator associated with this enumerable. The enumerator will execute the query specified
        /// in attribute and "lazily" grab rows corresponding to the query result. It will only read a
        /// row into memory if <see cref="PostgreSqlAsyncEnumerator.MoveNextAsync"/> is called
        /// </summary>
        /// <param name="cancellationToken">The cancellationToken is not used in this method</param>
        /// <returns>The enumerator</returns>
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new PostgreSqlAsyncEnumerator(Connection, _attribute);
        }


        private class PostgreSqlAsyncEnumerator : IAsyncEnumerator<T>
        {
            private readonly NpgsqlConnection _connection;
            private readonly PostgreSqlAttribute _attribute;
            private NpgsqlDataReader _reader;
            /// <summary>
            /// Initializes a new instance of the <see cref="PostgreSqlAsyncEnumerator"/> class.
            /// </summary>
            /// <param name="connection">The NpgsqlConnection to be used by the enumerator</param>
            /// <param name="attribute">The attribute containing the query, parameters, and query type</param>
            /// <exception cref="ArgumentNullException">
            /// Thrown if either connection or attribute is null
            /// </exception>
            public PostgreSqlAsyncEnumerator(NpgsqlConnection connection, PostgreSqlAttribute attribute)
            {
                _connection = connection ?? throw new ArgumentNullException(nameof(connection));
                _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            }

            /// <summary>
            /// Returns the current row of the query result that the enumerator is on. If Current is called before a call
            /// to <see cref="MoveNextAsync"/> is ever made, it will return null. If Current is called after
            /// <see cref="MoveNextAsync"/> has moved through all of the rows returned by the query, it will return
            /// the last row of the query.
            /// </summary>
            public T Current { get; private set; }

            /// <summary>
            /// Closes the NpgSQL connection and resources associated with reading the results of the query
            /// </summary>
            /// <returns></returns>
            public ValueTask DisposeAsync()
            {
                // Doesn't seem like there's an async version of closing the reader/connection
                _reader?.Close();
                _connection.Close();
                return new ValueTask(Task.CompletedTask);
            }

            /// <summary>
            /// Moves the enumerator to the next row of the PostgreSQL query result
            /// </summary>
            /// <returns>
            /// True if there is another row left in the query to process, or false if this was the last row
            /// </returns>
            public ValueTask<bool> MoveNextAsync()
            {
                return new ValueTask<bool>(GetNextRowAsync());
            }

            /// <summary>
            /// Attempts to grab the next row of the PostgreSQL query result.
            /// </summary>
            /// <returns>
            /// True if there is another row left in the query to process, or false if this was the last row
            /// </returns>
            private async Task<bool> GetNextRowAsync()
            {
                // check connection state before trying to access the reader
                // if DisposeAsync has already closed it due to the issue described here https://github.com/Azure/azure-functions-sql-extension/issues/350
                if (_connection.State != System.Data.ConnectionState.Closed)
                {
                    if (_reader == null)
                    {
                        using NpgsqlCommand command = PostgreSqlBindingUtilities.BuildCommand(_attribute, _connection);
                        _reader = await command.ExecuteReaderAsync();
                    }
                    if (await _reader.ReadAsync())
                    {
                        Current = Utils.JsonDeserializeObject<T>(SerializeRow());
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Serializes the reader's current PostgreSQL row into JSON
            /// </summary>
            /// <returns>JSON string version of the PostgreSQL row</returns>
            private string SerializeRow()
            {
                var jsonSerializerSettings = new JsonSerializerSettings()
                {
                    DateFormatString = ISO_8061_DATETIME_FORMAT
                };
                return Utils.JsonSerializeObject(PostgreSqlBindingUtilities.BuildDictionaryFromSqlRow(_reader), jsonSerializerSettings);
            }

        }
    }
}