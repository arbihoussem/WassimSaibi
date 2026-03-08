using System.Text.RegularExpressions;
using LogAggregator.Core.Domain;

namespace LogAggregator.Core.Infrastructure.Parsers
{
    public class TextLogParser : ILogParser
    {
        public string ParserName => "Plain Text Parser (.log, .txt)";

        // two formats exist in the test files, with and without brackets around the timestamp
        private static readonly Regex _bracketPattern = new(
            @"^\[(?<timestamp>[^\]]+)\]\s+(?<level>\w+)(?:\s+(?<service>\S+))?\s*(?:-\s*(?<message>.+))?$",
            RegexOptions.Compiled);

        private static readonly Regex _noBracketPattern = new(
            @"^(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2})\s+(?<level>\w+)(?:\s+(?<service>\S+))?\s*(?:-\s*(?<message>.+))?$",
            RegexOptions.Compiled);

        public bool CanParse(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return ext == ".log" || ext == ".txt";
        }

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
                    new LogEntry { IsCorrupted = true, SkipReason = $"Cannot read file: {ex.Message}", SourceFile = filePath }
                };
            }

            // file read is separate from iteration
            return ParseLines(lines, filePath);
        }

        private static IEnumerable<LogEntry> ParseLines(List<string> lines, string filePath)
        {
            int lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var match = _bracketPattern.Match(line);
                if (!match.Success)
                    match = _noBracketPattern.Match(line);

                if (!match.Success)
                {
                    yield return new LogEntry
                    {
                        IsCorrupted = true,
                        SkipReason  = $"Line {lineNumber}: unrecognized format",
                        SourceFile  = filePath,
                        RawContent  = line
                    };
                    continue;
                }

                var rawService = match.Groups["service"].Success
                    ? match.Groups["service"].Value.Trim()
                    : string.Empty;

                if (rawService == "-") rawService = string.Empty; // some lines have "-" where the service should be

                yield return new LogEntry
                {
                    Timestamp   = TimestampParser.TryParse(match.Groups["timestamp"].Value),
                    ServiceName = string.IsNullOrWhiteSpace(rawService) ? "Unknown" : rawService,
                    Level       = LevelParser.Parse(match.Groups["level"].Value),
                    Message     = match.Groups["message"].Success
                                    ? match.Groups["message"].Value.Trim()
                                    : string.Empty,
                    SourceFile  = filePath,
                    IsCorrupted = false
                };
            }
        }
    }
}
