using System.Globalization;

namespace LogAggregator.Core.Infrastructure.Parsers
{
    public static class TimestampParser
    {
        private static readonly string[] _formats =
        {
            "yyyy-MM-ddTHH:mm:ss",      // JSON/XML: 2025-10-16T08:00:00
            "yyyy-MM-dd HH:mm:ss",      // TXT:      2025-10-16 08:00:00
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "dd/MM/yyyy HH:mm:ss",
            "MM/dd/yyyy HH:mm:ss",
            "yyyy/MM/dd HH:mm:ss",
        };

        public static DateTime? TryParse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var cleaned = raw.Trim();

            // TryParseExact first, faster and won't misread ambiguous formats
            if (DateTime.TryParseExact(cleaned, _formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces, out var result))
                return result;

            // fallback for anything we didn't anticipate
            if (DateTime.TryParse(cleaned, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var fallback))
                return fallback;

            // "invalid-timestamp", "bad-timestamp" etc. it just returns null
            return null;
        }
    }
}
