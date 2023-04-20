#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable CA1835 // Prefer the 'Memory'-based overloads for 'ReadAsync' and 'WriteAsync'
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

// Encrypt blobs with individual keys they said... it will be fun, they said....
namespace Exos.Platform.Persistence.Encryption
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A stream that handles encryption and decryption of EXOS blobs.
    /// </summary>
    public class EncryptedBlobStream : Stream
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly EncryptionKey _key;
        private readonly CryptoStreamMode _mode;
        private readonly byte[] _iv;
        private readonly byte[] _hmac;

        private bool _initialized;

        private Stream _blobStream;
        private Aes _aes;
        private HMACSHA256 _hash;

        private ICryptoTransform _transform;
        private CryptoStream _cryptoStream;

        private EncryptedBlobStream(Stream blobStream, EncryptionKey key, CryptoStreamMode mode)
        {
            _blobStream = blobStream ?? throw new ArgumentNullException(nameof(blobStream));
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _mode = mode;

            if (!_key.IsKeyValueValid)
            {
                throw new ArgumentException("Encryption key is invalid.", nameof(key));
            }

            _aes = Aes.Create();
            _aes.Mode = CipherMode.CBC;
            _aes.Key = _key.KeyValueBytes;
            _aes.Padding = PaddingMode.PKCS7;

            _hash = new HMACSHA256(_key.KeyValueBytes);
            _iv = new byte[_aes.IV.Length];
            _hmac = new byte[_hash.HashSize / 8];

            switch (mode)
            {
                case CryptoStreamMode.Read:
                    if (!_blobStream.CanRead)
                    {
                        throw new ArgumentException("Stream was not readable.", nameof(blobStream));
                    }

                    CanRead = true;
                    break;
                case CryptoStreamMode.Write:
                    if (!_blobStream.CanRead)
                    {
                        throw new ArgumentException("Stream was not writable.", nameof(blobStream));
                    }

                    CanWrite = true;
                    break;
                default:
                    throw new ArgumentException("Value was invalid", nameof(mode));
            }
        }

        /// <inheritdoc />
        public override bool CanRead { get; }

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite { get; }

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException("Stream does not support seeking.");

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException("Stream does not support seeking.");
            set => throw new NotSupportedException("Stream does not support seeking.");
        }

        /// <summary>
        /// Creates a new instance of the <see cref="EncryptedBlobStream" /> class for decrypting EXOS blob streams
        /// containing an IV and HMAC header.
        /// Set the property HmacBase64 in the encryption key with the HMAC returned in the encryption operation (in the metadata).
        /// if the property is not set the assumption is that the HMAC is included in the content of the stream.
        /// </summary>
        /// <param name="blobStream">The blob stream.</param>
        /// <param name="key">The encryption key that was used to encrypt the blob.</param>
        /// <returns>An <see cref="EncryptedBlobStream" /> for reading the decrypted stream.</returns>
        public static EncryptedBlobStream CreateRead(Stream blobStream, EncryptionKey key)
        {
            return new EncryptedBlobStream(blobStream, key, CryptoStreamMode.Read);
        }

        /// <inheritdoc />
        public override void Flush()
        {
            _cryptoStream?.Flush();
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckReadArguments(buffer, offset, count);
            return ReadImplAsync(buffer, offset, count, useAsync: false, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckReadArguments(buffer, offset, count);

            // Guard against multiple calls to our initialized read
            await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await ReadImplAsync(buffer, offset, count, useAsync: true, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException("Stream does not support seeking.");
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException("Stream does not support seeking.");
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Writing is not currently supported.");
        }

        /// <inheritdoc />
        public override async ValueTask DisposeAsync()
        {
            // Same logic as regular Dispose
            try
            {
                if (_cryptoStream != null)
                {
                    await _cryptoStream.DisposeAsync().ConfigureAwait(false);
                }

                if (_blobStream != null)
                {
                    await _blobStream.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                try
                {
                    _semaphore?.Dispose();
                    _transform?.Dispose();
                    _hash?.Dispose();
                    _aes?.Dispose();
                }
                finally
                {
                    await base.DisposeAsync().ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            try
            {
                _cryptoStream?.Dispose();
                _blobStream?.Dispose();
            }
            finally
            {
                try
                {
                    _semaphore?.Dispose();
                    _transform?.Dispose();
                    _hash?.Dispose();
                    _aes?.Dispose();
                }
                finally
                {
                    base.Dispose(disposing);
                }
            }
        }

        private async Task<int> ReadImplAsync(byte[] buffer, int offset, int count, bool useAsync, CancellationToken cancellationToken)
        {
            if (!_initialized)
            {
                // Read the IV and HMAC from the start of the stream, if key has HMAC only ready IV.
                var headerLength = string.IsNullOrEmpty(_key.HmacBase64) ? _iv.Length + _hmac.Length : _iv.Length;
                var header = ArrayPool<byte>.Shared.Rent(headerLength);

                try
                {
                    var headerPos = 0;
                    while (headerPos < headerLength)
                    {
                        var headerRead = useAsync
                            ? await _blobStream.ReadAsync(header, headerPos, headerLength - headerPos, cancellationToken).ConfigureAwait(false)
                            : _blobStream.Read(header, headerPos, headerLength - headerPos);

                        if (headerRead == 0)
                        {
                            throw new EndOfStreamException();
                        }

                        headerPos += headerRead;
                    }

                    Buffer.BlockCopy(header, 0, _iv, 0, _iv.Length);
                    if (string.IsNullOrEmpty(_key.HmacBase64))
                    {
                        Buffer.BlockCopy(header, _iv.Length, _hmac, 0, _hmac.Length);
                    }
                    else
                    {
                        Buffer.BlockCopy(Convert.FromBase64String(_key.HmacBase64), 0, _hmac, 0, _hmac.Length);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(header, clearArray: true);
                }

                // Init the crypto stream
                _transform = _aes.CreateDecryptor(_key.KeyValueBytes, _iv);
                _cryptoStream = new CryptoStream(_blobStream, _transform, _mode, leaveOpen: false);

                // Init the HMAC
                _hash.Initialize();

                _initialized = true;
            }

            // Read from the stream
            var read = useAsync
                ? await _cryptoStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false)
                : _cryptoStream.Read(buffer, offset, count);

            // Append to our hash
            _hash.TransformBlock(buffer, offset, read, null, 0);

            // Finalize?
            if (read == 0)
            {
                _hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                if (!_hash.Hash.SequenceEqual(_hmac))
                {
                    throw new CryptographicException("Stream hash does not match HMAC.");
                }
            }

            return read;
        }

        private void CheckReadArguments(byte[] buffer, int offset, int count)
        {
            if (!CanRead)
            {
                throw new NotSupportedException("Stream does not support reading.");
            }
            else if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "Non-negative number required.");
            }
            else if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
            }
            else if (buffer.Length - offset < count)
            {
                throw new ArgumentException($"The {nameof(offset)} or {nameof(count)} were out of bounds for the array or {nameof(count)} is greater than the number of elements from {nameof(offset)} to the end of the source collection.");
            }
        }
    }
}

// References:
// https://github.com/dotnet/runtime/blob/cb5f173b9696d9d00a544b953d95190ab3b56df2/src/libraries/System.Security.Cryptography.Primitives/src/System/Security/Cryptography/CryptoStream.cs
