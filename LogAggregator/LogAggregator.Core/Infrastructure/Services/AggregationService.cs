using System.Collections.Concurrent;
using LogAggregator.Core.Domain;

namespace LogAggregator.Core.Infrastructure.Services
{
    public class AggregationService : IAggregationService
    {
        // ConcurrentDictionary because FileSystemWatcher fires on background threads
        // multiple files can arrive at the same time
        private readonly ConcurrentDictionary<string, ServiceSummary> _summaries = new();
        private readonly ConcurrentBag<LogEntry> _corruptedEntries = new();

        public void AddEntries(IEnumerable<LogEntry> entries)
        {
            foreach (var entry in entries)
            {
                if (entry.IsCorrupted)
                {
                    _corruptedEntries.Add(entry);
                    continue;
                }

                var summary = _summaries.GetOrAdd(
                    entry.ServiceName,
                    name => new ServiceSummary { ServiceName = name });

                // lock per service, threads on different services don't block each other
                lock (summary)
                {
                    summary.Entries.Add(entry);
                    switch (entry.Level)
                    {
                        case LogLevel.Error:   summary.ErrorCount++;   break;
                        case LogLevel.Warning: summary.WarningCount++; break;
                        case LogLevel.Info:    summary.InfoCount++;    break;
                        case LogLevel.Debug:   summary.DebugCount++;   break;
                        default:               summary.UnknownCount++; break;
                    }
                }
            }
        }

        public IReadOnlyList<ServiceSummary> GetSummaries()
        {
            return _summaries.Values
                .OrderByDescending(s => s.ErrorCount)
                .ThenByDescending(s => s.WarningCount)
                .ThenByDescending(s => s.InfoCount)
                .ThenBy(s => s.ServiceName)
                .ToList();
        }

        public IReadOnlyList<LogEntry> GetCorruptedEntries() => _corruptedEntries.ToList();

        public AggregationStats GetStats() => new()
        {
            TotalServices  = _summaries.Count,
            TotalEntries   = _summaries.Values.Sum(s => s.TotalCount),
            TotalErrors    = _summaries.Values.Sum(s => s.ErrorCount),
            TotalWarnings  = _summaries.Values.Sum(s => s.WarningCount),
            TotalInfo      = _summaries.Values.Sum(s => s.InfoCount),
            TotalDebug     = _summaries.Values.Sum(s => s.DebugCount),
            TotalUnknown   = _summaries.Values.Sum(s => s.UnknownCount),
            TotalCorrupted = _corruptedEntries.Count
        };

        public void Clear()
        {
            _summaries.Clear();
            // ConcurrentBag has no Clear() so we drain it manually
            while (_corruptedEntries.TryTake(out _)) { }
        }
    }
}
