// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.Common;


namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.DemoSamples
{
    public static class GetEntries
    {
        [FunctionName("GetEntries")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getentries/{language}")]
            HttpRequest req,
            [PostgreSql("select * from {language};",
                "PostgreSqlConnectionString")]
            IEnumerable<Entry> entries)
        {
            return new OkObjectResult(entries);
        }
    }
}
