// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Numerics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Integration
{
    /// <summary>
    /// Output binding test for compatability with converting various data types to their respective
    /// PostgreSQL types.
    /// </summary>
    public static class AddProductColumnTypes
    {
        /// <summary>
        /// This function is used to test compatability with converting various data types to their respective
        /// PostgreSQL types.
        /// </summary>
        [FunctionName(nameof(AddProductColumnTypes))]
        public static IActionResult Run(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "addproduct-columntypes")] HttpRequest req,
                [PostgreSql("ProductsColumnTypes", "PostgreSqlConnectionString")] out ProductColumnTypes product)
        {
            product = new ProductColumnTypes()
            {
                ProductId = int.Parse(req.Query["productId"]),
                Bigint = long.MaxValue,
                Bigserial = long.MaxValue,
                Bit = 1,
                BitVarying = "010011",
                Boolean = true,
                Bytea = new byte[] { 0x01, 0x02, 0x03, 0x04 },
                Character = "testCharacter",
                CharacterVarying = "testCharacterVarying",
                Date = DateTime.Now,
                DoublePrecision = 1.2345678910,
                Integer = int.MaxValue,
                Interval = new TimeSpan(1, 2, 3),
                Json = JObject.Parse("{ \"name\": \"John\", \"age\": 30, \"city\": \"New York\" }"),
                Jsonb = JObject.Parse("{ \"name\": \"Jane\", \"age\": 28, \"city\": \"San Francisco\" }"),
                Numeric = 1234.56M,
                Real = 1.23f,
                Smallint = short.MaxValue,
                Smallserial = short.MaxValue,
                Serial = int.MaxValue,
                Text = "testText",
                Time = DateTime.Now.TimeOfDay,
                Timestamp = DateTime.Now,
                Uuid = Guid.NewGuid()
            };


            // Items were inserted successfully so return success, an exception would be thrown if there
            // was any issues
            return new OkObjectResult("Success!");
        }
    }
}
