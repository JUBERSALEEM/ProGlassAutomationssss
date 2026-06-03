using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProGlassAutomation.Data.Database
{
    /// <summary>
    /// Loads and manages application configuration from multiple sources
    /// Priority: Environment Variables > appsettings.json > Default Values
    /// </summary>
    public static class ConfigurationLoader
    {
        /// <summary>
        /// Load configuration from default sources
        /// </summary>
        /// <param name="path">Optional path to appsettings.json</param>
        /// <returns>Loaded configuration</returns>
        public static AppConfiguration Load(string path = "appsettings.json")
        {
            var configuration = new AppConfiguration();

            // Step 1: Load from appsettings.json if exists
            LoadFromJsonFile(configuration, path);

            // Step 2: Override with environment variables
            LoadFromEnvironmentVariables(configuration);

            // Step 3: Ensure directories exist
            EnsureDirectoriesExist(configuration);

            // Step 4: Validate configuration
            ValidateConfiguration(configuration);

            // Step 5: Initialize singleton
            AppConfiguration.Initialize(configuration);

            return configuration;
        }

        /// <summary>
        /// Load configuration from JSON file
        /// </summary>
        private static void LoadFromJsonFile(AppConfiguration config, string path)
        {
            if (!File.Exists(path))
            {
                Logger?.Debug("Configuration file not found: {Path}, using defaults", path);
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                var jsonConfig = JsonSerializer.Deserialize<JsonElement>(json, options);

                if (jsonConfig.TryGetProperty("Database", out var dbElement))
                {
                    ParseDatabaseConfig(config.Database, dbElement);
                }

                if (jsonConfig.TryGetProperty("Logging", out var logElement))
                {
                    ParseLoggingConfig(config.Logging, logElement);
                }

                if (jsonConfig.TryGetProperty("Behavior", out var behaviorElement))
                {
                    ParseBehaviorConfig(config.Behavior, behaviorElement);
                }

                if (jsonConfig.TryGetProperty("Validation", out var validationElement))
                {
                    ParseValidationConfig(config.Validation, validationElement);
                }

                if (jsonConfig.TryGetProperty("Backup", out var backupElement))
                {
                    ParseBackupConfig(config.Backup, backupElement);
                }

                Logger?.Info("Configuration loaded from: {Path}", path);
            }
            catch (Exception ex)
            {
                Logger?.Warning("Failed to load configuration from {Path}: {Error}, using defaults",
                    path, ex.Message);
            }
        }

        /// <summary>
        /// Parse database configuration from JSON element
        /// </summary>
        private static void ParseDatabaseConfig(DatabaseConfig config, JsonElement element)
        {
            if (element.TryGetProperty("DbPath", out var prop))
                config.DbPath = prop.GetString() ?? config.DbPath;

            if (element.TryGetProperty("MaxPoolSize", out var prop2))
                config.MaxPoolSize = prop2.GetInt32();

            if (element.TryGetProperty("CommandTimeout", out var prop3))
                config.CommandTimeout = prop3.GetInt32();

            if (element.TryGetProperty("MaxRetryCount", out var prop4))
                config.MaxRetryCount = prop4.GetInt32();

            if (element.TryGetProperty("EnableWAL", out var prop5))
                config.EnableWAL = prop5.GetBoolean();

            if (element.TryGetProperty("ForeignKeys", out var prop6))
                config.ForeignKeys = prop6.GetBoolean();

            if (element.TryGetProperty("CacheSize", out var prop7))
                config.CacheSize = prop7.GetInt32();
        }

        /// <summary>
        /// Parse logging configuration from JSON element
        /// </summary>
        private static void ParseLoggingConfig(LoggingConfig config, JsonElement element)
        {
            if (element.TryGetProperty("EnableFileLogging", out var prop))
                config.EnableFileLogging = prop.GetBoolean();

            if (element.TryGetProperty("EnableConsoleLogging", out var prop2))
                config.EnableConsoleLogging = prop2.GetBoolean();

            if (element.TryGetProperty("MinimumLevel", out var prop3))
            {
                var levelStr = prop3.GetString();
                if (Enum.TryParse<LogLevel>(levelStr, true, out var level))
                    config.MinimumLevel = level;
            }

            if (element.TryGetProperty("LogPath", out var prop4))
                config.LogPath = prop4.GetString() ?? config.LogPath;

            if (element.TryGetProperty("RetainDays", out var prop5))
                config.RetainDays = prop5.GetInt32();

            if (element.TryGetProperty("MaxFileSizeMB", out var prop6))
                config.MaxFileSizeMB = prop6.GetInt32();
        }

        /// <summary>
        /// Parse behavior configuration from JSON element
        /// </summary>
        private static void ParseBehaviorConfig(BehaviorConfig config, JsonElement element)
        {
            if (element.TryGetProperty("AutoBackup", out var prop))
                config.AutoBackup = prop.GetBoolean();

            if (element.TryGetProperty("EnableValidation", out var prop2))
                config.EnableValidation = prop2.GetBoolean();

            if (element.TryGetProperty("EnableMetrics", out var prop3))
                config.EnableMetrics = prop3.GetBoolean();

            if (element.TryGetProperty("AutoMigrate", out var prop4))
                config.AutoMigrate = prop4.GetBoolean();

            if (element.TryGetProperty("DefaultVatPercent", out var prop5))
                config.DefaultVatPercent = (decimal)prop5.GetDouble();

            if (element.TryGetProperty("SurchargePercent", out var prop6))
                config.SurchargePercent = (decimal)prop6.GetDouble();

            if (element.TryGetProperty("SurchargeThreshold", out var prop7))
                config.SurchargeThreshold = (decimal)prop7.GetDouble();
        }

        /// <summary>
        /// Parse validation configuration from JSON element
        /// </summary>
        private static void ParseValidationConfig(ValidationConfig config, JsonElement element)
        {
            if (element.TryGetProperty("StrictMode", out var prop))
                config.StrictMode = prop.GetBoolean();

            if (element.TryGetProperty("MaxStringLength", out var prop2))
                config.MaxStringLength = prop2.GetInt32();

            if (element.TryGetProperty("RequireMandatoryFields", out var prop3))
                config.RequireMandatoryFields = prop3.GetBoolean();
        }

        /// <summary>
        /// Parse backup configuration from JSON element
        /// </summary>
        private static void ParseBackupConfig(BackupConfig config, JsonElement element)
        {
            if (element.TryGetProperty("EnableAutoBackup", out var prop))
                config.EnableAutoBackup = prop.GetBoolean();

            if (element.TryGetProperty("BackupPath", out var prop2))
                config.BackupPath = prop2.GetString() ?? config.BackupPath;

            if (element.TryGetProperty("BackupOnStartup", out var prop3))
                config.BackupOnStartup = prop3.GetBoolean();

            if (element.TryGetProperty("AutoBackupDays", out var prop4))
                config.AutoBackupDays = prop4.GetInt32();
        }

        /// <summary>
        /// Load configuration from environment variables
        /// </summary>
        private static void LoadFromEnvironmentVariables(AppConfiguration config)
        {
            // Database settings
            var dbPath = Environment.GetEnvironmentVariable("PROGLASS_DBPATH");
            if (!string.IsNullOrWhiteSpace(dbPath))
                config.Database.DbPath = dbPath;

            var poolSize = Environment.GetEnvironmentVariable("PROGLASS_MAXPOOLSIZE");
            if (int.TryParse(poolSize, out var pool))
                config.Database.MaxPoolSize = pool;

            var timeout = Environment.GetEnvironmentVariable("PROGLASS_TIMEOUT");
            if (int.TryParse(timeout, out var timeoutValue))
                config.Database.CommandTimeout = timeoutValue;

            // Logging settings
            var logLevel = Environment.GetEnvironmentVariable("PROGLASS_LOGLEVEL");
            if (Enum.TryParse<LogLevel>(logLevel, true, out var level))
                config.Logging.MinimumLevel = level;

            // Behavior settings
            var autoBackup = Environment.GetEnvironmentVariable("PROGLASS_AUTOBACKUP");
            if (bool.TryParse(autoBackup, out var autoBackupValue))
                config.Behavior.AutoBackup = autoBackupValue;

            var enableMetrics = Environment.GetEnvironmentVariable("PROGLASS_ENABLEMETRICS");
            if (bool.TryParse(enableMetrics, out var metricsValue))
                config.Behavior.EnableMetrics = metricsValue;
        }

        /// <summary>
        /// Ensure required directories exist
        /// </summary>
        private static void EnsureDirectoriesExist(AppConfiguration config)
        {
            try
            {
                // Database directory
                var dbDir = Path.GetDirectoryName(config.Database.DbPath);
                if (!string.IsNullOrWhiteSpace(dbDir))
                    Directory.CreateDirectory(dbDir);

                // Log directory
                if (config.Logging.EnableFileLogging)
                    Directory.CreateDirectory(config.Logging.LogPath);

                // Backup directory
                if (config.Backup.EnableAutoBackup)
                    Directory.CreateDirectory(config.Backup.BackupPath);
            }
            catch (Exception ex)
            {
                Logger?.Error(ex, "Failed to create required directories");
            }
        }

        /// <summary>
        /// Validate configuration values
        /// </summary>
        private static void ValidateConfiguration(AppConfiguration config)
        {
            var errors = new System.Collections.Generic.List<string>();

            // Validate database path
            if (string.IsNullOrWhiteSpace(config.Database.DbPath))
                errors.Add("Database.DbPath cannot be empty");

            // Validate pool size
            if (config.Database.MaxPoolSize < 1 || config.Database.MaxPoolSize > 100)
                errors.Add("Database.MaxPoolSize must be between 1 and 100");

            // Validate timeout
            if (config.Database.CommandTimeout < 5 || config.Database.CommandTimeout > 300)
                errors.Add("Database.CommandTimeout must be between 5 and 300");

            // ✅ FIXED: Line 291 - Was missing dot (.)
            // Validate log retention
            if (config.Logging.RetainDays < 1 || config.Logging.RetainDays > 365)
                errors.Add("Logging.RetainDays must be between 1 and 365");

            // Validate behavior settings
            if (config.Behavior.DefaultVatPercent < 0 || config.Behavior.DefaultVatPercent > 100)
                errors.Add("Behavior.DefaultVatPercent must be between 0 and 100");

            if (errors.Count > 0)
            {
                var message = string.Join(Environment.NewLine, errors);
                // ✅ FIXED: Line 301 - Changed to use Error(Exception, message, args)
                Logger?.Error(new InvalidOperationException(message),
                    "Configuration validation failed");
                throw new InvalidOperationException($"Configuration validation failed: {message}");
            }

            Logger?.Info("Configuration validated successfully");
        }

        /// <summary>
        /// Optional logger reference
        /// </summary>
        private static IBasicLogger? Logger { get; set; }

        /// <summary>
        /// Set custom logger
        /// </summary>
        public static void SetLogger(IBasicLogger logger)
        {
            Logger = logger;
        }
    }

    /// <summary>
    /// Simple logger interface for configuration loading
    /// </summary>
    public interface IBasicLogger
    {
        void Debug(string message, params object[] args);
        void Info(string message, params object[] args);
        void Warning(string message, params object[] args);
        void Error(Exception ex, string message, params object[] args);
    }

    /// <summary>
    /// Default no-op logger
    /// </summary>
    public sealed class NullLogger : IBasicLogger
    {
        public static readonly NullLogger Instance = new();

        private NullLogger() { }

        public void Debug(string message, params object[] args) { }
        public void Info(string message, params object[] args) { }
        public void Warning(string message, params object[] args) { }
        public void Error(Exception ex, string message, params object[] args) { }
    }
}