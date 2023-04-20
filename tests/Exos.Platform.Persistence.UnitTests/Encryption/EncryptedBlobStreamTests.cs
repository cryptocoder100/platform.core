#pragma warning disable SA1305 // Field names should not use Hungarian notation
namespace Exos.Platform.Persistence.UnitTests.Encryption
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Exos.Platform.Persistence.Encryption;
    using Exos.Platform.Persistence.UnitTests.Helpers;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Shouldly;
    using Xunit;

    [ExcludeFromCodeCoverage]
    public class EncryptedBlobStreamTests
    {
        [Fact]
        public void CreateRead_WithNullArguments_ThrowsArgumentNullExceptions()
        {
            // Arrange
            using MemoryStream blobStream = new MemoryStream();
            EncryptionKey key = EncryptionKeyHelper.CreateValidKey();

            // Assert
            Should.Throw<ArgumentNullException>(() => EncryptedBlobStream.CreateRead(null, null));
            Should.Throw<ArgumentNullException>(() => EncryptedBlobStream.CreateRead(null, key));
            Should.Throw<ArgumentNullException>(() => EncryptedBlobStream.CreateRead(blobStream, null));
        }

        [Fact]
        public void CreateRead_WithInvalidEncryptionKey_ThrowsArgumentException()
        {
            // Arrange
            using Stream blobStream = new MemoryStream();
            EncryptionKey key = new EncryptionKey(); // Not valid

            // Assert
            Should.Throw<ArgumentException>(() => EncryptedBlobStream.CreateRead(blobStream, key));
        }

        [Fact]
        public void CreateRead_WithValidArgumments_CannotSeek()
        {
            // Arrange
            using Stream blobStream = new MemoryStream();
            EncryptionKey key = EncryptionKeyHelper.CreateValidKey();
            using var decryptStream = EncryptedBlobStream.CreateRead(blobStream, key);

            // Assert
            Should.Throw<NotSupportedException>(() => decryptStream.Seek(0, SeekOrigin.Begin));
            Should.Throw<NotSupportedException>(() => decryptStream.SetLength(0));
            Should.Throw<NotSupportedException>(() => { _ = decryptStream.Length; });
            Should.Throw<NotSupportedException>(() => { _ = decryptStream.Position; });
            Should.Throw<NotSupportedException>(() => { decryptStream.Position = 0; });
        }

        [Fact]
        public void CreateRead_WithValidArgumments_CanReadButNotWrite()
        {
            // Arrange
            using Stream blobStream = new MemoryStream();
            EncryptionKey key = EncryptionKeyHelper.CreateValidKey();
            using var decryptStream = EncryptedBlobStream.CreateRead(blobStream, key);
            var buffer = new byte[5];

            // Assert
            decryptStream.CanRead.ShouldBe(true);
            decryptStream.CanWrite.ShouldBe(false);
            Should.Throw<NotSupportedException>(() => decryptStream.Write(buffer, 0, buffer.Length));
        }

        [Fact]
        public void Read_WithInvalidArguments_ThrowsArgumentExceptions()
        {
            // Arrange
            using Stream blobStream = new MemoryStream();
            EncryptionKey key = EncryptionKeyHelper.CreateValidKey();
            using var decryptStream = EncryptedBlobStream.CreateRead(blobStream, key);
            var buffer = new byte[5];

            // Assert
            Should.Throw<ArgumentOutOfRangeException>(() => decryptStream.Read(buffer, -1, 0));
            Should.Throw<ArgumentOutOfRangeException>(() => decryptStream.Read(buffer, 0, -1));
            Should.Throw<ArgumentException>(() => decryptStream.Read(buffer, 0, buffer.Length + 1));
        }

        [Fact]
        public void Read_WithIncompleteHeader_ThrowsEndOfStreamException()
        {
            // Arrange
            var key = EncryptionKeyHelper.CreateReadyToUseKey();
            var data = CryptographyHelper.GenerateRandomData(0);
            var logger = new Mock<ILogger<AesCbcBlobEncryption>>();
            var finder = new Mock<IEncryptionKeyFinder>();
            var blobEncryption = new AesCbcBlobEncryption(logger.Object, finder.Object);
            using var streamToEncrypt = new MemoryStream(data);
            using var encryptedStream = new MemoryStream();
            var buffer = new byte[64];

            // Act
            blobEncryption.Encrypt(streamToEncrypt, encryptedStream, out var metadata, encryptionKey: key).ShouldBe(true);
            encryptedStream.Position = 0;
            encryptedStream.SetLength(5); // Less than expected
            using var decryptStream = EncryptedBlobStream.CreateRead(encryptedStream, key);

            // Assert
            Should.Throw<EndOfStreamException>(() => decryptStream.Read(buffer, 0, buffer.Length));
        }

        [Fact]
        public void ReadAll_WithInvalidHmac_ThrowsCryptographicException()
        {
            // Arrange
            var ivLength = CryptographyHelper.GenerateIV().Length;
            var key = EncryptionKeyHelper.CreateReadyToUseKey();
            var data = CryptographyHelper.GenerateRandomData(256);
            var logger = new Mock<ILogger<AesCbcBlobEncryption>>();
            var finder = new Mock<IEncryptionKeyFinder>();
            var blobEncryption = new AesCbcBlobEncryption(logger.Object, finder.Object);
            using var streamToEncrypt = new MemoryStream(data);
            using var encryptedStream = new MemoryStream();

            // Act
            blobEncryption.Encrypt(streamToEncrypt, encryptedStream, out var metadata, encryptionKey: key).ShouldBe(true);
            encryptedStream.Position = 0;
            Buffer.BlockCopy(new[] { 1, 2, 3 }, 0, encryptedStream.GetBuffer(), ivLength, 3); // Corrupt the HMAC
            using var decryptStream = EncryptedBlobStream.CreateRead(encryptedStream, key);
            using var output = new MemoryStream();

            // Assert
            Should.Throw<CryptographicException>(() => decryptStream.CopyTo(output));
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(1024 * 64)]
        [InlineData(1024 * 512)]
        [InlineData(1024 * 1024)] // 1 MB
        [InlineData(1024 * 1024 * 5)] // 5 MB
        [InlineData(1024 * 1024 * 15)] // 15 MB
        public void EncryptedBlobStream_WithValidBlobAndEncryptionKey_DecryptsBlob(int length)
        {
            // Arrange
            var key = EncryptionKeyHelper.CreateReadyToUseKey();
            var data = CryptographyHelper.GenerateRandomData(length);
            var logger = new Mock<ILogger<AesCbcBlobEncryption>>();
            var finder = new Mock<IEncryptionKeyFinder>();
            var blobEncryption = new AesCbcBlobEncryption(logger.Object, finder.Object);
            using var streamToEncrypt = new MemoryStream(data);
            using var encryptedStream = new MemoryStream();

            // Act
            blobEncryption.Encrypt(streamToEncrypt, encryptedStream, out var metadata, encryptionKey: key).ShouldBe(true);
            // HMAC is stored in metadata should be passed in the key for successful decryption.
            metadata.TryGetValue(IBlobEncryption.HMAC, out var hmacBase64);
            key.HmacBase64 = hmacBase64;

            encryptedStream.Position = 0;
            using var decryptStream = EncryptedBlobStream.CreateRead(encryptedStream, key);
            using var output = new MemoryStream();
            decryptStream.CopyTo(output);
            decryptStream.Flush();

            // Assert
            output.ToArray().SequenceEqual(data).ShouldBeTrue("The decrypted content did not match the original.");
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(1024 * 64)]
        [InlineData(1024 * 512)]
        [InlineData(1024 * 1024)] // 1 MB
        [InlineData(1024 * 1024 * 5)] // 5 MB
        [InlineData(1024 * 1024 * 15)] // 15 MB
        public async Task EncryptedBlobStream_WithValidBlobAndEncryptionKey_DecryptsBlobAsync(int length)
        {
            // Arrange
            var key = EncryptionKeyHelper.CreateReadyToUseKey();
            var data = CryptographyHelper.GenerateRandomData(length);
            var logger = new Mock<ILogger<AesCbcBlobEncryption>>();
            var finder = new Mock<IEncryptionKeyFinder>();
            var blobEncryption = new AesCbcBlobEncryption(logger.Object, finder.Object);
            using var streamToEncrypt = new MemoryStream(data);
            using var encryptedStream = new MemoryStream();

            // Act
            blobEncryption.Encrypt(streamToEncrypt, encryptedStream, out var metadata, encryptionKey: key).ShouldBe(true);
            // HMAC is stored in metadata should be passed in the key for successful decryption.
            metadata.TryGetValue(IBlobEncryption.HMAC, out var hmacBase64);
            key.HmacBase64 = hmacBase64;

            encryptedStream.Position = 0;
            await using var decryptStream = EncryptedBlobStream.CreateRead(encryptedStream, key);
            using var output = new MemoryStream();
            await decryptStream.CopyToAsync(output);
            await decryptStream.FlushAsync();

            // Assert
            output.ToArray().SequenceEqual(data).ShouldBeTrue("The decrypted content did not match the original.");
        }

        [Theory]
        [InlineData(1024)]
        [InlineData(1024 * 64)]
        public void EncryptedBlobStream_WithPainfullySmallBuffer_DecryptsBlob(int length)
        {
            // Arrange
            var key = EncryptionKeyHelper.CreateReadyToUseKey();
            var data = CryptographyHelper.GenerateRandomData(length);
            var logger = new Mock<ILogger<AesCbcBlobEncryption>>();
            var finder = new Mock<IEncryptionKeyFinder>();
            var blobEncryption = new AesCbcBlobEncryption(logger.Object, finder.Object);
            using var streamToEncrypt = new MemoryStream(data);
            using var encryptedStream = new MemoryStream();

            // Act
            blobEncryption.Encrypt(streamToEncrypt, encryptedStream, out var metadata, encryptionKey: key).ShouldBe(true);
            // HMAC is stored in metadata should be passed in the key for successful decryption.
            metadata.TryGetValue(IBlobEncryption.HMAC, out var hmacBase64);
            key.HmacBase64 = hmacBase64;

            encryptedStream.Position = 0;
            using var decryptStream = EncryptedBlobStream.CreateRead(encryptedStream, key);
            using var output = new MemoryStream();

            var read = 0;
            var buffer = new byte[1];
            while ((read = decryptStream.Read(buffer, 0, 1)) != 0)
            {
                output.Write(buffer, 0, 1);
            }

            // Assert
            output.ToArray().SequenceEqual(data).ShouldBeTrue("The decrypted content did not match the original.");
        }
    }
}
