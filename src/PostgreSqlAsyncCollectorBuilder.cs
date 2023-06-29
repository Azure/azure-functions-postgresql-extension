// <copyright file="PostgreSqlAsyncCollectorBuilder.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    /// <summary>
    /// Builds a PostgreSqlAsyncCollector.
    /// </summary>
    /// <typeparam name="T">The user defined POCO.</typeparam>
    internal class PostgreSqlAsyncCollectorBuilder<T> : IConverter<PostgreSqlAttribute, IAsyncCollector<T>>
    {
        private readonly IConfiguration configuration;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlAsyncCollectorBuilder{T}"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="logger">The logger.</param>
        public PostgreSqlAsyncCollectorBuilder(IConfiguration configuration, ILogger logger)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        /// <inheritdoc/>
        IAsyncCollector<T> IConverter<PostgreSqlAttribute, IAsyncCollector<T>>.Convert(PostgreSqlAttribute attribute)
        {
            return new PostgreSqlAsyncCollector<T>(this.configuration, attribute, this.logger);
        }
    }
}