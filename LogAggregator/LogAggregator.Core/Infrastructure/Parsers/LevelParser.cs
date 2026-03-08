using LogAggregator.Core.Domain;

namespace LogAggregator.Core.Infrastructure.Parsers
{
    public static class LevelParser
    {
        public static LogLevel Parse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return LogLevel.Unknown;

            return raw.Trim().ToUpperInvariant() switch
            {
                "ERROR"                  => LogLevel.Error,
                "WARNING" or "WARN"      => LogLevel.Warning,
                "INFO" or "INFORMATION"  => LogLevel.Info,
                "DEBUG"                  => LogLevel.Debug,
                "CRITICAL" or "FATAL"    => LogLevel.Error,    // same severity bucket as ERROR
                "INVALID"  or "UNKNOWN"  => LogLevel.Unknown,  // present in the test files
                _                        => LogLevel.Unknown
            };
        }
    }
}
