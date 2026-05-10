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
    }
}