// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common
{
    /// <summary>
    /// Class to represent a product with more columns than the table
    /// </summary>
    public class ProductExtraColumns
    {
        /// <summary>
        /// The product id
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// The product name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The product cost
        /// </summary>
        public int Cost { get; set; }

        /// <summary>
        /// An extra integer column (not in the table)
        /// </summary>
        public int ExtraInt { get; set; }

        /// <summary>
        /// An extra string column (not in the table)
        /// </summary>
        public string ExtraString { get; set; }
    }
}