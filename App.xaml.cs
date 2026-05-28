using System;
using System.Globalization;
using System.Windows;
using ProGlassAutomation.Data.Database;

namespace ProGlassAutomation
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Set date format to dd-MM-yyyy
            CultureInfo culture = new CultureInfo("en-GB");
            culture.DateTimeFormat.ShortDatePattern = "dd-MM-yyyy";
            culture.DateTimeFormat.DateSeparator = "-";
            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
            System.Threading.Thread.CurrentThread.CurrentUICulture = culture;

            base.OnStartup(e);

            // Add global exception handlers FIRST
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            // INITIALIZE DATABASE AT STARTUP
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
        }

        // ==================== ADD THIS METHOD ====================
        protected override void OnExit(ExitEventArgs e)
        {
            // Save all ViewModels before exit
            try
            {
                if (MainWindow?.DataContext is ViewModels.ProformaInvoiceMainViewModel mainVm)
                {
                    mainVm.SaveOnExit();
                    System.Diagnostics.Debug.WriteLine("[App] ProformaInvoice data saved on exit");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[App] Error saving on exit: {ex.Message}");
            }

            base.OnExit(e);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            string message = $"FATAL ERROR (UnhandledException):\n\n{ex?.Message}\n\n{ex?.StackTrace}";

            System.Diagnostics.Debug.WriteLine(message);
            MessageBox.Show(message, "CRASH!", MessageBoxButton.OK, MessageBoxImage.Error);

            if (e.IsTerminating)
            {
                Environment.Exit(1);
            }
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string message = $"UI ERROR (DispatcherUnhandledException):\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}";

            System.Diagnostics.Debug.WriteLine(message);

            // Also log to file for crash reports
            LogErrorToFile(message);

            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // Prevent app from crashing
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            string message = $"TASK ERROR (UnobservedTaskException):\n\n{e.Exception?.Message}\n\n{e.Exception?.StackTrace}";

            System.Diagnostics.Debug.WriteLine(message);
            LogErrorToFile(message);

            MessageBox.Show(message, "Task Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.SetObserved(); // Prevent app from crashing
        }

        private void LogErrorToFile(string message)
        {
            try
            {
                string logFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n\n";
                System.IO.File.AppendAllText(logFile, logEntry);
            }
            catch { }
        }
    }
}