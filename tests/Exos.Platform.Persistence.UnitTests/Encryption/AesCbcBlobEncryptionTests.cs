namespace Exos.Platform.Persistence.UnitTests.Encryption
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using Exos.Platform.Persistence.Encryption;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Shouldly;
    using Xunit;

    [ExcludeFromCodeCoverage]
    public class AesCbcBlobEncryptionTests
    {
        [Fact]
        public void IsEncrypted_WithNullArgument_ThrowsArgumentNullExceptions()
        {
            // Arrange
            var logger = new Mock<ILogger<AesCbcBlobEncryption>>();
            var finder = new Mock<IEncryptionKeyFinder>();
            var blobEncryption = new AesCbcBlobEncryption(logger.Object, finder.Object);

            // Assert
            Should.Throw<ArgumentNullException>(() => blobEncryption.IsEncrypted(null));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("abc", null)]
        [InlineData(null, "123")]
        public void IsEncrypted_WithoutKeys_ReturnsFalse(string keyName, string keyVersion)
        {
            // Arrange
            var logger = new Mock<ILogger<AesCbcBlobEncryption>>();
            var finder = new Mock<IEncryptionKeyFinder>();
            var blobEncryption = new AesCbcBlobEncryption(logger.Object, finder.Object);
            var fileMetadata = new Dictionary<string, string>
            {
                [IBlobEncryption.BlobEncryptionKeyName] = keyName,
                [IBlobEncryption.BlobEncryptionKeyVersion] = keyVersion
            };

            // Assert
            blobEncryption.IsEncrypted(fileMetadata).ShouldBe(false);
        }

        [Fact]

        public void IsEncrypted_WithValidKeys_ReturnsTrue()
        {
            // Arrange
            var logger = new Mock<ILogger<AesCbcBlobEncryption>>();
            var finder = new Mock<IEncryptionKeyFinder>();
            var blobEncryption = new AesCbcBlobEncryption(logger.Object, finder.Object);
            var fileMetadata = new Dictionary<string, string>
            {
                [IBlobEncryption.BlobEncryptionKeyName] = "key--exos-svclnk",
                [IBlobEncryption.BlobEncryptionKeyVersion] = "4d3b7dfc55dd4671bbe9d93d1460090e"
            };

            // Assert
            blobEncryption.IsEncrypted(fileMetadata).ShouldBe(true);
        }

        [Fact]

        public void GetDecryptStream_WithNullArguments_ThrowsArgumentNullExceptions()
        {
            // Arrange
            var logger = new Mock<ILogger<AesCbcBlobEncryption>>();
            var finder = new Mock<IEncryptionKeyFinder>();
            var blobEncryption = new AesCbcBlobEncryption(logger.Object, finder.Object);
            using var encryptedStream = new MemoryStream();
            var fileMetadata = new Dictionary<string, string>
            {
                [IBlobEncryption.BlobEncryptionKeyName] = "key--exos-svclnk",
                [IBlobEncryption.BlobEncryptionKeyVersion] = "4d3b7dfc55dd4671bbe9d93d1460090e"
            };

            // Assert
            Should.Throw<ArgumentNullException>(() => blobEncryption.GetDecryptStream(null, fileMetadata));
            Should.Throw<ArgumentNullException>(() => blobEncryption.GetDecryptStream(encryptedStream, null));
        }

        [Fact]

        public void GetDecryptStream_WithValidArguments_ReturnsDecryptStream()
        {
            // Arrange
            using var encryptedStream = new MemoryStream();
            var fileMetadata = new Dictionary<string, string>
            {
                [IBlobEncryption.BlobEncryptionKeyName] = "key--exos-svclnk",
                [IBlobEncryption.BlobEncryptionKeyVersion] = "4d3b7dfc55dd4671bbe9d93d1460090e"
            };

            var logger = new Mock<ILogger<AesCbcBlobEncryption>>();
            var finder = new Mock<IEncryptionKeyFinder>();
            var blobEncryption = new AesCbcBlobEncryption(logger.Object, finder.Object)
            {
                ValidateKeyForDecryption = false,
                CurrentEncryptionKey = new EncryptionKey
                {
                    KeyName = fileMetadata[IBlobEncryption.BlobEncryptionKeyName],
                    KeyVersion = fileMetadata[IBlobEncryption.BlobEncryptionKeyVersion],
                    KeyValueBytes = EncryptionKey.GetKeyBytes("6GsBjyAwiskSp+R6X1uAP5mtaM+X4ykszMyhwn5PbO0=")
                }
            };

            // Act
            using var decryptStream = blobEncryption.GetDecryptStream(encryptedStream, fileMetadata);
        }
    }
}
