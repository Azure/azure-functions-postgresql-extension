// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Logging;
using Npgsql;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    /// <summary>
    /// Exposes PGSQL input and output bindings
    /// </summary>
    [Extension("postgresql")]
    internal class PostgreSqlBindingConfigProvider : IExtensionConfigProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlBindingConfigProvider"/> class.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown if either parameter is null
        /// </exception>
        public PostgreSqlBindingConfigProvider(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this._loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Initializes the PGSQL binding rules
        /// </summary>
        /// <param name="context"> The config context </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if context is null
        /// </exception>
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ILogger logger = this._loggerFactory.CreateLogger(LogCategories.Bindings);
            var inputOutputRule = context.AddBindingRule<PostgreSqlAttribute>();
            inputOutputRule.BindToCollector<string>(attr => new PostgreSqlAsyncCollector(CreateContext(attr), attr, logger));

        }

        internal PostgreSqlBindingContext CreateContext(PostgreSqlAttribute attribute)
        {

            NpgsqlConnection connection = GetService(attribute.ConnectionStringSetting);


            return new PostgreSqlBindingContext
            {
                Connection = connection,
                ResolvedAttribute = attribute,
            };
        }

        private NpgsqlConnection GetService(string connectionStringSetting)
        {
            if (string.IsNullOrEmpty(connectionStringSetting))
            {
                throw new InvalidOperationException("The PostgreSql connection string must be set either via a connection string named 'PostgreSql' in the connectionStrings section of the config file or via a PostgreSqlAttribute.");
            }

            Console.WriteLine($"Using connectionStringSetting: {connectionStringSetting}");

            return new NpgsqlConnection(connectionStringSetting);
        }
    }
}