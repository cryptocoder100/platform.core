#pragma warning disable CA1308 // Normalize strings to uppercase

namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using Microsoft.AspNetCore.Cryptography.KeyDerivation;

    /// <summary>
    /// Contains functions for managing API key security version 1.
    /// </summary>
    public class ApiKeyVersion1Security : ApiKeySecurity
    {
        /// <summary>
        /// Defines the ITERATIONCOUNT.
        /// </summary>
        private const int ITERATIONCOUNT = 10000;

        /// <summary>
        /// Defines the NUMBYTES.
        /// </summary>
        private const int NUMBYTES = 256 / 8; // 256-bit key

        /// <inheritdoc/>
        public override string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException(nameof(password));
            }

            // Generate some salt
            var salt = new byte[128 / 8]; // 128-bit salt
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return HashPassword(password, salt);
        }

        /// <inheritdoc/>
        public override bool ValidatePassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                throw new ArgumentNullException(nameof(hash));
            }

            // Extract salt
            var parts = hash.Split('.');
            var salt = ByteConverter.HexStringToBytes(parts[1]);

            // Compare
            var result = HashPassword(password, salt);
            return result == hash;
        }

        /// <summary>
        /// Hash password.
        /// </summary>
        /// <param name="password">Password to hash.</param>
        /// <param name="salt">Salt.</param>
        /// <returns>Hashed password.</returns>
        private static string HashPassword(string password, byte[] salt)
        {
            // Hash
            var hash = KeyDerivation.Pbkdf2(
                salt: salt,
                password: password,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: ITERATIONCOUNT,
                numBytesRequested: NUMBYTES);

            // Combine salt and hash into a single result
            // NOTE: To allow us to change our hash algorithm we version our hash <version>.<salt>.<hash>
            var result = new StringBuilder();
            result.Append("1.");
            result.Append(ByteConverter.BytesToHexString(salt).ToLowerInvariant());
            result.Append('.');
            result.Append(ByteConverter.BytesToHexString(hash).ToLowerInvariant());

            return result.ToString();
        }
    }
}
#pragma warning restore CA1308 // Normalize strings to uppercase