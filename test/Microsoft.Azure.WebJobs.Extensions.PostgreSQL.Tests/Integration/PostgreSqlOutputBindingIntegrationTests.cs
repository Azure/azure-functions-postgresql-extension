// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.OutputBindingSamples;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.Common;
using Xunit;
using Xunit.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Integration;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Integration
{
    /// <summary>
    /// End-to-end tests for the PostgreSql binding.
    /// </summary>
    [Collection(IntegrationTestsCollection.Name)]
    [LogTestName]
    public class PostgreSqlOutputBindingIntegrationTests : IntegrationTestBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlOutputBindingIntegrationTests"/> class.
        /// </summary>
        public PostgreSqlOutputBindingIntegrationTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <summary>
        /// Tests that a single row can be inserted into a table.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData(1, "Test", 5)]
        [PostgreSqlInlineData(0, "", 0)]
        [PostgreSqlInlineData(-500, "ABCD", 580)]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductTest(int id, string name, int cost, SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProduct), lang);

            var query = new Dictionary<string, object>()
            {
                { "ProductId", id },
                { "Name", name },
                { "Cost", cost }
            };

            var json = Utils.JsonSerializeObject(query);

            Console.WriteLine($"JSON: {json}");

            this.SendOutputPostRequest("addproduct", json).Wait();

            // Verify result
            Assert.Equal(name, this.ExecuteScalar($"select \"Name\" from Products where \"ProductId\"={id}"));
            Assert.Equal(cost, this.ExecuteScalar($"select \"Cost\" from Products where \"ProductId\"={id}"));
        }

        /// <summary>
        /// Tests that a single row can be inserted into a table with parameters.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData(1, "Test", 5)]
        [PostgreSqlInlineData(0, "", 0)]
        [PostgreSqlInlineData(-500, "ABCD", 580)]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductParamsTest(int id, string name, int cost, SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductParams), lang);

            var query = new Dictionary<string, string>()
            {
                { "productId", id.ToString() },
                { "name", name },
                { "cost", cost.ToString() }
            };

            this.SendOutputGetRequest("addproduct-params", query).Wait();

            // Verify result
            Assert.Equal(name, this.ExecuteScalar($"select \"Name\" from Products where \"ProductId\"={id}"));
            Assert.Equal(cost, this.ExecuteScalar($"select \"Cost\" from Products where \"ProductId\"={id}"));
        }

        /// <summary>
        /// Tests that an array of products can be inserted into a table.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductArrayTest(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductsArray), lang);

            // First insert some test data
            this.ExecuteNonQuery("INSERT INTO Products VALUES (1, 'test', 100)");
            this.ExecuteNonQuery("INSERT INTO Products VALUES (2, 'test', 100)");
            this.ExecuteNonQuery("INSERT INTO Products VALUES (3, 'test', 100)");

            Product[] prods = new[]
            {
                new Product()
                {
                    ProductId = 1,
                    Name = "Cup",
                    Cost = 2
                },
                new Product
                {
                    ProductId = 2,
                    Name = "Glasses",
                    Cost = 12
                }
            };

            this.SendOutputPostRequest("addproducts-array", Utils.JsonSerializeObject(prods)).Wait();

            // Function call changes first 2 rows to (1, 'Cup', 2) and (2, 'Glasses', 12)
            Assert.Equal(1, (int)(long)this.ExecuteScalar("SELECT COUNT(1) FROM Products WHERE \"Cost\" = 100"));
            Assert.Equal(2, this.ExecuteScalar("SELECT \"Cost\" FROM Products WHERE \"ProductId\" = 1"));
            Assert.Equal(2, this.ExecuteScalar("SELECT \"ProductId\" FROM Products WHERE \"Cost\" = 12"));
        }

        /// <summary>
        /// Test compatibility with converting various data types to their respective
        /// PostgreSql types.
        /// </summary>
        /// <param name="lang">The language to run the test against</param>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]

        public void AddProductColumnTypesTest(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductColumnTypes), lang, true);

            var queryParameters = new Dictionary<string, string>()
            {
                { "productId", "999" }
            };

            this.SendOutputGetRequest("addproduct-columntypes", queryParameters).Wait();

            // If we get here then the test is successful - an exception will be thrown if there were any problems
        }

        /// <summary>
        /// Tests that output bindings can be used with a collector.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        [UnsupportedLanguages(SupportedLanguages.JavaScript)] // Collectors are only available in C#
        public void AddProductsCollectorTest(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductsCollector), lang);

            // Function should add 5000 rows to the table
            this.SendOutputGetRequest("addproducts-collector").Wait();

            Assert.Equal(5000, (int)(long)this.ExecuteScalar("SELECT COUNT(1) FROM Products"));
        }

        /// <summary>
        /// Tests that output bindings can be used with a queue trigger.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void QueueTriggerProductsTest(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(QueueTriggerProducts), lang);

            string uri = $"http://localhost:{this.Port}/admin/functions/QueueTriggerProducts";
            string json = /*lang=json*/ "{ 'input': 'Test Data' }";

            this.SendPostRequest(uri, json).Wait();

            Thread.Sleep(5000);

            // Function should add 100 rows
            Assert.Equal(100, (int)(long)this.ExecuteScalar("SELECT COUNT(1) FROM Products"));
        }

        /// <summary>
        /// Tests that output bindings can be used with a timer trigger.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void TimerTriggerProductsTest(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(TimerTriggerProducts), lang);

            // Since this function runs on a schedule (every 5 seconds), we don't need to invoke it.
            // We will wait 6 seconds to guarantee that it has been fired at least once, and check that at least 1000 rows of data has been added.
            Thread.Sleep(6000);

            int rowsAdded = (int)(long)this.ExecuteScalar("SELECT COUNT(1) FROM Products");
            Assert.True(rowsAdded >= 1000);
        }

        /// <summary>
        /// Tests that output bindings can operate when there exists more columns in the object than in the table.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductExtraColumnsTest(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductExtraColumns), lang, true);

            // Since ProductExtraColumns has columns that does not exist in the table,
            // those columns should be ignored and the row should still be added successfully.
            this.SendOutputGetRequest("addproduct-extracolumns").Wait();
            Assert.Equal(1, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM Products"));
        }

        /// <summary>
        /// Tests that output bindings can operate when there exists less columns in the object than in the table.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductMissingColumnsTest(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductMissingColumns), lang, true);

            // Even though the ProductMissingColumns object is missing the Cost column,
            // the row should still be added successfully since Cost can be null.
            this.SendOutputPostRequest("addproduct-missingcolumns", string.Empty).Wait();
            Assert.Equal(1, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM Products"));
        }

        /// <summary>
        /// Tests that an exception is thrown when there exists less columns in the object than in the table and the table does not allow null values for the missing columns.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductMissingColumnsNotNullTest(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductMissingColumnsExceptionFunction), lang, true);

            // Since the PostgreSql table does not allow null for the Cost column,
            // inserting a row without a Cost value should throw an Exception.
            Assert.Throws<AggregateException>(() => this.SendOutputPostRequest("addproduct-missingcolumnsexception", string.Empty).Wait());
        }

        /// <summary>
        /// Makes sure that if there is an issue with one of the rows in a batch, the entire batch is rolled back.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductNoPartialUpsertTest(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductsNoPartialUpsert), lang, true);

            Assert.Throws<AggregateException>(() => this.SendOutputPostRequest("addproducts-nopartialupsert", string.Empty).Wait());
            // No rows should be upserted since there was a row with an invalid value
            Assert.Equal(0, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsNameNotNull"));
        }

        /// <summary>
        /// Tests that for tables with an identity column we are able to insert items.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductWithIdentity(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductWithIdentityColumn), lang);
            // Identity column (ProductId) is left out for new items
            var query = new Dictionary<string, string>()
            {
                { "name", "MyProduct" },
                { "cost", "1" }
            };
            Assert.Equal(0, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsWithIdentity"));
            this.SendOutputGetRequest(nameof(AddProductWithIdentityColumn), query).Wait();
            // Product should have been inserted correctly even without an ID when there's an identity column present
            Assert.Equal(1, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsWithIdentity"));
        }

        /// <summary>
        /// Tests that for tables with an identity column we are able to insert multiple items at once
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductsWithIdentityColumnArray(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductsWithIdentityColumnArray), lang);
            Assert.Equal(0, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsWithIdentity"));
            this.SendOutputGetRequest(nameof(AddProductsWithIdentityColumnArray)).Wait();
            // Multiple items should have been inserted
            Assert.Equal(2, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsWithIdentity"));
        }

        /// <summary>
        /// Tests that for tables with multiple primary columns (including an identity column) we are able to
        /// insert items.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductWithIdentity_MultiplePrimaryColumns(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductWithMultiplePrimaryColumnsAndIdentity), lang);
            var query = new Dictionary<string, string>()
            {
                { "externalId", "101" },
                { "name", "MyProduct" },
                { "cost", "1" }
            };
            Assert.Equal(0, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsWithMultiplePrimaryColumnsAndIdentity"));
            this.SendOutputGetRequest(nameof(AddProductWithMultiplePrimaryColumnsAndIdentity), query).Wait();
            // Product should have been inserted correctly even without an ID when there's an identity column present
            Assert.Equal(1, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsWithMultiplePrimaryColumnsAndIdentity"));
        }

        /// <summary>
        /// Tests that when using a table with an identity column that if the identity column is specified
        /// by the function we handle inserting/updating that correctly.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductWithIdentity_SpecifyIdentityColumn(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductWithIdentityColumnIncluded), lang);
            var query = new Dictionary<string, string>()
            {
                { "productId", "1" },
                { "name", "MyProduct" },
                { "cost", "1" }
            };
            Assert.Equal(0, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsWithIdentity"));
            this.SendOutputGetRequest(nameof(AddProductWithIdentityColumnIncluded), query).Wait();
            // New row should have been inserted
            Assert.Equal(1, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsWithIdentity"));
            query = new Dictionary<string, string>()
            {
                { "productId", "1" },
                { "name", "MyProduct2" },
                { "cost", "1" }
            };
            this.SendOutputGetRequest(nameof(AddProductWithIdentityColumnIncluded), query).Wait();
            // Existing row should have been updated
            Assert.Equal(1, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsWithIdentity"));
            Assert.Equal(1, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsWithIdentity WHERE \"Name\"='MyProduct2'"));
        }

        /// <summary>
        /// Tests that when using a table with an identity column we can handle a null (missing) identity column
        /// an error should be thrown because null was specified for the (non null) identity column.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductWithIdentity_NoIdentityColumn(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductWithIdentityColumnIncluded), lang);
            var query = new Dictionary<string, string>()
            {
                { "name", "MyProduct" },
                { "cost", "1" }
            };
            Assert.Equal(0, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsWithIdentity"));
            Assert.Throws<AggregateException>(() => this.SendOutputGetRequest(nameof(AddProductWithIdentityColumnIncluded), query).Wait());

            // No rows should have been inserted
            Assert.Equal(0, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsWithIdentity"));
        }

        /// <summary>
        /// Tests that when using a table with an identity column along with other primary
        /// keys an error is thrown if at least one of the primary keys is missing.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductWithIdentity_MissingPrimaryColumn(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductWithMultiplePrimaryColumnsAndIdentity), lang);
            var query = new Dictionary<string, string>()
            {
                // Missing externalId
                { "name", "MyProduct" },
                { "cost", "1" }
            };
            Assert.Equal(0, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsWithMultiplePrimaryColumnsAndIdentity"));
            Assert.Throws<AggregateException>(() => this.SendOutputGetRequest(nameof(AddProductWithMultiplePrimaryColumnsAndIdentity), query).Wait());
            // Nothing should have been inserted
            Assert.Equal(0, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM ProductsWithMultiplePrimaryColumnsAndIdentity"));
        }

        /// <summary>
        /// Tests that an error is thrown when the object field names and table column names do not match.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductIncorrectCasing(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductIncorrectCasing), lang);

            Assert.Throws<AggregateException>(() => this.SendOutputGetRequest("addproduct-incorrectcasing").Wait());
            Assert.Equal(0, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM Products"));
        }

        /// <summary>
        /// Tests that subsequent upserts work correctly when the object properties are different from the first upsert.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductWithDifferentPropertiesTest(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProduct), lang);

            var query1 = new Dictionary<string, object>()
            {
                { "ProductId", 0 },
                { "Name", "test" },
                { "Cost", 100 }
            };

            var query2 = new Dictionary<string, object>()
            {
                { "ProductId", 0 },
                { "Name", "test2" }
            };

            this.SendOutputPostRequest("addproduct", Utils.JsonSerializeObject(query1)).Wait();
            this.SendOutputPostRequest("addproduct", Utils.JsonSerializeObject(query2)).Wait();

            // Verify result
            Assert.Equal("test2", this.ExecuteScalar($"select \"Name\" from Products where \"ProductId\"=0"));
        }

        /// <summary>
        /// Tests that when upserting an item with no properties, an error is thrown.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        // Only the JavaScript function passes an empty JSON to the PostgreSql extension.
        // C# throws an error while creating the Product object in the function.
        [UnsupportedLanguages(SupportedLanguages.CSharp)]
        public async Task NoPropertiesThrows(SupportedLanguages lang)
        {
            var foundExpectedMessageSource = new TaskCompletionSource<bool>();
            this.StartFunctionHost(nameof(AddProductParams), lang, false, (object sender, DataReceivedEventArgs e) =>
            {
                if (e.Data.Contains("No property values found in item to upsert. If using query parameters, ensure that the casing of the parameter names and the property names match."))
                {
                    if (!foundExpectedMessageSource.Task.IsCompleted)
                    {
                        foundExpectedMessageSource.SetResult(true);
                    }
                }

            });

            var query = new Dictionary<string, string>() { };

            // The upsert should fail since no parameters were passed
            Exception exception = Assert.Throws<AggregateException>(() => this.SendOutputGetRequest("addproduct-params", query).Wait());
            // Verify the message contains the expected error so that other errors don't mistakenly make this test pass
            // Wait 2sec for message to get processed to account for delays reading output
            await foundExpectedMessageSource.Task.TimeoutAfter(TimeSpan.FromMilliseconds(2000), $"Timed out waiting for expected error message");
        }

        /// <summary>
        /// Tests that rows are inserted correctly when the table contains default values or identity columns even if the order of
        /// the properties in the POCO/JSON object is different from the order of the columns in the table.
        /// </summary>
        [Theory]
        [PostgreSqlInlineData()]
        [Trait("Category", "Integration")]
        [Trait("Binding", "Output")]
        public void AddProductDifferentColumnOrderTest(SupportedLanguages lang)
        {
            this.StartFunctionHost(nameof(AddProductDifferentColumnOrder), lang, true);

            Assert.Equal(0, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM Products"));
            this.SendOutputGetRequest("addproductdifferentcolumnorder").Wait();
            Assert.Equal(1, (int)(long)this.ExecuteScalar("SELECT COUNT(*) FROM Products"));
        }
    }
}
