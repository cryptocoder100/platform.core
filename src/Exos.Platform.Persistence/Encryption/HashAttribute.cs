namespace Exos.Platform.Persistence.Encryption
{
    using System;

    /// <summary>
    /// Defines the <see cref="HashAttribute" />.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class HashAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the column to Hash.
        /// </summary>
        public string ColumnToHash { get; set; }
    }
}
