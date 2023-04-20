#pragma warning disable SA1402 // File may only contain a single type
using System;
using System.Collections.Generic;

namespace Exos.Platform.Persistence
{
    /// <summary>
    /// Defines the <see cref="DapperQuery" />.
    /// </summary>
    public abstract class DapperQuery
    {
        /// <summary>
        /// Gets or sets the Query.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets the TableAlias.
        /// </summary>
        public IEnumerable<string> TableAlias { get; set; }

        /// <summary>
        /// Gets the ReturnType.
        /// </summary>
        public virtual Type ReturnType { get; }
    }

    /// <summary>
    /// Defines the <see cref="DapperQuery{T}" />.
    /// </summary>
    /// <typeparam name="T">Query return type (model or entity).</typeparam>
    public class DapperQuery<T> : DapperQuery
    {
        /// <summary>
        /// Gets the ReturnType.
        /// </summary>
        public override Type ReturnType => typeof(T);
    }
}
#pragma warning restore SA1402 // File may only contain a single type