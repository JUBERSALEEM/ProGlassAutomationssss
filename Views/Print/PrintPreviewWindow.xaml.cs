using System.Windows;

namespace ProGlassAutomation.Views.Print
{
    public partial class PrintPreviewWindow : Window
    {
        public PrintPreviewWindow()
        {
            InitializeComponent();
        }

        public PrintPreviewWindow(PrintData printData) : this()
        {
            DataContext = printData;
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(PrintBorder, "Print Preview");
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Print error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}