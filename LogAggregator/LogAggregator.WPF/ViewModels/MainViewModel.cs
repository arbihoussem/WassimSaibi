using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using LogAggregator.Core.Domain;
using LogAggregator.Core.Infrastructure.Parsers;
using LogAggregator.Core.Infrastructure.Services;
using LogAggregator.WPF.Helpers;

namespace LogAggregator.WPF.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly FileWatcherService   _fileSource;
        private readonly ParserRegistry       _parsers;
        private readonly AggregationService   _aggregation;
        private readonly ReportService        _report;

        private string _rootPath    = string.Empty;
        private string _statusText  = "Step 1: Click Browse. Step 2: Click Start.";
        private string _filterText  = string.Empty;
        private bool   _isWatching  = false;
        private AggregationStats _stats = new();
        private List<ServiceSummary> _allSummaries = new();
        public string RootPath
        {
            get => _rootPath;
            set => SetField(ref _rootPath, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetField(ref _statusText, value);
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                SetField(ref _filterText, value);
                ApplyFilter();
            }
        }

        public bool IsWatching
        {
            get => _isWatching;
            set
            {
                SetField(ref _isWatching, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public AggregationStats Stats
        {
            get => _stats;
            set => SetField(ref _stats, value);
        }

        public ObservableCollection<ServiceSummary> DisplayedSummaries { get; } = new();
        public ObservableCollection<LogEntry>       CorruptedEntries   { get; } = new();

        public ICommand BrowseFolderCommand   { get; }
        public ICommand StartWatchingCommand  { get; }
        public ICommand StopWatchingCommand   { get; }
        public ICommand GenerateReportCommand { get; }
        public ICommand ClearCommand          { get; }
        public MainViewModel()
        {
            _fileSource  = new FileWatcherService();
            _parsers     = new ParserRegistry();
            _aggregation = new AggregationService();
            _report      = new ReportService(_aggregation);

            _parsers.Register(new TextLogParser());
            _parsers.Register(new JsonLogParser());
            _parsers.Register(new XmlLogParser());

            // Plugins are loaded from a /plugins folder next to the exe.
            // Any DLL implementing ILogParser is picked up automatically.
            var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
            var loader    = new PluginLoader();
            foreach (var plugin in loader.LoadFromDirectory(pluginDir))
                _parsers.Register(plugin);

            BrowseFolderCommand = new RelayCommand(_ => BrowseFolder());

            // Start is only enabled when a folder is selected and we're not already watching
            StartWatchingCommand = new RelayCommand(
                _ => StartWatching(),
                _ => !IsWatching && !string.IsNullOrWhiteSpace(RootPath));

            StopWatchingCommand = new RelayCommand(
                _ => StopWatching(),
                _ => IsWatching);

            GenerateReportCommand = new AsyncRelayCommand(
                async _ => await GenerateReportAsync(),
                _ => _allSummaries.Any() || _aggregation.GetCorruptedEntries().Any());

            ClearCommand = new RelayCommand(_ => ClearAll());

            _fileSource.LogFileDetected += OnLogFileDetected;
        }

        private void BrowseFolder()
        {
            // Use WPF's OpenFolderDialog (available in .NET 8 WPF)
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select root log folder to monitor"
            };

            if (dialog.ShowDialog() == true)
            {
                RootPath   = dialog.FolderName;
                StatusText = $"Folder selected: {RootPath}";
            }
        }

        private void StartWatching()
        {
            if (string.IsNullOrWhiteSpace(RootPath))
            {
                StatusText = "No folder selected! Click Browse first.";
                return;
            }

            _aggregation.Clear();
            ClearCollections();

            try
            {
                StatusText = $"Starting scan of: {RootPath} ...";
                _fileSource.StartWatching(RootPath);
                IsWatching = true;
                StatusText = $"Watching: {RootPath} — waiting for files...";
            }
            catch (Exception ex)
            {
                StatusText = $"Error starting watcher: {ex.Message}";
            }
        }

        private void StopWatching()
        {
            _fileSource.StopWatching();
            IsWatching = false;
            StatusText = "Stopped.";
        }

        private async Task GenerateReportAsync()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title            = "Save Aggregation Report",
                Filter           = "Log files (*.log)|*.log|Text files (*.txt)|*.txt",
                FileName         = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.log",
                // default to Downloads so the report is never saved inside the watched folder
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                                   + @"\Downloads"
            };

            if (dialog.ShowDialog() == true)
            {
                StatusText = "Generating report...";
                try
                {
                    await _report.GenerateReportAsync(dialog.FileName);
                    StatusText = $"Report saved: {dialog.FileName}";
                }
                catch (Exception ex)
                {
                    StatusText = $"Report error: {ex.Message}";
                }
            }
        }

        private void ClearAll()
        {
            _aggregation.Clear();
            ClearCollections();
            StatusText = "Cleared.";
        }

        // Called on a background thread, UI updates must go through Dispatcher
        private void OnLogFileDetected(object? sender, LogFileDetectedArgs args)
        {
            try
            {
                // Parse on background thread
                var entries = _parsers.ParseFile(args.FilePath).ToList();
                _aggregation.AddEntries(entries);

                // UI update MUST be on UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    StatusText = $"Parsed: {Path.GetFileName(args.FilePath)} ({entries.Count} entries)";
                    RefreshUI();
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    StatusText = $"Parse error in {Path.GetFileName(args.FilePath)}: {ex.Message}");
            }
        }

        private void RefreshUI()
        {
            _allSummaries = _aggregation.GetSummaries().ToList();
            Stats         = _aggregation.GetStats();
            ApplyFilter();
            RefreshCorrupted();

            StatusText =
                $"Updated {DateTime.Now:HH:mm:ss}  |  " +
                $"{Stats.TotalEntries} entries  |  " +
                $"{Stats.TotalErrors} errors  |  " +
                $"{Stats.TotalCorrupted} skipped";
        }

        private void ApplyFilter()
        {
            var filtered = string.IsNullOrWhiteSpace(FilterText)
                ? _allSummaries
                : _allSummaries.Where(s =>
                    s.ServiceName.Contains(FilterText, StringComparison.OrdinalIgnoreCase));

            DisplayedSummaries.Clear();
            foreach (var s in filtered)
                DisplayedSummaries.Add(s);
        }

        private void RefreshCorrupted()
        {
            CorruptedEntries.Clear();
            foreach (var e in _aggregation.GetCorruptedEntries())
                CorruptedEntries.Add(e);
        }

        private void ClearCollections()
        {
            _allSummaries.Clear();
            DisplayedSummaries.Clear();
            CorruptedEntries.Clear();
        }
    }
}
