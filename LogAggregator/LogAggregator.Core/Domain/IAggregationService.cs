namespace LogAggregator.Core.Domain
{
    public interface IAggregationService
    {
        void AddEntries(IEnumerable<LogEntry> entries);
        void Clear();
        IReadOnlyList<ServiceSummary> GetSummaries();
        IReadOnlyList<LogEntry> GetCorruptedEntries();
        AggregationStats GetStats();
    }
}
