// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.Common;


namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.InputBindingSamples
{
    public static class AddProductsArray
    {
        [FunctionName("AddProductsArray")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproducts-array")]
            [FromBody] List<Product> products,
            [PostgreSql("Products", "PostgreSqlConnectionString")] out Product[] output)
        {
            // Upsert the products, which will insert them into the Products table if the primary key (ProductId) for that item doesn't exist. 
            // If it does then update it to have the new name and cost
            output = products.ToArray();
            return new CreatedResult($"/api/addproducts-array", output);
        }
    }
}
