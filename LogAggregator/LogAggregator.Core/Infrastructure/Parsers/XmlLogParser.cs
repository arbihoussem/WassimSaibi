using System.Xml.Linq;
using LogAggregator.Core.Domain;

namespace LogAggregator.Core.Infrastructure.Parsers
{
    // Same as JSON, the element is <severity>, not <level>.
    public class XmlLogParser : ILogParser
    {
        public string ParserName => "XML Parser (.xml)";

        public bool CanParse(string filePath) =>
            Path.GetExtension(filePath).ToLowerInvariant() == ".xml";

        public IEnumerable<LogEntry> Parse(string filePath)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(filePath);
            }
            catch (Exception ex)
            {
                return new[]
                {
                    new LogEntry { IsCorrupted = true, SkipReason = $"Malformed XML: {ex.Message}", SourceFile = filePath }
                };
            }

            return ParseEntries(doc, filePath);
        }

        private static IEnumerable<LogEntry> ParseEntries(XDocument doc, string filePath)
        {
            var entries = doc.Root?.Elements("log") ?? Enumerable.Empty<XElement>();

            int index = 0;
            foreach (var el in entries)
            {
                index++;

                var rawTimestamp = el.Element("timestamp")?.Value;
                var rawSeverity  = el.Element("severity")?.Value;
                var rawService   = el.Element("service")?.Value;
                var rawMessage   = el.Element("message")?.Value;

                if (string.IsNullOrWhiteSpace(rawSeverity))
                {
                    yield return new LogEntry
                    {
                        IsCorrupted = true,
                        SkipReason  = $"Entry #{index}: missing <severity> element",
                        SourceFile  = filePath,
                        RawContent  = el.ToString()
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
