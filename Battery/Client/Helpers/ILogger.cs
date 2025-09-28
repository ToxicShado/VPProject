using System;
using System.IO;

namespace Client.Helpers
{
    public interface ILogger
    {
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogError(string message, Exception ex);
    }

    /// <summary>
    /// File logger implementation that writes to log.txt
    /// </summary>
    public class FileLogger : ILogger, IDisposable
    {
        private readonly string logFilePath;
        private readonly object lockObject = new object();
        private bool disposed = false;

        public FileLogger(string logFilePath = "log.txt")
        {
            this.logFilePath = logFilePath;
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(Path.GetFullPath(logFilePath));
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public void LogInfo(string message)
        {
            WriteToLog("INFO", message);
        }

        public void LogWarning(string message)
        {
            WriteToLog("WARNING", message);
        }

        public void LogError(string message)
        {
            WriteToLog("ERROR", message);
        }

        public void LogError(string message, Exception ex)
        {
            var fullMessage = $"{message} | Exception: {ex.Message}";
            if (ex.InnerException != null)
            {
                fullMessage += $" | Inner Exception: {ex.InnerException.Message}";
            }
            WriteToLog("ERROR", fullMessage);
        }

        private void WriteToLog(string level, string message)
        {
            if (disposed) return;

            try
            {
                lock (lockObject)
                {
                    var logEntry = $"[{level}] {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
                    File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
                }
            }
            catch (Exception)
            {
                // Silently fail if we can't write to log file
                // Could fallback to console in debug scenarios
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
            }
        }

        ~FileLogger()
        {
            Dispose(false);
        }
    }

    /// <summary>
    /// Backward compatibility - renamed from ConsoleLogger
    /// </summary>
    public class ConsoleLogger : FileLogger
    {
        public ConsoleLogger() : base("log.txt")
        {
        }
    }
}