namespace Exos.Platform.AspNetCore.Security.Saml
{
    using Exos.Platform.AspNetCore.Models;

    /// <summary>
    /// SAML implementation.
    /// </summary>
    public interface ISaml
    {
        /// <summary>
        /// Create SAML assertion.
        /// </summary>
        /// <param name="ssoCustomData">ISsoData.</param>
        /// <param name="srcCertName">Source certificate Name.</param>
        /// <param name="sdestCertName">Destination Certificate Name.</param>
        /// <returns>SAML assertion.</returns>
        string CreateSamlAssertion(ISsoData ssoCustomData, string srcCertName, string sdestCertName);

        /// <summary>
        /// Read SAML assertion.
        /// </summary>
        /// <typeparam name="T">Implementation of <see cref="ISsoData" />.</typeparam>
        /// <param name="samlAssertion">SAML assertion.</param>
        /// <param name="srcCertName">Source certificate Name.</param>
        /// <param name="sdestCertName">Destination Certificate Name.</param>
        /// <returns>Object implementing <see cref="ISsoData" />.</returns>
        T ReadSamlAssertion<T>(string samlAssertion, string srcCertName, string sdestCertName) where T : ISsoData, new();
    }
}
