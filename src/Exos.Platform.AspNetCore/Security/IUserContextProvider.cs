#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Exos.Platform.AspNetCore.Security
{
    /// <summary>
    /// When provided, will override the user lookup performed in the <see cref="UserContextMiddleware" />.
    /// </summary>
    public interface IUserContextProvider
    {
        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        public ClaimsPrincipal? User { get; set; }
    }
}
