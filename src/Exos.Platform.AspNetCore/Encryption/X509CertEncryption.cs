namespace Exos.Platform.AspNetCore.Encryption
{
    using System;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    /// <inheritdoc/>
    public class X509CertEncryption : IEncryption
    {
        private readonly string _certificateName;

        /// <summary>
        /// Initializes a new instance of the <see cref="X509CertEncryption"/> class.
        /// </summary>
        /// <param name="certificateName">Certificate Name.</param>
        public X509CertEncryption(string certificateName)
        {
            if (string.IsNullOrEmpty(certificateName))
            {
                throw new ArgumentNullException(nameof(certificateName), "Certificate name must be provided.");
            }

            _certificateName = certificateName;
        }

        /// <inheritdoc/>
        public string Encrypt(string stringToEncrypt)
        {
            X509Certificate2 cert = GetX509Cert(_certificateName);
            return EncryptX509(cert, stringToEncrypt);
        }

        /// <inheritdoc/>
        public string Decrypt(string encryptedString)
        {
            X509Certificate2 cert = GetX509Cert(_certificateName);
            return DecryptX509(cert, encryptedString);
        }

        /// <summary>
        /// Get an X.509 certificate.
        /// </summary>
        /// <param name="friendlyName">Certificate's friendly name.</param>
        /// <returns>Returns an X.509 certificate.</returns>
        internal static X509Certificate2 GetX509Cert(string friendlyName)
        {
            X509Store store = null;
            X509Certificate2 cert = null;
            try
            {
                store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);

                X509Certificate2Collection allCerts = store.Certificates;

                cert = null;
                if (allCerts != null && allCerts.Count > 0)
                {
                    foreach (X509Certificate2 x509 in allCerts)
                    {
                        if (x509.FriendlyName == friendlyName)
                        {
                            cert = x509;
                            break;
                        }
                    }
                }

                if (cert == null)
                {
                    throw new CryptographicException("The X.509 certificate could not be found.");
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (store != null)
                {
                    store.Close();
                }
            }

            return cert;
        }

        /// <summary>
        /// Encrypt string using X.509 certificate.
        /// </summary>
        /// <param name="cert">X.509 certificate.</param>
        /// <param name="secret">Secret key.</param>
        /// <returns>Encrypted string.</returns>
        private static string EncryptX509(X509Certificate2 cert, string secret)
        {
            // RSACryptoServiceProvider rsa = cert.PublicKey.Key as RSACryptoServiceProvider;
            RSA rsa = cert.GetRSAPublicKey();
            byte[] stringToEncrypt = Encoding.UTF8.GetBytes(secret);
            byte[] encryptedData = rsa.Encrypt(stringToEncrypt, RSAEncryptionPadding.Pkcs1);
            string encryptedString = Convert.ToBase64String(encryptedData);
            rsa.Clear();
            return encryptedString;
        }

        /// <summary>
        /// Encrypt string using X.509 certificate.
        /// </summary>
        /// <param name="cert">X.509 certificate.</param>
        /// <param name="encodedEncryptedString">Encrypted string.</param>
        /// <returns>Decrypted string.</returns>
        private static string DecryptX509(X509Certificate2 cert, string encodedEncryptedString)
        {
            // RSACryptoServiceProvider rsa = cert.PrivateKey as RSACryptoServiceProvider;
            RSA rsa = cert.GetRSAPrivateKey();
            byte[] encryptedString = Convert.FromBase64String(encodedEncryptedString);
            byte[] decodedData = rsa.Decrypt(encryptedString, RSAEncryptionPadding.Pkcs1);
            string decodedStr = Encoding.UTF8.GetString(decodedData);
            rsa.Clear();
            return decodedStr;
        }
    }
}
