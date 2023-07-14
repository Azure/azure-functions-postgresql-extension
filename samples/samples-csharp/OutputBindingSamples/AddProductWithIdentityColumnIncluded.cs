// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.Common;


namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.OutputBindingSamples
{

    public static class AddProductWithIdentityColumnIncluded
    {
        /// <summary>
        /// This shows an example of a PostgreSQL Output binding where the target table has a primary key identity column which is included in the output object. If the primary key is non null, then the row is upserted as expected. 
        /// However, if the primary key is null, the identity column will NOT be generated automatically and an error will be thrown.
        /// This is different than SQL Server where the identity column will be generated automatically on null.
        /// PostgreSQL thinks that the user wants to insert null into the identity column which is not allowed.
        /// </summary>
        /// <param name="req">The original request that triggered the function</param>
        /// <param name="product">The created Product object</param>
        /// <returns>The CreatedResult containing the new object that was inserted</returns>
        [FunctionName(nameof(AddProductWithIdentityColumnIncluded))]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "addproductwithidentitycolumnincluded")]
            HttpRequest req,
            [PostgreSql("ProductsWithIdentity", "PostgreSqlConnectionString")] out ProductWithOptionalId product)
        {
            product = new ProductWithOptionalId
            {
                Name = req.Query["name"],
                ProductId = string.IsNullOrEmpty(req.Query["productId"]) ? null : int.Parse(req.Query["productId"]),
                Cost = int.Parse(req.Query["cost"])
            };
            return new CreatedResult($"/api/addproductwithidentitycolumnincluded", product);
        }
    }
}
