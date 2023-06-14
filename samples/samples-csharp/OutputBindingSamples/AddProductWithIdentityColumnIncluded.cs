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

    public static class AddProductWithIdentityColumnIncluded
    {
        /// <summary>
        /// This shows an example of a SQL Output binding where the target table has a primary key
        /// which is an identity column and the identity column is included in the output object. In
        /// this case the identity column is used to match the rows and all non-identity columns are
        /// updated if a match is found. Otherwise a new row is inserted (with the identity column being
        /// ignored since that will be generated by the engine).
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
