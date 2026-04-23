using System;
using System.Windows;
using ProGlassAutomation.Data.Database;

namespace ProGlassAutomation
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 🔵 Initialize database
            try
            {
                DbHelper.Init();
                System.Diagnostics.Debug.WriteLine("✔ DATABASE INITIALIZED");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("❌ DB INIT FAILED: " + ex.Message);

                MessageBox.Show(
                    "Database initialization failed:\n" + ex.Message,
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Shutdown();
            }
        }
    }
}