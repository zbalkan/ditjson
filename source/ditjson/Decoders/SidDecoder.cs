using System;
using System.Security.Principal;

namespace ditjson.Decoders
{
    internal static class SidDecoder
    {
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
