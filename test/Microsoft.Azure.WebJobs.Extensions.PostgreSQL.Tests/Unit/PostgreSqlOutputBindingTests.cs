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
using Xunit.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Unit
{
    /// <summary>
    /// This is a test class for testing numbers and strings.
    /// </summary>
    public class PostgreSqlOutputBindingTests
    {

        private static readonly Mock<IConfiguration> config = new();
        private static readonly Mock<ILogger> logger = new();

        private static readonly NpgsqlConnection connection = new();

        private readonly ITestOutputHelper _output;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlOutputBindingTests"/> class.
        /// </summary>
        public PostgreSqlOutputBindingTests(ITestOutputHelper output)
        {
            _output = output;
        }


        /// <summary>
        /// Tests the constructor of PostgreSqlAsyncCollector with null arguments.
        /// </summary>
        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Binding", "Output")]
        public void TestNullCollectorConstructorArguments()
        {
            var arg = new PostgreSqlAttribute(string.Empty, "PostgreSqlConnectionString");
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlAsyncCollector<string>(config.Object, null, logger.Object));
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlAsyncCollector<string>(null, arg, logger.Object));
        }

        /// <summary>
        /// Tests the VerifyCleanTableName method with valid table names.
        /// </summary>
        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Binding", "Output")]
        public void TestVerifyCleanTableName_ValidNames()
        {
            // Arrange
            var validTableNames = new[] { "valid_table", "valid.table", "validtable1", "valid_table_1", "VALID_TABLE", "V123", "v_1_2_3" };

            // Act & Assert
            foreach (var validTableName in validTableNames)
            {
                PostgreSqlAsyncCollector<string>.VerifyCleanTableName(validTableName);
            }
        }

        /// <summary>
        /// Tests the VerifyCleanTableName method with edge cases.
        /// </summary>
        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Binding", "Output")]
        public void TestVerifyCleanTableName_EdgeCases()
        {
            // Arrange
            var edgeCaseTableNames = new[] { "" };

            // Act & Assert
            foreach (var edgeCaseTableName in edgeCaseTableNames)
            {
                _output.WriteLine($"Testing edge case table name: '{edgeCaseTableName}'");
                Assert.Throws<ArgumentException>(() => PostgreSqlAsyncCollector<string>.VerifyCleanTableName(edgeCaseTableName));
            }
        }

        /// <summary>
        /// Tests the VerifyCleanTableName method with invalid table names.
        /// </summary>
        [Fact]
        [Trait("Category", "Unit")]
        [Trait("Binding", "Output")]
        public void TestVerifyCleanTableName_InvalidNames()
        {
            // Arrange
            var invalidTableNames = new[] { "invalid-table", "invalid table", "invalid;drop table;", "invalid'table", "invalid\"table" };

            // Act & Assert
            foreach (var invalidTableName in invalidTableNames)
            {
                Assert.Throws<ArgumentException>(() => PostgreSqlAsyncCollector<string>.VerifyCleanTableName(invalidTableName));
            }
        }
    }
}
