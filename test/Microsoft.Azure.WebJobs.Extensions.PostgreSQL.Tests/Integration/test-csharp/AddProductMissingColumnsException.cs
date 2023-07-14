// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Integration
{
    /// <summary>
    /// This class is used to test the case where the object has missing columns and the table does not allow nulls.
    /// </summary>
    public static class AddProductMissingColumnsExceptionFunction
    {
        /// <summary>
        /// This output binding should throw an error since the ProductsCostNotNull table does not
        /// allows rows without a Cost value.
        /// </summary>
        [FunctionName("AddProductMissingColumnsExceptionFunction")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproduct-missingcolumnsexception")]
            HttpRequest req,
            [PostgreSql("ProductsCostNotNull", "PostgreSqlConnectionString")] out ProductMissingColumns product)
        {
            product = new ProductMissingColumns
            {
                Name = "test",
                ProductId = 1
                // Cost is missing
            };
            return new CreatedResult($"/api/addproduct-missingcolumnsexception", product);
        }
    }
}
