// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Integration
{
    /// <summary>
    /// Test class to test binding to a table with less columns than the object
    /// </summary>
    public static class AddProductExtraColumns
    {
        /// <summary>
        /// Test method to test binding to a table with less columns than the object
        /// the extra columns should be ignored
        /// </summary>
        [FunctionName("AddProductExtraColumns")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "addproduct-extracolumns")]
            HttpRequest req,
            [PostgreSql("Products", "PostgreSqlConnectionString")] out ProductExtraColumns product)
        {
            product = new ProductExtraColumns
            {
                Name = "test",
                ProductId = 1,
                Cost = 100,
                ExtraInt = 1,
                ExtraString = "test"
            };
            return new CreatedResult($"/api/addproduct-extracolumns", product);
        }
    }
}
