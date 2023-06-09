using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql;
using System.Collections.Generic;

namespace WebJobs.Extensions.PostgreSql.Samples
{
    /// <summary>
    /// This class contains the sample code used in the HttpTrigger documentation.
    /// </summary>
    public static class HttpTriggerSample
    {
        /// <summary>
        /// This sample demonstrates how to use the PostgreSql extension for Azure Functions.
        /// </summary>
        [FunctionName("HttpTriggerSample")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
            [PostgreSql("dbo.inventory", "ConnectionString")] IAsyncCollector<Fruit> collector)
        {
            Console.WriteLine("HttpTriggerSample Start");

            Fruit kiwi = new Fruit("kiwi", true, "green", 100);

            await collector.AddAsync(kiwi);

            Console.WriteLine("HttpTriggerSample END");


            return new CreatedResult($"HttpTriggerSample", kiwi);
        }
    }

    /// <summary>
    /// This sample demonstrates how to use the PostgreSql extension for Azure Functions.
    /// </summary>
    public class Fruit
    {
        /// <summary>
        /// This sample demonstrates how to use the PostgreSql extension for Azure Functions.
        /// </summary>
        public Fruit(string name, bool isFruit, string color, int quantity = 1)
        {
            this.name = name;
            this.isFruit = isFruit;
            this.color = color;
            this.quantity = quantity;
            this.created = DateTime.Now;
        }
        /// <summary>
        /// This sample demonstrates how to use the PostgreSql extension for Azure Functions.
        /// </summary>
        public string name { get; set; }


        /// <summary>
        /// This sample demonstrates how to use the PostgreSql extension for Azure Functions.
        /// </summary>
        public bool isFruit { get; set; }

        /// <summary>
        /// This sample demonstrates how to use the PostgreSql extension for Azure Functions.
        /// </summary>
        public string color { get; set; }

        /// <summary>
        /// This sample demonstrates how to use the PostgreSql extension for Azure Functions.
        /// </summary>
        public int quantity { get; set; }


        /// <summary>
        /// This sample demonstrates how to use the PostgreSql extension for Azure Functions.
        /// </summary>
        public DateTime created { get; set; }
    }
}
