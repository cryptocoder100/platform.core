#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable CA2227 // Collection properties should be read only
#pragma warning disable CA1819 // Properties should not return arrays
namespace Exos.Platform.AspNetCore.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Defines the <see cref="UserClaims" />.
    /// </summary>
    public class UserClaims
    {
        /// <summary>
        /// Gets or sets the UserName.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Gets or sets the Claims.
        /// </summary>
        public List<ClaimModel> Claims { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="ClaimModel" />.
    /// </summary>
    public class ClaimModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClaimModel"/> class.
        /// </summary>
        /// <param name="type">The type of claim<see cref="string"/>.</param>
        /// <param name="value">The value of the claim <see cref="string"/>.</param>
        public ClaimModel(string type, string value)
        {
            Type = type;
            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClaimModel"/> class.
        /// </summary>
        public ClaimModel()
        {
        }

        /// <summary>
        /// Gets or sets the Type of Claim.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the Value of the Claim.
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// Defines the <see cref="CachedClaimModel" />.
    /// </summary>
    public class CachedClaimModel
    {
        /// <summary>
        /// Gets or sets the Claims.
        /// </summary>
        public List<ClaimModel> Claims { get; set; }

        /// <summary>
        /// Gets or sets the Signature.
        /// </summary>
        public byte[] Signature { get; set; }
    }
}
#pragma warning restore SA1402 // File may only contain a single type
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore CA1819 // Properties should not return arrays