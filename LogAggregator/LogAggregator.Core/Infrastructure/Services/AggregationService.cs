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

        public void RemoveFile(string filePath)
        {
            // remove all entries that came from this file, then rebuild counts
            foreach (var key in _summaries.Keys.ToList())
            {
                var summary = _summaries[key];
                lock (summary)
                {
                    summary.Entries.RemoveAll(e => e.SourceFile == filePath);

                    // rebuild counts from remaining entries
                    summary.ErrorCount   = summary.Entries.Count(e => e.Level == LogLevel.Error);
                    summary.WarningCount = summary.Entries.Count(e => e.Level == LogLevel.Warning);
                    summary.InfoCount    = summary.Entries.Count(e => e.Level == LogLevel.Info);
                    summary.DebugCount   = summary.Entries.Count(e => e.Level == LogLevel.Debug);
                    summary.UnknownCount = summary.Entries.Count(e => e.Level == LogLevel.Unknown);

                    // remove the service entirely if it has no entries left
                    if (summary.TotalCount == 0)
                        _summaries.TryRemove(key, out _);
                }
            }

            // remove corrupted entries from this file
            var remaining = _corruptedEntries.Where(e => e.SourceFile != filePath).ToList();
            while (_corruptedEntries.TryTake(out _)) { }
            foreach (var e in remaining)
                _corruptedEntries.Add(e);
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
