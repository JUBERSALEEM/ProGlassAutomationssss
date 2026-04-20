using System.Windows;
using System.Windows.Input;

using ProGlassAutomation.Views.SGU;
using ProGlassAutomation.Views.DGU;

// SAFE ADD (ONLY NEW MODULE)
using ProGlassAutomation.Views.Lamination;

namespace ProGlassAutomation
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // 🔵 RESTORED DEFAULT START PAGE (SGU like before)
            MainPanel.Content = new SguView();
        }

        // ================= SGU (UNCHANGED) =================
        private void SGU_Click(object sender, MouseButtonEventArgs e)
        {
            MainPanel.Content = new SguView();
        }

        // ================= DGU (UNCHANGED) =================
        private void DGU_Click(object sender, MouseButtonEventArgs e)
        {
            MainPanel.Content = new DguView();
        }

        // ================= LAMINATION (NEW SAFE ADDITION) =================
        private void LAM_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                MainPanel.Content = new LaminationView();
            }
            catch
            {
                MessageBox.Show("Lamination module not ready.");
            }
        }
    }
}