// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Integration
{
    /// <summary>
    /// Test function that adds a product to the database using incorrect casing for the POCO field 'ProductID'. Should throw an error.
    /// </summary>
    public static class AddProductIncorrectCasing
    {
        /// <summary>
        /// This output binding should throw an error since the casing of the POCO field 'ProductID' and
        /// table column name 'ProductId' do not match.
        /// </summary>
        [FunctionName("AddProductIncorrectCasing")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "addproduct-incorrectcasing")]
            HttpRequest req,
            [PostgreSql("Products", "PostgreSqlConnectionString")] out ProductIncorrectCasing product)
        {
            product = new ProductIncorrectCasing
            {
                ProductID = 1,
                Name = "test",
                Cost = 1
            };
            return new CreatedResult($"/api/addproduct-incorrectcasing", product);
        }
    }
}