using System;

namespace ditjson.Decoders
{
    internal static class TimestampDecoder
    {
        private const long FileTimeEpoch = 116444736000000000;

        internal static string? Decode(string? hexValue)
        {
            if (string.IsNullOrEmpty(hexValue))
                return null;

            if (!long.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber,
                null, out var filetime))
                return null;

            return ConvertFileTimeToUtc(filetime);
        }

        internal static string? DecodeFromInt64(long filetime) => ConvertFileTimeToUtc(filetime);

        private static string? ConvertFileTimeToUtc(long filetime)
        {
            if (filetime == 0)
                return null;

            if (filetime == long.MaxValue)
                return null;

            try
            {
                var dateTime = DateTime.FromFileTimeUtc(filetime);
                return dateTime.ToString("O");
            }
            catch
            {
                return null;
            }
        }
    }
}
