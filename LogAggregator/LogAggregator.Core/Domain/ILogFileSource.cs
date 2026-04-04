namespace LogAggregator.Core.Domain
{
    public interface ILogFileSource
    {
        event EventHandler<LogFileDetectedArgs> LogFileDetected;
        event EventHandler<LogFileDeletedArgs>  LogFileDeleted;
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

    public class LogFileDeletedArgs : EventArgs
    {
        public string FilePath { get; }

        public LogFileDeletedArgs(string filePath)
        {
            FilePath = filePath;
        }
    }
}
