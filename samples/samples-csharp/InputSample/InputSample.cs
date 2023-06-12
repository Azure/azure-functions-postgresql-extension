using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace InputSample
{
    public static class InputSample
    {
        [FunctionName("InputSample")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
            [PostgreSql("SELECT * FROM inventory;", "ConnectionString")] IEnumerable<Item> products
            )
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
    }

    /// <summary>
    /// This sample demonstrates how to use the PostgreSql extension for Azure Functions.
    /// </summary>
    public class Item
    {
        /// <summary>
        /// This sample demonstrates how to use the PostgreSql extension for Azure Functions.
        /// </summary>
        public Item(string name, bool isFruit, string color, int quantity = 1)
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
