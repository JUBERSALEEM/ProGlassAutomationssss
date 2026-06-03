using System;
using System.IO;
using System.Text;

namespace ProGlassAutomation.Data.Database
{
    /// <summary>
    /// Structured file logger
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static string? _logDirectory;
        private static int _retainDays = 30;
        private static long _maxFileSizeMB = 10;

        /// <summary>
        /// Initialize logger with configuration
        /// </summary>
        public static void Initialize()
        {
            var config = AppConfiguration.Instance.Logging;
            _logDirectory = config.LogPath;
            _retainDays = config.RetainDays;
            _maxFileSizeMB = config.MaxFileSizeMB;

            // Create log directory
            if (!string.IsNullOrEmpty(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        /// <summary>
        /// Log debug message
        /// </summary>
        public static void Debug(string message) => Log(LogLevel.Debug, message);

        /// <summary>
        /// Log info message
        /// </summary>
        public static void Info(string message) => Log(LogLevel.Info, message);

        /// <summary>
        /// Log warning message
        /// </summary>
        public static void Warn(string message) => Log(LogLevel.Warning, message);

        /// <summary>
        /// Log error message
        /// </summary>
        public static void Error(string message) => Log(LogLevel.Error, message);

        /// <summary>
        /// Log error with exception
        /// </summary>
        public static void Error(string message, Exception ex)
            => Log(LogLevel.Error, $"{message}\n{ex}");

        /// <summary>
        /// Log fatal message
        /// </summary>
        public static void Fatal(string message) => Log(LogLevel.Fatal, message);

        /// <summary>
        /// Internal log method
        /// </summary>
        private static void Log(LogLevel level, string message)
        {
            // Check minimum level
            var config = AppConfiguration.Instance.Logging;
            if (level < config.MinimumLevel)
                return;

            try
            {
                lock (_lock)
                {
                    // Console output
                    if (config.EnableConsoleLogging)
                    {
                        var oldColor = Console.ForegroundColor;
                        Console.ForegroundColor = GetColor(level);
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}");
                        Console.ForegroundColor = oldColor;
                    }

                    // File output
                    if (config.EnableFileLogging && !string.IsNullOrEmpty(_logDirectory))
                    {
                        WriteToFile(level, message);
                    }
                }
            }
            catch { /* Fail silently */ }
        }

        private static void WriteToFile(LogLevel level, string message)
        {
            if (string.IsNullOrEmpty(_logDirectory))
                return;

            // Daily log file
            var fileName = $"app_{DateTime.Now:yyyyMMdd}.log";
            var filePath = Path.Combine(_logDirectory, fileName);

            // Check file size
            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists && fileInfo.Length > (_maxFileSizeMB * 1024 * 1024))
                {
                    // Rename old file
                    var archiveName = $"app_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                    File.Move(filePath, Path.Combine(_logDirectory, archiveName));
                }
            }
            catch { }

            // Write log entry
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}\n";
            File.AppendAllText(filePath, entry, Encoding.UTF8);
        }

        private static ConsoleColor GetColor(LogLevel level) => level switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Fatal => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };

        /// <summary>
        /// Clean up old log files
        /// </summary>
        public static void CleanupOldLogs()
        {
            try
            {
                if (string.IsNullOrEmpty(_logDirectory))
                    return;

                var cutoff = DateTime.Now.AddDays(-_retainDays);
                foreach (var file in Directory.GetFiles(_logDirectory, "app_*.log"))
                {
                    var info = new FileInfo(file);
                    if (info.CreationTime < cutoff)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch { /* Fail silently */ }
        }
    }
}