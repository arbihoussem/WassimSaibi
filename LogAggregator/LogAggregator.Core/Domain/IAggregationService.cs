namespace LogAggregator.Core.Domain
{
    public interface IAggregationService
    {
        void AddEntries(IEnumerable<LogEntry> entries);
        void RemoveFile(string filePath);
        void Clear();
        IReadOnlyList<ServiceSummary> GetSummaries();
        IReadOnlyList<LogEntry> GetCorruptedEntries();
        AggregationStats GetStats();
    }
}
