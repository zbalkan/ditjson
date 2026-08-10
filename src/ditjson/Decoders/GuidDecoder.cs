using System;

namespace ditjson.Decoders
{
    internal static class GuidDecoder
    {
        internal static Guid Decode(byte[]? guidBytes)
        {
            if (guidBytes == null || guidBytes.Length != 16)
            {
                return Guid.Empty;
            }

            try
            {
                return new Guid(guidBytes);
            }
            catch
            {
                return Guid.Empty;
            }
        }

        internal static Guid Decode(string? hexValue)
        {
            if (string.IsNullOrEmpty(hexValue))
            {
                return Guid.Empty;
            }

            if (Guid.TryParse(hexValue, out var guid))
            {
                return guid;
            }

            try
            {
                var bytes = HexToBytes(hexValue.Replace("-", string.Empty));
                return Decode(bytes);
            }
            catch
            {
                return Guid.Empty;
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
