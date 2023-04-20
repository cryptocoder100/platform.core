namespace Exos.Platform.Persistence.UnitTests.Helpers
{
    using System.Diagnostics.CodeAnalysis;
    using Exos.Platform.Persistence.Encryption;

    [ExcludeFromCodeCoverage]
    public static class EncryptionKeyHelper
    {
        public static EncryptionKey CreateValidKey()
        {
            var key = new EncryptionKey
            {
                KeyValueBytes = CryptographyHelper.GenerateKey()
            };

            return key;
        }

        public static EncryptionKey CreateReadyToUseKey()
        {
            var key = new EncryptionKey
            {
                KeyValueBytes = CryptographyHelper.GenerateKey(),
                KeyName = "abc",
                KeyVersion = "123"
            };

            return key;
        }
    }
}
