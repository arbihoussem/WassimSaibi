namespace LogAggregator.Core.Domain
{
    public class ServiceSummary
    {
        public string ServiceName  { get; set; } = string.Empty;
        public int ErrorCount      { get; set; }
        public int WarningCount    { get; set; }
        public int InfoCount       { get; set; }
        public int DebugCount      { get; set; }
        public int UnknownCount    { get; set; }

        public List<LogEntry> Entries { get; set; } = new();

        public int TotalCount =>
            ErrorCount + WarningCount + InfoCount + DebugCount + UnknownCount;

        public string Summary =>
            $"{ErrorCount} ERROR, {WarningCount} WARNING, {InfoCount} INFO, {DebugCount} DEBUG";
    }
}
