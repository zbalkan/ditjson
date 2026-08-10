using System;

namespace ditjson.Decoders
{
    internal static class TimestampDecoder
    {
        private static readonly DateTime DsTimeEpoch =
            new DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static string? Decode(string? hexValue)
        {
            if (string.IsNullOrEmpty(hexValue))
            {
                return null;
            }

            if (!long.TryParse(hexValue, System.Globalization.NumberStyles.HexNumber,
                null, out var filetime))
            {
                return null;
            }

            return ConvertFileTimeToUtc(filetime);
        }

        // Values using the LDAP UTC-time syntax are stored in ntds.dit as DSTIME:
        // whole seconds elapsed since 1601-01-01, rather than as strings or FILETIME.
        internal static string? DecodeDsTime(long seconds)
        {
            if (seconds <= 0)
            {
                return null;
            }

            try
            {
                return DsTimeEpoch.AddSeconds(seconds).ToString("O");
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        internal static string? DecodeFromInt64(long filetime) => ConvertFileTimeToUtc(filetime);

        private static string? ConvertFileTimeToUtc(long filetime)
        {
            if (filetime == 0)
            {
                return null;
            }

            if (filetime == long.MaxValue)
            {
                return null;
            }

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
