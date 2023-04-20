#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
namespace Exos.Platform.AspNetCore.Security.Saml
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Xml;
    using Exos.Platform.AspNetCore.Encryption;
    using Exos.Platform.AspNetCore.Middleware;
    using Exos.Platform.AspNetCore.Models;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.IdentityModel.Tokens.Saml2;

    /// <inheritdoc/>
    public class SamlImpl : ISaml
    {
        private const string USERCONTEXT = "userContext";
        private const string PLATFORMSSO = "BkPlatformSso";
        private const string SAMLASSERTION = "SAMLAssertion";

        /// <inheritdoc/>
        public string CreateSamlAssertion(ISsoData ssoCustomData, string srcCertName, string sdestCertName)
        {
            if (ssoCustomData == null)
            {
                throw new ArgumentNullException(nameof(ssoCustomData), "ssoCustomData is null, Saml Assertion fails");
            }

            if (string.IsNullOrEmpty(sdestCertName))
            {
                throw new ArgumentNullException(nameof(sdestCertName), "Destination certificate name is null, Destination certificate name is required");
            }

            if (string.IsNullOrEmpty(srcCertName))
            {
                throw new ArgumentNullException(nameof(srcCertName), "Source certificate name is null, Source certificate name is required");
            }

            if (ssoCustomData.UserContext != null)
            {
                // Just encrypt the important pieces
                UserInfo userClone = new UserInfo() { UserId = ssoCustomData.UserContext.UserId, UserType = ssoCustomData.UserContext.UserType, UserName = ssoCustomData.UserContext.UserName };
                ssoCustomData.UserContext = userClone;
            }

            var cert = X509CertEncryption.GetX509Cert(srcCertName);

            if (cert == null)
            {
                throw new NotFoundException("srcCertName", "Could not find/locate certificate for Saml Assertion");
            }

            var assertion = new Saml2Assertion(new Saml2NameIdentifier(PLATFORMSSO))
            {
                SigningCredentials = new SigningCredentials(
                    new X509SecurityKey(cert),
                    SecurityAlgorithms.RsaSha256Signature,
                    SecurityAlgorithms.Sha256Digest),
            };

            Saml2Subject samlSubject = new Saml2Subject(new Saml2NameIdentifier(SAMLASSERTION));

            // Create one SAML attribute with few values.
            Saml2Attribute attr = new Saml2Attribute(USERCONTEXT);

            // encrypt the identity
            string identity = new X509CertEncryption(sdestCertName).Encrypt(JsonSerializer.Serialize(ssoCustomData.UserContext, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault }));
            attr.Values.Add(identity);

            attr.Name = USERCONTEXT;

            Saml2Attribute attrOther = new Saml2Attribute("CustomData");

            // Base64 the other custom data
            string customData = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ssoCustomData.CustomData, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull })));

            attrOther.Values.Add(customData);

            // Now create the SAML statement containing one attribute and one subject.
            Saml2AttributeStatement samlAttributeStatement = new Saml2AttributeStatement();
            samlAttributeStatement.Attributes.Add(attr);

            samlAttributeStatement.Attributes.Add(attrOther);

            assertion.Subject = samlSubject;

            // Append the statement to the SAML assertion.
            assertion.Statements.Add(samlAttributeStatement);

            StringBuilder sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.OmitXmlDeclaration = true;
            settings.Indent = false;
            settings.Encoding = Encoding.UTF8;

            using (StringWriter stringWriter = new StringWriter(sb))
            {
                using (XmlWriter responseWriter = XmlWriter.Create(stringWriter, settings))
                {
                    new Saml2SecurityTokenHandler().Serializer.WriteAssertion(responseWriter, assertion);
                    return stringWriter.ToString();
                }
            }
        }

        /// <inheritdoc/>
        public T ReadSamlAssertion<T>(string samlAssertion, string srcCertName, string sdestCertName) where T : ISsoData, new()
        {
            if (string.IsNullOrEmpty(samlAssertion))
            {
                return default(T);
            }

            if (string.IsNullOrEmpty(sdestCertName))
            {
                throw new ArgumentNullException(nameof(sdestCertName), "Destination certificate name is null, Destination certificate name is required");
            }

            if (string.IsNullOrEmpty(srcCertName))
            {
                throw new ArgumentNullException(nameof(srcCertName), "Source certificate name is null, Source certificate name is required");
            }

            T retVal = new T();

            using (var stringReader = new StringReader(samlAssertion))
            {
                using (XmlReader reader = XmlReader.Create(stringReader))
                {
                    var samlAsser = new Saml2SecurityTokenHandler().Serializer.ReadAssertion(reader);

                    var staementUserCtx = (Saml2AttributeStatement)samlAsser.Statements.Where(st => ((Saml2AttributeStatement)st).Attributes.Select(att => att).Where(at => at.Name == USERCONTEXT).Any()).FirstOrDefault();

                    Saml2Attribute attribute = staementUserCtx.Attributes.Where(att => att.Name == USERCONTEXT).FirstOrDefault();

                    if (attribute == null)
                    {
                        return default(T);
                    }

                    var value = attribute.Values.FirstOrDefault();

                    if (value == null)
                    {
                        return default(T);
                    }

                    string identity = new X509CertEncryption(sdestCertName).Decrypt(value);

                    UserInfo ctx = JsonSerializer.Deserialize<UserInfo>(identity);
                    retVal.UserContext = ctx;

                    var statementUserCustomData = (Saml2AttributeStatement)samlAsser.Statements.Where(st => ((Saml2AttributeStatement)st).Attributes.Select(att => att).Where(at => at.Name == "CustomData").Any()).FirstOrDefault();
                    if (statementUserCustomData != null)
                    {
                        Saml2Attribute attributeCustomData = statementUserCustomData.Attributes.Where(att => att.Name == "CustomData").FirstOrDefault();

                        if (attributeCustomData != null)
                        {
                            var valueCustomData = attributeCustomData.Values.FirstOrDefault();

                            retVal.CustomData = JsonSerializer.Deserialize<List<NameValue>>(Encoding.UTF8.GetString(Convert.FromBase64String(valueCustomData)));
                        }
                    }

                    return retVal;
                }
            }
        }
    }
}
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix