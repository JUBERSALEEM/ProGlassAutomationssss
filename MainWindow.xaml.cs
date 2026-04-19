using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProGlassAutomation
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // DEFAULT VIEW (IMPORTANT)
            LoadView("SGU");
        }

        // ================= ACTIVE BUTTON STYLE =================

        private void SetActive(Border active)
        {
            SGUButton.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));
            DGUButton.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));
            LAMButton.Background = new SolidColorBrush(Color.FromRgb(45, 45, 48));

            active.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
        }

        // ================= VIEW LOADER =================

        private void LoadView(string view)
        {
            switch (view)
            {
                case "SGU":
                    SetActive(SGUButton);
                    MainPanel.Content =
                        new Views.SGU.SguView();
                    break;

                case "DGU":
                    SetActive(DGUButton);
                    MainPanel.Content =
                        new Views.DGU.DguView();
                    break;

                case "LAM":
                    SetActive(LAMButton);

                    MessageBox.Show("Lamination module coming soon!",
                        "Info",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    break;
            }
        }

        // ================= EVENTS =================

        private void SGU_Click(object sender, MouseButtonEventArgs e)
        {
            LoadView("SGU");
        }

        private void DGU_Click(object sender, MouseButtonEventArgs e)
        {
            LoadView("DGU");
        }

        private void LAM_Click(object sender, MouseButtonEventArgs e)
        {
            LoadView("LAM");
        }
    }
}