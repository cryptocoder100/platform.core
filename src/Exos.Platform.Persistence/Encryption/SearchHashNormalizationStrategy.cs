namespace Exos.Platform.Persistence.Encryption
{
    /// <summary>
    /// Normalization scheme to use for the hashing algorithm.
    /// </summary>
    public enum SearchHashNormalizationStrategy
    {
        /// <summary>
        /// No strategy, hash as is.
        /// </summary>
        None,

        /// <summary>
        /// standard strategy for names.
        /// </summary>
        Name,

        /// <summary>
        /// standard strategy for phone numbers.
        /// </summary>
        Phone,

        /// <summary>
        /// Standard strategy for emails.
        /// </summary>
        Email
    }
}