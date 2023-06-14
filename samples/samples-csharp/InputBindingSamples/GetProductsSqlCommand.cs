// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.Common;
using Npgsql;
using System;


namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.InputBindingSamples
{
    public static class GetProductsSqlCommand
    {

        [FunctionName("GetProductsSqlCommand")]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getproducts-sqlcommand/{cost}")]
            HttpRequest req,
            [PostgreSql("select * from Products where cost = @Cost::int",
                "PostgreSqlConnectionString",
                parameters: "@Cost={cost}")]
            NpgsqlCommand command)
        {
            string result = string.Empty;
            using (NpgsqlConnection connection = command.Connection)
            {
                connection.Open();
                using NpgsqlDataReader reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result += $"ProductId: {reader["ProductId"]},  Name: {reader["Name"]}, Cost: {reader["Cost"]}\n";
                }
            }
            return new OkObjectResult(result);
        }
    }
}
