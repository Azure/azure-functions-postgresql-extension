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
            [PostgreSql("SELECT 1;", "ConnectionString")] string result)
        {

            Console.WriteLine("HttpTriggerSample Start");
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            string responseMessage = string.IsNullOrEmpty(name)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {name}. This HTTP triggered function executed successfully.";


            Console.WriteLine(result);

            Console.WriteLine("HttpTriggerSample END");


            return new OkObjectResult(responseMessage);
        }
    }
}
