namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Class that parses the encryption header and breaks it down into its base properties.
    /// </summary>
    public class EncryptedFieldHeaderParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptedFieldHeaderParser"/> class.
        /// </summary>
        /// <param name="stringToDecrypt">String value from end of line.</param>
        public EncryptedFieldHeaderParser(string stringToDecrypt)
        {
            OriginalInput = stringToDecrypt;

            if (!string.IsNullOrEmpty(stringToDecrypt))
            {
                var splitField = stringToDecrypt.Split(IDatabaseEncryption.EncryptedHeaderDelimiter);

                if (splitField.Length == 3 && ValidSecretName(splitField[0]))
                {
                    EncryptionKey = new EncryptionKey
                    {
                        KeyName = splitField[0],
                        KeyVersion = splitField[1],
                    };
                    Cypher = splitField[2];
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the value is encrypted.
        /// </summary>
        public bool IsEncrypted
        {
            get
            {
                if (!string.IsNullOrEmpty(Cypher) && EncryptionKey.IsHeaderComplete)
                {
                    bool isValid = Guid.TryParse(EncryptionKey.KeyVersion, out _);
                    if (isValid)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the cypher from the encrypted field.
        /// </summary>
        public string Cypher { get; }

        /// <summary>
        /// Gets or sets the ClientEncryptionKey.
        /// </summary>
        public EncryptionKey EncryptionKey { get; set; }

        /// <summary>
        /// Gets the constructor input that created this object.
        /// </summary>
        public string OriginalInput { get; }

        /// <summary>
        /// An object-name is a user provided name for and must be unique within a Key Vault.
        /// The name must be a 1-127 character string, starting with a letter and containing only 0-9, a-z, A-Z, and -.
        /// </summary>
        /// <param name="secretName">name to check.</param>
        /// <returns>if valid or not.</returns>
        private static bool ValidSecretName(string secretName)
        {
            if (secretName.Length < 1 || secretName.Length > 127)
            {
                return false;
            }

            string azureVaultSecretNameAllowedPattern = @"^[0-9a-zA-Z-]+$";

            foreach (Match m in Regex.Matches(secretName, azureVaultSecretNameAllowedPattern))
            {
                return true;
            }

            return false;
        }
    }
}
