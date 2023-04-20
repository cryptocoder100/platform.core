#pragma warning disable CA2201 // Do not raise reserved exception types

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Exos.Platform.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Exos.Platform.AspNetCore.UnitTests.Helpers
{
    [TestClass]
    public class Base64WriterTests
    {
        private const int _kb1 = 1024;
        private const int _kb5 = 1024 * 5;
        private const int _mb1 = 1024 * 1024;
        private const int _mb5 = 1024 * 1024 * 5;

        [TestMethod]
        public async Task WriteAsBase64Async_WithNullArgument_ThrowArgumentNullException()
        {
            await Should.ThrowAsync<ArgumentNullException>(async () => await Base64Writer.WriteAsBase64Async(new MemoryStream(), null));
            await Should.ThrowAsync<ArgumentNullException>(async () => await Base64Writer.WriteAsBase64Async(null, new MockPipeWriter()));
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(_kb1)]
        [DataRow(_kb5)]
        [DataRow(_mb1)]
        [DataRow(_mb5)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(_kb1)]
        [DataRow(_kb5)]
        [DataRow(_mb1)]
        [DataRow(_mb5)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(_kb1)]
        [DataRow(_kb5)]
        [DataRow(_mb1)]
        [DataRow(_mb5)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(_kb1)]
        [DataRow(_kb5)]
        [DataRow(_mb1)]
        [DataRow(_mb5)]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(_kb1)]
        [DataRow(_kb5)]
        [DataRow(_mb1)]
        [DataRow(_mb5)]
        public async Task WriteAsBase64Async_WithN_MatchesConvertedBase64(int inputLength)
        {
            // Ensure we're not always on 32-bit boundary
            for (int j = 0; j < 4; j++)
            {
                inputLength += j;

                // Arrange
                var mockWriter = new MockPipeWriter();
                using var mockStream = new MemoryStream(inputLength);
                FillStream(mockStream, inputLength);

                // Act
                var str = Convert.ToBase64String(mockStream.GetBuffer().AsSpan()[..inputLength]);
                await Base64Writer.WriteAsBase64Async(mockStream, mockWriter);

                // Assert
                str.Length.ShouldBeEquivalentTo((int)mockWriter.MemoryStream.Length);
                str.ShouldBeEquivalentTo(Encoding.UTF8.GetString(mockWriter.MemoryStream.ToArray()));
            }
        }

        private static void FillStream(MemoryStream stream, int count)
        {
            for (int i = 0; i < count; i++)
            {
                stream.WriteByte((byte)'A');
            }

            stream.Position = 0;
        }

        private class MockPipeWriter : PipeWriter
        {
            private byte[] _buffer;

            public MockPipeWriter()
            {
                MemoryStream = new MemoryStream();
            }

            public MemoryStream MemoryStream { get; }

            public override void Advance(int bytes)
            {
                MemoryStream.Write(_buffer, 0, bytes);
            }

            public override void CancelPendingFlush()
            {
                throw new NotImplementedException();
            }

            public override void Complete(Exception exception = null)
            {
                throw new NotImplementedException();
            }

            public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
            {
                return ValueTask.FromResult(new FlushResult(false, false));
            }

            public override Memory<byte> GetMemory(int sizeHint = 0)
            {
                _buffer = new byte[Math.Max(sizeHint, 4096)];
                return _buffer;
            }

            public override Span<byte> GetSpan(int sizeHint = 0)
            {
                throw new NotImplementedException();
            }
        }
    }
}
