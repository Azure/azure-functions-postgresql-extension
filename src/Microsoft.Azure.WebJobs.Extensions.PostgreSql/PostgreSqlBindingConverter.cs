// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    /// <summary>
    /// Converts a <see cref="PostgreSqlAttribute"/> to a connection string
    /// </summary>
    internal class PostgreSqlBindingConverter : IConverter<PostgreSqlAttribute, string>
    {
        private readonly PostgreSqlBindingConfigProvider configProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlBindingConverter"/> class.
        /// </summary>
        public PostgreSqlBindingConverter(PostgreSqlBindingConfigProvider configProvider)
        {
            this.configProvider = configProvider;
        }

        /// <summary>
        /// Converts a <see cref="PostgreSqlAttribute"/> to a connection string
        /// </summary>
        public string Convert(PostgreSqlAttribute attribute)
        {
            return "Hello World!";
        }
    }
}
