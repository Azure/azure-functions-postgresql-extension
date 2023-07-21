using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.Common;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.DemoSamples
{
    public static class TranslateEntry
    {
        private static readonly string endpoint = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&to=es";
        private static readonly string location = "eastus";

        [FunctionName("TranslateEntry")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "translate-entry")] HttpRequest req,
            [PostgreSql("spanish", "PostgreSqlConnectionString")] IAsyncCollector<Entry> outputEntry,
            ILogger log)
        {
            //get body text
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic entryFromBody = JsonConvert.DeserializeObject(requestBody);
            Entry newEntry = new();
            try
            {
                newEntry.body = entryFromBody.body;
                // set the created time to now
                newEntry.created = DateTime.Now;
            }
            catch (Exception e)
            {
                log.LogError(e.Message);
                return new BadRequestObjectResult("Please pass a valid entry in the request body");
            }

            string key = Environment.GetEnvironmentVariable("TranslatorKey");
            string textToTranslate = newEntry.body;
            object[] body = new object[] { new { Text = textToTranslate } };
            var outgoingRequestBody = JsonConvert.SerializeObject(body);

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage())
            {
                // Build the request.
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(endpoint);
                request.Content = new StringContent(outgoingRequestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);
                request.Headers.Add("Ocp-Apim-Subscription-Region", location);

                // Send the request and get response.
                HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
                // Read response as a string.
                string result = await response.Content.ReadAsStringAsync();
                dynamic resultObject = JsonConvert.DeserializeObject(result);
                result = resultObject[0].translations[0].text;
                newEntry.body = result;
            }
            await outputEntry.AddAsync(newEntry);

            return new OkObjectResult(newEntry);
        }
    }
}