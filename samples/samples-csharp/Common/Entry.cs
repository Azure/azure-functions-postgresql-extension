// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Samples.Common
{
    public class Entry
    {
        public string body { get; set; }

        public DateTime created { get; set; }

    }
}