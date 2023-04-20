namespace Exos.Platform.AspNetCore.Models
{
    /// <summary>
    /// Specifies the type of model.
    /// </summary>
    public enum ModelType
    {
        /// <summary>
        /// An <see cref="ErrorModel" /> object.
        /// </summary>
        Error,

        /// <summary>
        /// A <see cref="ListModel{T}" /> object.
        /// </summary>
        List,

        // Identity
    }
}
