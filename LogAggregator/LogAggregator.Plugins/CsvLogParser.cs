using LogAggregator.Core.Domain;
using LogAggregator.Core.Infrastructure.Parsers;

namespace LogAggregator.Plugins
{
    // Drop the compiled DLL into /plugins and the app picks it up automatically.
    public class CsvLogParser : ILogParser
    {
        public string ParserName => "CSV Plugin Parser (.csv)";

        public bool CanParse(string filePath) => 

            Path.GetExtension(filePath).ToLowerInvariant() == ".csv";

        public IEnumerable<LogEntry> Parse(string filePath)
        {
            List<string> lines;
            try
            {
                lines = File.ReadAllLines(filePath).ToList();
            }
            catch (Exception ex)
            {
                return new[]
                {
                    new LogEntry
                    {
                        IsCorrupted = true,
                        SkipReason  = $"Cannot read file: {ex.Message}",
                        SourceFile  = filePath
                    }
                };
            }

            return ParseLines(lines, filePath);
        }

        private static IEnumerable<LogEntry> ParseLines(List<string> lines, string filePath)
        {
            bool firstLine = true;

            foreach (var line in lines)
            {
                // Skip header row
                if (firstLine)
                {
                    firstLine = false;
                    if (line.StartsWith("timestamp", StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',', 4); // max 4 parts, message may contain commas

                if (parts.Length < 3)
                {
                    yield return new LogEntry
                    {
                        IsCorrupted = true,
                        SkipReason  = $"CSV line has fewer than 3 columns: '{line}'",
                        SourceFile  = filePath,
                        RawContent  = line
                    };
                    continue;
                }

                var rawTimestamp = parts[0].Trim();
                var rawSeverity  = parts[1].Trim();
                var rawService   = parts[2].Trim();
                var rawMessage   = parts.Length >= 4 ? parts[3].Trim() : string.Empty;

                yield return new LogEntry
                {
                    Timestamp   = TimestampParser.TryParse(rawTimestamp),
                    Level       = LevelParser.Parse(rawSeverity),
                    ServiceName = string.IsNullOrWhiteSpace(rawService) ? "Unknown" : rawService,
                    Message     = rawMessage,
                    SourceFile  = filePath,
                    IsCorrupted = false
                };
            }
        }
    }
}
