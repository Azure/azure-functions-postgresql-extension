// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.WebJobs.Logging;
using System.Reflection;
using System.Diagnostics;
using Newtonsoft.Json;
using static Microsoft.Azure.WebJobs.Extensions.PostgreSql.PostgreSqlConverters;

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
            // inputOutputRule.BindToInput<string>(new PostgreSqlBindingConverter(this));

            // BindingRule<PostgreSqlAttribute> inputOutputRule = context.AddBindingRule<PostgreSqlAttribute>();
            // var converter = new PostgreSqlConverter(this._configuration);
            // inputOutputRule.BindToInput(converter);
            // inputOutputRule.BindToInput<string>(typeof(PostgreSqlGenericsConverter<string>), this._configuration, logger);
            inputOutputRule.BindToCollector<OpenType.Poco>(typeof(PostgreSqlAsyncCollectorBuilder<>), this._configuration, logger);
            // inputOutputRule.BindToInput<OpenType>(typeof(PostgreSqlGenericsConverter<>), this._configuration, logger);

        }
    }
}