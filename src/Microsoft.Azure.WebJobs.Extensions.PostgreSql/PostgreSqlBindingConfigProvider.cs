// <copyright file="PostgreSqlBindingConfigProvider.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>

using System;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Azure.WebJobs.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static Microsoft.Azure.WebJobs.Extensions.PostgreSql.PostgreSqlConverters;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    /// <summary>
    /// Exposes PGSQL input and output bindings.
    /// </summary>
    [Extension("postgresql")]
    internal class PostgreSqlBindingConfigProvider : IExtensionConfigProvider
    {
        private readonly IConfiguration configuration;
        private readonly ILoggerFactory loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlBindingConfigProvider"/> class.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown if either parameter is null.
        /// </exception>
        /// <param name="configuration"> The configuration. </param>
        /// <param name="loggerFactory"> The logger factory. </param>
        public PostgreSqlBindingConfigProvider(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <summary>
        /// Initializes the PGSQL binding rules.
        /// </summary>
        /// <param name="context"> The config context. </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if context is null.
        /// </exception>
        public void Initialize(ExtensionConfigContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            ILogger logger = this.loggerFactory.CreateLogger(LogCategories.Bindings);
            var inputOutputRule = context.AddBindingRule<PostgreSqlAttribute>();

            var converter = new PostgreSqlConverter(this.configuration);
            inputOutputRule.BindToInput(converter);

            inputOutputRule.BindToInput<string>(typeof(PostgreSqlGenericsConverter<string>), this.configuration, logger);

            inputOutputRule.BindToCollector<PostgreSqlObjectOpenType>(typeof(PostgreSqlAsyncCollectorBuilder<>), this.configuration, logger);

            inputOutputRule.BindToInput<OpenType>(typeof(PostgreSqlGenericsConverter<>), this.configuration, logger);
        }
    }
}