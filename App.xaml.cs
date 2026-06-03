using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using ProGlassAutomation.Data.Database;

namespace ProGlassAutomation
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // ==================== SET DATE FORMAT FIRST ====================
            CultureInfo culture = new CultureInfo("en-GB");
            culture.DateTimeFormat.ShortDatePattern = "dd-MM-yyyy";
            culture.DateTimeFormat.DateSeparator = "-";
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

            // Call base BEFORE our code
            base.OnStartup(e);

            // ==================== ADD GLOBAL EXCEPTION HANDLERS ====================
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // ==================== STEP 1: LOAD CONFIGURATION ====================
            try
            {
                System.Diagnostics.Debug.WriteLine("[App] Loading configuration...");

                // Determine which config file to load based on environment
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                var configPath = string.IsNullOrEmpty(environment)
                    ? "appsettings.json"
                    : $"appsettings.{environment}.json";

                // Load configuration (falls back to defaults if file not found)
                ConfigurationLoader.Load(configPath);

                var dbPath = AppConfiguration.Instance.Database.DbPath;
                System.Diagnostics.Debug.WriteLine($"[App] Configuration loaded: {dbPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Configuration load error: {ex.Message}");

                MessageBox.Show(
                    $"Failed to load configuration:\n\n{ex.Message}\n\nThe application will now close.",
                    "Configuration Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Environment.Exit(1);
            }

            // ==================== STEP 2: INITIALIZE DATABASE ====================
            try
            {
                System.Diagnostics.Debug.WriteLine("[App] Starting database initialization...");
                DbHelper.Init();
                System.Diagnostics.Debug.WriteLine("[App] Database initialization complete!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Database initialization failed: {ex.Message}");
                MessageBox.Show(
                    $"Failed to initialize database:\n\n{ex.Message}\n\nThe application will now close.",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Environment.Exit(1);
            }

            // ==================== STEP 3: OPTIONAL AUTO-BACKUP ====================
            try
            {
                if (AppConfiguration.Instance.Backup.EnableAutoBackup &&
                    AppConfiguration.Instance.Backup.BackupOnStartup)
                {
                    System.Diagnostics.Debug.WriteLine("[App] Performing startup backup...");
                    DbHelper.AutoBackup();
                    System.Diagnostics.Debug.WriteLine("[App] Startup backup complete!");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Startup backup skipped: {ex.Message}");
            }

            // ==================== STEP 4: INITIALIZE LOGGER ====================
            try
            {
                Logger.Initialize();
                Logger.Info("Application starting...");

                if (AppConfiguration.Instance.Logging.EnableFileLogging)
                {
                    // Run cleanup in background
                    Task.Run(() =>
                    {
                        try { Logger.CleanupOldLogs(); }
                        catch { /* Silently fail */ }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Logger init skipped: {ex.Message}");
            }

            // ==================== STEP 5: START BACKUP SCHEDULER ====================
            try
            {
                if (AppConfiguration.Instance.Backup.EnableAutoBackup)
                {
                    BackupScheduler.Start();
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"Backup scheduler init skipped: {ex.Message}");
            }
        }

        // ==================== SAVE DATA ON EXIT ====================
        protected override void OnExit(ExitEventArgs e)
        {
            // Save all ViewModels before exit
            try
            {
                if (MainWindow?.DataContext is ViewModels.ProformaInvoiceListViewModel mainVm)
                {
                    mainVm.SaveOnExit();
                    System.Diagnostics.Debug.WriteLine("[App] ProformaInvoice data saved on exit");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error saving on exit: {ex.Message}");
            }

            // Stop backup scheduler
            try { BackupScheduler.Stop(); }
            catch { }

            // Log application exit
            Logger.Info("Application exiting");
            System.Diagnostics.Debug.WriteLine("[App] Application exiting...");

            base.OnExit(e);
        }

        // ==================== UNHANDLED EXCEPTION HANDLERS ====================

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            string message = $"FATAL ERROR (UnhandledException):\n\n{ex?.Message}\n\n{ex?.StackTrace}";

            System.Diagnostics.Debug.WriteLine(message);

            // Log to file
            LogErrorToFile(message);

            MessageBox.Show(
                message + "\n\nThe application will now close.",
                "CRITICAL ERROR",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            if (e.IsTerminating)
            {
                Environment.Exit(1);
            }
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string message = $"UI ERROR (DispatcherUnhandledException):\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}";

            System.Diagnostics.Debug.WriteLine(message);

            // Log to file
            LogErrorToFile(message);

            MessageBox.Show(
                message,
                "ERROR",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true; // Prevent app from crashing
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            string message = $"TASK ERROR (UnobservedTaskException):\n\n{e.Exception?.Message}\n\n{e.Exception?.StackTrace}";

            System.Diagnostics.Debug.WriteLine(message);

            // Log to file
            LogErrorToFile(message);

            MessageBox.Show(
                message,
                "TASK ERROR",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.SetObserved(); // Prevent app from crashing
        }

        // ==================== HELPER METHOD ====================

        private void LogErrorToFile(string message)
        {
            try
            {
                string logDirectory = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "logs");

                System.IO.Directory.CreateDirectory(logDirectory);

                string logFile = System.IO.Path.Combine(logDirectory, "error.log");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n\n";
                System.IO.File.AppendAllText(logFile, logEntry);
            }
            catch { /* Fail silently */ }
        }
    }
}