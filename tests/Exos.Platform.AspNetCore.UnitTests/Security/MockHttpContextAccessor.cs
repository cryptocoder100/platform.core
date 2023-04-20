using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Exos.Platform.AspNetCore.UnitTests.Security
{
    /// <summary>
    /// The mock http context accessor.
    /// </summary>
    public class MockHttpContextAccessor : IHttpContextAccessor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MockHttpContextAccessor"/> class.
        /// </summary>
        public MockHttpContextAccessor()
        {
            HttpContext = new DefaultHttpContext
            {
                User = ClaimsPrincipalApiUser()
            };

            HttpContext.Request.Headers.Clear();
            HttpContext.Request.Headers.Add("X-Client-Tag", "ui.dev.exostechnology.com");
        }

        /// <summary>
        /// Gets or Sets the http context.
        /// </summary>
        public HttpContext HttpContext { get; set; }

        private static ClaimsPrincipal ClaimsPrincipalApiUser()
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "systemapiuser"),
                new Claim(ClaimTypes.NameIdentifier, "c174068b-fc60-494c-8102-b99cd2266639"),
                new Claim(ClaimTypes.GivenName, "System"),
                new Claim(ClaimTypes.Surname, "User"),
                // Zero or more roles (API resources)
                new Claim(ClaimTypes.Role, "testhost"),
                new Claim(ClaimTypes.Role, "Exos.UserSvc"),
                // Zero or more LOBs
                new Claim("lob", "1"),
                new Claim("lob", "5"),
                new Claim("lob", "7"),
                // Tenant Type and identifier
                new Claim("tenanttype", "servicer"),
                new Claim("tenantidentifier", "1"),
                new Claim("associatedservicertenantidentifier", "1"),
                new Claim("servicergroup:servicergroups", "1"),
                new Claim("servicergroup:servicergroups", "2"),
                // Issuer
                new Claim("iss", "https://www.svclnk.com"),
                new Claim("aud", "https://www.svclnk.com")
            };
            var claimsIdentity = new ClaimsIdentity(claims, "Jwt");
            return new ClaimsPrincipal(claimsIdentity);
        }
    }
}
