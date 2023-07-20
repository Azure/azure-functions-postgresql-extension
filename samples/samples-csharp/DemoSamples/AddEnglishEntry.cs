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
    public static class AddEnglishEntry
    {
        [FunctionName("AddEnglishEntry")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "add-english-entry")]
            [FromBody] Entry entryFromBody,
            [PostgreSql("english", "PostgreSqlConnectionString")] out Entry newEntry)

        {
            entryFromBody.created = DateTime.Now;
            newEntry = entryFromBody;
            return new CreatedResult($"/api/add-english-entry", newEntry);
        }
    }
}
