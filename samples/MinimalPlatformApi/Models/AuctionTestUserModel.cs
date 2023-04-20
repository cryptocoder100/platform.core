namespace Exos.MinimalPlatformApi.Models
{
    using Exos.Platform.AspNetCore.Models;

    /// <summary>
    /// User data for Auction tests.
    /// </summary>
    public class AuctionTestUserModel
    {
        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the claims.
        /// </summary>
        public UserClaims Claims { get; set; }
    }
}
