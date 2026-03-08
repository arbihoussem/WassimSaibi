namespace LogAggregator.Core.Domain
{
    public interface ILogFileSource
    {
        event EventHandler<LogFileDetectedArgs> LogFileDetected;
        void StartWatching(string rootPath);
        void StopWatching();
    }

    public class LogFileDetectedArgs : EventArgs
    {
        public string FilePath { get; }
        public bool IsNewFile  { get; }

        public LogFileDetectedArgs(string filePath, bool isNewFile)
        {
            FilePath  = filePath;
            IsNewFile = isNewFile;
        }
    }
}
