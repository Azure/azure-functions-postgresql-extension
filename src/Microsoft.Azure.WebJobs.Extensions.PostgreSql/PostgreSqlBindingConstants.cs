// <copyright file="PostgreSqlBindingConstants.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    /// <summary>
    /// Provides constants for use with the PostgreSQL binding extension.
    /// </summary>
    internal static class PostgreSqlBindingConstants
    {
        /// <summary>
        /// The ISO 8601 DateTime format used for PostgreSQL interactions.
        /// </summary>
        public const string ISO8061DATETIMEFORMAT = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffZ";

        /// <summary>
        /// Represents the different editions of a PostgreSQL Server instance.
        /// </summary>
        public enum EngineEdition
        {
            /// <summary>
            /// PostgreSQL Desktop Engine Edition.
            /// </summary>
            DesktopEngine,

            /// <summary>
            /// PostgreSQL Standard Edition.
            /// </summary>
            Standard,

            /// <summary>
            /// PostgreSQL Enterprise Edition.
            /// </summary>
            Enterprise,

            /// <summary>
            /// PostgreSQL Express Edition.
            /// </summary>
            Express,

            /// <summary>
            /// Azure SQL Database Edition.
            /// </summary>
            SQLDatabase,

            /// <summary>
            /// Azure Synapse Analytics Edition.
            /// </summary>
            AzureSynapseAnalytics,

            /// <summary>
            /// Azure SQL Managed Instance Edition.
            /// </summary>
            AzureSQLManagedInstance,

            /// <summary>
            /// Azure SQL Edge Edition.
            /// </summary>
            AzureSQLEdge,

            /// <summary>
            /// Azure Synapse serverless SQL pool Edition.
            /// </summary>
            AzureSynapseserverlessSQLpool,
        }

        /// <summary>
        /// Represents the different types of conversion being performed by the input binding.
        /// </summary>
        public enum ConvertType
        {
            /// <summary>
            /// Asynchronous enumeration of objects.
            /// </summary>
            IAsyncEnumerable,

            /// <summary>
            /// Synchronous enumeration of objects.
            /// </summary>
            IEnumerable,

            /// <summary>
            /// Conversion to JSON format.
            /// </summary>
            Json,

            /// <summary>
            /// PostgreSQL command.
            /// </summary>
            PostgreSqlCommand,

            /// <summary>
            /// Conversion to JArray object.
            /// </summary>
            JArray,
        }
    }
}