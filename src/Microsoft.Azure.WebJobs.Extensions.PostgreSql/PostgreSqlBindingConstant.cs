// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    internal static class PostgreSqlBindingConstants
    {
        public const string ISO_8061_DATETIME_FORMAT = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffZ";

        /// <summary>
        /// PostgreSql Server Edition of the target server
        /// TODO update for PostgreSql 
        /// </summary>
        public enum EngineEdition
        {
            DesktopEngine,
            Standard,
            Enterprise,
            Express,
            SQLDatabase,
            AzureSynapseAnalytics,
            AzureSQLManagedInstance,
            AzureSQLEdge,
            AzureSynapseserverlessSQLpool,
        }

        /// <summary>
        /// The type of conversion being performed by the input binding
        /// </summary>
        public enum ConvertType
        {
            IAsyncEnumerable,
            IEnumerable,
            Json,
            PostgreSqlCommand,
            JArray
        }
    }
}