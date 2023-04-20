namespace Exos.Platform.Persistence.Tests
{
    using System.Collections.Generic;
    using System.Security.Claims;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// MockHttpContextAccessor.
    /// </summary>
    public class MockHttpContextAccessor : IHttpContextAccessor
    {
        public MockHttpContextAccessor()
        {
            HttpContext = new DefaultHttpContext
            {
                User = ClaimsPrincipalTitleApiUser()
            };

            HttpContext.Request.Headers.Clear();
            HttpContext.Request.Headers.Add("X-Forwarded-Host", "boadev-s3.exostechnology.com, gatewaysvc-s3-v1.dev.exostechnology.local");
            HttpContext.Request.Headers.Add("X-Client-Tag", "boa.dev.exostechnology.com");
            HttpContext.Request.Host = new HostString("boa.dev.exostechnology.com");
        }

        /// <summary>
        /// Gets or sets HttpContext.
        /// </summary>
        public HttpContext HttpContext { get; set; }

        private static ClaimsPrincipal ClaimsPrincipalTitleApiUser()
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "titleapiuser"),
                new Claim(ClaimTypes.NameIdentifier, "58a257b4-3ed4-45da-a9fb-2c5dbc173160"),
                new Claim(ClaimTypes.GivenName, "titleapi"),
                new Claim(ClaimTypes.Surname, "User"),
                // Zero or more roles (API resources)
                new Claim(ClaimTypes.Role, "testhost"),
                new Claim(ClaimTypes.Role, "Exos.GatewaySvc"),
                new Claim(ClaimTypes.Role, "Exos.UserSvc"),
                new Claim(ClaimTypes.Role, "Exos.TitleDataCollectionSvc"),
                new Claim(ClaimTypes.Role, "Exos.ClientManagementApi"),
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
                new Claim("servicergroup:servicergroups", "11"),
                new Claim("servicergroup:servicergroups", "12"),
                new Claim("servicergroup:servicergroups", "19"),
                new Claim("servicergroup:servicergroups", "18"),
                // Issuer
                new Claim("iss", "https://www.svclnk.com"),
                new Claim("aud", "https://www.svclnk.com")
            };
            var claimsIdentity = new ClaimsIdentity(claims, "Jwt");
            return new ClaimsPrincipal(claimsIdentity);
        }
    }
}
