using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LogAggregator.Core.Domain;
using LogAggregator.Core.Infrastructure.Parsers;
using LogAggregator.Core.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace LogAggregator.WPF.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ILogFileSource _fileSource;
        private readonly ParserRegistry _parsers;
        private readonly IAggregationService _aggregation;
        private readonly IReportService _report;
        private List<ServiceSummary> _allSummaries = new();

        // Observable properties

        [ObservableProperty]
        private string rootPath = string.Empty;

        [ObservableProperty]
        private string statusText = "Step 1: Click Browse. Step 2: Click Start.";

        [ObservableProperty]
        private string filterText = string.Empty;

        [ObservableProperty]
        private bool isWatching;

        [ObservableProperty]
        private AggregationStats stats = new();

        public ObservableCollection<ServiceSummary> DisplayedSummaries { get; } = new();
        public ObservableCollection<LogEntry> CorruptedEntries { get; } = new();

        // Constructor (DI-ready)
        public MainViewModel(
            ILogFileSource fileSource,
            ParserRegistry parsers,
            IAggregationService aggregation,
            IReportService report)
        {
            _fileSource = fileSource;
            _parsers = parsers;
            _aggregation = aggregation;
            _report = report;

            _fileSource.LogFileDetected += OnLogFileDetected;
            _fileSource.LogFileDeleted += OnLogFileDeleted;
        }

        // Property change hooks

        partial void OnFilterTextChanged(string value)
        {
            ApplyFilter();
        }

        partial void OnIsWatchingChanged(bool value)
        {
            StartWatchingCommand.NotifyCanExecuteChanged();
            StopWatchingCommand.NotifyCanExecuteChanged();
        }

        // Commands (MVVM Toolkit)

        [RelayCommand]
        private void BrowseFolder()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select root log folder to monitor"
            };

            if (dialog.ShowDialog() == true)
            {
                RootPath = dialog.FolderName;
                StatusText = $"Folder selected: {RootPath}";
                StartWatchingCommand.NotifyCanExecuteChanged();
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartWatching))]
        private void StartWatching()
        {         _aggregation.Clear();
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

        private bool CanStartWatching()
            => !IsWatching && !string.IsNullOrWhiteSpace(RootPath);

        [RelayCommand(CanExecute = nameof(IsWatching))]
        private void StopWatching()
        {
            _fileSource.StopWatching();
            IsWatching = false;
            StatusText = "Stopped.";
        }

        [RelayCommand]
        private async Task GenerateReportAsync()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Aggregation Report",
                Filter = "Log files (*.log)|*.log|Text files (*.txt)|*.txt",
                FileName = $"report_{DateTime.Now:yyyyMMdd_HHmmss}.log",
                InitialDirectory =
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads")
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

        [RelayCommand]
        private void Clear()
        {
            _aggregation.Clear();
            ClearCollections();
            StatusText = "Cleared.";
        }

        // File watcher callbacks

        private void OnLogFileDetected(object? sender, LogFileDetectedArgs args)
        {
            try
            {
                var entries = _parsers.ParseFile(args.FilePath).ToList();
                _aggregation.AddEntries(entries);

                Application.Current.Dispatcher.Invoke(RefreshUI);
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    StatusText = $"Parse error in {Path.GetFileName(args.FilePath)}: {ex.Message}");
            }
        }

        private void OnLogFileDeleted(object? sender, LogFileDeletedArgs args)
        {
            _aggregation.RemoveFile(args.FilePath);
            Application.Current.Dispatcher.Invoke(RefreshUI);
        }

        // UI refresh helpers

        private void RefreshUI()
        {
            _allSummaries = _aggregation.GetSummaries().ToList();
            Stats = _aggregation.GetStats();

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
