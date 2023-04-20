namespace Exos.Platform.AspNetCore.Security
{
    using System.Threading.Tasks;

    /// <summary>
    /// IOnDemandUserContext.
    /// </summary>
    public interface IOnDemandUserContext
    {
        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="userCtxOptions">userCtxOptions.</param>
        /// <returns>Init User Context.</returns>
        Task Init(UserInitOptions userCtxOptions);
    }
}
