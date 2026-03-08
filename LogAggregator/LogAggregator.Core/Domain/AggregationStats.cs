namespace LogAggregator.Core.Domain
{
    public class AggregationStats
    {
        public int TotalServices   { get; init; }
        public int TotalEntries    { get; init; }
        public int TotalErrors     { get; init; }
        public int TotalWarnings   { get; init; }
        public int TotalInfo       { get; init; }
        public int TotalDebug      { get; init; }
        public int TotalCorrupted  { get; init; }
        public int TotalUnknown    { get; init; }
    }
}
