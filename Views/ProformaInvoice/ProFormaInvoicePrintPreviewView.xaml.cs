using ProGlassAutomation.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProGlassAutomation.Views.ProformaInvoice
{
    public partial class ProformaInvoicePrintPreviewView : UserControl
    {
        // Bindable properties for Other Charges
        public static readonly DependencyProperty AllOtherChargesProperty =
            DependencyProperty.Register("AllOtherCharges", typeof(ObservableCollection<OtherChargeDisplay>), typeof(ProformaInvoicePrintPreviewView));

        public static readonly DependencyProperty TotalOtherChargesProperty =
            DependencyProperty.Register("TotalOtherCharges", typeof(double), typeof(ProformaInvoicePrintPreviewView));

        public static readonly DependencyProperty HasOtherChargesProperty =
            DependencyProperty.Register("HasOtherCharges", typeof(bool), typeof(ProformaInvoicePrintPreviewView));

        public ObservableCollection<OtherChargeDisplay> AllOtherCharges
        {
            get => (ObservableCollection<OtherChargeDisplay>)GetValue(AllOtherChargesProperty);
            set => SetValue(AllOtherChargesProperty, value);
        }

        public double TotalOtherCharges
        {
            get => (double)GetValue(TotalOtherChargesProperty);
            set => SetValue(TotalOtherChargesProperty, value);
        }

        public bool HasOtherCharges
        {
            get => (bool)GetValue(HasOtherChargesProperty);
            set => SetValue(HasOtherChargesProperty, value);
        }

        // A4 dimensions in pixels (96 DPI)
        private const double A4_WIDTH_PX = 794;      // 210mm
        private const double A4_HEIGHT_PX = 1123;   // 297mm
        private const double PAGE_MARGIN = 35;
        private bool _isLandscape = false;

        public ProformaInvoicePrintPreviewView()
        {
            InitializeComponent();
            AllOtherCharges = new ObservableCollection<OtherChargeDisplay>();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdatePreview();
            LoadOtherChargesData();
        }

        private void LoadOtherChargesData()
        {
            try
            {
                if (DataContext is ProformaInvoiceViewModel vm)
                {
                    var allCharges = new ObservableCollection<OtherChargeDisplay>();
                    double totalCharges = 0;

                    if (vm.Invoice?.Specifications != null)
                    {
                        foreach (var spec in vm.Invoice.Specifications)
                        {
                            if (spec.OtherCharges != null)
                            {
                                foreach (var charge in spec.OtherCharges)
                                {
                                    string linkedSpecsDisplay = "All Specs";
                                    if (!string.IsNullOrWhiteSpace(charge.LinkedSpecIndices))
                                    {
                                        linkedSpecsDisplay = charge.TargetsAllSpecs
                                            ? "All Specs"
                                            : $"{charge.SpecIndexList.Count} specs";
                                    }

                                    allCharges.Add(new OtherChargeDisplay
                                    {
                                        Name = charge.Name ?? "Charge",
                                        TypeDisplay = charge.TypeDisplay ?? "FIXED",
                                        LinkedSpecsDisplay = linkedSpecsDisplay,
                                        ValueDisplay = charge.ValueDisplay ?? "0",
                                        Rate = charge.Rate,
                                        AmountDisplay = charge.AmountDisplay ?? "AED 0.00"
                                    });
                                    totalCharges += charge.Amount;
                                }
                            }
                        }
                    }

                    AllOtherCharges = allCharges;
                    HasOtherCharges = allCharges.Count > 0;
                    TotalOtherCharges = totalCharges;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadOtherChargesData error: {ex.Message}");
            }
        }

        private void OrientationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OrientationCombo?.SelectedItem is ComboBoxItem item)
            {
                _isLandscape = item.Content?.ToString() == "Landscape";
            }
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            try
            {
                if (PreviewPage == null) return;

                // Set page size based on orientation
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

                PrintArea?.UpdateLayout();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePreview error: {ex.Message}");
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog == null || !printDialog.ShowDialog().GetValueOrDefault()) return;

                double pageWidth = _isLandscape ? A4_HEIGHT_PX : A4_WIDTH_PX;
                double pageHeight = _isLandscape ? A4_WIDTH_PX : A4_HEIGHT_PX;

                double contentWidth = pageWidth - (PAGE_MARGIN * 2);
                double contentHeight = pageHeight - (PAGE_MARGIN * 2);

                // Calculate scale to fit the content on the printable area
                double scaleX = printDialog.PrintableAreaWidth / contentWidth;
                double scaleY = printDialog.PrintableAreaHeight / contentHeight;
                double scale = Math.Min(scaleX, scaleY);

                // Apply scale transform for printing
                PrintArea.LayoutTransform = new ScaleTransform(scale, scale);
                PrintArea.UpdateLayout();

                // Print the content
                printDialog.PrintVisual(PrintArea, "ProForma Invoice");

                // Reset transform after printing
                PrintArea.LayoutTransform = null;
                PrintArea.UpdateLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Window.GetWindow(this)?.Close();
            }
            catch { }
        }
    }

    // Helper class for Other Charges display
    public class OtherChargeDisplay
    {
        public string Name { get; set; }
        public string TypeDisplay { get; set; }
        public string LinkedSpecsDisplay { get; set; }
        public string ValueDisplay { get; set; }
        public double Rate { get; set; }
        public string AmountDisplay { get; set; }
    }
}