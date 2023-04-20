#pragma warning disable CA1305 // Specify IFormatProvider
#pragma warning disable CA1308 // Normalize strings to uppercase
namespace Exos.Platform.AspNetCore.IntegrationTests.Mock
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Security.Claims;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Microsoft.AspNetCore.Http;

    public class MockHttpContextAccessor : IHttpContextAccessor
    {
        private static JsonSerializerOptions _serializerOptions = CreateSerializerOptions();

        /// <summary>
        /// Initializes a new instance of the <see cref="MockHttpContextAccessor"/> class.
        /// </summary>
        public MockHttpContextAccessor()
        {
            HttpContext = new DefaultHttpContext
            {
                User = GenerateUserContext()
            };
        }

        /// <summary>
        /// Gets or sets httpContext.
        /// </summary>
        public HttpContext HttpContext { get; set; }

        public static ClaimsPrincipal GenerateUserContext(string userModelPath = null)
        {
            List<Claim> claims = MockHttpContextAccessor.ReadClaims(userModelPath);
            var claimsIdentity = new ClaimsIdentity(claims, "Jwt");
            return new ClaimsPrincipal(claimsIdentity);
        }

        public static List<Claim> ReadClaims(string userModelPath = null)
        {
            if (string.IsNullOrEmpty(userModelPath) || !File.Exists(userModelPath))
            {
                userModelPath = "./TestData/systemapiuser_context.json";
            }

            MockUserModel userModel = JsonSerializer.Deserialize<MockUserModel>(File.ReadAllText(userModelPath), _serializerOptions);
            List<Claim> claims = new List<Claim>
            {
                // Issuer
                new Claim("iss", "https://www.svclnk.com"),
                new Claim("aud", "https://www.svclnk.com")
            };
            claims.Add(new Claim(ClaimTypes.Name, userModel.Username));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userModel.Id));
            claims.Add(new Claim(ClaimTypes.GivenName, userModel.FirstName));
            claims.Add(new Claim(ClaimTypes.Surname, userModel.LastName));
            claims.Add(new Claim("tenanttype", userModel.TenantType));

            claims.Add(new Claim("tenantidentifier", userModel.TenantId.ToString()));
            if (userModel.ApiResources != null)
            {
                foreach (var r in userModel.ApiResources)
                {
                    claims.Add(new Claim(ClaimTypes.Role, r));
                }
            }

            if (userModel.LinesOfBusiness != null)
            {
                foreach (var l in userModel.LinesOfBusiness)
                {
                    claims.Add(new Claim("lob", l.ToString()));
                }
            }

            if (userModel.ServicerAssociations != null)
            {
                foreach (var sa in userModel.ServicerAssociations)
                {
                    claims.Add(new Claim("associatedservicertenantidentifier", sa.ToString(CultureInfo.InvariantCulture)));
                }
            }

            if (userModel.ServicerGroups != null)
            {
                foreach (var sg in userModel.ServicerGroups)
                {
                    foreach (var g in sg.Value)
                    {
                        claims.Add(new Claim($"servicergroup:{sg.Key.ToLowerInvariant()}", g.ToString(CultureInfo.InvariantCulture)));
                    }
                }
            }

            return claims;
        }

        private static JsonSerializerOptions CreateSerializerOptions()
        {
            // Maximize compatibility with existing behavior
            var serializerOptions = new JsonSerializerOptions();
            serializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            // serializerOptions.IgnoreNullValues = true;
            serializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            serializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            serializerOptions.PropertyNameCaseInsensitive = true;

            return serializerOptions;
        }
    }
}
#pragma warning restore CA1305 // Specify IFormatProvider
#pragma warning restore CA1308 // Normalize strings to uppercase