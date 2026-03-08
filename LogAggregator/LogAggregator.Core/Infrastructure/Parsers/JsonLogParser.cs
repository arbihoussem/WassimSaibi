using System.Text.Json;
using LogAggregator.Core.Domain;

namespace LogAggregator.Core.Infrastructure.Parsers
{
    public class JsonLogParser : ILogParser
    {
        public string ParserName => "JSON Parser (.json)";

        public bool CanParse(string filePath) =>
            Path.GetExtension(filePath).ToLowerInvariant() == ".json";

        public IEnumerable<LogEntry> Parse(string filePath)
        {
            string raw;
            try
            {
                raw = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                return new[]
                {
                    new LogEntry { IsCorrupted = true, SkipReason = $"Cannot read file: {ex.Message}", SourceFile = filePath }
                };
            }

            JsonDocument doc;
            try
            {
                // using JsonDocument here because JsonNode throws on non-object array elements
                doc = JsonDocument.Parse(raw);
            }
            catch (Exception ex)
            {
                return new[]
                {
                    new LogEntry { IsCorrupted = true, SkipReason = $"Invalid JSON: {ex.Message}", SourceFile = filePath }
                };
            }

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return new[]
                {
                    new LogEntry { IsCorrupted = true, SkipReason = "Root JSON element is not an array", SourceFile = filePath }
                };
            }

            return ParseEntries(doc, filePath);
        }

        private static IEnumerable<LogEntry> ParseEntries(JsonDocument doc, string filePath)
        {
            int index = 0;
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                index++;

                // some test files had plain strings inside the array
                if (element.ValueKind != JsonValueKind.Object)
                {
                    yield return new LogEntry
                    {
                        IsCorrupted = true,
                        SkipReason  = $"Entry #{index}: expected object, got {element.ValueKind}",
                        SourceFile  = filePath,
                        RawContent  = element.GetRawText()
                    };
                    continue;
                }

                string? rawTimestamp = null, rawSeverity = null, rawService = null, rawMessage = null;

                // field is "severity" in these files, not "level"
                if (element.TryGetProperty("timestamp", out var ts)  && ts.ValueKind  == JsonValueKind.String) rawTimestamp = ts.GetString();
                if (element.TryGetProperty("severity",  out var sev) && sev.ValueKind == JsonValueKind.String) rawSeverity  = sev.GetString();
                if (element.TryGetProperty("service",   out var svc) && svc.ValueKind == JsonValueKind.String) rawService   = svc.GetString();
                if (element.TryGetProperty("message",   out var msg) && msg.ValueKind == JsonValueKind.String) rawMessage   = msg.GetString();

                if (string.IsNullOrWhiteSpace(rawSeverity))
                {
                    yield return new LogEntry
                    {
                        IsCorrupted = true,
                        SkipReason  = $"Entry #{index}: missing 'severity' field",
                        SourceFile  = filePath,
                        RawContent  = element.GetRawText()
                    };
                    continue;
                }

                yield return new LogEntry
                {
                    Timestamp   = TimestampParser.TryParse(rawTimestamp),
                    ServiceName = string.IsNullOrWhiteSpace(rawService) ? "Unknown" : rawService.Trim(),
                    Level       = LevelParser.Parse(rawSeverity),
                    Message     = rawMessage?.Trim() ?? string.Empty,
                    SourceFile  = filePath
                };
            }
        }
    }
}
