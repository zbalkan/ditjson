using System;
using System.Security.Principal;

namespace ditjson.Decoders
{
    internal static class SidDecoder
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        internal static string? Decode(byte[]? sidBytes)
        {
            if (sidBytes == null || sidBytes.Length < 8)
                return null;

            try
            {
                var sid = new SecurityIdentifier(sidBytes, 0);
                return sid.Value;
            }
            catch
            {
                return null;
            }
        }

        internal static string? Decode(string hexValue)
        {
            if (string.IsNullOrEmpty(hexValue))
                return null;

            try
            {
                var bytes = HexToBytes(hexValue);
                if (bytes != null && bytes.Length >= 12 && bytes[1] > 0 &&
                    bytes[2] == 0 && bytes[3] == 0 && bytes[4] == 0 &&
                    bytes[5] == 0 && bytes[6] == 0 && bytes[7] == 0)
                {
                    // Accept legacy hex values that stored the identifier
                    // authority as a little-endian 32-bit value.
                    var authority = BitConverter.ToUInt32(bytes, 8);
                    if (authority > 0 && authority <= byte.MaxValue)
                    {
                        var normalized = new byte[bytes.Length - 4];
                        normalized[0] = bytes[0];
                        normalized[1] = (byte)(bytes[1] - 1);
                        normalized[7] = (byte)authority;
                        Array.Copy(bytes, 12, normalized, 8, bytes.Length - 12);
                        bytes = normalized;
                    }
                }
                return Decode(bytes);
            }
            catch
            {
                return null;
            }
        }

        private static byte[]? HexToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
                return null;

            var result = new byte[hex.Length / 2];
            for (var i = 0; i < hex.Length; i += 2)
            {
                result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return result;
        }
    }
}
