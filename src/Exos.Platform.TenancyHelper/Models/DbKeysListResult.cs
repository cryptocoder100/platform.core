namespace Exos.Platform.TenancyHelper.Models
{
    /// <summary>
    /// Contains the result from a call to the Azure List Keys API.
    /// </summary>
    public class DbKeysListResult
    {
        /// <summary>
        /// Gets or sets primary DB Key.
        /// </summary>
        public string PrimaryMasterKey { get; set; }

        /// <summary>
        /// Gets or sets primary Read-Only Key.
        /// </summary>
        public string PrimaryReadonlyMasterKey { get; set; }

        /// <summary>
        /// Gets or sets secondary DB Key.
        /// </summary>
        public string SecondaryMasterKey { get; set; }

        /// <summary>
        /// Gets or sets secondary Read-Only Key.
        /// </summary>
        public string SecondaryReadonlyMasterKey { get; set; }
    }
}
