// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.Common;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common;
using Newtonsoft.Json.Linq;
using Npgsql;
using Xunit;
using Xunit.Abstractions;
// using static Microsoft.Azure.WebJobs.Extensions.PostgreSql.Telemetry.Telemetry;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Integration
{
    /// <summary>
    /// Base class for integration tests.
    /// </summary>
    public class IntegrationTestBase : IDisposable
    {
        /// <summary>
        /// The first Function Host process that was started. Null if no process has been started yet.
        /// </summary>
        protected Process FunctionHost => this.FunctionHostList.FirstOrDefault();

        /// <summary>
        /// Host processes for Azure Function CLI.
        /// </summary>
        protected List<Process> FunctionHostList { get; } = new List<Process>();

        /// <summary>
        /// Connection to the database for the current test.
        /// </summary>
        private NpgsqlConnection Connection;

        /// <summary>
        /// Connection string to the master database on the test server, mainly used for database setup and teardown.
        /// </summary>
        private string MasterConnectionString;

        /// <summary>
        /// Connection string to the database created for the test
        /// </summary>
        protected string DbConnectionString { get; private set; }

        /// <summary>
        /// Name of the database used for the current test.
        /// </summary>
        protected string DatabaseName { get; private set; }

        /// <summary>
        /// Output redirect for XUnit tests.
        /// Please use LogOutput() instead of Console or Debug.
        /// </summary>
        protected ITestOutputHelper TestOutput { get; private set; }

        /// <summary>
        /// The port the Functions Host is running on. Default is 7071.
        /// </summary>
        protected int Port { get; private set; } = 7071;

        /// <summary>
        /// Creates a new instance of <see cref="IntegrationTestBase"/>.
        /// </summary>
        public IntegrationTestBase(ITestOutputHelper output = null)
        {
            this.TestOutput = output;
            this.SetupDatabase();
        }

        /// <summary>
        /// Sets up a test database for the current test to use.
        /// </summary>
        private void SetupDatabase()
        {
            NpgsqlConnectionStringBuilder connectionStringBuilder;
            string json;
            try
            {
                // Read the connection string from local.settings.json
                json = File.ReadAllText("./test.settings.json");
            }
            catch (FileNotFoundException)
            {
                string files = string.Join(", ", Directory.GetFiles("./"));
                throw new FileNotFoundException("test.settings.json not found. Please create a test.settings.json file in the test project root with the following contents:\n{\n  \"PostgreSqlConnectionString\": \"<your connection string>\"\n}\n" + files);
            }
            JObject settings;
            try
            {
                settings = JObject.Parse(json);
            }
            catch (Exception e)
            {
                throw new Exception("test.settings.json is not valid JSON. Please make sure it is valid JSON and try again.", e);
            }
            string connectionString;
            try
            {
                connectionString = (string)settings["PostgreSqlConnectionString"];
            }
            catch (Exception e)
            {
                throw new Exception("test.settings.json does not contain a PostgreSqlConnectionString property. Please add one and try again.", e);
            }


            if (connectionString != null)
            {
                this.MasterConnectionString = connectionString;
                connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
            }
            else
            {
                throw new InvalidOperationException("PostgreSqlConnectionString in local.settings.json must be set to a valid PostgreSql connection string.");
            }

            // Create database
            // Retry this in case the server isn't fully initialized yet
            this.DatabaseName = TestUtils.GetUniqueDBName("PostgreSqlBindingsTest");
            using var masterConnection = new NpgsqlConnection(this.MasterConnectionString);
            masterConnection.Open();
            NpgsqlCommand createDatabaseCommand = new($"CREATE DATABASE {this.DatabaseName}", masterConnection);
            createDatabaseCommand.ExecuteNonQuery();
            masterConnection.Close();
            masterConnection.Dispose();


            // Setup subconnection
            this.Connection = new NpgsqlConnection(this.MasterConnectionString);
            this.Connection.Open();
            this.Connection.ChangeDatabase(this.DatabaseName);

            // Create the database definition
            // Create these in a specific order since things like views require that their underlying objects have been created already
            // Ideally all the sql files would be in a sqlproj and can just be deployed
            this.ExecuteAllScriptsInFolder(Path.Combine(TestUtils.GetPathToBin(), "Database", "Tables"));
            // TODO this.ExecuteAllScriptsInFolder(Path.Combine(TestUtils.GetPathToBin(), "Database", "Views"));
            // TODO this.ExecuteAllScriptsInFolder(Path.Combine(TestUtils.GetPathToBin(), "Database", "StoredProcedures"));
            // Set PostgreSqlConnectionString env var for the Function to use
            connectionStringBuilder.Database = this.DatabaseName;
            Environment.SetEnvironmentVariable("PostgreSqlConnectionString", connectionStringBuilder.ToString());
        }

        private void ExecuteAllScriptsInFolder(string folder)
        {
            foreach (string file in Directory.EnumerateFiles(folder, "*.sql"))
            {
                this.LogOutput($"Executing script ${file}");
                this.ExecuteNonQuery(File.ReadAllText(file));
            }
        }

        /// <summary>
        /// This starts the Functions runtime with the specified function(s).
        /// </summary>
        /// <remarks>
        /// - The functionName is different than its route.<br/>
        /// - You can start multiple functions by passing in a space-separated list of function names.<br/>
        /// </remarks>
        public void StartFunctionHost(string functionName, SupportedLanguages language, bool useTestFolder = false, DataReceivedEventHandler customOutputHandler = null, IDictionary<string, string> environmentVariables = null)
        {
            this.LogOutput($"Starting Functions host for {functionName} in {Enum.GetName(typeof(SupportedLanguages), language)}");
            string workingDirectory = language == SupportedLanguages.CSharp && useTestFolder ? TestUtils.GetPathToBin() : Path.Combine(TestUtils.GetPathToBin(), "PostgreSqlExtensionSamples", Enum.GetName(typeof(SupportedLanguages), language));
            if (!Directory.Exists(workingDirectory))
            {
                throw new FileNotFoundException("Working directory not found at " + workingDirectory);
            }

            // Use a different port for each new host process, starting with the default port number: 7071.
            int port = this.Port + this.FunctionHostList.Count;

            var startInfo = new ProcessStartInfo
            {
                // The full path to the Functions CLI is required in the ProcessStartInfo because UseShellExecute is set to false.
                // We cannot both use shell execute and redirect output at the same time: https://docs.microsoft.com//dotnet/api/system.diagnostics.processstartinfo.redirectstandardoutput#remarks
                FileName = GetFunctionsCoreToolsPath(),
                Arguments = $"start --verbose --port {port} --functions {functionName}",
                WorkingDirectory = workingDirectory,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            environmentVariables?.ToList().ForEach(ev => startInfo.EnvironmentVariables[ev.Key] = ev.Value);

            // Always disable telemetry during test runs
            // startInfo.EnvironmentVariables[TelemetryOptoutEnvVar] = "1";

            this.LogOutput($"Starting {startInfo.FileName} {startInfo.Arguments} in {startInfo.WorkingDirectory}");

            var functionHost = new Process
            {
                StartInfo = startInfo
            };

            this.FunctionHostList.Add(functionHost);

            // Register all handlers before starting the functions host process.
            var taskCompletionSource = new TaskCompletionSource<bool>();
            void SignalStartupHandler(object sender, DataReceivedEventArgs e)
            {
                // This string is printed after the function host is started up - use this to ensure that we wait long enough
                // since sometimes the host can take a little while to fully start up
                if (e.Data?.Contains(" Host initialized ") == true)
                {
                    taskCompletionSource.SetResult(true);
                }
            };
            functionHost.OutputDataReceived += SignalStartupHandler;
            functionHost.OutputDataReceived += customOutputHandler;

            functionHost.Start();
            functionHost.OutputDataReceived += this.GetTestOutputHandler(functionHost.Id);
            functionHost.ErrorDataReceived += this.GetTestOutputHandler(functionHost.Id);
            functionHost.BeginOutputReadLine();
            functionHost.BeginErrorReadLine();

            this.LogOutput("Waiting for Azure Function host to start...");

            const int FunctionHostStartupTimeoutInSeconds = 60;
            bool isCompleted = taskCompletionSource.Task.Wait(TimeSpan.FromSeconds(FunctionHostStartupTimeoutInSeconds));
            Assert.True(isCompleted, "Functions host did not start within specified time.");

            // Give additional time to Functions host to setup routes for the HTTP triggers so that the HTTP requests
            // made from the test methods do not get refused.
            const int BufferTimeInSeconds = 5;
            Task.Delay(TimeSpan.FromSeconds(BufferTimeInSeconds)).Wait();

            this.LogOutput("Azure Function host started!");
            functionHost.OutputDataReceived -= SignalStartupHandler;
        }

        private static string GetFunctionsCoreToolsPath()
        {
            // Determine npm install path from either env var set by pipeline or OS defaults
            // Pipeline env var is needed as the Windows hosted agents installs to a non-traditional location
            string nodeModulesPath = Environment.GetEnvironmentVariable("NODE_MODULES_PATH");
            if (string.IsNullOrEmpty(nodeModulesPath))
            {
                nodeModulesPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"npm\node_modules\") :
                    @"/usr/local/lib/node_modules";
            }

            string funcExe = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "func.exe" : "func";
            string funcPath = Path.Combine(nodeModulesPath, "azure-functions-core-tools", "bin", funcExe);

            if (!File.Exists(funcPath))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Search Program Files folder as well
                    string programFilesFuncPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Azure Functions Core Tools", funcExe);
                    if (File.Exists(programFilesFuncPath))
                    {
                        return programFilesFuncPath;
                    }
                    throw new FileNotFoundException($"Azure Function Core Tools not found at {funcPath} or {programFilesFuncPath}");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // Search Mac to see if brew installed location has azure function core tools
                    string usrBinFuncPath = Path.Combine("/usr", "local", "bin", "func");
                    if (File.Exists(usrBinFuncPath))
                    {
                        return usrBinFuncPath;
                    }
                    throw new FileNotFoundException($"Azure Function Core Tools not found at {funcPath} or {usrBinFuncPath}");
                }
                throw new FileNotFoundException($"Azure Function Core Tools not found at {funcPath}");
            }

            return funcPath;
        }

        /// <summary>
        /// Log output from the Functions host process.
        /// </summary>
        protected void LogOutput(string output)
        {
            if (this.TestOutput != null)
            {
                this.TestOutput.WriteLine(output);
            }
            else
            {
                Console.WriteLine(output);
            }
        }

        private DataReceivedEventHandler GetTestOutputHandler(int processId)
        {
            void TestOutputHandler(object sender, DataReceivedEventArgs e)
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    this.LogOutput($"[{processId}] {e.Data}");
                }
            }
            return TestOutputHandler;
        }

        /// <summary>
        /// Send a GET request to the specified URI.
        /// </summary>
        protected async Task<HttpResponseMessage> SendGetRequest(string requestUri, bool verifySuccess = true)
        {
            string timeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", System.Globalization.CultureInfo.InvariantCulture);
            this.LogOutput($"[{timeStamp}] Sending GET request: {requestUri}");

            if (string.IsNullOrEmpty(requestUri))
            {
                throw new ArgumentException("URI cannot be null or empty.");
            }

            var client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(requestUri);

            if (verifySuccess)
            {
                Assert.True(response.IsSuccessStatusCode, $"Http request failed with code {response.StatusCode}. Please check output for more detailed message.");
            }

            return response;
        }

        /// <summary>
        /// Send a POST request to the specified URI.
        /// </summary>
        protected async Task<HttpResponseMessage> SendPostRequest(string requestUri, string json, bool verifySuccess = true)
        {
            this.LogOutput("Sending POST request: " + requestUri);

            if (string.IsNullOrEmpty(requestUri))
            {
                throw new ArgumentException("URI cannot be null or empty.");
            }

            var client = new HttpClient();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage response = await client.PostAsync(requestUri, content);

            if (verifySuccess)
            {
                Assert.True(response.IsSuccessStatusCode, $"Http request failed with code {response.StatusCode}. Please check output for more detailed message.");
            }

            return response;
        }

        /// <summary>
        /// Executes a command against the current connection.
        /// </summary>
        /// <param name="commandText">Command text to execute</param>
        /// <param name="message">Optional message to write when this query is executed. Defaults to writing the query commandText</param>
        protected void ExecuteNonQuery(string commandText, string message = null)
        {
            TestUtils.ExecuteNonQuery(this.Connection, commandText, this.LogOutput, message: message);
        }

        /// <summary>
        /// Executes a command against the current connection and the result is returned.
        /// </summary>
        protected object ExecuteScalar(string commandText)
        {
            return TestUtils.ExecuteScalar(this.Connection, commandText, this.LogOutput);
        }


        /// <summary>
        /// Cleans up the test database and disposes the connection.
        /// </summary>
        public void Dispose()
        {
            // Try to clean up after test run, but don't consider it a failure if we can't for some reason
            try
            {
                this.Connection.Close();
                this.Connection.Dispose();
            }
            catch (Exception e1)
            {
                this.LogOutput($"Failed to close connection. Error: {e1.Message}");
            }

            this.DisposeFunctionHosts();


            string dropConnectionsSql = $"SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '{this.DatabaseName}' AND pid <> pg_backend_pid();";

            try
            {
                using var masterConnection = new NpgsqlConnection(this.MasterConnectionString);
                masterConnection.Open();
                TestUtils.ExecuteNonQuery(masterConnection, dropConnectionsSql, this.LogOutput);
                TestUtils.ExecuteNonQuery(masterConnection, $"DROP DATABASE IF EXISTS {this.DatabaseName}", this.LogOutput);
            }
            catch (Exception e4)
            {
                this.LogOutput($"Failed to drop {this.DatabaseName}, Error: {e4.Message}");
            }


            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes all the running function hosts
        /// </summary>
        protected void DisposeFunctionHosts()
        {
            foreach (Process functionHost in this.FunctionHostList)
            {
                if (functionHost.HasExited)
                {
                    continue;
                }
                try
                {
                    functionHost.CancelOutputRead();
                    functionHost.CancelErrorRead();
                    functionHost.Kill(true);
                    functionHost.WaitForExit();
                    functionHost.Dispose();
                }
                catch (Exception ex)
                {
                    this.LogOutput($"Failed to stop function host, Error: {ex.Message}. Stack: {ex.StackTrace}");
                }
            }
            this.FunctionHostList.Clear();
        }

        /// <summary>
        /// Sends an input binding request to the specified function.
        /// </summary>
        protected async Task<HttpResponseMessage> SendInputRequest(string functionName, string query = "")
        {
            string requestUri = $"http://localhost:{this.Port}/api/{functionName}/{query}";

            return await this.SendGetRequest(requestUri);
        }

        /// <summary>
        /// Sends an output binding request to the specified function.
        /// </summary>
        protected Task<HttpResponseMessage> SendOutputGetRequest(string functionName, IDictionary<string, string> query = null)
        {
            string requestUri = $"http://localhost:{this.Port}/api/{functionName}";

            if (query != null)
            {
                requestUri = QueryHelpers.AddQueryString(requestUri, query);
            }

            return this.SendGetRequest(requestUri);
        }

        /// <summary>
        /// Sends an output binding post request to the specified function.
        /// </summary>
        protected Task<HttpResponseMessage> SendOutputPostRequest(string functionName, string query)
        {
            string requestUri = $"http://localhost:{this.Port}/api/{functionName}";

            return this.SendPostRequest(requestUri, query);
        }

        /// <summary>
        /// Inserts a list of products into the database
        /// </summary>
        protected void InsertProducts(Product[] products)
        {
            if (products.Length == 0)
            {
                return;
            }

            var queryBuilder = new StringBuilder();
            foreach (Product p in products)
            {
                queryBuilder.AppendLine($"INSERT INTO public.Products VALUES({p.ProductId}, '{p.Name}', {p.Cost});");
            }

            this.ExecuteNonQuery(queryBuilder.ToString(), $"Inserting {products.Length} products");
        }
        /// <summary>
        /// Generates an array of products
        /// </summary>
        /// <param name="n">Number of products to generate</param>
        /// <param name="cost">Represents the starting cost. itemCost = cost * i where i is the index in the list starting at 1</param>
        protected static Product[] GetProducts(int n, int cost)
        {
            var result = new Product[n];
            for (int i = 1; i <= n; i++)
            {
                result[i - 1] = new Product
                {
                    ProductId = i,
                    Name = "test",
                    Cost = cost * i
                };
            }
            return result;
        }

        /// <summary>
        /// Gets an array of products with the same cost
        /// </summary>
        protected static Product[] GetProductsWithSameCost(int n, int cost)
        {
            var result = new Product[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = new Product
                {
                    ProductId = i,
                    Name = "test",
                    Cost = cost
                };
            }
            return result;
        }

        /// <summary>
        /// Gets an array of products with the same name and cost
        /// </summary>
        protected static Product[] GetProductsWithSameCostAndName(int n, int cost, string name, int offset = 0)
        {
            var result = new Product[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = new Product
                {
                    ProductId = i + offset,
                    Name = name,
                    Cost = cost
                };
            }
            return result;
        }
    }
}