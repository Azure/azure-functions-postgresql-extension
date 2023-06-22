// <copyright file="BatchInsertCommandComponents.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    /// <summary>
    /// Class for storing the components of a batch insert command.
    /// </summary>
    internal class BatchInsertCommandComponents
    {
        /// <summary>
        /// Gets or sets the insert clause of the command.
        /// </summary>
        public string InsertClause { get; set; }

        /// <summary>
        /// Gets or sets the values clause of the command.
        /// </summary>
        public string ValuesClause { get; set; }

        /// <summary>
        /// Gets or sets the conflict clause of the command.
        /// </summary>
        public string ConflictClause { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"InsertClause: {this.InsertClause}, ValuesClause: {this.ValuesClause}, ConflictClause: {this.ConflictClause}";
        }
    }
}