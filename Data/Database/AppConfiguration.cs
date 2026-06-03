using System;
using System.IO;

namespace ProGlassAutomation.Data.Database
{
    /// <summary>
    /// Strongly-typed application configuration
    /// </summary>
    public sealed class AppConfiguration
    {
        private static AppConfiguration? _instance;

        /// <summary>
        /// Current singleton instance
        /// </summary>
        public static AppConfiguration Instance
            => _instance ?? throw new InvalidOperationException(
                "Configuration not initialized. Call ConfigurationLoader.Load() first.");

        /// <summary>
        /// Initialize with configuration instance
        /// </summary>
        public static void Initialize(AppConfiguration configuration)
        {
            _instance = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// Database configuration section
        /// </summary>
        public DatabaseConfig Database { get; set; } = new();

        /// <summary>
        /// Logging configuration section
        /// </summary>
        public LoggingConfig Logging { get; set; } = new();

        /// <summary>
        /// Behavior configuration section
        /// </summary>
        public BehaviorConfig Behavior { get; set; } = new();

        /// <summary>
        /// Validation settings
        /// </summary>
        public ValidationConfig Validation { get; set; } = new();

        /// <summary>
        /// Backup settings
        /// </summary>
        public BackupConfig Backup { get; set; } = new();
    }

    /// <summary>
    /// Database connection and behavior settings
    /// </summary>
    public class DatabaseConfig
    {
        /// <summary>
        /// Database file path
        /// </summary>
        public string DbPath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ProGlassAutomation",
            "glass.db");

        /// <summary>
        /// Maximum connection pool size
        /// </summary>
        public int MaxPoolSize { get; set; } = 10;

        /// <summary>
        /// Command execution timeout in seconds
        /// </summary>
        public int CommandTimeout { get; set; } = 30;

        /// <summary>
        /// Maximum retry attempts for transient failures
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// Enable Write-Ahead Logging mode
        /// </summary>
        public bool EnableWAL { get; set; } = true;

        /// <summary>
        /// Enable foreign key constraints
        /// </summary>
        public bool ForeignKeys { get; set; } = true;

        /// <summary>
        /// Cache size in pages (negative = KB)
        /// </summary>
        public int CacheSize { get; set; } = -2000;
    }

    /// <summary>
    /// Logging configuration settings
    /// </summary>
    public class LoggingConfig
    {
        /// <summary>
        /// Enable file logging
        /// </summary>
        public bool EnableFileLogging { get; set; } = true;

        /// <summary>
        /// Enable console debug output
        /// </summary>
        public bool EnableConsoleLogging { get; set; } = false;

        /// <summary>
        /// Minimum log level
        /// </summary>
        public LogLevel MinimumLevel { get; set; } = LogLevel.Warning;

        /// <summary>
        /// Log file directory path
        /// </summary>
        public string LogPath { get; set; } = "logs";

        /// <summary>
        /// Days to retain log files
        /// </summary>
        public int RetainDays { get; set; } = 30;

        /// <summary>
        /// Maximum log file size in MB
        /// </summary>
        public int MaxFileSizeMB { get; set; } = 10;
    }

    /// <summary>
    /// Application behavior settings
    /// </summary>
    public class BehaviorConfig
    {
        /// <summary>
        /// Enable automatic database backup
        /// </summary>
        public bool AutoBackup { get; set; } = true;

        /// <summary>
        /// Enable input validation
        /// </summary>
        public bool EnableValidation { get; set; } = true;

        /// <summary>
        /// Enable metrics collection
        /// </summary>
        public bool EnableMetrics { get; set; } = true;

        /// <summary>
        /// Enable automatic migrations
        /// </summary>
        public bool AutoMigrate { get; set; } = true;

        /// <summary>
        /// Default VAT percentage
        /// </summary>
        public decimal DefaultVatPercent { get; set; } = 5m;

        /// <summary>
        /// Default surcharge percentage
        /// </summary>
        public decimal SurchargePercent { get; set; } = 20m;

        /// <summary>
        /// SQM threshold for surcharge
        /// </summary>
        public decimal SurchargeThreshold { get; set; } = 4m;
    }

    /// <summary>
    /// Validation configuration
    /// </summary>
    public class ValidationConfig
    {
        /// <summary>
        /// Enable strict validation mode
        /// </summary>
        public bool StrictMode { get; set; } = false;

        /// <summary>
        /// Maximum string field length
        /// </summary>
        public int MaxStringLength { get; set; } = 500;

        /// <summary>
        /// Require mandatory fields
        /// </summary>
        public bool RequireMandatoryFields { get; set; } = true;
    }

    /// <summary>
    /// Backup configuration
    /// </summary>
    public class BackupConfig
    {
        /// <summary>
        /// Enable automatic backup
        /// </summary>
        public bool EnableAutoBackup { get; set; } = true;

        /// <summary>
        /// Backup directory path
        /// </summary>
        public string BackupPath { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "GlassBackup");

        /// <summary>
        /// Backup on startup
        /// </summary>
        public bool BackupOnStartup { get; set; } = false;

        /// <summary>
        /// Days between automatic backups
        /// </summary>
        public int AutoBackupDays { get; set; } = 7;
    }

    /// <summary>
    /// Log severity levels
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4
    }
}