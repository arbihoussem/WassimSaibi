using LogAggregator.Core.Domain;

namespace LogAggregator.Core.Infrastructure.Services
{
    public class FileWatcherService : ILogFileSource, IDisposable
    {
        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly Dictionary<string, long> _fileOffsets = new();
        private readonly object _offsetLock = new();

        private static readonly string[] _supportedExtensions =
            { ".log", ".txt", ".json", ".xml", ".csv", ".dat" };

        public event EventHandler<LogFileDetectedArgs>? LogFileDetected;
        public event EventHandler<LogFileDeletedArgs>?  LogFileDeleted;

        public void StartWatching(string rootPath)
        {
            if (!Directory.Exists(rootPath))
                throw new DirectoryNotFoundException($"Root path not found: {rootPath}");

            // scan everything that already exists before setting up the live watcher
            var existingFiles = Directory
                .GetFiles(rootPath, "*.*", SearchOption.AllDirectories)
                .Where(IsSupported);

            foreach (var file in existingFiles)
                RaiseDetected(file, isNew: false);

            var watcher = new FileSystemWatcher(rootPath)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents   = true,
                InternalBufferSize    = 65536, // default 8KB overflows on busy folders
                NotifyFilter          = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName
            };

            watcher.Created += (_, e) => { if (IsSupported(e.FullPath)) RaiseDetected(e.FullPath, isNew: true);  };
            watcher.Changed += (_, e) => { if (IsSupported(e.FullPath)) RaiseDetected(e.FullPath, isNew: false); };
            watcher.Deleted += (_, e) => { if (IsSupported(e.FullPath)) RaiseDeleted(e.FullPath); };
            watcher.Error   += (_, _) => RescanAll(rootPath); // buffer overflow fallback

            _watchers.Add(watcher);
        }

        public void StopWatching()
        {
            foreach (var w in _watchers) { w.EnableRaisingEvents = false; w.Dispose(); }
            _watchers.Clear();
            lock (_offsetLock) { _fileOffsets.Clear(); }
        }

        private void RescanAll(string rootPath)
        {
            foreach (var file in Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories).Where(IsSupported))
                RaiseDetected(file, isNew: false);
        }

        private void RaiseDetected(string path, bool isNew) =>
            LogFileDetected?.Invoke(this, new LogFileDetectedArgs(path, isNew));

        private void RaiseDeleted(string path) =>
            LogFileDeleted?.Invoke(this, new LogFileDeletedArgs(path));

        private static bool IsSupported(string path) =>
            _supportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());

        public void Dispose() => StopWatching();
    }
}
