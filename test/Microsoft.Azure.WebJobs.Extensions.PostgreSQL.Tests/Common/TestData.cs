// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common
{
    
    /// <summary>
    /// Represents test data.
    /// </summary>
    public class TestData
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the cost.
        /// </summary>
        public double Cost { get; set; }

        /// <summary>
        /// Gets or sets the timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is not TestData otherData)
            {
                return false;
            }
            return this.ID == otherData.ID && this.Cost == otherData.Cost && ((this.Name == null && otherData.Name == null) ||
                string.Equals(this.Name, otherData.Name, StringComparison.OrdinalIgnoreCase)) && this.Timestamp.Equals(otherData.Timestamp);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(this.ID, this.Name, this.Cost, this.Timestamp);
        }
    }

}

