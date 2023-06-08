// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.PostgreSql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    internal class PostgreSqlAsyncCollectorBuilder : IConverter<PostgreSqlAttribute, IAsyncCollector<string>>
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        public PostgreSqlAsyncCollectorBuilder(IConfiguration configuration, ILogger logger)
        {
            this._configuration = configuration;
            this._logger = logger;
        }

        IAsyncCollector<string> IConverter<PostgreSqlAttribute, IAsyncCollector<string>>.Convert(PostgreSqlAttribute attribute)
        {
            // return new PostgreSqlAsyncCollector(this._configuration, attribute, this._logger);
            throw new System.NotImplementedException();
        }
    }
}