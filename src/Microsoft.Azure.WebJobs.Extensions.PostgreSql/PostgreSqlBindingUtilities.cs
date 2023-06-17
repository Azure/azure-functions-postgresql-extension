// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    internal static class PostgreSqlBindingUtilities
    {
        /// <summary>
        /// Builds a connection using the connection string attached to the app setting with name ConnectionStringSetting
        /// </summary>
        /// <param name="connectionStringSetting">The name of the app setting that stores the PostgreSQL connection string</param>
        /// <param name="configuration">Used to obtain the value of the app setting</param>
        /// <exception cref="ArgumentException">
        /// Thrown if ConnectionStringSetting is empty or null
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if configuration is null
        /// </exception>
        /// <returns>The built connection </returns>
        public static NpgsqlConnection BuildConnection(string connectionStringSetting, IConfiguration configuration)
        {
            return new NpgsqlConnection(connectionStringSetting);
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
        /// Parses the parameter string into a list of parameters, where each parameter is separated by "," and has the form
        /// "@param1=param2". "@param1" is the parameter name to be used in the query or stored procedure, and param1 is the
        /// parameter value. Parameter name and parameter value are separated by "=". Parameter names/values cannot contain ',' or '='.
        /// A valid parameter string would be "@param1=param1,@param2=param2". Attaches each parsed parameter to command.
        /// If the value of a parameter should be null, use "null", as in @param1=null,@param2=param2".
        /// If the value of a parameter should be an empty string, do not add anything after the equals sign and before the comma,
        /// as in "@param1=,@param2=param2"
        /// </summary>
        /// <param name="parameters">The parameter string to be parsed</param>
        /// <param name="command">The PostgreSqlCommand to which the parsed parameters will be added to</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if command is null
        /// </exception>
        public static void ParseParameters(string parameters, NpgsqlCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            // If parameters is null, user did not specify any parameters in their function so nothing to parse
            if (!string.IsNullOrEmpty(parameters))
            {
                // Because we remove empty entries, we will ignore any commas that appear at the beginning/end of the parameter list,
                // as well as extra commas that appear between parameter pairs.
                string[] paramPairs = parameters.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string pair in paramPairs)
                {
                    // Note that we don't throw away empty entries here, so a parameter pair that looks like "=@param1=param1"
                    // or "@param2=param2=" is considered malformed
                    string[] items = pair.Split('=');
                    if (items.Length != 2)
                    {
                        throw new ArgumentException("Parameters must be separated by \",\" and parameter name and parameter value must be separated by \"=\", " +
                           "i.e. \"@param1=param1,@param2=param2\". To specify a null value, use null, as in \"@param1=null,@param2=param2\"." +
                           "To specify an empty string as a value, simply do not add anything after the equals sign, as in \"@param1=,@param2=param2\".");
                    }
                    if (!items[0].StartsWith("@", StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new ArgumentException("Parameter name must start with \"@\", i.e. \"@param1=param1,@param2=param2\"");
                    }

                    NpgsqlParameter parameter = new NpgsqlParameter(items[0], NpgsqlDbType.Text);

                    if (items.Length == 1 || string.IsNullOrEmpty(items[1]))
                    {
                        parameter.Value = string.Empty; // handle as empty string
                    }
                    else if (items[1].Equals("null", StringComparison.OrdinalIgnoreCase))
                    {
                        parameter.Value = DBNull.Value; // handle as null
                    }
                    else
                    {
                        parameter.Value = items[1]; // assign the value
                    }

                    command.Parameters.Add(parameter);
                }
            }
        }

        /// <summary>
        /// Builds a PostgreSqlCommand using the query/stored procedure and parameters specified in attribute.
        /// </summary>
        /// <param name="attribute">The PostgreSqlAttribute with the parameter, command type, and command text</param>
        /// <param name="connection">The connection to attach to the PostgreSqlCommand</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the CommandType specified in attribute is neither StoredProcedure nor Text. We only support
        /// commands that refer to the name of a StoredProcedure (the StoredProcedure CommandType) or are themselves
        /// raw queries (the Text CommandType).
        /// </exception>
        /// <returns>The built PostgreSqlCommand</returns>
        public static NpgsqlCommand BuildCommand(PostgreSqlAttribute attribute, NpgsqlConnection connection)
        {
            var command = new NpgsqlCommand
            {
                Connection = connection,
                CommandText = attribute.CommandText
            };
            if (attribute.CommandType == CommandType.StoredProcedure)
            {
                command.CommandType = CommandType.StoredProcedure;
            }
            else if (attribute.CommandType != CommandType.Text)
            {
                throw new ArgumentException("The type of the PostgreSQL attribute for an input binding must be either CommandType.Text for a direct PostgreSQL query, or CommandType.StoredProcedure for a stored procedure.");
            }
            ParseParameters(attribute.Parameters, command);
            return command;
        }

        /// <summary>
        /// Returns a dictionary where each key is a column name and each value is the PostgreSQL row's value for that column
        /// </summary>
        /// <param name="reader">Used to determine the columns of the table as well as the next PostgreSQL row to process</param>
        /// <returns>The built dictionary</returns>
        public static IReadOnlyDictionary<string, object> BuildDictionaryFromSqlRow(NpgsqlDataReader reader)
        {
            return Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, i => reader.GetValue(i));
        }

        /// <summary>
        /// Escape any existing closing brackets and add brackets around the string
        /// </summary>
        /// <param name="s">The string to bracket quote.</param>
        /// <returns>The escaped and bracket quoted string.</returns>
        public static string AsBracketQuotedString(this string s)
        {
            return $"[{s.Replace("]", "]]")}]";
        }

        /// <summary>
        /// Escape any existing quotes and add quotes around the string.
        /// </summary>
        /// <param name="s">The string to quote.</param>
        /// <returns>The escaped and quoted string.</returns>
        public static string AsSingleQuotedString(this string s)
        {
            return $"'{s.AsSingleQuoteEscapedString()}'";
        }

        /// <summary>
        /// Returns the string with any single quotes in it escaped (replaced with '')
        /// </summary>
        /// <param name="s">The string to escape.</param>
        /// <returns>The escaped string.</returns>
        public static string AsSingleQuoteEscapedString(this string s)
        {
            return s.Replace("'", "''");
        }

        /// <summary>
        /// Verifies that the database we're connected to is supported
        /// </summary>
        /// <exception cref="InvalidOperationException">Throw if an error occurs while querying the compatibility level or if the database is not supported</exception>
        public static async Task VerifyDatabaseSupported(NpgsqlConnection connection, ILogger logger, CancellationToken cancellationToken)
        {
            // do nothing for now
            await Task.Yield();
        }

        /// <summary>
        /// Opens a connection and handles some specific errors if they occur.
        /// </summary>
        /// <param name="connection">The connection to open</param>
        /// <param name="cancellationToken">The cancellation token to pass to the OpenAsync call</param>
        /// <returns>The task that will be completed when the connection is made</returns>
        /// <exception cref="InvalidOperationException">Thrown if an error occurred that we want to wrap with more information</exception>
        internal static async Task OpenAsyncWithSqlErrorHandling(this NpgsqlConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                await connection.OpenAsync(cancellationToken);

                // make sure that the connection is actually open
                if (connection.State != ConnectionState.Open)
                {
                    throw new InvalidOperationException("The connection is not open after the OpenAsync call completed.");
                }
            }
            catch (NpgsqlException ex)
            {
                // Specific error handling can be added here.
                throw new InvalidOperationException("An error occurred when attempting to open the connection.", ex);
            }
        }

        /// <summary>
        /// Checks whether an exception is a fatal PostgreSqlException. It is deteremined to be fatal
        /// if the Class value of the Exception is 20 or higher, see
        /// https://learn.microsoft.com/dotnet/api/microsoft.data.sqlclient.sqlexception#remarks
        /// for details
        /// </summary>
        /// <param name="e">The exception to check</param>
        /// <returns>True if the exception is a fatal PostgreSqlClientException, false otherwise</returns>
        internal static bool IsFatalPostgreSqlException(this Exception e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Attempts to ensure that this connection is open, if it currently is in a broken state
        /// then it will close the connection and re-open it.
        /// </summary>
        /// <param name="conn">The connection</param>
        /// <param name="forceReconnect">Whether to force the connection to be re-established, regardless of its current state</param>
        /// <param name="logger">Logger to log events to</param>
        /// <param name="connectionName">The name of the connection to display in the log messages</param>
        /// <param name="token">Cancellation token to pass to the Open call</param>
        /// <returns>True if the connection is open, either because it was able to be re-established or because it was already open. False if the connection could not be re-established.</returns>
        internal static async Task<bool> TryEnsureConnected(this NpgsqlConnection conn,
            bool forceReconnect,
            ILogger logger,
            string connectionName,
            CancellationToken token)
        {
            if (forceReconnect || conn.State.HasFlag(ConnectionState.Broken | ConnectionState.Closed))
            {
                logger.LogWarning($"{connectionName} is broken, attempting to reconnect...");
                try
                {
                    // Sometimes the connection state is listed as open even if a fatal exception occurred, see
                    // https://github.com/dotnet/SqlClient/issues/1874 for details. So in that case we want to first
                    // close the connection so we can retry (otherwise it'll throw saying the connection is still open)
                    if (conn.State == ConnectionState.Open)
                    {
                        conn.Close();
                    }
                    await conn.OpenAsync(token);
                    logger.LogInformation($"Successfully re-established {connectionName}!");
                    return true;
                }
                catch (Exception e)
                {
                    logger.LogError($"Exception reconnecting {connectionName}. Exception = {e.Message}");
                    return false;
                }
            }
            return true;
        }



        /// <summary>
        /// Calls ExecuteNonQueryAsync and logs an error if it fails before rethrowing.
        /// </summary>
        /// <param name="cmd">The PostgreSqlCommand being executed</param>
        /// <param name="logger">The logger</param>
        /// <param name="cancellationToken">The cancellation token to pass to the call</param>
        /// <returns>The result of the call</returns>
        public static async Task<int> ExecuteNonQueryAsyncWithLogging(this NpgsqlCommand cmd, ILogger logger, CancellationToken cancellationToken)
        {
            try
            {
                return await cmd.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError($"Exception executing query. Message={e.Message}\nQuery={cmd.CommandText}");
                throw;
            }
        }

        /// <summary>
        /// Calls ExecuteReaderAsync and logs an error if it fails before rethrowing.
        /// </summary>
        /// <param name="cmd">The PostgreSqlCommand being executed</param>
        /// <param name="logger">The logger</param>
        /// <param name="cancellationToken">The cancellation token to pass to the call</param>
        /// <returns>The result of the call</returns>
        public static async Task<NpgsqlDataReader> ExecuteReaderAsyncWithLogging(this NpgsqlCommand cmd, ILogger logger, CancellationToken cancellationToken)
        {
            try
            {
                return await cmd.ExecuteReaderAsync(cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError($"Exception executing query. Message={e.Message}\nQuery={cmd.CommandText}");
                throw;
            }
        }
    }
}
