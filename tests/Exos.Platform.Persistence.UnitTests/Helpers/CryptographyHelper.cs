namespace Exos.Platform.Persistence.UnitTests.Helpers
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.Cryptography;

    [ExcludeFromCodeCoverage]
    public static class CryptographyHelper
    {
        private static readonly Random _random = new Random();

        public static byte[] GenerateKey()
        {
            using var aes = Aes.Create();
            aes.GenerateKey();

            return aes.Key;
        }

        public static byte[] GenerateIV()
        {
            using var aes = Aes.Create();
            aes.GenerateIV();

            return aes.IV;
        }

        public static byte[] GenerateRandomData(int length)
        {
            var bytes = new byte[length];
            _random.NextBytes(bytes);

            return bytes;
        }
    }
}
