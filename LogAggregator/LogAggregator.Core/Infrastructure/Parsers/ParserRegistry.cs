using LogAggregator.Core.Domain;

namespace LogAggregator.Core.Infrastructure.Parsers
{
    public class ParserRegistry
    {
        private readonly List<ILogParser> _parsers = new();

        // built-ins are registered before plugins so they always take priority
        public void Register(ILogParser parser) => _parsers.Add(parser);

        public ILogParser? GetParser(string filePath) =>
            _parsers.FirstOrDefault(p => p.CanParse(filePath));

        public IEnumerable<LogEntry> ParseFile(string filePath)
        {
            var parser = GetParser(filePath);

            if (parser == null)
            {
                // still yield an entry so it shows up in the Skipped tab
                yield return new LogEntry
                {
                    IsCorrupted = true,
                    SkipReason  = $"No parser for extension '{Path.GetExtension(filePath)}'",
                    SourceFile  = filePath
                };
                yield break;
            }

            foreach (var entry in parser.Parse(filePath))
                yield return entry;
        }

        public IEnumerable<string> GetRegisteredParserNames() =>
            _parsers.Select(p => p.ParserName);
    }
}
