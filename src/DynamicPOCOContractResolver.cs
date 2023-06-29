// <copyright file="DynamicPOCOContractResolver.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    /// <summary>
    /// Custom contract resolver that only serializes POCO properties that correspond to PostgreSql columns.
    /// </summary>
    public class DynamicPOCOContractResolver : DefaultContractResolver
    {
        private readonly IDictionary<string, string> propertiesToSerialize;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicPOCOContractResolver"/> class.
        /// </summary>
        /// <param name="columns">The SQL columns.</param>
        public DynamicPOCOContractResolver(IDictionary<string, string> columns)
        {
            // we only want to serialize POCO properties that correspond to columns in the table
            this.propertiesToSerialize = columns;
        }

        /// <summary>
        /// Creates the properties.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="memberSerialization">The member serialization.</param>
        /// <returns>A list of JSON properties.</returns>
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            var properties = base
                .CreateProperties(type, memberSerialization)
                .ToDictionary(p => p.PropertyName);

            // Make sure the ordering of columns matches that of SQL
            // Necessary for proper matching of column names to JSON that is generated for each batch of data
            IList<JsonProperty> propertiesToSerialize = new List<JsonProperty>(properties.Count);
            foreach (KeyValuePair<string, string> column in this.propertiesToSerialize)
            {
                if (properties.TryGetValue(column.Key, out JsonProperty value))
                {
                    JsonProperty sqlColumn = value;
                    sqlColumn.PropertyName = sqlColumn.PropertyName;
                    propertiesToSerialize.Add(sqlColumn);
                }
            }

            return propertiesToSerialize;
        }
    }
}