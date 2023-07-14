// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Integration
{
    /// <summary>
    /// This class is used to test the case where the object has missing columns.
    /// </summary>
    public static class AddProductMissingColumns
    {
        /// <summary>
        /// This output binding should successfully add the ProductMissingColumns object
        /// to the SQL table.
        /// </summary>
        [FunctionName("AddProductMissingColumns")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproduct-missingcolumns")]
            HttpRequest req,
            [PostgreSql("Products", "PostgreSqlConnectionString")] out ProductMissingColumns product)
        {
            product = new ProductMissingColumns
            {
                Name = "test",
                ProductId = 1
                // Cost is missing
            };
            return new CreatedResult($"/api/addproduct-missingcolumns", product);
        }
    }
}
