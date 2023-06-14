// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.Common;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System;




namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.InputBindingSamples
{
    public static class QueueTriggerProducts
    {
        [FunctionName("QueueTriggerProducts")]
        public static void Run(
            [QueueTrigger("testqueue")] string queueMessage, ILogger log,
            [PostgreSql("Products", "PostgreSqlConnectionString")] ICollector<Product> products)
        {
            int totalUpserts = 100;
            log.LogInformation($"[QueueTrigger]: {DateTime.Now} starting execution {queueMessage}. Rows to generate={totalUpserts}.");

            var sw = new Stopwatch();
            sw.Start();

            List<Product> newProducts = ProductUtilities.GetNewProducts(totalUpserts);
            foreach (Product product in newProducts)
            {
                products.Add(product);
            }

            string line = $"[QueueTrigger]: {DateTime.Now} finished execution {queueMessage}. Total time to create {totalUpserts} rows={sw.ElapsedMilliseconds}.";
            log.LogInformation(line);

        }
    }
}
