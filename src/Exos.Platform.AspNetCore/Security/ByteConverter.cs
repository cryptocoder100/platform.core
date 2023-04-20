namespace Exos.Platform.AspNetCore.Security
{
    using System;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Methods for converting byte arrays to hexadecimal strings and vice versa.
    /// </summary>
    public static class ByteConverter
    {
        /// <summary>
        /// Returns a hexadecimal string representing the byte array input.
        /// </summary>
        /// <param name="bytes">A byte array.</param>
        /// <returns>Hex string.</returns>
        public static string BytesToHexString(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            var hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                hex.AppendFormat(CultureInfo.InvariantCulture, "{0:x2}", b);
            }

            return hex.ToString();
        }

        /// <summary>
        /// Returns a byte array representing the hexadecimal string input.
        /// </summary>
        /// <param name="hex">A hexadecimal string.</param>
        /// <returns>Byte array.</returns>
        public static byte[] HexStringToBytes(string hex)
        {
            if (hex == null)
            {
                throw new ArgumentNullException(nameof(hex));
            }

            var numOfChars = hex.Length;
            var bytes = new byte[numOfChars / 2];
            for (int i = 0; i < numOfChars; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }
    }
}
