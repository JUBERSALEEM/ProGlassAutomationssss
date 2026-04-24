using QuestPDF;
using QuestPDF.Infrastructure;
using System.Windows;

namespace ProGlassAutomation
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Apply QuestPDF Community License (Free for non-commercial)
            QuestPDF.Settings.License = LicenseType.Community;
        }
    }
}