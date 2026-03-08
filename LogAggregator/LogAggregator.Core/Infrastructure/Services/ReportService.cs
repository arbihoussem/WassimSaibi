using LogAggregator.Core.Domain;

namespace LogAggregator.Core.Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly IAggregationService _aggregation;

        public ReportService(IAggregationService aggregation)
        {
            _aggregation = aggregation;
        }

        public async Task GenerateReportAsync(string outputPath, CancellationToken ct = default)
        {
            var summaries = _aggregation.GetSummaries();
            var corrupted = _aggregation.GetCorruptedEntries();
            var stats     = _aggregation.GetStats();
            var generated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // await using so the file is properly closed even if something fails mid-write
            await using var writer = new StreamWriter(outputPath, append: false);

            await writer.WriteLineAsync(new string('=', 65));
            await writer.WriteLineAsync("  LOG AGGREGATION REPORT");
            await writer.WriteLineAsync($"  Generated : {generated}");
            await writer.WriteLineAsync($"  Tool      : Log Aggregator & Analyzer — DRÄXLMAIER");
            await writer.WriteLineAsync(new string('=', 65));
            await writer.WriteLineAsync();

            await writer.WriteLineAsync("[ OVERVIEW ]");
            await writer.WriteLineAsync($"  Total Services  : {stats.TotalServices}");
            await writer.WriteLineAsync($"  Total Entries   : {stats.TotalEntries}");
            await writer.WriteLineAsync($"  Errors          : {stats.TotalErrors}");
            await writer.WriteLineAsync($"  Warnings        : {stats.TotalWarnings}");
            await writer.WriteLineAsync($"  Info            : {stats.TotalInfo}");
            await writer.WriteLineAsync($"  Debug           : {stats.TotalDebug}");
            await writer.WriteLineAsync($"  Unknown/Invalid : {stats.TotalUnknown}");
            await writer.WriteLineAsync($"  Skipped/Corrupt : {stats.TotalCorrupted}");
            await writer.WriteLineAsync();

            await writer.WriteLineAsync("[ SERVICE BREAKDOWN ]");
            foreach (var s in summaries)
            {
                await writer.WriteLineAsync(
                    $"  {s.ServiceName,-22} → " +
                    $"{s.ErrorCount,4} ERROR  " +
                    $"{s.WarningCount,4} WARNING  " +
                    $"{s.InfoCount,4} INFO  " +
                    $"{s.DebugCount,4} DEBUG  " +
                    $"{s.UnknownCount,4} UNKNOWN");
            }
            await writer.WriteLineAsync();

            var errorServices = summaries.Where(s => s.ErrorCount > 0).ToList();
            if (errorServices.Any())
            {
                await writer.WriteLineAsync("[ ERROR DETAILS ]");
                foreach (var s in errorServices)
                {
                    await writer.WriteLineAsync($"  >> {s.ServiceName}");
                    foreach (var e in s.Entries.Where(e => e.Level == LogLevel.Error))
                    {
                        var ts = e.Timestamp?.ToString("yyyy-MM-dd HH:mm:ss") ?? "NO TIMESTAMP";
                        await writer.WriteLineAsync($"     [{ts}] {e.Message}");
                    }
                }
                await writer.WriteLineAsync();
            }

            if (corrupted.Any())
            {
                await writer.WriteLineAsync("[ SKIPPED ENTRIES ]");
                foreach (var c in corrupted)
                {
                    await writer.WriteLineAsync($"  File   : {c.SourceFile}");
                    await writer.WriteLineAsync($"  Reason : {c.SkipReason}");
                    if (!string.IsNullOrEmpty(c.RawContent))
                    {
                        // cap the preview so the report doesn't get flooded by one bad entry
                        var preview = c.RawContent.Length > 100
                            ? c.RawContent[..100] + "..."
                            : c.RawContent;
                        await writer.WriteLineAsync($"  Raw    : {preview}");
                    }
                    await writer.WriteLineAsync();
                }
            }

            await writer.WriteLineAsync(new string('=', 65));
            await writer.WriteLineAsync("  END OF REPORT");
            await writer.WriteLineAsync(new string('=', 65));
        }
    }
}
