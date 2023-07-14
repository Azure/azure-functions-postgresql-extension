// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common
{
    /// <summary>
    /// This class is used to test the case where the object has missing columns.
    /// </summary>
    public class ProductMissingColumns
    {
        /// <summary>
        /// This is the primary key.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// The name of the product.
        /// </summary>
        public string Name { get; set; }
    }
}