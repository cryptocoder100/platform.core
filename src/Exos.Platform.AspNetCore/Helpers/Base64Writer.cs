#pragma warning disable SA1008 // Opening parenthesis should be spaced correctly

using System;
using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Text;

namespace Exos.Platform.Helpers;

/// <summary>
/// Helper methods for writing Base64 data.
/// </summary>
public static class Base64Writer
{
    /// <summary>
    /// Writes a stream of bytes as Base64 data using minimal allocations.
    /// </summary>
    /// <param name="src">The stream of bytes to convert.</param>
    /// <param name="dst">The writer to receive Base64 data.</param>
    /// <param name="cancellationToken">An optional token to monitor for cancellation requests.</param>
    public static async ValueTask WriteAsBase64Async(Stream src, PipeWriter dst, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(src);
        ArgumentNullException.ThrowIfNull(dst);

        var srcMemory = new Memory<byte>(new byte[1024 * 32]);
        var leftover = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var dstMemory = dst.GetMemory(Base64.GetMaxEncodedToUtf8Length(srcMemory.Length));

            // Read from the src stream into a buffer (preserving any leftover we had from a previous loop)
            var bytesRead = await src.ReadAsync(srcMemory[leftover..], cancellationToken);
            var workingMemory = srcMemory[..(bytesRead + leftover)];

            if (bytesRead == 0)
            {
                Base64.EncodeToUtf8(workingMemory.Span, dstMemory.Span, out _, out var finalBytesWritten);

                dst.Advance(finalBytesWritten);
                await dst.FlushAsync(cancellationToken);

                break;
            }

            // The encoder expects multiples of 3 bytes (24 bits) unless we're on the final block (above)
            leftover = workingMemory.Length % 3;
            Base64.EncodeToUtf8(workingMemory[..^leftover].Span, dstMemory.Span, out _, out var bytesWritten, isFinalBlock: false);

            dst.Advance(bytesWritten);
            await dst.FlushAsync(cancellationToken);

            // Copy the leftover to the buffer start
            workingMemory[^leftover..].CopyTo(srcMemory);
        }
    }
}
