using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static Microsoft.Azure.WebJobs.Extensions.PostgreSql.PostgreSqlConverters;
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Unit
{
    /// <summary>
    /// This is a test class for testing numbers and strings.
    /// </summary>
    public class PostgreSqlInputBindingTests
    {
    
        private static readonly Mock<IConfiguration> config = new();
        private static readonly Mock<ILoggerFactory> loggerFactory = new();
        private static readonly Mock<ILogger> logger = new();
        private static readonly NpgsqlConnection connection = new();

        /// <summary>
        /// TestNullConfiguration method tests if the constructors of PostgreSqlBindingConfigProvider, PostgreSqlConverter, and PostgreSqlGenericsConverter classes throw an ArgumentNullException when null is passed as a parameter.
        /// </summary>
        [Fact]
        public void TestNullConfiguration()
        {
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlBindingConfigProvider(null, loggerFactory.Object));
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlBindingConfigProvider(config.Object, null));
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlConverter(null));
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlGenericsConverter<string>(null, logger.Object));
        }

        /// <summary>
        /// TestNullCommandText method tests if the constructor of the PostgreSqlAttribute class throws an ArgumentNullException when null is passed as the command text.
        /// </summary>
        [Fact]
        public void TestNullCommandText()
        {
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlAttribute(null, "connectionStringSetting"));
        }

        /// <summary>
        /// TestNullConnectionStringSetting method tests if the constructor of the PostgreSqlAttribute class throws an ArgumentNullException when null is passed as the connection string setting.
        /// </summary>
        [Fact]
        public void TestNullConnectionStringSetting()
        {
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlAttribute("SELECT * FROM Products", null));
        }

        /// <summary>
        /// TestNullContext method tests if the Initialize method of the PostgreSqlBindingConfigProvider class throws an ArgumentNullException when null is passed as the context.
        /// </summary>
        [Fact]
        public void TestNullContext()
        {
            var configProvider = new PostgreSqlBindingConfigProvider(config.Object, loggerFactory.Object);
            Assert.Throws<ArgumentNullException>(() => configProvider.Initialize(null));
        }

        /// <summary>
        /// TestNullBuilder method tests if the AddPostgreSql extension method throws an ArgumentNullException when called on a null IWebJobsBuilder instance.
        /// </summary>
        [Fact]
        public void TestNullBuilder()
        {
            IWebJobsBuilder builder = null;
            Assert.Throws<ArgumentNullException>(() => builder.AddPostgreSql());
        }

        /// <summary>
        /// TestNullCommand method tests if the ParseParameters method of the PostgreSqlBindingUtilities class throws an ArgumentNullException when null is passed as the command.
        /// </summary>
        [Fact]
        public void TestNullCommand()
        {
            Assert.Throws<ArgumentNullException>(() => PostgreSqlBindingUtilities.ParseParameters("", null));
        }

        /// <summary>
        /// TestNullArgumentsPostgreSqlAsyncEnumerableConstructor method tests if the constructor of the PostgreSqlAsyncEnumerable class throws an ArgumentNullException when null is passed as any of its arguments.
        /// </summary>
        [Fact]
        public void TestNullArgumentsPostgreSqlAsyncEnumerableConstructor()
        {
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlAsyncEnumerable<string>(connection, null));
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlAsyncEnumerable<string>(null, new PostgreSqlAttribute("", "connectionStringSetting")));
        }

        /// <summary>
        /// PostgreSqlAsyncEnumerable should throw InvalidOperationExcepion when invoked with an invalid connection
        /// string setting. It should fail here since we're passing an empty connection string.
        /// </summary>
        [Fact]
        public void TestInvalidOperationPostgreSqlAsyncEnumerableConstructor()
        {
            Assert.Throws<InvalidOperationException>(() => new PostgreSqlAsyncEnumerable<string>(connection, new PostgreSqlAttribute("", "connectionStringSetting")));
        }

        /// <summary>
        /// Tests the scenario where invalid arguments are passed to the BuildConnection method.
        /// </summary>
        [Fact]
        public void TestInvalidArgumentsBuildConnection()
        {
            var attribute = new PostgreSqlAttribute("", "");
            Assert.Throws<ArgumentException>(() => PostgreSqlBindingUtilities.BuildConnection(attribute.ConnectionStringSetting, config.Object));

            attribute = new PostgreSqlAttribute("", "ConnectionStringSetting");
            Assert.Throws<ArgumentNullException>(() => PostgreSqlBindingUtilities.BuildConnection(attribute.ConnectionStringSetting, null));
        }

        /// <summary>
        /// Tests the scenario where an invalid command type is specified.
        /// </summary>
        [Fact]
        public void TestInvalidCommandType()
        {
            // Specify an invalid type
            var attribute = new PostgreSqlAttribute("", "connectionStringSetting", System.Data.CommandType.TableDirect);
            Assert.Throws<ArgumentException>(() => PostgreSqlBindingUtilities.BuildCommand(attribute, null));
        }

        /// <summary>
        /// Tests the scenario where the default command type is used.
        /// </summary>
        [Fact]
        public void TestDefaultCommandType()
        {
            string query = "select * from Products";
            var attribute = new PostgreSqlAttribute(query, "connectionStringSetting");
            NpgsqlCommand command = PostgreSqlBindingUtilities.BuildCommand(attribute, null);
            // CommandType should default to Text
            Assert.Equal(System.Data.CommandType.Text, command.CommandType);
            Assert.Equal(query, command.CommandText);

        }

        /// <summary>
        /// Tests the scenario where valid command types are specified.
        /// </summary>
        [Fact]
        public void TestValidCommandType()
        {
            string query = "select * from Products";
            var attribute = new PostgreSqlAttribute(query, "connectionStringSetting", System.Data.CommandType.Text);
            NpgsqlCommand command = PostgreSqlBindingUtilities.BuildCommand(attribute, null);
            Assert.Equal(System.Data.CommandType.Text, command.CommandType);
            Assert.Equal(query, command.CommandText);

            string procedure = "StoredProcedure";
            attribute = new PostgreSqlAttribute(procedure, "connectionStringSetting", System.Data.CommandType.StoredProcedure);
            command = PostgreSqlBindingUtilities.BuildCommand(attribute, null);
            Assert.Equal(System.Data.CommandType.StoredProcedure, command.CommandType);
            Assert.Equal(procedure, command.CommandText);
        }

        /// <summary>
        /// Tests the scenario where the parameters string is malformed.
        /// </summary>
        [Fact]
        public void TestMalformedParametersString()
        {
            var command = new NpgsqlCommand();
            // Second param name doesn't start with "@"
            string parameters = "@param1=param1,param2=param2";
            Assert.Throws<ArgumentException>(() => PostgreSqlBindingUtilities.ParseParameters(parameters, command));

            // Second param not separated by "=", or contains extra "="
            parameters = "@param1=param1,@param2==param2";
            Assert.Throws<ArgumentException>(() => PostgreSqlBindingUtilities.ParseParameters(parameters, command));
            parameters = "@param1=param1,@param2;param2";
            Assert.Throws<ArgumentException>(() => PostgreSqlBindingUtilities.ParseParameters(parameters, command));
            parameters = "@param1=param1,@param2=param2=";
            Assert.Throws<ArgumentException>(() => PostgreSqlBindingUtilities.ParseParameters(parameters, command));

            // Params list not separated by "," correctly
            parameters = "@param1=param1;@param2=param2";
            Assert.Throws<ArgumentException>(() => PostgreSqlBindingUtilities.ParseParameters(parameters, command));
            parameters = "@param1=param1,@par,am2=param2";
            Assert.Throws<ArgumentException>(() => PostgreSqlBindingUtilities.ParseParameters(parameters, command));
        }

        /// <summary>
        /// Tests the scenario where the parameters string is well-formed.
        /// </summary>
        [Fact]
        public void TestWellformedParametersString()
        {
            var command = new NpgsqlCommand();
            string parameters = "@param1=param1,@param2=param2";
            PostgreSqlBindingUtilities.ParseParameters(parameters, command);

            // Apparently SqlParameter doesn't implement an Equals method, so have to do this manually
            Assert.Equal(2, command.Parameters.Count);
            foreach (NpgsqlParameter param in command.Parameters)
            {
                Assert.True(param.ParameterName.Equals("@param1") || param.ParameterName.Equals("@param2"));
                if (param.ParameterName.Equals("@param1"))
                {
                    Assert.True(param.Value.Equals("param1"));
                }
                else
                {
                    Assert.True(param.Value.Equals("param2"));
                }
            }

            // Confirm we throw away empty entries at the beginning/end and ignore multiple commas in between
            // parameter pairs
            command = new NpgsqlCommand();
            parameters = ",,@param1=param1,,@param2=param2,,,";
            PostgreSqlBindingUtilities.ParseParameters(parameters, command);

            Assert.Equal(2, command.Parameters.Count);
            foreach (NpgsqlParameter param in command.Parameters)
            {
                Assert.True(param.ParameterName.Equals("@param1") || param.ParameterName.Equals("@param2"));
                if (param.ParameterName.Equals("@param1"))
                {
                    Assert.True(param.Value.Equals("param1"));
                }
                else
                {
                    Assert.True(param.Value.Equals("param2"));
                }
            }

            // Confirm we interpret "null" as being a NULL parameter value, and that we interpret
            // a string like "@param1=,@param2=param2" as @param1 having an empty string as its value
            command = new NpgsqlCommand();
            parameters = "@param1=,@param2=null";
            PostgreSqlBindingUtilities.ParseParameters(parameters, command);

            Assert.Equal(2, command.Parameters.Count);
            foreach (NpgsqlParameter param in command.Parameters)
            {
                Assert.True(param.ParameterName.Equals("@param1") || param.ParameterName.Equals("@param2"));
                if (param.ParameterName.Equals("@param1"))
                {
                    Assert.True(param.Value.Equals(string.Empty));
                }
                else
                {
                    Assert.True(param.Value.Equals(DBNull.Value));
                }
            }

            // Confirm nothing is done when parameters are not specified
            command = new NpgsqlCommand();
            parameters = null;
            PostgreSqlBindingUtilities.ParseParameters(parameters, command);
            Assert.Empty(command.Parameters);
        }
    }
}
