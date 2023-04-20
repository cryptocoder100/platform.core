namespace Exos.Platform.AspNetCore.Security
{
    using System;

    /// <summary>
    /// Provides base functionality for API key security.
    /// </summary>
    public abstract class ApiKeySecurity
    {
        /// <summary>
        /// Defines the V1.
        /// </summary>
        private static readonly Lazy<ApiKeyVersion1Security> V1 = new Lazy<ApiKeyVersion1Security>();

        /// <summary>
        /// Gets the Version1
        /// Gets ApiKeySecurity Version1..
        /// </summary>
        public static ApiKeySecurity Version1
        {
            get
            {
                return V1.Value;
            }
        }

        /// <summary>
        /// Get ApiKeySecurity from version.
        /// </summary>
        /// <param name="version">Version Number.</param>
        /// <returns>ApiKeySecurity.</returns>
        public static ApiKeySecurity FromVersion(int version)
        {
            switch (version)
            {
                case 1:
                    return Version1;
            }

            throw new ArgumentOutOfRangeException(nameof(version));
        }

        /// <summary>
        /// Find API key Version.
        /// </summary>
        /// <param name="hash">Hash key.</param>
        /// <returns>Return Version.</returns>
        public static int DetectVersion(string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                throw new ArgumentNullException(nameof(hash));
            }

            var parts = hash.Split('.');
            if (parts.Length > 0 && int.TryParse(parts[0], out int version))
            {
                return version;
            }

            return 0;
        }

        /// <summary>
        /// Hash a password.
        /// </summary>
        /// <param name="password">Password to hash.</param>
        /// <returns>Hashed password.</returns>
        public abstract string HashPassword(string password);

        /// <summary>
        /// Validate a hashed password.
        /// </summary>
        /// <param name="password">Password.</param>
        /// <param name="hash">Hash String.</param>
        /// <returns>True if password is valid.</returns>
        public abstract bool ValidatePassword(string password, string hash);
    }
}