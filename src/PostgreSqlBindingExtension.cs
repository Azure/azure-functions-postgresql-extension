// <copyright file="PostgreSqlBindingExtension.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>

using System;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{
    /// <summary>
    /// Extension class used to register PostgreSql configuration.
    /// </summary>
    public static class PostgreSqlBindingExtension
    {
        /// <summary>
        /// Extension method used to register PostgreSql configuration.
        /// </summary>
        /// <param name="builder"> The <see cref="IWebJobsBuilder"/> to register the binding with. </param>
        /// <returns> The <see cref="IWebJobsBuilder"/>. </returns>
        public static IWebJobsBuilder AddPostgreSql(this IWebJobsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddExtension<PostgreSqlBindingConfigProvider>();
            return builder;
        }
    }
}