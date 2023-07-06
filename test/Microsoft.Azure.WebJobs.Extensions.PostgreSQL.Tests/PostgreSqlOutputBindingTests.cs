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

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Unit
{
    /// <summary>
    /// This is a test class for testing numbers and strings.
    /// </summary>
    public class PostgreSqlOutputBindingTests
    {
    
        private static readonly Mock<IConfiguration> config = new();
        private static readonly Mock<ILogger> logger = new();

        /// <summary>
        /// Tests the constructor of PostgreSqlAsyncCollector with null arguments.
        /// </summary>
        [Fact]
        public void TestNullCollectorConstructorArguments()
        {
            var arg = new PostgreSqlAttribute(string.Empty, "connectionStringSetting");
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlAsyncCollector<string>(config.Object, null, logger.Object));
            Assert.Throws<ArgumentNullException>(() => new PostgreSqlAsyncCollector<string>(null, arg, logger.Object));
        }
        
    }
}
