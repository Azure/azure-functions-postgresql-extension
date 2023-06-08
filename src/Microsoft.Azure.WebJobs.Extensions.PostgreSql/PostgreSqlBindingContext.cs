using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Npgsql;


namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql
{

    /// <summary>
    /// Defines the resolved PostgreSql binding context.
    /// </summary>
    public class PostgreSqlBindingContext
    {
        /// <summary>
        /// Gets or sets the name of the parameter being bound to.
        /// </summary>
        public PostgreSqlAttribute ResolvedAttribute { get; set; }

        /// <summary>
        /// Gets or sets the connection parameter being bound to.
        /// </summary>
        public NpgsqlConnection Connection { get; set; }
    }
}