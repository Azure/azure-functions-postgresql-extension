// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using Npgsql;
using static Microsoft.Azure.WebJobs.Extensions.PostgreSql.PostgreSqlBindingConstants;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    internal class PostgreSqlConverters
    {
        internal class PostgreSqlConverter : IConverter<PostgreSqlAttribute, NpgsqlCommand>
        {
            private readonly IConfiguration _configuration;

            /// <summary>
            /// Initializes a new instance of the <see cref="PostgreSqlConverter"/> class.
            /// </summary>
            /// <param name="configuration"></param>
            /// <exception cref="ArgumentNullException">
            /// Thrown if the configuration is null
            /// </exception>
            public PostgreSqlConverter(IConfiguration configuration)
            {
                _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            }

            /// <summary>
            /// Creates a NpgsqlCommand containing a PostgreSQL connection and the PostgreSQL query and parameters specified in attribute.
            /// The user can open the connection in the NpgsqlCommand and use it to read in the results of the query themselves.
            /// </summary>
            /// <param name="attribute">
            /// Contains the PostgreSQL query and parameters as well as the information necessary to build the PostgreSQL Connection
            /// </param>
            /// <returns>The NpgsqlCommand</returns>
            public NpgsqlCommand Convert(PostgreSqlAttribute attribute)
            {
                return PostgreSqlBindingUtilities.BuildCommand(attribute, CreateConnection(attribute.ConnectionStringSetting));
            }

            public static NpgsqlConnection CreateConnection(string connectionString)
            {
                NpgsqlConnection connection = new NpgsqlConnection(connectionString);
                return connection;
            }

        }

        /// <typeparam name="T">A user-defined POCO that represents a row of the user's table</typeparam>
        internal class PostgreSqlGenericsConverter<T> : IAsyncConverter<PostgreSqlAttribute, IEnumerable<T>>, IConverter<PostgreSqlAttribute, IAsyncEnumerable<T>>,
            IAsyncConverter<PostgreSqlAttribute, string>, IAsyncConverter<PostgreSqlAttribute, JArray>
        {
            private readonly IConfiguration _configuration;

            private readonly ILogger _logger;

            /// <summary>
            /// Initializes a new instance of the <see cref="PostgreSqlGenericsConverter{T}"/> class.
            /// </summary>
            /// <param name="configuration"></param>
            /// <param name="logger">ILogger used to log information and warnings</param>
            /// <exception cref="ArgumentNullException">
            /// Thrown if the configuration is null
            /// </exception>
            public PostgreSqlGenericsConverter(IConfiguration configuration, ILogger logger)
            {
                _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
                _logger = logger;
            }

            /// <summary>
            /// Opens a NpgSqlConnection, reads in the data from the user's database, and returns it as a list of POCOs.
            /// </summary>
            /// <param name="attribute">
            /// Contains the information necessary to establish a NpgSqlConnection, and the query to be executed on the database
            /// </param>
            /// <param name="cancellationToken">The cancellationToken is not used in this method</param>
            /// <returns>An IEnumerable containing the rows read from the user's database in the form of the user-defined POCO</returns>
            public async Task<IEnumerable<T>> ConvertAsync(PostgreSqlAttribute attribute, CancellationToken cancellationToken)
            {
                try
                {
                    string json = await BuildItemFromAttributeAsync(attribute, ConvertType.IEnumerable);
                    return Utils.JsonDeserializeObject<IEnumerable<T>>(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ConvertAsync Exception");
                    throw;
                }
            }

            /// <summary>
            /// Opens a NpgSqlConnection, reads in the data from the user's database, and returns it as a list of POCOs.
            /// </summary>
            /// <param name="query">
            /// The query to be executed on the database
            /// </param>
            /// <param name="attribute">
            /// Contains the information necessary to establish a NpgSqlConnection
            /// </param>
            /// <param name="cancellationToken">The cancellationToken is not used in this method</param>
            /// <returns>An IEnumerable containing the rows read from the user's database in the form of the user-defined POCO</returns>
            public async Task<IEnumerable<T>> ConvertAsync(string query, PostgreSqlAttribute attribute, CancellationToken cancellationToken)
            {
                try
                {
                    PostgreSqlAttribute queryAttribute = new PostgreSqlAttribute(query, attribute.ConnectionStringSetting);

                    string json = await BuildItemFromAttributeAsync(queryAttribute, ConvertType.IEnumerable);
                    return Utils.JsonDeserializeObject<IEnumerable<T>>(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ConvertAsync Exception");
                    throw;
                }
            }

            /// <summary>
            /// Opens a NpgSqlConnection, reads in the data from the user's database, and returns it as a JSON-formatted string.
            /// </summary>
            /// <param name="attribute">
            /// Contains the information necessary to establish a NpgSqlConnection, and the query to be executed on the database
            /// </param>
            /// <param name="cancellationToken">The cancellationToken is not used in this method</param>
            /// <returns>
            /// The JSON string. I.e., if the result has two rows from a table with schema ProductId: int, Name: varchar, Cost: int,
            /// then the returned JSON string could look like
            /// [{"productId":3,"name":"Bottle","cost":90},{"productId":5,"name":"Cup","cost":100}]
            /// </returns>
            async Task<string> IAsyncConverter<PostgreSqlAttribute, string>.ConvertAsync(PostgreSqlAttribute attribute, CancellationToken cancellationToken)
            {
                try
                {
                    return await BuildItemFromAttributeAsync(attribute, ConvertType.Json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ConvertAsync Exception");
                    throw;
                }
            }

            /// <summary>
            /// Extracts the <see cref="PostgreSqlAttribute.ConnectionStringSetting"/> in attribute and uses it to establish a connection
            /// to the PostgreSQL database. (Must be virtual for mocking the method in unit tests)
            /// </summary>
            /// <param name="attribute">
            /// The binding attribute that contains the name of the connection string app setting and query.
            /// </param>
            /// <param name="type">
            /// The type of conversion being performed by the input binding.
            /// </param>
            /// <returns></returns>
            public virtual async Task<string> BuildItemFromAttributeAsync(PostgreSqlAttribute attribute, ConvertType type)
            {
                using NpgsqlConnection connection = PostgreSqlBindingUtilities.BuildConnection(attribute.ConnectionStringSetting, _configuration);
                // Ideally, we would like to move away from using NpgsqlDataAdapter both here and in the
                // PostgreSqlAsyncCollector since it does not support asynchronous operations.
                using var adapter = new NpgsqlDataAdapter();
                using NpgsqlCommand command = PostgreSqlBindingUtilities.BuildCommand(attribute, connection);
                adapter.SelectCommand = command;
                await connection.OpenAsyncWithSqlErrorHandling(CancellationToken.None);
                var dataTable = new DataTable();
                adapter.Fill(dataTable);
                _logger.LogInformation($"{dataTable.Rows.Count} row(s) queried from database: {connection.Database} using Command: {command.CommandText}");
                // Serialize any DateTime objects in UTC format
                var jsonSerializerSettings = new JsonSerializerSettings()
                {
                    DateFormatString = ISO_8061_DATETIME_FORMAT
                };
                return Utils.JsonSerializeObject(dataTable, jsonSerializerSettings);

            }

            IAsyncEnumerable<T> IConverter<PostgreSqlAttribute, IAsyncEnumerable<T>>.Convert(PostgreSqlAttribute attribute)
            {
                try
                {
                    var asyncEnumerable = new PostgreSqlAsyncEnumerable<T>(PostgreSqlBindingUtilities.BuildConnection(attribute.ConnectionStringSetting, _configuration), attribute);
                    return asyncEnumerable;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Convert Exception");
                    throw;
                }
            }

            /// <summary>
            /// Opens a NpgSqlConnection, reads in the data from the user's database, and returns it as JArray.
            /// </summary>
            /// <param name="attribute">
            /// Contains the information necessary to establish a NpgSqlConnection, and the query to be executed on the database
            /// </param>
            /// <param name="cancellationToken">The cancellationToken is not used in this method</param>
            /// <returns>JArray containing the rows read from the user's database in the form of the user-defined POCO</returns>
            async Task<JArray> IAsyncConverter<PostgreSqlAttribute, JArray>.ConvertAsync(PostgreSqlAttribute attribute, CancellationToken cancellationToken)
            {
                try
                {
                    string json = await BuildItemFromAttributeAsync(attribute, ConvertType.JArray);
                    return JArray.Parse(json);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error converting to JArray");
                    throw;
                }
            }

        }
    }
}