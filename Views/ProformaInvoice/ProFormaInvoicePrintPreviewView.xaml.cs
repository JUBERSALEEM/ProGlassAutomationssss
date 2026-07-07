using ProGlassAutomation.Models;
using ProGlassAutomation.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        private const double A4_WIDTH_PX = 794;
        private const double A4_HEIGHT_PX = 1123;
        private const double MARGIN_LEFT_RIGHT = 67;
        private const double MARGIN_TOP_BOTTOM = 29;

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

                var allItems = new List<ItemWithSpec>();
                foreach (var spec in specs)
                {
                    var items = spec.Items?.ToList() ?? new List<InvoiceItemModel>();
                    if (items.Count == 0)
                    {
                        items.Add(new InvoiceItemModel { SrNo = 1, GlassRef = "No items", Qty = 1 });
                    }
                    foreach (var item in items)
                    {
                        allItems.Add(new ItemWithSpec { Spec = spec, Item = item });
                    }
                }

                double pageContentHeight = 1065;
                double pageBadgeHeight = 24;
                double footerHeight = 24;
                double netTotalHeight = 40;
                double summaryHeight = 60;
                double otherChargesHeight = 80;
                double firstPageHeaderHeight = 160;
                double continuationHeaderHeight = 32;
                double bfRowHeight = 36;
                double specHeaderHeight = 26;
                double specFooterHeight = 26;
                double rowHeight = 18;
                double specMargin = 3;
                double specBorderPadding = 6;

                double firstPageAvailable = pageContentHeight - pageBadgeHeight - firstPageHeaderHeight - summaryHeight - netTotalHeight - otherChargesHeight - footerHeight;
                double continuationPageAvailable = pageContentHeight - pageBadgeHeight - continuationHeaderHeight - bfRowHeight - footerHeight;
                double lastPageAvailable = pageContentHeight - pageBadgeHeight - continuationHeaderHeight - bfRowHeight - summaryHeight - netTotalHeight - otherChargesHeight - footerHeight;

                double safetyBuffer = rowHeight * 3;
                firstPageAvailable -= safetyBuffer;
                continuationPageAvailable -= safetyBuffer;
                lastPageAvailable -= safetyBuffer;

                if (firstPageAvailable < 50) firstPageAvailable = 50;
                if (continuationPageAvailable < 50) continuationPageAvailable = 50;
                if (lastPageAvailable < 50) lastPageAvailable = 50;

                var pageContents = new List<PageContent>();
                var currentPageSpecs = new List<SpecWithItems>();
                double currentPageHeight = 0;
                bool isFirstPage = true;
                int currentSpecId = -1;
                var itemsForCurrentSpec = new List<InvoiceItemModel>();
                int specContinuationCount = 0;

                foreach (var itemWithSpec in allItems)
                {
                    var spec = itemWithSpec.Spec;
                    var item = itemWithSpec.Item;

                    double available = isFirstPage ? firstPageAvailable : continuationPageAvailable;

                    if (spec.Id != currentSpecId)
                    {
                        if (itemsForCurrentSpec.Count > 0 && currentSpecId >= 0)
                        {
                            var prevSpec = specs.FirstOrDefault(s => s.Id == currentSpecId);
                            if (prevSpec != null)
                            {
                                double specBlockHeight = specHeaderHeight + (itemsForCurrentSpec.Count * rowHeight) + specFooterHeight + specMargin + specBorderPadding;

                                if (currentPageHeight + specBlockHeight > available && currentPageSpecs.Count > 0)
                                {
                                    pageContents.Add(new PageContent
                                    {
                                        IsFirstPage = isFirstPage,
                                        IsLastPage = false,
                                        Specs = new List<SpecWithItems>(currentPageSpecs)
                                    });
                                    currentPageSpecs.Clear();
                                    currentPageHeight = 0;
                                    isFirstPage = false;
                                    available = continuationPageAvailable;
                                    specContinuationCount = 0;
                                }

                                currentPageSpecs.Add(new SpecWithItems
                                {
                                    Specification = prevSpec,
                                    Items = new List<InvoiceItemModel>(itemsForCurrentSpec),
                                    IsContinuation = false,
                                    ContinuationCount = 0
                                });
                                currentPageHeight += specBlockHeight;
                            }
                            itemsForCurrentSpec.Clear();
                        }

                        double minSpecHeight = specHeaderHeight + specFooterHeight + specMargin + specBorderPadding;
                        if (currentPageHeight + minSpecHeight > available && currentPageSpecs.Count > 0)
                        {
                            pageContents.Add(new PageContent
                            {
                                IsFirstPage = isFirstPage,
                                IsLastPage = false,
                                Specs = new List<SpecWithItems>(currentPageSpecs)
                            });
                            currentPageSpecs.Clear();
                            currentPageHeight = 0;
                            isFirstPage = false;
                            available = continuationPageAvailable;
                            specContinuationCount = 0;
                        }

                        currentSpecId = spec.Id;
                        specContinuationCount = 0;
                    }

                    double currentSpecHeight = specHeaderHeight + ((itemsForCurrentSpec.Count + 1) * rowHeight) + specFooterHeight + specMargin + specBorderPadding;
                    double potentialPageHeight = currentPageHeight + currentSpecHeight;

                    if (potentialPageHeight > available && itemsForCurrentSpec.Count > 0)
                    {
                        var currentSpec = specs.FirstOrDefault(s => s.Id == currentSpecId);
                        if (currentSpec != null)
                        {
                            double currentSpecBlockHeight = specHeaderHeight + (itemsForCurrentSpec.Count * rowHeight) + specFooterHeight + specMargin + specBorderPadding;

                            currentPageSpecs.Add(new SpecWithItems
                            {
                                Specification = currentSpec,
                                Items = new List<InvoiceItemModel>(itemsForCurrentSpec),
                                IsContinuation = specContinuationCount > 0,
                                ContinuationCount = specContinuationCount
                            });
                            currentPageHeight += currentSpecBlockHeight;
                        }

                        itemsForCurrentSpec.Clear();
                        itemsForCurrentSpec.Add(item);
                        specContinuationCount++;

                        double newSpecHeight = specHeaderHeight + rowHeight + specFooterHeight + specMargin + specBorderPadding;
                        if (currentPageHeight + newSpecHeight > available)
                        {
                            pageContents.Add(new PageContent
                            {
                                IsFirstPage = isFirstPage,
                                IsLastPage = false,
                                Specs = new List<SpecWithItems>(currentPageSpecs)
                            });
                            currentPageSpecs.Clear();
                            currentPageHeight = 0;
                            isFirstPage = false;
                            available = continuationPageAvailable;
                            specContinuationCount = 0;

                            var continuationSpec = specs.FirstOrDefault(s => s.Id == currentSpecId);
                            if (continuationSpec != null)
                            {
                                currentPageSpecs.Add(new SpecWithItems
                                {
                                    Specification = continuationSpec,
                                    Items = new List<InvoiceItemModel>(itemsForCurrentSpec),
                                    IsContinuation = true,
                                    ContinuationCount = 1
                                });
                                currentPageHeight = specHeaderHeight + rowHeight + specFooterHeight + specMargin + specBorderPadding;
                            }
                            itemsForCurrentSpec.Clear();
                        }
                    }
                    else
                    {
                        itemsForCurrentSpec.Add(item);
                    }
                }

                if (itemsForCurrentSpec.Count > 0)
                {
                    var lastSpec = specs.FirstOrDefault(s => s.Id == currentSpecId);
                    if (lastSpec != null)
                    {
                        double lastSpecBlockHeight = specHeaderHeight + (itemsForCurrentSpec.Count * rowHeight) + specFooterHeight + specMargin + specBorderPadding;

                        if (currentPageHeight + lastSpecBlockHeight > continuationPageAvailable && currentPageSpecs.Count > 0)
                        {
                            pageContents.Add(new PageContent
                            {
                                IsFirstPage = isFirstPage,
                                IsLastPage = false,
                                Specs = new List<SpecWithItems>(currentPageSpecs)
                            });
                            currentPageSpecs.Clear();
                            currentPageHeight = 0;
                            isFirstPage = false;
                        }

                        currentPageSpecs.Add(new SpecWithItems
                        {
                            Specification = lastSpec,
                            Items = new List<InvoiceItemModel>(itemsForCurrentSpec),
                            IsContinuation = specContinuationCount > 0,
                            ContinuationCount = specContinuationCount
                        });
                        currentPageHeight += lastSpecBlockHeight;
                    }
                }

                if (currentPageSpecs.Count > 0)
                {
                    double lastPageSectionsHeight = summaryHeight + netTotalHeight + otherChargesHeight + footerHeight + pageBadgeHeight + continuationHeaderHeight + bfRowHeight + safetyBuffer;
                    double remainingHeight = continuationPageAvailable - currentPageHeight;

                    if (remainingHeight >= lastPageSectionsHeight)
                    {
                        pageContents.Add(new PageContent
                        {
                            IsFirstPage = isFirstPage,
                            IsLastPage = true,
                            Specs = new List<SpecWithItems>(currentPageSpecs)
                        });
                    }
                    else
                    {
                        pageContents.Add(new PageContent
                        {
                            IsFirstPage = isFirstPage,
                            IsLastPage = false,
                            Specs = new List<SpecWithItems>(currentPageSpecs)
                        });

                        pageContents.Add(new PageContent
                        {
                            IsFirstPage = false,
                            IsLastPage = true,
                            Specs = new List<SpecWithItems>()
                        });
                    }
                }

                if (pageContents.Count == 0)
                    pageContents.Add(new PageContent { IsFirstPage = true, IsLastPage = true, Specs = new List<SpecWithItems>() });

                if (pageContents.Count > 0)
                {
                    for (int i = 0; i < pageContents.Count - 1; i++)
                        pageContents[i].IsLastPage = false;
                    pageContents[pageContents.Count - 1].IsLastPage = true;
                }

                int totalPages = pageContents.Count;
                double cumQty = 0, cumSQM = 0, cumLM = 0, cumPrice = 0;

                for (int i = 0; i < pageContents.Count; i++)
                {
                    bool isFirst = i == 0;
                    bool isLast = pageContents[i].IsLastPage;

                    double pageQty = 0, pageSQM = 0, pageLM = 0, pagePrice = 0;
                    var specsForPage = new List<SpecificationModel>();

                    foreach (var specItem in pageContents[i].Specs)
                    {
                        var spec = specItem.Specification;

                        var specCopy = new SpecificationModel
                        {
                            Id = spec.Id,
                            SpecificationName = spec.SpecificationName + (specItem.IsContinuation ? " (Cont.)" : ""),
                            ModuleType = spec.ModuleType,
                            WorkType = spec.WorkType,
                            BasePrice = spec.BasePrice,
                            SurchargePercent = spec.SurchargePercent,
                            Items = new ObservableCollection<InvoiceItemModel>(specItem.Items)
                        };

                        specCopy.CalculateSpecTotals();

                        pageQty += specCopy.SpecTotalQty;
                        pageSQM += specCopy.SpecTotalSQM;
                        pageLM += specCopy.SpecTotalLM;
                        pagePrice += specCopy.SpecTotalPrice;

                        specsForPage.Add(specCopy);
                    }

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
                        Specifications = new ObservableCollection<SpecificationModel>(specsForPage),

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

                        PageTotalQty = cumQty,
                        PageTotalSQM = cumSQM,
                        PageTotalLM = cumLM,
                        PageTotalPrice = cumPrice,

                        TotalSQM = invoice.TotalSQM,
                        TotalLM = invoice.TotalLM,
                        TotalLM1 = invoice.TotalLM,
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
                System.Diagnostics.Debug.WriteLine($"GeneratePages error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog == null || !printDialog.ShowDialog().GetValueOrDefault()) return;

                Dispatcher.Invoke(new Action(() => { }), System.Windows.Threading.DispatcherPriority.Render);

                foreach (var page in _pages)
                {
                    var container = PagesContainer.ItemContainerGenerator.ContainerFromItem(page) as ContentPresenter;
                    if (container != null)
                    {
                        var border = FindChild<Border>(container, "PreviewPage");
                        if (border != null)
                        {
                            border.UpdateLayout();
                            border.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                            border.Arrange(new Rect(border.DesiredSize));

                            int pixelWidth = (int)Math.Ceiling(border.ActualWidth * 96 / 96);
                            int pixelHeight = (int)Math.Ceiling(border.ActualHeight * 96 / 96);

                            if (pixelWidth <= 0) pixelWidth = 794;
                            if (pixelHeight <= 0) pixelHeight = 1123;

                            var renderBitmap = new RenderTargetBitmap(
                                pixelWidth,
                                pixelHeight,
                                96, 96, PixelFormats.Pbgra32);

                            renderBitmap.Render(border);

                            var visual = new Image();
                            visual.Source = renderBitmap;
                            visual.Width = border.ActualWidth;
                            visual.Height = border.ActualHeight;

                            printDialog.PrintVisual(visual, $"ProForma Invoice - Page {page.PageNumber}");
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
        public ObservableCollection<SpecificationModel>? Specifications { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyTRN { get; set; }
        public string? CompanyLocation { get; set; }
        public string? InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime ValidUntil { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerTRN { get; set; }
        public string? CustomerAddress { get; set; }
        public string? ProjectName { get; set; }
        public string? ProjectLocation { get; set; }
        public string? LPONo { get; set; }
        public string? AttentionName { get; set; }
        public string? ContactNo { get; set; }

        public double PageTotalQty { get; set; }
        public double PageTotalSQM { get; set; }
        public double PageTotalLM { get; set; }
        public double PageTotalPrice { get; set; }

        public double TotalSQM { get; set; }
        public double TotalLM { get; set; }
        public double TotalLM1 { get; set; }
        public int TotalQty { get; set; }
        public double GrandTotal { get; set; }
        public double VatAmount { get; set; }
        public double NetTotal { get; set; }
        public ObservableCollection<OtherChargeDisplay>? AllOtherCharges { get; set; }
        public bool HasOtherCharges { get; set; }
        public double TotalOtherCharges { get; set; }
    }

    public class OtherChargeDisplay
    {
        public string? Name { get; set; }
        public string? TypeDisplay { get; set; }
        public string? LinkedSpecsDisplay { get; set; }
        public string? ValueDisplay { get; set; }
        public double Rate { get; set; }
        public string? AmountDisplay { get; set; }
    }

    public class PageContent
    {
        public bool IsFirstPage { get; set; }
        public bool IsLastPage { get; set; }
        public List<SpecWithItems> Specs { get; set; } = new List<SpecWithItems>();
    }

    public class SpecWithItems
    {
        public SpecificationModel? Specification { get; set; }
        public List<InvoiceItemModel> Items { get; set; } = new List<InvoiceItemModel>();
        public bool IsContinuation { get; set; }
        public int ContinuationCount { get; set; }
    }

    public class ItemWithSpec
    {
        public SpecificationModel? Spec { get; set; }
        public InvoiceItemModel? Item { get; set; }
    }
}