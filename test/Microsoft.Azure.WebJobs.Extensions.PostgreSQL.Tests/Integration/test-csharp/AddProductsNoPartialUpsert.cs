// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.Common;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Integration
{
    /// <summary>
    /// Makes sure that all rows are rolled back if any row fails to upsert.
    /// </summary>
    public static class AddProductsNoPartialUpsert
    {
        /// <summary>
        /// The number of rows to upsert in a single batch.
        /// Should match the set batch size in PostgreSqlAsyncCollector.cs.
        /// </summary>
        public const int UpsertBatchSize = 1000;

        /// <summary>
        /// This output binding should throw an error since the ProductsNameNotNull table does not 
        /// allows rows without a Name value. No rows should be upserted to the Sql table.
        /// </summary>
        [FunctionName("AddProductsNoPartialUpsert")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "addproducts-nopartialupsert")]
            HttpRequest req,
            [PostgreSql("ProductsNameNotNull", "PostgreSqlConnectionString")] ICollector<Product> products)
        {
            List<Product> newProducts = ProductUtilities.GetNewProducts(UpsertBatchSize);
            foreach (Product product in newProducts)
            {
                products.Add(product);
            }

            var invalidProduct = new Product
            {
                Name = null,
                ProductId = UpsertBatchSize,
                Cost = 100
            };
            products.Add(invalidProduct);

            return new CreatedResult($"/api/addproducts-nopartialupsert", "done");
        }
    }
}
