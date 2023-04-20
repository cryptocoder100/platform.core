namespace Exos.Platform.AspNetCore.Security
{
    using System.Collections.Generic;

    /// <summary>
    /// UserInitOptions.
    /// </summary>
    public class UserInitOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserInitOptions"/> class.
        /// UserInitOptions.
        /// </summary>
        /// <param name="keyValuePairs">keyValuePairs.</param>
        public UserInitOptions(Dictionary<string, string> keyValuePairs)
        {
            KeyValuePairs = keyValuePairs;
        }

        /// <summary>
        /// Gets or sets the UserName.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the user's password.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets the AuthSchemeName.
        /// </summary>
        public string AuthSchemeName { get; set; }

        /// <summary>
        /// Gets the Headers.
        /// </summary>
        public Dictionary<string, string> KeyValuePairs { get; }
    }
}
