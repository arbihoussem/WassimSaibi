namespace LogAggregator.Core.Domain
{
    public interface ILogParser
    {
        string ParserName { get; }
        bool CanParse(string filePath);
        IEnumerable<LogEntry> Parse(string filePath);
    }
}
