// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common
{
    /// <summary>
    /// This class makes sure that the output binding works when the order of the properties in the POCO is different than the order of the columns in the PostgreSQL table.
    /// </summary>
    public class ProductDifferentColumnOrder
    {
        /// <summary>
        /// Cost of the product
        /// </summary>
        public int Cost { get; set; }

        /// <summary>
        /// Name of the product
        /// </summary>
        public string Name { get; set; }
    }
}