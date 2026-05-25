using ProGlassAutomation.Models;
using ProGlassAutomation.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ProGlassAutomation.Views.ProformaInvoice
{
    public partial class ProformaInvoicePrintPreviewView : UserControl
    {
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

        // A4 Page dimensions at 96 DPI
        private const double A4_WIDTH_PX = 794;
        private const double A4_HEIGHT_PX = 1123;
        private const double MARGIN_LEFT_RIGHT = 67;   // ~0.7 inch
        private const double MARGIN_TOP_BOTTOM = 29;   // ~0.3 inch

        private bool _isLandscape = false;
        private ObservableCollection<PageModel> _pages = new ObservableCollection<PageModel>();

        public ProformaInvoicePrintPreviewView()
        {
            InitializeComponent();
            AllOtherCharges = new ObservableCollection<OtherChargeDisplay>();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadOtherChargesData();
            GeneratePages();
            UpdatePreview();
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
            GeneratePages();
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            try
            {
                StatusText.Text = (_isLandscape ? "Landscape" : "Portrait") + " | A4 | Margins: T/B 0.3in, L/R 0.7in";
            }
            catch { }
        }

        private void GeneratePages()
        {
            try
            {
                if (!(DataContext is ProformaInvoiceViewModel vm) || vm.Invoice == null)
                    return;

                _pages.Clear();

                var invoice = vm.Invoice;
                var specs = invoice.Specifications?.ToList() ?? new List<SpecificationModel>();

                if (specs.Count == 0)
                    specs.Add(new SpecificationModel { SpecificationName = "No Items", Items = new ObservableCollection<InvoiceItemModel>() });

                // ============================================
                // PAGE HEIGHT CALCULATIONS (Updated for compact design)
                // ============================================
                double pageHeight = 1065;
                double firstPageHeader = 200;   // Header + Invoice info + Project details
                double continuationHeader = 40; // Minimal header for continuation pages
                double lastPageFooter = 200;    // Other charges + Summary + Net Total + Footer
                double otherPageFooter = 25;     // Just footer for non-last pages
                double specHeaderHeight = 20;   // Spec title bar
                double specFooterHeight = 20;   // Spec total bar
                double rowHeight = 18;          // Data grid row

                // Calculate available height per page
                double firstPageAvailable = pageHeight - firstPageHeader - otherPageFooter;
                double continuationAvailable = pageHeight - continuationHeader - otherPageFooter;

                // ============================================
                // SPLIT SPECS INTO PAGES
                // ============================================
                var pages = new List<List<SpecificationModel>>();
                var currentPageSpecs = new List<SpecificationModel>();
                double currentHeight = 0;
                bool isFirstPage = true;

                foreach (var spec in specs)
                {
                    int itemCount = spec.Items?.Count ?? 0;
                    double specTotalHeight = specHeaderHeight + (itemCount * rowHeight) + specFooterHeight;

                    double available = isFirstPage ? firstPageAvailable : continuationAvailable;

                    if (currentHeight + specTotalHeight > available && currentPageSpecs.Count > 0)
                    {
                        // Move to next page
                        pages.Add(currentPageSpecs);
                        currentPageSpecs = new List<SpecificationModel>();
                        currentHeight = 0;
                        isFirstPage = false;
                    }

                    currentPageSpecs.Add(spec);
                    currentHeight += specTotalHeight;
                }

                // Add last page
                if (currentPageSpecs.Count > 0)
                {
                    pages.Add(currentPageSpecs);
                }

                // Ensure at least one page
                if (pages.Count == 0)
                    pages.Add(new List<SpecificationModel>());

                // ============================================
                // CREATE PAGE MODELS WITH CUMULATIVE BALANCES
                // ============================================
                int totalPages = pages.Count;
                double cumQty = 0, cumSQM = 0, cumLM = 0, cumPrice = 0;

                for (int i = 0; i < pages.Count; i++)
                {
                    bool isFirst = i == 0;
                    bool isLast = i == pages.Count - 1;

                    // Calculate page totals
                    double pageQty = 0, pageSQM = 0, pageLM = 0, pagePrice = 0;
                    foreach (var spec in pages[i])
                    {
                        pageQty += spec.SpecTotalQty;
                        pageSQM += spec.SpecTotalSQM;
                        pageLM += spec.SpecTotalLM;
                        pagePrice += spec.SpecTotalPrice;
                    }

                    // Update cumulative
                    cumQty += pageQty;
                    cumSQM += pageSQM;
                    cumLM += pageLM;
                    cumPrice += pagePrice;

                    _pages.Add(new PageModel
                    {
                        PageNumber = i + 1,
                        TotalPages = totalPages,
                        IsFirstPage = isFirst,
                        IsLastPage = isLast,
                        PreviousPageNo = i,
                        Specifications = new ObservableCollection<SpecificationModel>(pages[i]),

                        // Header data
                        CompanyName = vm.CompanyName,
                        CompanyTRN = vm.CompanyTRN,
                        CompanyLocation = vm.CompanyLocation,
                        InvoiceNo = invoice.InvoiceNo,
                        InvoiceDate = invoice.InvoiceDate,
                        ValidUntil = invoice.ValidUntil,
                        CustomerName = invoice.CustomerName,
                        CustomerTRN = invoice.CustomerTRN,
                        CustomerAddress = invoice.CustomerAddress,
                        ProjectName = invoice.ProjectName,
                        ProjectLocation = invoice.ProjectLocation,
                        LPONo = invoice.LPONo,
                        AttentionName = invoice.AttentionName,
                        ContactNo = invoice.ContactNo,

                        // Balance C/F (cumulative after this page)
                        PageTotalQty = cumQty,
                        PageTotalSQM = cumSQM,
                        PageTotalLM = cumLM,
                        PageTotalPrice = cumPrice,

                        // Grand totals (always show full totals)
                        TotalSQM = invoice.TotalSQM,
                        TotalLM = invoice.TotalLM,
                        TotalQty = invoice.TotalQty,
                        GrandTotal = invoice.GrandTotal,
                        VatAmount = invoice.VatAmount,
                        NetTotal = invoice.NetTotal,

                        AllOtherCharges = isLast ? AllOtherCharges : new ObservableCollection<OtherChargeDisplay>(),
                        HasOtherCharges = isLast && HasOtherCharges,
                        TotalOtherCharges = isLast ? TotalOtherCharges : 0
                    });
                }

                PagesContainer.ItemsSource = _pages;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GeneratePages error: {ex.Message}");
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog == null || !printDialog.ShowDialog().GetValueOrDefault()) return;

                double contentWidth = A4_WIDTH_PX - (MARGIN_LEFT_RIGHT * 2);
                double contentHeight = A4_HEIGHT_PX - (MARGIN_TOP_BOTTOM * 2);

                double scaleX = printDialog.PrintableAreaWidth / contentWidth;
                double scaleY = printDialog.PrintableAreaHeight / contentHeight;
                double scale = Math.Min(scaleX, scaleY);
                scale = Math.Min(scale, 1.0);

                foreach (var page in _pages)
                {
                    var container = PagesContainer.ItemContainerGenerator.ContainerFromItem(page) as ContentPresenter;
                    if (container != null)
                    {
                        var border = FindChild<Border>(container, "PreviewPage");
                        if (border != null)
                        {
                            border.LayoutTransform = new ScaleTransform(scale, scale);
                            printDialog.PrintVisual(border, $"ProForma Invoice - Page {page.PageNumber}");
                            border.LayoutTransform = null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Print error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild && child is FrameworkElement fe && fe.Name == childName)
                    return typedChild;

                var result = FindChild<T>(child, childName);
                if (result != null) return result;
            }
            return null;
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

    public class PageModel
    {
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public int PreviousPageNo { get; set; }
        public bool IsFirstPage { get; set; }
        public bool IsLastPage { get; set; }
        public ObservableCollection<SpecificationModel> Specifications { get; set; }
        public string CompanyName { get; set; }
        public string CompanyTRN { get; set; }
        public string CompanyLocation { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime ValidUntil { get; set; }
        public string CustomerName { get; set; }
        public string CustomerTRN { get; set; }
        public string CustomerAddress { get; set; }
        public string ProjectName { get; set; }
        public string ProjectLocation { get; set; }
        public string LPONo { get; set; }
        public string AttentionName { get; set; }
        public string ContactNo { get; set; }

        // Balance C/F (cumulative after this page)
        public double PageTotalQty { get; set; }
        public double PageTotalSQM { get; set; }
        public double PageTotalLM { get; set; }
        public double PageTotalPrice { get; set; }

        // Grand totals
        public double TotalSQM { get; set; }
        public double TotalLM { get; set; }
        public int TotalQty { get; set; }
        public double GrandTotal { get; set; }
        public double VatAmount { get; set; }
        public double NetTotal { get; set; }
        public ObservableCollection<OtherChargeDisplay> AllOtherCharges { get; set; }
        public bool HasOtherCharges { get; set; }
        public double TotalOtherCharges { get; set; }
    }

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