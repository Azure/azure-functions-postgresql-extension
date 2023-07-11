// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Integration
{
    /// <summary>
    /// Collection of integration tests that require a PostgreSQL database.
    /// </summary>
    [CollectionDefinition(Name)]
    public class IntegrationTestsCollection : ICollectionFixture<IntegrationTestFixture>
    {
        /// <summary>
        /// The name of the integration test collection.
        /// </summary>
        public const string Name = "IntegrationTests";

        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
