namespace Exos.Platform.TenancyHelper.MultiTenancy.FragmentsImpl
{
    using System.Collections.Generic;

    /// <summary>
    /// Gets the Tenancy values from ProcessIfElse section of the Policy Document.
    /// </summary>
    internal class ProcessIfElseCtxDataResponse
    {
        /// <summary>
        /// Gets or sets ReturnVal.
        /// </summary>
        public List<long> ReturnVal { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether UseReturnValue.
        /// </summary>
        public bool UseReturnValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ProcessIf.
        /// </summary>
        public bool ProcessIf { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ProcessElse.
        /// </summary>
        public bool ProcessElse { get; set; }
    }
}
