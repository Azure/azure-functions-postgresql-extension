// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Integration.Startup))]

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Integration
{
    /// <summary>
    /// Startup class for the test project.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <summary>
        /// Configure the host builder.
        /// </summary>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            // Set default settings for JsonConvert to simulate a user doing the same in their function.
            // This will cause test failures if serialization/deserialization isn't done correctly
            // (using the helper methods in Utils.cs)
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Objects,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
        }
    }
}

