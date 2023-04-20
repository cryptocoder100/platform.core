namespace Exos.Platform.TenancyHelper.Interfaces
{
    /// <summary>
    ///  MultiTenancy Policy context.
    /// </summary>
    public interface IPolicyContext
    {
        /// <summary>
        /// Gets or sets the name of the Policy Document.
        /// </summary>
        string PolicyDocName { get; set; }

        /// <summary>
        /// Gets or sets the Policy Document.
        /// </summary>
        string PolicyDoc { get; set; }

        /// <summary>
        /// Set the User Context.
        /// </summary>
        /// <param name="context">User Context.</param>
        void SetUserContext(IUserContext context);

        /// <summary>
        /// Get the User Context.
        /// </summary>
        /// <returns>User Context.</returns>
        IUserContext GetUserContext();

        /// <summary>
        /// Set a Custom long field.
        /// </summary>
        /// <param name="fieldName">Field Name.</param>
        /// <param name="fieldValue">Value.</param>
        void SetCustomIntField(string fieldName, long fieldValue);

        /// <summary>
        /// Get a Custom long field.
        /// </summary>
        /// <param name="fieldName">Field Name.</param>
        /// <returns>Field Value.</returns>
        long GetCustomIntField(string fieldName);

        /// <summary>
        /// Check if Field exists in the Policy Context.
        /// </summary>
        /// <param name="fieldName">Field Name.</param>
        /// <returns>True if Field Exists, false otherwise.</returns>
        bool ContainsCustomField(string fieldName);

        /// <summary>
        /// Check if Field value is null or empty.
        /// </summary>
        /// <param name="fieldName">Field name.</param>
        /// <returns>True if field is null or doesn't have a value, false otherwise.</returns>
        bool IsCustomFieldNullOrEmpty(string fieldName);
    }
}
