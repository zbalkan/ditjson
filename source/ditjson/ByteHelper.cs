using System;
using System.Text;

namespace ditjson
{
    internal static class ByteHelper
    {

        /// <summary>
        ///     Return the string format of a byte array.
        /// </summary>
        /// <param name="data">
        ///     The data to format.
        /// </param>
        /// <returns>
        ///     A string representation of the data.
        /// </returns>
        /// <exception cref="FormatException">
        /// </exception>
        /// <exception cref="OverflowException">
        /// </exception>
        internal static string FormatBytes(byte[] data)
        {
            if (data == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(data.Length * 2);
            foreach (var b in data)
            {
                sb.AppendFormat("{0:x2}", b);
            }

            return sb.ToString();
        }

        internal static byte[] ConvertHexStringToBytes(string hexString)
        {
            if (hexString.Length % 2 != 0)
                throw new ArgumentException("Invalid hex string length");

            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                var byteValue = hexString.Substring(i * 2, 2);
                bytes[i] = Convert.ToByte(byteValue, 16);
            }
            return bytes;
        }
    }
}