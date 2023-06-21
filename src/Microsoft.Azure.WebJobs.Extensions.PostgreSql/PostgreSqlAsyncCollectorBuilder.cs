// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    internal class PostgreSqlAsyncCollectorBuilder<T> : IConverter<PostgreSqlAttribute, IAsyncCollector<T>>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public PostgreSqlAsyncCollectorBuilder(IConfiguration configuration, ILogger logger)
        {
            this._configuration = configuration;
            this._logger = logger;
        }

        IAsyncCollector<T> IConverter<PostgreSqlAttribute, IAsyncCollector<T>>.Convert(PostgreSqlAttribute attribute)
        {
            return new PostgreSqlAsyncCollector<T>(this._configuration, attribute, this._logger);
        }

    }
}