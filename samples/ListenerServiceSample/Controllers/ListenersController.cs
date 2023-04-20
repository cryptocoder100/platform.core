using System;
using System.Collections.Generic;
using Exos.Platform.Messaging.Core.Listener;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ListenerServiceSample.Controllers
{
    /// <summary>
    /// Listeners contoller.
    /// </summary>
    [AllowAnonymous]
    [Produces("application/json")]
    [Route("api/v1/[controller]")]
    [ApiController]
    public class ListenersController : ControllerBase
    {
        private readonly IExosMessageListener _exosMessageListener;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListenersController"/> class.
        /// </summary>
        /// <param name="exosMessageListener">exos message listener.</param>
        public ListenersController(IExosMessageListener exosMessageListener)
        {
            _exosMessageListener = exosMessageListener ?? throw new ArgumentNullException(nameof(exosMessageListener));
        }

        /// <summary>
        /// Gets the active message listeners.
        /// </summary>
        /// <returns>list of active message listeners.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(List<string>), 200)]
        public ActionResult GetActiveEntityListeners()
        {
            var result = _exosMessageListener.GetActiveEntityListeners();
            return Ok(result);
        }
    }
}
