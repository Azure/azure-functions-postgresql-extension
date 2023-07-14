// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common
{
    /// <summary>
    /// Product class with incorrect casing for ProductID
    /// </summary>
    public class ProductIncorrectCasing
    {
        /// <summary>
        /// ProductID property (should be ProductId)
        /// </summary>
        public int ProductID { get; set; }

        /// <summary>
        /// Name property
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Cost property
        /// </summary>
        public int Cost { get; set; }
    }
}