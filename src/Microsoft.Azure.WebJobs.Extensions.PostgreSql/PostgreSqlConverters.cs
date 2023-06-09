// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;
using static Microsoft.Azure.WebJobs.Extensions.PostgreSql.PostgreSqlBindingConstants;
using System.Net.Mime;
using Xunit.Sdk;
using Npgsql;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    internal class PostgreSqlConverters
    {
        internal class PostgreSqlConverter : IConverter<PostgreSqlAttribute, NpgsqlCommand>
        {
            private readonly IConfiguration _configuration;

            /// <summary>
            /// Initializes a new instance of the <see cref="PostgreSqlConverter"/> class.
            /// </summary>
            /// <param name="configuration"></param>
            /// <exception cref="ArgumentNullException">
            /// Thrown if the configuration is null
            /// </exception>
            public PostgreSqlConverter(IConfiguration configuration)
            {
                this._configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            }

            public NpgsqlCommand Convert(PostgreSqlAttribute attribute)
            {
                return new NpgsqlCommand(attribute.CommandText);
            }


        }
    }
}