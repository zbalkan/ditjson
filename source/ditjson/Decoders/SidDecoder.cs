using System;
using System.Text;

namespace ditjson.Decoders
{
    internal static class SidDecoder
    {
        // Manual binary SID parsing instead of System.Security.Principal.SecurityIdentifier,
        // which throws PlatformNotSupportedException on non-Windows platforms.
        internal static string? Decode(byte[]? sidBytes)
        {
            if (sidBytes == null || sidBytes.Length < 8)
            {
                return null;
            }

            try
            {
                var revision = sidBytes[0];
                if (revision > 15)
                {
                    return null;
                }

                var subAuthorityCount = sidBytes[1];
                if (sidBytes.Length < 8 + (subAuthorityCount * 4))
                {
                    return null;
                }

                long authority = 0;
                for (var i = 2; i <= 7; i++)
                {
                    authority = (authority << 8) | sidBytes[i];
                }

                var sb = new StringBuilder("S-").Append(revision).Append('-').Append(authority);

                var offset = 8;
                for (var i = 0; i < subAuthorityCount; i++)
                {
                    var subAuthority = BitConverter.ToUInt32(sidBytes, offset);
                    sb.Append('-').Append(subAuthority);
                    offset += 4;
                }

                return sb.ToString();
            }
            catch
            {
                return null;
            }
        }

        internal static string? Decode(string hexValue)
        {
            if (string.IsNullOrEmpty(hexValue))
            {
                return null;
            }

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
            {
                return null;
            }

            var result = new byte[hex.Length / 2];
            for (var i = 0; i < hex.Length; i += 2)
            {
                result[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return result;
        }
    }
}
