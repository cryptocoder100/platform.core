using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Exos.Platform.AspNetCore.Models;

namespace Exos.Platform.AspNetCore.Security
{
    /// <summary>
    /// A fast, zero-allocation, reader of UTF8 JSON user claims and hash signature from a <see cref="CachedClaimModel" />.
    /// </summary>
    /// <remarks>
    /// Deserializing claims and validating their hash signature is a hot path in our platform.
    /// Using a raw <see cref="Utf8JsonReader" /> and computing the hash as we go, we can deserialize
    /// claims and compute the hash in a single pass with no additional string or buffer allocations.
    /// </remarks>
    internal class UserClaimsReader
    {
        private readonly byte[] _utf8Json;
        private readonly HashAlgorithm _hashAlgorithm;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserClaimsReader" /> class.
        /// </summary>
        /// <param name="utf8Json">The UTF8 stream of JSON to parse.</param>
        /// <param name="claims">A list of claims to populate from the parsed JSON.</param>
        /// <param name="hashAlgorithm">The hash algorithm to use to compute the hash of the claims.</param>
        public UserClaimsReader(byte[] utf8Json, List<Claim> claims, HashAlgorithm hashAlgorithm)
        {
            _utf8Json = utf8Json ?? throw new ArgumentNullException(nameof(utf8Json));
            _hashAlgorithm = hashAlgorithm ?? throw new ArgumentNullException(nameof(hashAlgorithm));

            Claims = claims ?? throw new ArgumentNullException(nameof(claims));
            Hash = null;
            Signature = null;
        }

        /// <summary>
        /// Gets the SHA256 hash computed from the claims.
        /// </summary>
        public byte[] Hash { get; private set; }

        /// <summary>
        /// Gets the signature of the claims.
        /// </summary>
        public byte[] Signature { get; private set; }

        /// <summary>
        /// Gets the string representation of the signature of the claims.
        /// </summary>
        public string SignatureValue { get; private set; }

        /// <summary>
        /// Gets the list of claims parsed from the JSON.
        /// </summary>
        public List<Claim> Claims { get; }

        /// <summary>
        /// Reads the UTF8 JSON and updates the properties with the parsed values.
        /// Exceptions will be thrown on errors.
        /// </summary>
        public void Read()
        {
            var reader = new Utf8JsonReader(_utf8Json);
            string propertyName;

            while (reader.Read())
            {
                // Look for the "claims" and "signature" top-level properties
                switch (reader.TokenType)
                {
                    case JsonTokenType.PropertyName:
                        propertyName = reader.GetString();
                        if ("claims".Equals(propertyName, StringComparison.Ordinal))
                        {
                            reader.Read(); // Advance to array start
                            ConsumeClaimsArray(reader);
                        }
                        else if ("signature".Equals(propertyName, StringComparison.Ordinal))
                        {
                            reader.Read(); // Advance to value
                            ConsumeSignatureString(reader);
                        }

                        break;
                }
            }
        }

        private bool ConsumeClaimsArray(Utf8JsonReader reader)
        {
            int arrayStart = (int)reader.TokenStartIndex;
            int arrayEnd;

            string propertyName = null;
            string claimType = null;
            string claimValue = null;

            // Assumes a flat array of claim models with every model
            // having values for "type" and "value".
            while (true)
            {
                reader.Read();
                switch (reader.TokenType)
                {
                    // Property name
                    case JsonTokenType.PropertyName:
                        propertyName = reader.GetString();
                        break;

                    // Property value
                    case JsonTokenType.Null:
                    case JsonTokenType.String:
                        if ("type".Equals(propertyName, StringComparison.Ordinal))
                        {
                            claimType = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                        }
                        else
                        {
                            claimValue = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                        }

                        break;

                    // End of claim
                    case JsonTokenType.EndObject:
                        Claims.Add(new Claim(claimType, claimValue));
                        break;

                    // End of all claim
                    case JsonTokenType.EndArray:
                        arrayEnd = (int)(reader.TokenStartIndex + reader.ValueSpan.Length);
                        goto DONE;
                }
            }

        DONE:
            // Compute a hash on the entire claims array.
            // We can do this at the end because the entire claims array is already in memory.
            using var sha = SHA256.Create();
            Hash = sha.ComputeHash(_utf8Json, arrayStart, arrayEnd - arrayStart);
            return true;
        }

        private bool ConsumeSignatureString(Utf8JsonReader reader)
        {
            Signature = reader.TokenType == JsonTokenType.String ? reader.GetBytesFromBase64() : null;
            SignatureValue = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

            return true;
        }
    }
}
