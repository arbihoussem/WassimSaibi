namespace LogAggregator.Core.Domain
{
    
    public class LogEntry
    {
        public DateTime? Timestamp  { get; set; }
        public string ServiceName   { get; set; } = "Unknown";
        public LogLevel Level       { get; set; } = LogLevel.Unknown;
        public string Message       { get; set; } = string.Empty;
        public string SourceFile    { get; set; } = string.Empty;
        public bool IsCorrupted     { get; set; } = false;
        public string? SkipReason   { get; set; }
        public string? RawContent   { get; set; }
    }
}
