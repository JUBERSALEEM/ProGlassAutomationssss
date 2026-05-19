using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProGlassAutomation.Views.ProformaInvoice
{
    public partial class ProformaInvoicePrintPreviewView : UserControl
    {
        private const double A4_WIDTH_PX = 794;
        private const double A4_HEIGHT_PX = 1123;
        private const double MARGIN_LEFT_RIGHT = 67;
        private const double MARGIN_TOP_BOTTOM = 72;
        private const double MARGIN_HEADER_FOOTER = 29;

        private bool _isLandscape = false;

        public ProformaInvoicePrintPreviewView()
        {
            InitializeComponent();
            UpdatePreview();
        }

        private void OrientationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrientationCombo == null) return;
            _isLandscape = OrientationCombo.SelectedIndex == 1;
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            try
            {
                if (_isLandscape)
                {
                    PreviewPage.Width = A4_HEIGHT_PX;
                    PreviewPage.Height = A4_WIDTH_PX;
                    StatusText.Text = "Landscape | A4 | Margins: T/B 0.75in, L/R 0.7in";
                }
                else
                {
                    PreviewPage.Width = A4_WIDTH_PX;
                    PreviewPage.Height = A4_HEIGHT_PX;
                    StatusText.Text = "Portrait | A4 | Margins: T/B 0.75in, L/R 0.7in";
                }
            }
            catch { }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog == null) return;

                bool? result = printDialog.ShowDialog();
                if (result != true) return;

                double contentWidth;
                double contentHeight;

                if (_isLandscape)
                {
                    contentWidth = A4_HEIGHT_PX - (MARGIN_LEFT_RIGHT * 2);
                    contentHeight = A4_WIDTH_PX - (MARGIN_TOP_BOTTOM * 2);
                }
                else
                {
                    contentWidth = A4_WIDTH_PX - (MARGIN_LEFT_RIGHT * 2);
                    contentHeight = A4_HEIGHT_PX - (MARGIN_TOP_BOTTOM * 2);
                }

                double printableWidth = printDialog.PrintableAreaWidth;
                double printableHeight = printDialog.PrintableAreaHeight;

                double scaleX = printableWidth / contentWidth;
                double scaleY = printableHeight / contentHeight;
                double scale = System.Math.Min(scaleX, scaleY);
                scale = System.Math.Min(scale, 1.0);

                PrintArea.LayoutTransform = new ScaleTransform(scale, scale);
                PrintArea.UpdateLayout();

                printDialog.PrintVisual(PrintArea, "ProForma Invoice");

                PrintArea.LayoutTransform = null;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Print error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window window = Window.GetWindow(this);
                if (window != null)
                    window.Close();
            }
            catch { }
        }
    }
}