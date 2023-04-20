using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Exos.Platform.AspNetCore.Security;

namespace Exos.Platform.IntegrationTesting
{
    /// <summary>
    /// Default implementation of the <see cref="IUserContextProvider" /> interface.
    /// </summary>
    public class UserContextProvider : IUserContextProvider
    {
        /// <inheritdoc />
        public ClaimsPrincipal? User { get; set; }
    }
}
