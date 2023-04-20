#pragma warning disable SA1401 // FieldsMustBePrivate
#pragma warning disable SA1306 // FieldNamesMustBeginWithLowerCaseLetter
#pragma warning disable CA1051 // Do not declare visible instance fields
namespace Exos.Platform.Messaging.Core.Listener.BaseController
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Listener Base Controller.
    /// </summary>
    public abstract class ListenerBaseController : ControllerBase
    {
        /// <summary>
        /// Expose Message Listener.
        /// </summary>
        protected IExosMessageListener MessageListener;

        /// <summary>
        /// Configured Listeners.
        /// </summary>
        /// <returns>Configured listeners.</returns>
        public abstract Task<IActionResult> Listeners();
    }
}
#pragma warning restore SA1401 // FieldsMustBePrivate
#pragma warning restore SA1306 // FieldNamesMustBeginWithLowerCaseLetter
#pragma warning restore CA1051 // Do not declare visible instance fields
