// <copyright file="Column.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    /// <summary>
    /// Class for storing the properties of a column.
    /// </summary>
    internal class Column
    {
        /// <summary>
        /// Gets or sets the name of the column.
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets or sets the data type of the column.
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the column is a primary key.
        /// </summary>
        public string IsPrimaryKey { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ColumnName: {this.ColumnName}, DataType: {this.DataType}, IsPrimaryKey: {this.IsPrimaryKey}";
        }
    }
}