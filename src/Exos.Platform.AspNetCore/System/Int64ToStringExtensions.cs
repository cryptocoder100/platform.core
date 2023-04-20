using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// Extension methods for working with 64-bit integers.
/// </summary>
public static class Int64ToStringExtensions
{
    /// <summary>
    /// Converts the value of this instance to its equivalent string representation using <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    /// <param name="value">The integer value to convert.</param>
    /// <returns>The string representation of the value of this instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToInvariantString(this long value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
