// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.PostgreSql;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(PostgreSqlBindingStartup))]
namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{

    /// <summary>
    /// This class represents the startup process for the PostgreSQL binding.
    /// </summary>
    public class PostgreSqlBindingStartup : IWebJobsStartup
    {
        /// <summary>
        /// Configures the PostgreSQL binding.
        /// </summary>
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddPostgreSql();
        }
    }
}