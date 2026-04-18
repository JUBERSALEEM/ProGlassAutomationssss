using System.Windows;
using ProGlassAutomation.Views.SGU;

namespace ProGlassAutomation
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // DEFAULT VIEW = SGU IN MAIN AREA
            MainContent.Content = new SguView();
        }

        private void SGU_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new SguView(); // ✔ shows SGU in main panel
        }

        private void DGU_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("DGU will load in main panel next step");
        }

        private void Lamination_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Lamination coming soon");
        }
    }
}