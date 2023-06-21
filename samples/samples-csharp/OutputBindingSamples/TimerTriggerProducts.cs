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
    public static class TimerTriggerProducts
    {
        private static int _executionNumber = 0;

        /// <summary>
        /// This timer function runs every 5 seconds, each time it upserts 1000 rows of data.
        /// </summary>
        [FunctionName("TimerTriggerProducts")]
        public static void Run(
            [TimerTrigger("*/5 * * * * *")] TimerInfo req, ILogger log,
            [PostgreSql("Products", "PostgreSqlConnectionString")] ICollector<Product> products)
        {
            int totalUpserts = 1000;
            log.LogInformation($"{DateTime.Now} starting execution #{_executionNumber}. Rows to generate={totalUpserts}.");

            var sw = new Stopwatch();
            sw.Start();

            List<Product> newProducts = ProductUtilities.GetNewProducts(totalUpserts);
            foreach (Product product in newProducts)
            {
                products.Add(product);
            }

            sw.Stop();

            string line = $"{DateTime.Now} finished execution #{_executionNumber}. Total time to create {totalUpserts} rows={sw.ElapsedMilliseconds}.";
            log.LogInformation(line);

            _executionNumber++;
        }
    }
}
