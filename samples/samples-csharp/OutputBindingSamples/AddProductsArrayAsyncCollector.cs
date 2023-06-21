// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.Common;
using System.Threading.Tasks;


namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.InputBindingSamples
{
    public static class AddProductsArrayAsyncCollector
    {
        [FunctionName("AddProductsArrayAsyncCollector")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproductsarray-asynccollector")]
            [FromBody] List<Product> productsToAdd,
            [PostgreSql("Products", "PostgreSqlConnectionString")] IAsyncCollector<Product> products)
        {
            // add the list of products from the request body to the IAsyncCollector
            foreach (Product product in productsToAdd)
            {
                await products.AddAsync(product);
            }

            // flush is called automatically after the function completes

            return new CreatedResult($"/api/addproductsarray-asynccollector", "done");
        }
    }
}
