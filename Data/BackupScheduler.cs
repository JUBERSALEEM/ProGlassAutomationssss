using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProGlassAutomation.Data.Database
{
    /// <summary>
    /// Automatic backup scheduler
    /// </summary>
    public static class BackupScheduler
    {
        private static Timer? _timer;
        private static bool _isRunning;
        private static DateTime _lastBackup;
        private static int _autoBackupDays = 7;

        /// <summary>
        /// Start the backup scheduler
        /// </summary>
        public static void Start()
        {
            var config = AppConfiguration.Instance.Backup;

            if (!config.EnableAutoBackup || config.AutoBackupDays <= 0)
            {
                Logger.Info("Backup scheduler disabled");
                return;
            }

            _autoBackupDays = config.AutoBackupDays;
            _lastBackup = DateTime.MinValue;

            // Check if backup is needed on startup
            if (config.BackupOnStartup || ShouldBackup())
            {
                Task.Run(() => PerformBackup());
            }

            // Schedule daily check
            var dueTime = TimeSpan.FromHours(24);
            _timer = new Timer(_ =>
            {
                if (ShouldBackup())
                {
                    Task.Run(() => PerformBackup());
                }
            }, null, dueTime, dueTime);

            Logger.Info($"Backup scheduler started (interval: {_autoBackupDays} days)");
        }

        /// <summary>
        /// Stop the backup scheduler
        /// </summary>
        public static void Stop()
        {
            _timer?.Dispose();
            _timer = null;
            Logger.Info("Backup scheduler stopped");
        }

        /// <summary>
        /// Check if backup should run
        /// </summary>
        private static bool ShouldBackup()
        {
            if (_lastBackup == DateTime.MinValue)
            {
                // First backup or check last backup time from file
                var markerFile = GetLastBackupMarker();
                if (File.Exists(markerFile))
                {
                    var lastStr = File.ReadAllText(markerFile);
                    if (DateTime.TryParse(lastStr, out var last))
                    {
                        _lastBackup = last;
                    }
                }
            }

            return (DateTime.Now - _lastBackup).TotalDays >= _autoBackupDays;
        }

        /// <summary>
        /// Perform backup
        /// </summary>
        private static void PerformBackup()
        {
            if (_isRunning) return;

            lock (BackupLocker)
            {
                if (_isRunning) return;
                _isRunning = true;
            }

            try
            {
                Logger.Info("Starting scheduled backup...");
                DbHelper.AutoBackup();
                _lastBackup = DateTime.Now;

                // Save last backup time
                File.WriteAllText(GetLastBackupMarker(), _lastBackup.ToString("yyyy-MM-dd HH:mm:ss"));

                Logger.Info($"Scheduled backup completed: {_lastBackup:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Logger.Error("Scheduled backup failed", ex);
            }
            finally
            {
                _isRunning = false;
            }
        }

        private static readonly object BackupLocker = new object();

        private static string GetLastBackupMarker()
        {
            var config = AppConfiguration.Instance.Backup;
            var dir = Path.GetDirectoryName(config.BackupPath);
            return Path.Combine(dir ?? "logs", "last_backup.txt");
        }

        /// <summary>
        /// Manually trigger backup
        /// </summary>
        public static void TriggerNow()
        {
            Task.Run(() => PerformBackup());
        }
    }
}