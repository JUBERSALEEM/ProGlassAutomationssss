using System;
using System.Windows;
using System.Windows.Media;

namespace ProGlassAutomation
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Enable hardware acceleration
            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.Default;

            // Check render capability tier
            int tier = (RenderCapability.Tier >> 16);
            if (tier >= 2)
            {
                // Tier 2 = Hardware acceleration enabled
                Console.WriteLine($"Hardware acceleration enabled (Tier {tier})");
            }
            else
            {
                Console.WriteLine($"Software rendering mode (Tier {tier})");
            }

            base.OnStartup(e);
        }
    }
}