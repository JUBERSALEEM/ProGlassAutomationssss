using LiveChartsCore.SkiaSharpView.WPF;
using Microsoft.Win32;
using Newtonsoft.Json;
using ProGlassAutomation.Models;
using ProGlassAutomation.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.VisualBasic;

namespace ProGlassAutomation.ViewModels
{
    public class DimensionOption { public string Value { get; set; } = ""; public string Label { get; set; } = ""; }
    public class ChargeTypeOption { public string Value { get; set; } = ""; public string Label { get; set; } = ""; }
    public class AirSpacerOption { public string Thickness { get; set; } = ""; public string Type { get; set; } = ""; public double Price { get; set; } public string Display => $"{Thickness}mm {Type} - AED {Price:F2}"; }
    public class FileListItem { public string FilePath { get; set; } = ""; public string InvoiceNo { get; set; } = ""; public string CustomerName { get; set; } = ""; public DateTime InvoiceDate { get; set; } public string FileName => Path.GetFileNameWithoutExtension(FilePath); public string DateDisplay => InvoiceDate.ToString("dd MMM yyyy"); }

    public class GlassPriceCalculator
    {
        public double CalculateSGU(double sheetPrice, double cutting, double tempering, double wasteFactor, double profitPercent, string thickness, string color, string workType, out string description, out string summary)
        {
            double step1 = sheetPrice / wasteFactor;
            double step2 = step1 + cutting + tempering;
            double final = step2 * (1 + profitPercent / 100.0);
            description = $"{thickness}mm {color} {workType}";
            summary = $"({sheetPrice} / {wasteFactor:F2}) + {cutting} + {tempering} = {step2:F2} × {1 + profitPercent / 100.0:F2} = {final:F2}";
            return Math.Round(final, 2);
        }
        public double CalculateDGU(double outerPrice, double innerPrice, double aspPrice, double wasteFactor, double profitPercent, string outerThickness, string outerColor, string workType, string spacerThickness, string spacerType, bool includeUInsert, string innerThickness, string innerColor, out string description, out string summary)
        {
            double glassTotal = outerPrice + innerPrice;
            double step1 = glassTotal / wasteFactor;
            double step2 = step1 + aspPrice;
            double final = step2 * (1 + profitPercent / 100.0);
            string uInsertText = includeUInsert ? " with U-Insert" : "";
            description = $"{outerThickness}mm {outerColor} {workType} + {spacerThickness}mm {spacerType} ASP{uInsertText} + {innerThickness}mm {innerColor} {workType}";
            summary = $"(({outerPrice} + {innerPrice}) / {wasteFactor:F2}) + {aspPrice} = {step2:F2} × {1 + profitPercent / 100.0:F2} = {final:F2}";
            return Math.Round(final, 2);
        }
        public double CalculateLAM(double outerPrice, double pvbPrice, double innerPrice, double cutting, double tempering, double wasteFactor, double profitPercent, string outerThickness, string outerColor, string workType, string pvbThickness, string pvbColor, string innerThickness, string innerColor, out string description, out string summary)
        {
            double glassTotal = outerPrice + pvbPrice + innerPrice;
            double step1 = glassTotal / wasteFactor;
            double step2 = step1 + cutting + tempering;
            double final = step2 * (1 + profitPercent / 100.0);
            description = $"{outerThickness}mm {outerColor} {workType} + {pvbThickness}mm PVB ({pvbColor}) + {innerThickness}mm {innerColor} {workType}";
            summary = $"({outerPrice} + {pvbPrice} + {innerPrice}) / {wasteFactor:F2} + {cutting} + {tempering} = {step2:F2} × {1 + profitPercent / 100.0:F2} = {final:F2}";
            return Math.Round(final, 2);
        }
    }

    public class ProformaInvoiceViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly ExcelCsvService _excelCsvService = new ExcelCsvService();
        private readonly GlassPriceCalculator _priceCalculator = new GlassPriceCalculator();
        private static readonly object _invoiceLock = new object();
        private static int _lastGeneratedNumber;

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };
        private int _currentPINumber = 0;
        private bool _isUpdatingASPPrice = false;

        // PATCH 8: Bulk update tracking
        private bool _isBulkUpdating = false;
        public bool IsBulkUpdating
        {
            get => _isBulkUpdating;
            set
            {
                if (_isBulkUpdating != value)
                {
                    _isBulkUpdating = value;
                    OnPropertyChanged();
                }
            }
        }

        public ProformaInvoiceViewModel()
        {
            _excelCsvService.StatusChanged += status => Application.Current?.Dispatcher.Invoke(() => StatusMessage = status);
            InitializeCommands();
            LoadSavedFiles();
            CreateNewInvoice();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null) { if (Equals(field, value)) return false; field = value; OnPropertyChanged(propertyName); return true; }

        private void OnInvoicePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProformaInvoiceModel.IsDirty))
                OnPropertyChanged(nameof(HasUnsavedChanges));

            if (e.PropertyName == nameof(SpecificationModel.SpecTotalSQM) ||
                e.PropertyName == nameof(SpecificationModel.SpecTotalQty) ||
                e.PropertyName == nameof(SpecificationModel.SpecTotalSQM1) ||
                e.PropertyName == nameof(SpecificationModel.SpecTotalSQM2))
            {
                OnPropertyChanged(nameof(SpecTotalLM1));
                OnPropertyChanged(nameof(SpecTotalLM2));
            }

            // Notify all total properties when any total-related property changes
            if (e.PropertyName == nameof(ProformaInvoiceModel.GrandTotal) ||
                e.PropertyName == nameof(ProformaInvoiceModel.VatAmount) ||
                e.PropertyName == nameof(ProformaInvoiceModel.NetTotal) ||
                e.PropertyName == nameof(ProformaInvoiceModel.OtherChargesTotal) ||
                e.PropertyName == nameof(ProformaInvoiceModel.TotalSQM) ||
                e.PropertyName == nameof(ProformaInvoiceModel.TotalQty) ||
                e.PropertyName == nameof(ProformaInvoiceModel.TotalSQM1) ||
                e.PropertyName == nameof(ProformaInvoiceModel.TotalSQM2) ||
                e.PropertyName == nameof(ProformaInvoiceModel.TotalLM) ||
                e.PropertyName == nameof(ProformaInvoiceModel.TotalLM1) ||
                e.PropertyName == nameof(ProformaInvoiceModel.TotalLM2))
            {
                OnPropertyChanged(nameof(InvoiceGrandTotal));
                OnPropertyChanged(nameof(InvoiceVatAmount));
                OnPropertyChanged(nameof(InvoiceNetTotal));
                OnPropertyChanged(nameof(InvoiceOtherChargesTotal));
                OnPropertyChanged(nameof(InvoiceTotalSQM));
                OnPropertyChanged(nameof(InvoiceTotalQty));
                OnPropertyChanged(nameof(InvoiceTotalSQM1));
                OnPropertyChanged(nameof(InvoiceTotalSQM2));
                OnPropertyChanged(nameof(InvoiceTotalLM));
                OnPropertyChanged(nameof(InvoiceTotalLM1));
                OnPropertyChanged(nameof(InvoiceTotalLM2));
            }
        }

        private string GetNextSequentialInvoiceNo() { _currentPINumber++; return $"PI-{DateTime.Now.Year}-{_currentPINumber:D2}"; }

        private ProformaInvoiceModel _invoice = new();
        public ProformaInvoiceModel Invoice
        {
            get => _invoice;
            set
            {
                if (_invoice != null)
                    _invoice.PropertyChanged -= OnInvoicePropertyChanged;

                SetProperty(ref _invoice, value);

                if (_invoice != null)
                {
                    _invoice.PropertyChanged += OnInvoicePropertyChanged;
                    SubscribeToOtherChargeChanges();
                }
            }
        }

        public bool HasUnsavedChanges => Invoice?.IsDirty == true;

        // PATCH 2: IsLocked property
        public bool IsLocked => Invoice?.IsLocked == true;

        private ObservableCollection<FileListItem> _savedFiles = new();
        public ObservableCollection<FileListItem> SavedFiles { get => _savedFiles; set => SetProperty(ref _savedFiles, value); }
        public bool HasSavedFiles => SavedFiles?.Any() == true;
        private string _currentFileName = "Untitled";
        public string CurrentFileName { get => _currentFileName; set => SetProperty(ref _currentFileName, value); }
        private string _statusMessage = "Ready";
        public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }
        private bool _isLMVisible = true;
        public bool IsLMVisible { get => _isLMVisible; set { if (SetProperty(ref _isLMVisible, value)) OnPropertyChanged(nameof(IsLMToggleText)); } }
        public string IsLMToggleText => IsLMVisible ? "HIDE LM" : "SHOW LM";

        public string CompanyName { get; set; } = "PROGLASS AUTOMATION";

        // ==================== JOB ORDER CONVERSION (PATCH 1) ====================

        private bool _isJobOrder;
        public bool IsJobOrder
        {
            get => _isJobOrder;
            set
            {
                if (SetProperty(ref _isJobOrder, value))
                {
                    OnPropertyChanged(nameof(FormTitle));
                    OnPropertyChanged(nameof(InvoiceNoLabel));
                    OnPropertyChanged(nameof(IsPriceColumnVisible));
                    OnPropertyChanged(nameof(IsTotalPriceColumnVisible));
                    OnPropertyChanged(nameof(IsVatSectionVisible));
                    OnPropertyChanged(nameof(IsNetTotalVisible));
                }
            }
        }

        public string FormTitle => IsJobOrder ? "Job Order" : "Proforma Invoice";
        public string InvoiceNoLabel => IsJobOrder ? "Job Number" : "Invoice No";
        public System.Windows.Visibility IsPriceColumnVisible => IsJobOrder ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        public System.Windows.Visibility IsTotalPriceColumnVisible => IsJobOrder ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        public System.Windows.Visibility IsVatSectionVisible => IsJobOrder ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
        public System.Windows.Visibility IsNetTotalVisible => IsJobOrder ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

        private JobOrderViewModel _jobOrderVM;
        public JobOrderViewModel JobOrderVM
        {
            get => _jobOrderVM;
            set => SetProperty(ref _jobOrderVM, value);
        }

        public string CompanyTRN { get; set; } = "100458979400003";
        public string CompanyLocation { get; set; } = "Dubai, UAE";
        public string CompanyPhone { get; set; } = "+971-50-123-4567";

        private bool _isSGUSelected = true;
        public bool IsSGUSelected { get => _isSGUSelected; set { if (SetProperty(ref _isSGUSelected, value) && value) { _isDGUSelected = false; OnPropertyChanged(nameof(IsDGUSelected)); _isLAMSelected = false; OnPropertyChanged(nameof(IsLAMSelected)); } } }
        private bool _isDGUSelected;
        public bool IsDGUSelected { get => _isDGUSelected; set { if (SetProperty(ref _isDGUSelected, value) && value) { _isSGUSelected = false; OnPropertyChanged(nameof(IsSGUSelected)); _isLAMSelected = false; OnPropertyChanged(nameof(IsLAMSelected)); } } }
        private bool _isLAMSelected;
        public bool IsLAMSelected { get => _isLAMSelected; set { if (SetProperty(ref _isLAMSelected, value) && value) { _isSGUSelected = false; OnPropertyChanged(nameof(IsSGUSelected)); _isDGUSelected = false; OnPropertyChanged(nameof(IsDGUSelected)); } } }

        public ObservableCollection<string> ThicknessOptions { get; } = new() { "4", "5", "6", "8", "10", "12", "15", "19" };
        public ObservableCollection<string> ColorHistory { get; set; } = new() { "Clear", "Grey", "Green", "Blue", "Bronze", "Black" };

        public ObservableCollection<AirSpacerOption> AirSpacerOptions { get; } = new()
        {
            new() { Thickness = "6", Type = "Normal", Price = 45 }, new() { Thickness = "6", Type = "Black", Price = 50 },
            new() { Thickness = "8", Type = "Normal", Price = 45 }, new() { Thickness = "8", Type = "Black", Price = 50 },
            new() { Thickness = "10", Type = "Normal", Price = 45 }, new() { Thickness = "10", Type = "Black", Price = 50 },
            new() { Thickness = "12", Type = "Normal", Price = 45 }, new() { Thickness = "12", Type = "Black", Price = 50 },
            new() { Thickness = "14", Type = "Normal", Price = 45 }, new() { Thickness = "14", Type = "Black", Price = 50 },
            new() { Thickness = "16", Type = "Normal", Price = 50 }, new() { Thickness = "16", Type = "Black", Price = 55 },
            new() { Thickness = "18", Type = "Normal", Price = 50 }, new() { Thickness = "18", Type = "Black", Price = 55 },
            new() { Thickness = "20", Type = "Normal", Price = 55 }, new() { Thickness = "20", Type = "Black", Price = 60 },
            new() { Thickness = "22", Type = "Normal", Price = 55 }, new() { Thickness = "22", Type = "Black", Price = 60 },
            new() { Thickness = "24", Type = "Normal", Price = 60 }, new() { Thickness = "24", Type = "Black", Price = 65 }
        };

        public ObservableCollection<string> AirSpacerThicknessOptions { get; } = new() { "6", "8", "10", "12", "14", "16", "18", "20", "22", "24" };
        public ObservableCollection<string> AirSpacerTypeOptions { get; } = new() { "Normal", "Black" };
        public ObservableCollection<DimensionOption> LMDimensionOptions { get; } = new()
        {
            new() { Value = "w1h1", Label = "2×(W1+H1)" },
            new() { Value = "4w1h1", Label = "4×(W1+H1) DGU/LAM" },
            new() { Value = "2w1", Label = "2×W1" },
            new() { Value = "2h1", Label = "2×H1" },
            new() { Value = "w1_only", Label = "1×W1" },
            new() { Value = "h1_only", Label = "1×H1" },
            new() { Value = "w2h2", Label = "2×(W2+H2)" },
            new() { Value = "2w2", Label = "2×W2" },
            new() { Value = "2h2", Label = "2×H2" }
        };
        public ObservableCollection<ChargeTypeOption> ChargeTypeOptions { get; } = new()
        {
            new() { Value = "lm", Label = "LM" },
            new() { Value = "sqm", Label = "SQM" },
            new() { Value = "sqm1", Label = "SQM1" },
            new() { Value = "sqm2", Label = "SQM2" },
            new() { Value = "qty", Label = "QTY" },
            new() { Value = "1x", Label = "1X" },
            new() { Value = "2x", Label = "2X" }
        };

        private string _selectedThickness = "6";
        public string SelectedThickness { get => _selectedThickness; set => SetProperty(ref _selectedThickness, value); }
        public string SelectedColor { get; set; } = "Clear";
        public bool IsAnnealedSelected { get; set; } = true;
        public bool IsFTSelected { get; set; }
        public string WorkTypeText => IsFTSelected ? "FT Glass" : "Annealed";
        public double SGUSheetPrice { get; set; } = 150;
        public double SGUCutting { get; set; } = 10;
        public double SGUTempering { get; set; } = 10;
        public double SGUWasteFactor { get; set; } = 0.85;
        public double SGUProfitPercent { get; set; } = 15;

        public bool IsDGUAnnealedSelected { get; set; } = true;
        public bool IsDGUFTSelected { get; set; }
        public string DGUWorkTypeText => IsDGUFTSelected ? "FT Glass" : "Annealed";
        public bool IsDGUIncludeInSpec { get; set; } = true;
        public bool IsDGUInternalOnly { get; set; }
        public string DGUOuterThickness { get; set; } = "6";
        public string DGUOuterColor { get; set; } = "Clear";
        public double DGUOuterPrice { get; set; } = 100;

        private string _dGUAirSpacerThickness = "12";
        public string DGUAirSpacerThickness
        {
            get => _dGUAirSpacerThickness;
            set
            {
                if (SetProperty(ref _dGUAirSpacerThickness, value))
                {
                    if (_isASPPriceManual)
                    {
                        _isASPPriceManual = false;
                        OnPropertyChanged(nameof(IsASPPriceManual));
                        OnPropertyChanged(nameof(ASPPriceModeText));
                    }
                    UpdateAirSpacerPrice();
                }
            }
        }

        private string _dGUAirSpacerType = "Normal";
        public string DGUAirSpacerType
        {
            get => _dGUAirSpacerType;
            set
            {
                if (SetProperty(ref _dGUAirSpacerType, value))
                {
                    if (_isASPPriceManual)
                    {
                        _isASPPriceManual = false;
                        OnPropertyChanged(nameof(IsASPPriceManual));
                        OnPropertyChanged(nameof(ASPPriceModeText));
                    }
                    UpdateAirSpacerPrice();
                }
            }
        }

        private double _dGUASPPrice = 45;
        public double DGUASPPrice
        {
            get => _dGUASPPrice;
            set
            {
                if (SetProperty(ref _dGUASPPrice, value))
                {
                    if (!_isUpdatingASPPrice)
                    {
                        SetProperty(ref _isASPPriceManual, true);
                    }
                }
            }
        }

        private bool _isASPPriceManual = false;
        public bool IsASPPriceManual
        {
            get => _isASPPriceManual;
            set { SetProperty(ref _isASPPriceManual, value); OnPropertyChanged(nameof(ASPPriceModeText)); }
        }

        public string ASPPriceModeText => IsASPPriceManual ? "MANUAL ✓" : "AUTO";

        public string DGUInnerThickness { get; set; } = "6";
        public string DGUInnerColor { get; set; } = "Clear";
        public double DGUInnerPrice { get; set; } = 100;
        public double DGUWasteFactor { get; set; } = 0.85;
        public double DGUProfitPercent { get; set; } = 15;

        public bool IsLAMAnnealedSelected { get; set; } = true;
        public bool IsLAMFTSelected { get; set; }
        public string LAMWorkTypeText => IsLAMFTSelected ? "FT Glass" : "Annealed";
        public string LAMOuterThickness { get; set; } = "6";
        public string LAMOuterColor { get; set; } = "Clear";
        public double LAMOuterPrice { get; set; } = 100;
        public string LAMPVBThickness { get; set; } = "0.76";
        public string LAMPVBColor { get; set; } = "Clear";
        public double LAMPVBPrice { get; set; } = 25;
        public string LAMInnerThickness { get; set; } = "6";
        public string LAMInnerColor { get; set; } = "Clear";
        public double LAMInnerPrice { get; set; } = 100;
        public double LAMCutting { get; set; } = 10;
        public double LAMTempering { get; set; } = 10;
        public double LAMWasteFactor { get; set; } = 0.85;
        public double LAMProfitPercent { get; set; } = 15;

        private string _generatedDescription = "";
        public string GeneratedDescription { get => _generatedDescription; set => SetProperty(ref _generatedDescription, value); }
        private double _calculatedPrice;
        public double CalculatedPrice { get => _calculatedPrice; set => SetProperty(ref _calculatedPrice, value); }
        private string _priceCalculationSummary = "";
        public string PriceCalculationSummary { get => _priceCalculationSummary; set => SetProperty(ref _priceCalculationSummary, value); }

        private SpecificationModel _selectedTargetSpecification;
        public SpecificationModel SelectedTargetSpecification
        {
            get => _selectedTargetSpecification;
            set
            {
                SetProperty(ref _selectedTargetSpecification, value);
                OnPropertyChanged(nameof(SelectedSpecificationOtherCharges));
            }
        }

        public ObservableCollection<OtherChargeModel> SelectedSpecificationOtherCharges =>
            SelectedTargetSpecification?.OtherCharges ?? new ObservableCollection<OtherChargeModel>();

        private int _selectedSpecificationId;
        public int SelectedSpecificationId
        {
            get => _selectedSpecificationId;
            set
            {
                SetProperty(ref _selectedSpecificationId, value);
                if (Invoice?.Specifications != null)
                    SelectedTargetSpecification = Invoice.Specifications.FirstOrDefault(s => s.Id == value);
            }
        }

        // Currently selected charge for multi-spec editing
        private OtherChargeModel _selectedCharge;
        public OtherChargeModel SelectedCharge
        {
            get => _selectedCharge;
            set => SetProperty(ref _selectedCharge, value);
        }

        // ==================== COMMANDS ====================
        public ICommand NewInvoiceCommand { get; set; }
        public ICommand SaveInvoiceCommand { get; set; }
        public ICommand OpenInvoiceCommand { get; set; }
        public ICommand CreateJobOrderCommand { get; set; }
        public ICommand DeleteInvoiceCommand { get; private set; } = null!;
        public ICommand AddSpecificationCommand { get; private set; } = null!;
        public ICommand RemoveSpecificationCommand { get; private set; } = null!;
        public ICommand ToggleLMCommand { get; private set; } = null!;
        public ICommand CalculatePriceCommand { get; private set; } = null!;
        public ICommand IncludeInSpecificationCommand { get; private set; } = null!;
        public ICommand PrintCommand { get; private set; } = null!;
        public ICommand PasteFromExcelCommand { get; private set; } = null!;
        public ICommand SelectSGUCommand { get; private set; } = null!;
        public ICommand SelectDGUCommand { get; private set; } = null!;
        public ICommand SelectLAMCommand { get; private set; } = null!;
        public ICommand ExportCsvCommand { get; private set; } = null!;
        public ICommand ImportCsvCommand { get; private set; } = null!;
        public ICommand ImportItemsCommand { get; private set; } = null!;
        public ICommand AddOtherChargeCommand { get; private set; } = null!;
        public ICommand RemoveOtherChargeCommand { get; private set; } = null!;
        public ICommand CalculateOtherChargeCommand { get; private set; } = null!;
        public ICommand ResetASPPriceCommand { get; private set; } = null!;
        public ICommand DebugCsvCommand { get; private set; } = null!;
        public ICommand TestCsvRoundTripCommand { get; private set; } = null!;
        public ICommand AddSpecToChargeCommand { get; private set; } = null!;
        public ICommand RemoveSpecFromChargeCommand { get; private set; } = null!;
        public ICommand ToggleSpecForChargeCommand { get; private set; } = null!;
        public ICommand RefreshChargesCommand { get; private set; } = null!;

        // Event raised when invoice is saved
        public event Action<ProformaInvoiceModel>? InvoiceSaved;

        // PATCH: Event for main view to subscribe
        public event Action<ProformaInvoiceModel>? InvoiceToBeAdded;

        private void InitializeCommands()
        {
            NewInvoiceCommand = new RelayCommand(_ => NewInvoice());
            SaveInvoiceCommand = new RelayCommand(_ => SaveInvoice(), _ => CanExecuteSaveInvoice());
            OpenInvoiceCommand = new RelayCommand(_ => OpenInvoice());
            CreateJobOrderCommand = new RelayCommand(_ => ExecuteCreateJobOrder());
            DeleteInvoiceCommand = new RelayCommand(_ => DeleteInvoice());
            AddSpecificationCommand = new RelayCommand(_ => AddSpecification());
            RemoveSpecificationCommand = new RelayCommand(_ => RemoveSpecification(), _ => Invoice?.Specifications?.Count > 0);
            ToggleLMCommand = new RelayCommand(_ => ToggleLM());
            CalculatePriceCommand = new RelayCommand(_ => CalculatePrice());
            IncludeInSpecificationCommand = new RelayCommand(_ => IncludeInSpecification(), _ => CanIncludeInSpecification());
            PrintCommand = new RelayCommand(_ => PrintInvoice());
            PasteFromExcelCommand = new RelayCommand(_ => PasteFromExcel());
            SelectSGUCommand = new RelayCommand(_ => { IsSGUSelected = true; });
            SelectDGUCommand = new RelayCommand(_ => { IsDGUSelected = true; });
            SelectLAMCommand = new RelayCommand(_ => { IsLAMSelected = true; });
            ExportCsvCommand = new RelayCommand(_ => ExportToCsv());
            ImportCsvCommand = new RelayCommand(_ => ImportFromCsv());
            ImportItemsCommand = new RelayCommand(_ => ImportItemsFromCsv());
            AddOtherChargeCommand = new RelayCommand(_ => AddOtherCharge());
            RemoveOtherChargeCommand = new RelayCommand(param => RemoveOtherCharge(param as OtherChargeModel));
            CalculateOtherChargeCommand = new RelayCommand(_ => CalculateAllOtherCharges());
            ResetASPPriceCommand = new RelayCommand(_ => ResetASPPriceToAuto());
            DebugCsvCommand = new RelayCommand(_ => DebugCsvImport());
            TestCsvRoundTripCommand = new RelayCommand(_ => TestCsvRoundTrip());
            RefreshChargesCommand = new RelayCommand(_ => RefreshAllChargeAutoValues());
        }

        // ==================== BULK OPERATIONS (PATCH 8) ====================
        public IDisposable BulkUpdateScope()
        {
            return new ViewModelBulkUpdateScope(this);
        }

        private class ViewModelBulkUpdateScope : IDisposable
        {
            private readonly ProformaInvoiceViewModel _vm;

            public ViewModelBulkUpdateScope(ProformaInvoiceViewModel vm)
            {
                _vm = vm;
                _vm.IsBulkUpdating = true;
                _vm.Invoice?.BeginBulkUpdate();
            }

            public void Dispose()
            {
                _vm.Invoice?.EndBulkUpdate();
                _vm.IsBulkUpdating = false;
            }
        }

        // ==================== METHODS ====================

        private void ResetASPPriceToAuto()
        {
            IsASPPriceManual = false;
            UpdateAirSpacerPrice();
        }

        private bool CanExecuteSaveInvoice()
        {
            return Invoice != null;
        }

        private void ExecuteCreateJobOrder()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[PIViewModel] ExecuteCreateJobOrder called");

                if (Invoice == null)
                {
                    MessageBox.Show("Please create or load an invoice first.", "No Invoice",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Invoice.Specifications == null || Invoice.Specifications.Count == 0)
                {
                    MessageBox.Show("Please add at least one specification before creating job order.",
                        "No Specifications", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Set default customer name if empty
                if (string.IsNullOrWhiteSpace(Invoice.CustomerName))
                {
                    Invoice.CustomerName = "New Customer";
                }

                // Navigate to Job Order and create from PI
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainVM)
                {
                    System.Diagnostics.Debug.WriteLine("[PIViewModel] Calling mainVM.CreateJobOrderFromProformaInvoice");
                    mainVM.CreateJobOrderFromProformaInvoice(Invoice);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[PIViewModel] MainWindow or DataContext is null!");
                    MessageBox.Show($"Creating Job Order from PI: {Invoice.InvoiceNo}\nCustomer: {Invoice.CustomerName}",
                        "Create Job Order", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PIViewModel] CreateJobOrder error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                MessageBox.Show($"Failed to create job order: {ex.Message}\n\n{ex.StackTrace}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateAirSpacerPrice()
        {
            if (_isASPPriceManual) return;
            _isUpdatingASPPrice = true;
            var option = AirSpacerOptions.FirstOrDefault(x => x.Thickness == _dGUAirSpacerThickness && x.Type == _dGUAirSpacerType);
            if (option != null)
            {
                _dGUASPPrice = option.Price;
                OnPropertyChanged(nameof(DGUASPPrice));
            }
            _isUpdatingASPPrice = false;
        }

        public void CreateNewInvoice()
        {
            Invoice = new ProformaInvoiceModel
            {
                InvoiceNo = GetNextSequentialInvoiceNo(),
                InvoiceDate = DateTime.Now,
                ValidUntil = DateTime.Now.AddDays(2)
            };
            CurrentFileName = "Untitled";

            AddSpecification();
            if (Invoice.Specifications.Count > 0)
                Invoice.Specifications[0].Invoice = Invoice;
        }

        private void NewInvoice()
        {
            if (Invoice.IsDirty)
            {
                var result = MessageBox.Show("Save changes before creating new invoice?", "Save Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) SaveInvoice();
                else if (result == MessageBoxResult.Cancel) return;
            }
            CreateNewInvoice();
            StatusMessage = "✅ New invoice created";
        }

        private void SaveInvoice()
        {
            try
            {
                // Auto-set default customer if empty
                if (string.IsNullOrWhiteSpace(Invoice?.CustomerName))
                {
                    Invoice!.CustomerName = "New Customer";
                }

                Invoice.CalculateTotals();

                // PATCH: Add to main view model's list FIRST
                AddToMainViewModelList();

                // Then save to individual file (optional - for backup)
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    InitialDirectory = GetDataFolder(),
                    FileName = $"{Invoice.InvoiceNo}.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    string json = JsonConvert.SerializeObject(Invoice, Formatting.Indented, _jsonSettings);
                    File.WriteAllText(dialog.FileName, json);
                    CurrentFileName = Path.GetFileNameWithoutExtension(dialog.FileName);
                    LoadSavedFiles();
                }

                Invoice.IsDirty = false;
                StatusMessage = $"✅ Saved: {CurrentFileName}";

                // Raise event for other views to update
                System.Diagnostics.Debug.WriteLine($"[ProformaInvoice] About to raise InvoiceSaved event. InvoiceNo={Invoice?.InvoiceNo}");
                InvoiceSaved?.Invoke(Invoice);
                System.Diagnostics.Debug.WriteLine($"[ProformaInvoice] InvoiceSaved event raised");

                // Check and convert to Job Order if status is Confirmed
                CheckAndConvertToJobOrder();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // PATCH: Add current invoice to main view model's list
        private void AddToMainViewModelList()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PIViewModel] About to raise InvoiceToBeAdded event for: {Invoice?.InvoiceNo}");

                // Raise event for main view to handle
                InvoiceToBeAdded?.Invoke(Invoice);

                System.Diagnostics.Debug.WriteLine($"[PIViewModel] InvoiceToBeAdded event raised");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PIViewModel] AddToMainViewModelList error: {ex.Message}");
            }
        }

        private void OpenInvoice()
        {
            try
            {
                var dialog = new OpenFileDialog { Filter = "JSON Files (*.json)|*.json", InitialDirectory = GetDataFolder() };
                if (dialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var invoice = JsonConvert.DeserializeObject<ProformaInvoiceModel>(json, _jsonSettings);
                    if (invoice != null)
                    {
                        Invoice = invoice;
                        CurrentFileName = Path.GetFileNameWithoutExtension(dialog.FileName);

                        // PATCH: Add to main view model's list
                        AddToMainViewModelList();

                        // PATCH: Reconstruct after load
                        ReconstructAfterLoad();

                        Invoice.CalculateTotals();
                        StatusMessage = $"✅ Opened: {CurrentFileName}";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteInvoice()
        {
            var result = MessageBox.Show($"Delete '{CurrentFileName}'?\nThis cannot be undone.", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string filePath = Path.Combine(GetDataFolder(), $"{CurrentFileName}.json");
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        LoadSavedFiles();
                        StatusMessage = $"✅ Deleted: {CurrentFileName}";
                        CreateNewInvoice();
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = $"❌ Delete failed: {ex.Message}";
                }
            }
        }

        private string GetDataFolder()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }

        private void LoadSavedFiles()
        {
            SavedFiles.Clear();
            string folder = GetDataFolder();
            if (!Directory.Exists(folder)) return;

            foreach (var file in Directory.GetFiles(folder, "*.json").OrderByDescending(f => new FileInfo(f).LastWriteTime))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var invoice = JsonConvert.DeserializeObject<ProformaInvoiceModel>(json, _jsonSettings);
                    if (invoice != null)
                        SavedFiles.Add(new FileListItem
                        {
                            FilePath = file,
                            InvoiceNo = invoice.InvoiceNo,
                            CustomerName = invoice.CustomerName,
                            InvoiceDate = invoice.InvoiceDate
                        });
                }
                catch { }
            }
            OnPropertyChanged(nameof(HasSavedFiles));
        }

        private void PrintInvoice()
        {
            try
            {
                var window = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.DataContext == this);
                if (window?.Content is System.Windows.Media.Visual visual)
                {
                    var printDialog = new System.Windows.Controls.PrintDialog();
                    if (printDialog.ShowDialog() == true)
                    {
                        printDialog.PrintVisual(visual, "ProForma Invoice");
                        StatusMessage = "✅ Printed successfully";
                    }
                }
                else
                {
                    StatusMessage = "❌ Cannot find window to print";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Print failed: {ex.Message}";
            }
        }

        // ==================== SPECIFICATIONS ====================

        private void AddSpecification()
        {
            int nextSrNo = GetNextSrNo();
            var spec = new SpecificationModel
            {
                SpecificationName = $"Specification {Invoice.Specifications.Count + 1}",
                Id = Invoice.Specifications.Count,
                Invoice = Invoice
            };

            if (spec.Items.Count == 0)
            {
                var firstItem = new InvoiceItemModel
                {
                    SrNo = nextSrNo,
                    SurchargePercent = 20,
                    Specification = spec
                };
                spec.Items.Add(firstItem);
            }

            Invoice.Specifications.Add(spec);

            // Keep Other Charges section visible - only auto-select if no spec was selected
            if (SelectedTargetSpecification == null)
            {
                SelectedTargetSpecification = spec;
            }

            SubscribeToOtherChargeChanges();
            RefreshAllChargeAutoValues();
        }

        private void RemoveSpecification()
        {
            if (Invoice.Specifications.Count <= 0) return;
            Invoice.Specifications.RemoveAt(Invoice.Specifications.Count - 1);
            SelectedTargetSpecification = Invoice.Specifications.LastOrDefault();
            RenumberAllSrNumbers();
            RefreshAllChargeAutoValues();
        }

        // ==================== SR NUMBERING (PATCH 9) ====================

        public int GetNextSrNo()
        {
            int maxSr = 0;
            if (Invoice?.Specifications == null) return 1;

            foreach (var spec in Invoice.Specifications)
            {
                foreach (var item in spec.Items)
                {
                    if (item.SrNo > maxSr)
                        maxSr = item.SrNo;
                }
            }
            return maxSr + 1;
        }

        public void RenumberAllSrNumbers()
        {
            if (Invoice?.Specifications == null) return;

            int srNo = 1;
            foreach (var spec in Invoice.Specifications)
            {
                foreach (var item in spec.Items)
                {
                    item.SrNo = srNo;
                    srNo++;
                }
            }
        }

        public void RefreshAllChargeAutoValues()
        {
            if (Invoice?.Specifications == null) return;

            foreach (var spec in Invoice.Specifications)
            {
                if (spec?.OtherCharges == null) continue;

                foreach (var charge in spec.OtherCharges)
                {
                    if (charge != null)
                    {
                        charge.BoundSpecs = Invoice.Specifications.ToList();
                        UpdateChargeValue(charge);
                    }
                }
            }

            Invoice.CalculateTotals();
        }

        // ==================== PATCH 3: RECONSTRUCT AFTER LOAD ====================
        public void ReconstructAfterLoad()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[PIViewModel] Reconstructing after load...");

                // 1. Rebuild Invoice-Specification relationships
                if (Invoice?.Specifications != null)
                {
                    foreach (var spec in Invoice.Specifications)
                    {
                        spec.Invoice = Invoice;
                    }
                }

                // 2. Rebuild Specification-Item relationships
                if (Invoice?.Specifications != null)
                {
                    foreach (var spec in Invoice.Specifications)
                    {
                        foreach (var item in spec.Items)
                        {
                            item.Specification = spec;
                        }
                    }
                }

                // 3. Reattach event handlers
                AttachAllEventHandlers();

                // 4. Rebuild OtherCharge relationships
                if (Invoice?.Specifications != null)
                {
                    foreach (var spec in Invoice.Specifications)
                    {
                        if (spec.OtherCharges != null)
                        {
                            foreach (var charge in spec.OtherCharges)
                            {
                                charge.BoundSpecs = Invoice.Specifications.ToList();
                            }
                        }
                    }
                }

                // 5. Refresh charge auto values
                RefreshAllChargeAutoValues();

                // 6. Renumber SR numbers
                RenumberAllSrNumbers();

                // 7. Recalculate all totals
                Invoice?.CalculateTotals();

                System.Diagnostics.Debug.WriteLine("[PIViewModel] Reconstruction complete");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PIViewModel] Reconstruction error: {ex.Message}");
            }
        }

        // ==================== PATCH 6: ATTACH ALL HANDLERS ====================
        private void AttachAllEventHandlers()
        {
            // Detach first to prevent duplicates
            DetachAllEventHandlers();

            if (Invoice == null) return;

            // Attach to Invoice
            Invoice.PropertyChanged -= OnInvoicePropertyChanged;
            Invoice.PropertyChanged += OnInvoicePropertyChanged;

            // Attach to Specifications
            if (Invoice.Specifications != null)
            {
                foreach (var spec in Invoice.Specifications)
                {
                    AttachSpecificationHandlers(spec);
                }

                // Attach to Specification collection changes
                Invoice.Specifications.CollectionChanged -= Specifications_CollectionChanged;
                Invoice.Specifications.CollectionChanged += Specifications_CollectionChanged;
            }
        }

        private void AttachSpecificationHandlers(SpecificationModel spec)
        {
            if (spec == null) return;

            spec.PropertyChanged -= Spec_PropertyChanged;
            spec.PropertyChanged += Spec_PropertyChanged;

            // Items
            if (spec.Items != null)
            {
                spec.Items.CollectionChanged -= SpecItems_CollectionChanged;
                spec.Items.CollectionChanged += SpecItems_CollectionChanged;

                foreach (var item in spec.Items)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }

            // Other Charges
            if (spec.OtherCharges != null)
            {
                spec.OtherCharges.CollectionChanged -= OtherCharges_CollectionChanged;
                spec.OtherCharges.CollectionChanged += OtherCharges_CollectionChanged;

                foreach (var charge in spec.OtherCharges)
                {
                    charge.PropertyChanged -= Charge_PropertyChanged;
                    charge.PropertyChanged += Charge_PropertyChanged;
                }
            }
        }

        private void DetachAllEventHandlers()
        {
            if (Invoice == null) return;

            // Detach Invoice
            Invoice.PropertyChanged -= OnInvoicePropertyChanged;

            if (Invoice.Specifications != null)
            {
                Invoice.Specifications.CollectionChanged -= Specifications_CollectionChanged;

                foreach (var spec in Invoice.Specifications)
                {
                    DetachSpecificationHandlers(spec);
                }
            }
        }

        private void DetachSpecificationHandlers(SpecificationModel spec)
        {
            if (spec == null) return;

            spec.PropertyChanged -= Spec_PropertyChanged;

            if (spec.Items != null)
            {
                spec.Items.CollectionChanged -= SpecItems_CollectionChanged;
                foreach (var item in spec.Items)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }

            if (spec.OtherCharges != null)
            {
                spec.OtherCharges.CollectionChanged -= OtherCharges_CollectionChanged;
                foreach (var charge in spec.OtherCharges)
                {
                    charge.PropertyChanged -= Charge_PropertyChanged;
                }
            }
        }

        // ==================== PATCH 6: EVENT HANDLERS ====================
        private void Specifications_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (SpecificationModel spec in e.NewItems)
                {
                    AttachSpecificationHandlers(spec);
                }
            }

            if (e.OldItems != null)
            {
                foreach (SpecificationModel spec in e.OldItems)
                {
                    DetachSpecificationHandlers(spec);
                }
            }

            Invoice?.CalculateTotals();
            Invoice.IsDirty = true;
        }

        private void Spec_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is SpecificationModel spec)
            {
                if (e.PropertyName == nameof(SpecificationModel.SpecTotalSQM) ||
                    e.PropertyName == nameof(SpecificationModel.SpecTotalQty) ||
                    e.PropertyName == nameof(SpecificationModel.SpecTotalSQM1) ||
                    e.PropertyName == nameof(SpecificationModel.SpecTotalSQM2) ||
                    e.PropertyName == nameof(SpecificationModel.SpecTotalLM) ||
                    e.PropertyName == nameof(SpecificationModel.SpecTotalPrice) ||
                    e.PropertyName == nameof(SpecificationModel.OtherChargesTotal))
                {
                    Invoice?.CalculateTotals();
                    Invoice.IsDirty = true;
                }
            }
        }

        private void SpecItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (InvoiceItemModel item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (InvoiceItemModel item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }

            Invoice?.CalculateTotals();
            Invoice.IsDirty = true;
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is InvoiceItemModel item && item.Specification != null)
            {
                item.Specification.CalculateTotals();
                Invoice?.CalculateTotals();
                Invoice.IsDirty = true;
            }
        }

        private void OtherCharges_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (OtherChargeModel charge in e.NewItems)
                {
                    charge.PropertyChanged += Charge_PropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (OtherChargeModel charge in e.OldItems)
                {
                    charge.PropertyChanged -= Charge_PropertyChanged;
                }
            }

            Invoice?.CalculateTotals();
            Invoice.IsDirty = true;
        }

        private void Charge_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is OtherChargeModel charge)
            {
                if (e.PropertyName == nameof(OtherChargeModel.Type) ||
                    e.PropertyName == nameof(OtherChargeModel.Rate) ||
                    e.PropertyName == nameof(OtherChargeModel.LinkedSpecIndices) ||
                    e.PropertyName == nameof(OtherChargeModel.Value))
                {
                    UpdateChargeValue(charge);
                    CalculateOtherChargeValue(charge, null);

                    if (SelectedTargetSpecification != null)
                    {
                        SelectedTargetSpecification.CalculateOtherChargesTotal();
                        Invoice.CalculateTotals();
                        Invoice.IsDirty = true;

                        // Notify all total properties
                        OnPropertyChanged(nameof(InvoiceGrandTotal));
                        OnPropertyChanged(nameof(InvoiceVatAmount));
                        OnPropertyChanged(nameof(InvoiceNetTotal));
                        OnPropertyChanged(nameof(InvoiceOtherChargesTotal));
                        OnPropertyChanged(nameof(InvoiceTotalSQM));
                        OnPropertyChanged(nameof(InvoiceTotalQty));
                        OnPropertyChanged(nameof(InvoiceTotalSQM1));
                        OnPropertyChanged(nameof(InvoiceTotalSQM2));
                        OnPropertyChanged(nameof(InvoiceTotalLM));
                        OnPropertyChanged(nameof(InvoiceTotalLM1));
                        OnPropertyChanged(nameof(InvoiceTotalLM2));
                    }
                }
            }
        }

        public void AddItemWithPrice(SpecificationModel spec)
        {
            if (spec == null) return;

            int nextSrNo = GetNextSrNo();
            var newItem = spec.AddItem(nextSrNo);
            newItem.Price = spec.BasePrice;
            newItem.SurchargePercent = spec.SurchargePercent;

            // PATCH 9: Trigger automatic renumbering of all specs
            spec.RenumberItems();
            Invoice.IsDirty = true;
        }

        public void RemoveItem(InvoiceItemModel item)
        {
            if (item == null) return;
            var spec = Invoice.Specifications.FirstOrDefault(s => s.Items.Contains(item));
            if (spec != null && spec.Items.Count > 1)
            {
                spec.Items.Remove(item);
                RenumberAllSrNumbers();
                Invoice.IsDirty = true;
            }
        }

        private void ToggleLM() => IsLMVisible = !IsLMVisible;

        private void CalculatePrice()
        {
            if (IsSGUSelected) CalculateSGUPrice();
            else if (IsDGUSelected) CalculateDGUPrice();
            else if (IsLAMSelected) CalculateLAMPrice();
        }

        private void CalculateSGUPrice()
        {
            CalculatedPrice = _priceCalculator.CalculateSGU(
                SGUSheetPrice, SGUCutting, SGUTempering, SGUWasteFactor, SGUProfitPercent,
                SelectedThickness, SelectedColor, WorkTypeText,
                out string description, out string summary);

            GeneratedDescription = description;
            PriceCalculationSummary = summary;
        }

        private void CalculateDGUPrice()
        {
            CalculatedPrice = _priceCalculator.CalculateDGU(
                DGUOuterPrice, DGUInnerPrice, DGUASPPrice, DGUWasteFactor, DGUProfitPercent,
                DGUOuterThickness, DGUOuterColor, DGUWorkTypeText,
                DGUAirSpacerThickness, DGUAirSpacerType, IsDGUIncludeInSpec,
                DGUInnerThickness, DGUInnerColor,
                out string description, out string summary);

            GeneratedDescription = description;
            PriceCalculationSummary = summary;
        }

        private void CalculateLAMPrice()
        {
            CalculatedPrice = _priceCalculator.CalculateLAM(
                LAMOuterPrice, LAMPVBPrice, LAMInnerPrice, LAMCutting, LAMTempering,
                LAMWasteFactor, LAMProfitPercent,
                LAMOuterThickness, LAMOuterColor, LAMWorkTypeText,
                LAMPVBThickness, LAMPVBColor, LAMInnerThickness, LAMInnerColor,
                out string description, out string summary);

            GeneratedDescription = description;
            PriceCalculationSummary = summary;

            // Update selected spec's generated description immediately
            if (SelectedTargetSpecification != null && IsLAMSelected)
            {
                SelectedTargetSpecification.PVBThickness = LAMPVBThickness;
                SelectedTargetSpecification.PVBColor = LAMPVBColor;
                SelectedTargetSpecification.PVBPrice = LAMPVBPrice.ToString();
                SelectedTargetSpecification.SpecificationName = description;
                OnPropertyChanged(nameof(GeneratedDescription));
            }
        }

        private bool CanIncludeInSpecification() => CalculatedPrice > 0 && SelectedTargetSpecification != null;

        private void IncludeInSpecification()
        {
            if (SelectedTargetSpecification == null)
            {
                MessageBox.Show("Please select a target specification!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CalculatedPrice <= 0)
            {
                MessageBox.Show("Please calculate price first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (IsSGUSelected)
            {
                SelectedTargetSpecification.ModuleType = "SGU";
                SelectedTargetSpecification.WorkType = WorkTypeText;
                SelectedTargetSpecification.OuterThickness = SelectedThickness;
                SelectedTargetSpecification.OuterColor = SelectedColor;
            }
            else if (IsDGUSelected)
            {
                SelectedTargetSpecification.ModuleType = "DGU";
                SelectedTargetSpecification.WorkType = DGUWorkTypeText;
                SelectedTargetSpecification.IncludeInSpec = IsDGUIncludeInSpec;
                SelectedTargetSpecification.OuterThickness = DGUOuterThickness;
                SelectedTargetSpecification.OuterColor = DGUOuterColor;
                SelectedTargetSpecification.OuterPrice = DGUOuterPrice.ToString();
                SelectedTargetSpecification.SpacerThickness = $"{DGUAirSpacerThickness}mm {DGUAirSpacerType}";
                SelectedTargetSpecification.ASPPrice = DGUASPPrice.ToString();
                SelectedTargetSpecification.InnerThickness = DGUInnerThickness;
                SelectedTargetSpecification.InnerColor = DGUInnerColor;
                SelectedTargetSpecification.InnerPrice = DGUInnerPrice.ToString();
            }
            else if (IsLAMSelected)
            {
                SelectedTargetSpecification.ModuleType = "LAM";
                SelectedTargetSpecification.WorkType = LAMWorkTypeText;
                SelectedTargetSpecification.OuterThickness = LAMOuterThickness;
                SelectedTargetSpecification.OuterColor = LAMOuterColor;
                SelectedTargetSpecification.OuterPrice = LAMOuterPrice.ToString();
                SelectedTargetSpecification.InnerThickness = LAMInnerThickness;
                SelectedTargetSpecification.InnerColor = LAMInnerColor;
                SelectedTargetSpecification.InnerPrice = LAMInnerPrice.ToString();

                // PVB Layer - critical for Lamination
                SelectedTargetSpecification.PVBThickness = LAMPVBThickness;
                SelectedTargetSpecification.PVBColor = LAMPVBColor;
                SelectedTargetSpecification.PVBPrice = LAMPVBPrice.ToString();

                // Trigger recalculation of GeneratedDescription for LAM
                OnPropertyChanged(nameof(GeneratedDescription));
            }

            SelectedTargetSpecification.SpecificationName = GeneratedDescription;
            SelectedTargetSpecification.BasePrice = CalculatedPrice;

            if (SelectedTargetSpecification.Items.Count > 0)
            {
                SelectedTargetSpecification.SurchargePercent = SelectedTargetSpecification.Items[0].SurchargePercent;
                SelectedTargetSpecification.Items[0].Price = CalculatedPrice;
                foreach (var item in SelectedTargetSpecification.Items)
                {
                    if (item.Price == 0) item.Price = CalculatedPrice;
                }
            }

            Invoice.CalculateTotals();
            Invoice.IsDirty = true;
            StatusMessage = $"✅ Applied '{GeneratedDescription}' @ AED {CalculatedPrice:N2}";
        }

        // ==================== OTHER CHARGES ====================

        private void SubscribeToOtherChargeChanges()
        {
            if (Invoice?.Specifications == null) return;

            foreach (var spec in Invoice.Specifications)
            {
                if (spec?.OtherCharges == null) continue;

                foreach (var charge in spec.OtherCharges)
                {
                    if (charge != null)
                    {
                        charge.PropertyChanged += OtherCharge_PropertyChanged;
                        charge.BoundSpecs = Invoice.Specifications.ToList();
                    }
                }
            }
        }

        private void OtherCharge_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not OtherChargeModel charge) return;

            if (e.PropertyName == nameof(OtherChargeModel.Type) ||
                e.PropertyName == nameof(OtherChargeModel.LinkedSpecIndices) ||
                e.PropertyName == nameof(OtherChargeModel.Rate))
            {
                UpdateChargeValue(charge);
                CalculateOtherChargeValue(charge, null);
            }

            if (SelectedTargetSpecification != null)
            {
                SelectedTargetSpecification.CalculateOtherChargesTotal();
                Invoice.CalculateTotals();
                Invoice.IsDirty = true;
            }
        }

        private void AddOtherCharge()
        {
            if (SelectedTargetSpecification == null)
            {
                MessageBox.Show("Please select a specification first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int specIndex = Invoice.Specifications.IndexOf(SelectedTargetSpecification);
            var charge = new OtherChargeModel
            {
                Name = "New Charge",
                Type = "lm",
                Value = 0,
                Rate = 0,
                Amount = 0,
                LinkedSpecIndex = specIndex,
                LinkedSpecIndices = specIndex.ToString()
            };

            // Bind specs for auto-calculation
            charge.BoundSpecs = Invoice.Specifications.ToList();

            SelectedTargetSpecification.OtherCharges.Add(charge);
            charge.PropertyChanged += OtherCharge_PropertyChanged;
            Invoice.IsDirty = true;
            OnPropertyChanged(nameof(SelectedSpecificationOtherCharges));

            SelectedCharge = charge;
            UpdateChargeValue(charge);
        }

        private void RemoveOtherCharge(OtherChargeModel? charge)
        {
            if (charge == null || SelectedTargetSpecification == null) return;

            charge.PropertyChanged -= OtherCharge_PropertyChanged;
            SelectedTargetSpecification.OtherCharges.Remove(charge);
            SelectedTargetSpecification.CalculateOtherChargesTotal();
            Invoice.CalculateTotals();
            Invoice.IsDirty = true;
            OnPropertyChanged(nameof(SelectedSpecificationOtherCharges));
        }

        private void CalculateAllOtherCharges()
        {
            if (SelectedTargetSpecification == null) return;

            foreach (var charge in SelectedTargetSpecification.OtherCharges)
            {
                var linkedSpecs = GetLinkedSpecifications(charge);
                CalculateOtherChargeValue(charge, linkedSpecs.FirstOrDefault());
            }

            SelectedTargetSpecification.CalculateOtherChargesTotal();
            Invoice.CalculateTotals();
            Invoice.IsDirty = true;

            // Notify all total properties
            OnPropertyChanged(nameof(InvoiceGrandTotal));
            OnPropertyChanged(nameof(InvoiceVatAmount));
            OnPropertyChanged(nameof(InvoiceNetTotal));
            OnPropertyChanged(nameof(InvoiceOtherChargesTotal));
            OnPropertyChanged(nameof(InvoiceTotalSQM));
            OnPropertyChanged(nameof(InvoiceTotalQty));
            OnPropertyChanged(nameof(InvoiceTotalSQM1));
            OnPropertyChanged(nameof(InvoiceTotalSQM2));
            OnPropertyChanged(nameof(InvoiceTotalLM));
            OnPropertyChanged(nameof(InvoiceTotalLM1));
            OnPropertyChanged(nameof(InvoiceTotalLM2));
        }

        private void UpdateChargeValue(OtherChargeModel charge)
        {
            if (charge == null) return;

            var linkedSpecs = GetLinkedSpecifications(charge);
            if (linkedSpecs.Count == 0) return;

            switch (charge.Type?.ToLower())
            {
                case "lm":
                    charge.Value = CalculateTotalLMValue(linkedSpecs, "w1h1");
                    break;
                case "sqm":
                    charge.Value = CalculateTotalSQMValue(linkedSpecs);
                    break;
                case "sqm1":
                    charge.Value = CalculateTotalSQM1Value(linkedSpecs);
                    break;
                case "sqm2":
                    charge.Value = CalculateTotalSQM2Value(linkedSpecs);
                    break;
                case "qty":
                case "1x":
                    charge.Value = CalculateTotalQtyValue(linkedSpecs);
                    break;
                case "2x":
                    charge.Value = CalculateTotalQtyValue(linkedSpecs) * 2;
                    break;
                default:
                    charge.Value = 0;
                    break;
            }
        }

        private void CalculateOtherChargeValue(OtherChargeModel charge, SpecificationModel spec)
        {
            if (charge == null) return;

            switch (charge.Type?.ToLower())
            {
                case "lm":
                case "sqm":
                case "qty":
                case "1x":
                case "2x":
                    charge.Amount = Math.Round(charge.Value * charge.Rate, 2);
                    break;
                default:
                    charge.Amount = 0;
                    break;
            }
        }

        private List<SpecificationModel> GetLinkedSpecifications(OtherChargeModel charge)
        {
            var specs = new List<SpecificationModel>();
            if (Invoice?.Specifications == null) return specs;

            // Parse comma-separated indices from LinkedSpecIndices
            if (!string.IsNullOrEmpty(charge.LinkedSpecIndices))
            {
                var indices = charge.LinkedSpecIndices
                    .Split(',')
                    .Select(s => int.TryParse(s.Trim(), out int idx) ? idx : -1)
                    .Where(idx => idx >= 0 && idx < Invoice.Specifications.Count)
                    .ToList();

                foreach (var idx in indices)
                    specs.Add(Invoice.Specifications[idx]);
            }
            else if (charge.LinkedSpecIndex >= 0 && charge.LinkedSpecIndex < Invoice.Specifications.Count)
            {
                specs.Add(Invoice.Specifications[charge.LinkedSpecIndex]);
            }

            if (specs.Count == 0 && SelectedTargetSpecification != null)
                specs.Add(SelectedTargetSpecification);

            return specs;
        }

        private double CalculateTotalLMValue(List<SpecificationModel> specs, string dimType)
        {
            double totalLM = 0;
            foreach (var spec in specs)
            {
                int multiplier = GetModuleMultiplier(spec.ModuleType);
                foreach (var item in spec.Items)
                {
                    double rowLM = CalculateRowLM(item, dimType);
                    totalLM += rowLM * item.Qty * multiplier;
                }
            }
            return Math.Round(totalLM, 4);
        }

        private double CalculateTotalSQMValue(List<SpecificationModel> specs)
        {
            double totalSQM = 0;
            foreach (var spec in specs)
                totalSQM += spec.SpecTotalSQM;
            return Math.Round(totalSQM, 4);
        }

        private double CalculateTotalQtyValue(List<SpecificationModel> specs)
        {
            double totalQty = 0;
            foreach (var spec in specs)
                totalQty += spec.SpecTotalQty;
            return totalQty;
        }

        private double CalculateTotalSQM1Value(List<SpecificationModel> specs)
        {
            double totalSQM1 = 0;
            foreach (var spec in specs)
                totalSQM1 += spec.SpecTotalSQM1;
            return Math.Round(totalSQM1, 4);
        }

        private double CalculateTotalSQM2Value(List<SpecificationModel> specs)
        {
            double totalSQM2 = 0;
            foreach (var spec in specs)
                totalSQM2 += spec.SpecTotalSQM2;
            return Math.Round(totalSQM2, 4);
        }

        private double CalculateRowLM(InvoiceItemModel item, string dimType)
        {
            double w1 = item.Width1 / 1000.0, h1 = item.Height1 / 1000.0;
            double w2 = item.Width2 / 1000.0, h2 = item.Height2 / 1000.0;

            return dimType switch
            {
                "w1h1" => 2 * (w1 + h1),
                "4w1h1" => 4 * (w1 + h1),
                "2w1" => 2 * w1,
                "2h1" => 2 * h1,
                "w1_only" => w1,
                "h1_only" => h1,
                "w2h2" => 2 * (w2 + h2),
                "2w2" => 2 * w2,
                "2h2" => 2 * h2,
                "w2_only" => w2,
                "h2_only" => h2,
                _ => 2 * (w1 + h1)
            };
        }

        private int GetModuleMultiplier(string moduleType) => moduleType switch
        {
            "DGU" => 2,
            "LAM" => 2,
            _ => 1
        };

        // ==================== MULTI-SPEC CHARGE MANAGEMENT ====================

        private void AddSpecToCharge(object? param)
        {
            var charge = param as OtherChargeModel ?? SelectedCharge;
            if (charge == null || Invoice?.Specifications == null) return;

            var availableIndices = new List<int>();
            for (int i = 0; i < Invoice.Specifications.Count; i++)
            {
                var existingIndices = string.IsNullOrEmpty(charge.LinkedSpecIndices)
                    ? new List<int>()
                    : charge.LinkedSpecIndices.Split(',').Select(s => int.TryParse(s.Trim(), out int idx) ? idx : -1).Where(idx => idx >= 0).ToList();

                if (!existingIndices.Contains(i))
                    availableIndices.Add(i);
            }

            if (availableIndices.Count == 0)
            {
                MessageBox.Show("All specifications are already linked to this charge!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var specsText = string.Join("\n", availableIndices.Select(i => $"  {i}: {Invoice.Specifications[i].SpecificationName}"));

            var input = Interaction.InputBox(
                $"Available Specifications:\n{specsText}\n\nEnter spec index to add (e.g., 0, 1, 2):",
                "Add Spec to Charge", "");

            if (string.IsNullOrWhiteSpace(input)) return;

            var newIndices = input.Split(',')
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out int idx) && idx >= 0 && idx < Invoice.Specifications.Count && availableIndices.Contains(idx))
                .Select(s => int.Parse(s))
                .ToList();

            if (newIndices.Count == 0) return;

            var existing = string.IsNullOrEmpty(charge.LinkedSpecIndices)
                ? new List<string>()
                : charge.LinkedSpecIndices.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

            var allIndices = existing.Union(newIndices.Select(i => i.ToString())).ToList();
            charge.LinkedSpecIndices = string.Join(",", allIndices);

            UpdateChargeValue(charge);
            Invoice.CalculateTotals();
        }

        private void RemoveSpecFromCharge()
        {
            if (SelectedCharge == null || Invoice?.Specifications == null) return;

            if (string.IsNullOrEmpty(SelectedCharge.LinkedSpecIndices))
            {
                MessageBox.Show("No specifications linked to this charge!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var linkedIndices = SelectedCharge.LinkedSpecIndices.Split(',')
                .Where(s => !string.IsNullOrWhiteSpace(s) && int.TryParse(s.Trim(), out int idx))
                .Select(s => int.Parse(s.Trim()))
                .ToList();

            if (linkedIndices.Count <= 1)
            {
                MessageBox.Show("Charge must target at least one specification!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var linkedText = string.Join("\n", linkedIndices.Select(i => $"  {i}: {Invoice.Specifications[i].SpecificationName}"));

            var input = Interaction.InputBox(
                $"Currently Linked Specifications:\n{linkedText}\n\nEnter spec index to remove:",
                "Remove Spec from Charge", "");

            if (string.IsNullOrWhiteSpace(input)) return;

            var toRemove = input.Split(',')
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out int idx) && linkedIndices.Contains(idx))
                .Select(s => int.Parse(s))
                .ToHashSet();

            var remaining = linkedIndices.Where(i => !toRemove.Contains(i)).ToList();

            if (remaining.Count == 0)
            {
                MessageBox.Show("Charge must target at least one specification!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedCharge.LinkedSpecIndices = string.Join(",", remaining);
            UpdateChargeValue(SelectedCharge);
            Invoice.CalculateTotals();
        }

        private void ToggleSpecForCharge(object? param)
        {
            var charge = param as OtherChargeModel ?? SelectedCharge;
            if (charge == null || Invoice?.Specifications == null) return;

            var allSpecs = string.Join("\n", Invoice.Specifications.Select((s, i) =>
            {
                bool isLinked = string.IsNullOrEmpty(charge.LinkedSpecIndices)
                    ? false
                    : charge.LinkedSpecIndices.Split(',').Any(idx => int.TryParse(idx.Trim(), out int parsedIdx) && parsedIdx == i);
                string check = isLinked ? "[X]" : "[ ]";
                return $"  {check} {i}: {s.SpecificationName}";
            }));

            var input = Interaction.InputBox(
                $"Toggle specifications for this charge:\n{allSpecs}\n\nEnter indices to toggle (e.g., 0,2,3):",
                "Toggle Specs for Charge", "");

            if (string.IsNullOrWhiteSpace(input)) return;

            var toToggle = input.Split(',')
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out int idx) && idx >= 0 && idx < Invoice.Specifications.Count)
                .Select(s => int.Parse(s))
                .ToHashSet();

            if (toToggle.Count == 0) return;

            var current = string.IsNullOrEmpty(charge.LinkedSpecIndices)
                ? new HashSet<int>()
                : charge.LinkedSpecIndices.Split(',')
                    .Where(s => !string.IsNullOrWhiteSpace(s) && int.TryParse(s.Trim(), out int idx))
                    .Select(s => int.Parse(s.Trim()))
                    .ToHashSet();

            foreach (var idx in toToggle)
            {
                if (current.Contains(idx))
                    current.Remove(idx);
                else
                    current.Add(idx);
            }

            if (current.Count == 0)
            {
                MessageBox.Show("Charge must target at least one specification!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            charge.LinkedSpecIndices = string.Join(",", current.OrderBy(x => x));
            UpdateChargeValue(charge);
            Invoice.CalculateTotals();
        }

        // ==================== LM TOTALS ====================
        public double SpecTotalLM1 => SelectedTargetSpecification?.Items?.Sum(x => x.LM1 * x.Qty) ?? 0;
        public double SpecTotalLM2 => SelectedTargetSpecification?.Items?.Sum(x => x.LM2 * x.Qty) ?? 0;

        // ==================== INVOICE TOTALS ====================

        public double InvoiceGrandTotal => Invoice?.GrandTotal ?? 0;
        public double InvoiceVatAmount => Invoice?.VatAmount ?? 0;
        public double InvoiceNetTotal => Invoice?.NetTotal ?? 0;
        public double InvoiceOtherChargesTotal => Invoice?.OtherChargesTotal ?? 0;
        public double InvoiceTotalSQM => Invoice?.TotalSQM ?? 0;
        public int InvoiceTotalQty => Invoice?.TotalQty ?? 0;
        public double InvoiceTotalSQM1 => Invoice?.TotalSQM1 ?? 0;
        public double InvoiceTotalSQM2 => Invoice?.TotalSQM2 ?? 0;
        public double InvoiceTotalLM => Invoice?.TotalLM ?? 0;
        public double InvoiceTotalLM1 => Invoice?.TotalLM1 ?? 0;
        public double InvoiceTotalLM2 => Invoice?.TotalLM2 ?? 0;

        // ==================== CSV IMPORT/EXPORT ====================

        private void ExportToCsv()
        {
            if (Invoice == null || Invoice.Specifications.Count == 0)
            {
                StatusMessage = "❌ No invoice data to export";
                MessageBox.Show("No invoice data to export!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Invoice.CalculateTotals();
                _excelCsvService.ExportToCsv(Invoice);
                StatusMessage = "✅ Exported to CSV";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ CSV export failed: {ex.Message}";
                Debug.WriteLine($"[Export Error] {ex}");
            }
        }

        private void ImportFromCsv()
        {
            try
            {
                var importedInvoice = _excelCsvService.ImportFromCsv();
                if (importedInvoice == null)
                {
                    StatusMessage = "❌ Import returned null";
                    return;
                }

                Invoice = new ProformaInvoiceModel
                {
                    InvoiceNo = importedInvoice.InvoiceNo,
                    InvoiceDate = importedInvoice.InvoiceDate,
                    CustomerName = importedInvoice.CustomerName,
                    CustomerTRN = importedInvoice.CustomerTRN,
                    ValidUntil = importedInvoice.ValidUntil,
                    Specifications = importedInvoice.Specifications
                };

                // PATCH 10: Ensure all specs are properly linked to Invoice BEFORE renumbering
                foreach (var spec in Invoice.Specifications)
                {
                    spec.Invoice = Invoice;
                    // PATCH 9: Reset SrNo to 0 so RenumberAllSrNumbers can set proper values
                    foreach (var item in spec.Items)
                    {
                        item.SrNo = 0;
                    }
                    spec.CalculateTotals();
                }

                // PATCH 9: Now renumber all SRs in sequence
                RenumberAllSrNumbers();

                SubscribeToOtherChargeChanges();

                SelectedTargetSpecification = Invoice.Specifications.FirstOrDefault();
                SelectedSpecificationId = SelectedTargetSpecification?.Id ?? 0;
                CurrentFileName = "Imported";

                OnPropertyChanged(nameof(Invoice));
                OnPropertyChanged(nameof(SelectedTargetSpecification));
                OnPropertyChanged(nameof(SelectedSpecificationId));

                Invoice.CalculateTotals();
                Invoice.IsDirty = true;

                int totalItems = Invoice.Specifications.Sum(s => s.Items.Count);
                StatusMessage = $"✅ Imported {totalItems} items from {Invoice.Specifications.Count} specification(s)";
                MessageBox.Show($"Imported:\nSpecs: {Invoice.Specifications.Count}\nItems: {totalItems}", "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Import failed: {ex.Message}";
                MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportItemsFromCsv()
        {
            try
            {
                var items = _excelCsvService.ImportItemsFromCsv();
                if (items == null || items.Count == 0)
                {
                    StatusMessage = "❌ No items found in file";
                    return;
                }

                if (Invoice.Specifications.Count == 0)
                    AddSpecification();

                var targetSpec = Invoice.Specifications[0];
                int startSrNo = GetNextSrNo();

                foreach (var item in items)
                {
                    var newItem = new InvoiceItemModel
                    {
                        SrNo = startSrNo++,
                        GlassRef = item.GlassRef,
                        Qty = item.Qty,
                        Price = item.Price,
                        SurchargePercent = 20,
                        Specification = targetSpec
                    };
                    newItem.Width1 = item.Width1;
                    newItem.Height1 = item.Height1;
                    newItem.Width2 = item.Width2;
                    newItem.Height2 = item.Height2;

                    targetSpec.Items.Add(newItem);
                    newItem.PropertyChanged += (s, e) =>
                    {
                        targetSpec.CalculateTotals();
                        Invoice.CalculateTotals();
                        Invoice.IsDirty = true;
                    };
                }

                RenumberAllSrNumbers();
                Invoice.CalculateTotals();
                Invoice.IsDirty = true;

                // Notify all total properties
                OnPropertyChanged(nameof(InvoiceGrandTotal));
                OnPropertyChanged(nameof(InvoiceVatAmount));
                OnPropertyChanged(nameof(InvoiceNetTotal));
                OnPropertyChanged(nameof(InvoiceOtherChargesTotal));
                OnPropertyChanged(nameof(InvoiceTotalSQM));
                OnPropertyChanged(nameof(InvoiceTotalQty));
                OnPropertyChanged(nameof(InvoiceTotalSQM1));
                OnPropertyChanged(nameof(InvoiceTotalSQM2));
                OnPropertyChanged(nameof(InvoiceTotalLM));
                OnPropertyChanged(nameof(InvoiceTotalLM1));
                OnPropertyChanged(nameof(InvoiceTotalLM2));

                StatusMessage = $"✅ Applied '{GeneratedDescription}' @ AED {CalculatedPrice:N2}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Item import failed: {ex.Message}";
            }
        }

        private void PasteFromExcel()
        {
            try
            {
                if (!Clipboard.ContainsText())
                {
                    StatusMessage = "❌ Clipboard is empty";
                    return;
                }

                string clipboardText = Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(clipboardText))
                {
                    StatusMessage = "❌ No text data in clipboard";
                    return;
                }

                var spec = SelectedTargetSpecification ?? (Invoice.Specifications.Count == 0 ? null : Invoice.Specifications[0]);
                if (spec == null)
                {
                    AddSpecification();
                    spec = SelectedTargetSpecification;
                }

                var rows = clipboardText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (rows.Length == 0)
                {
                    StatusMessage = "❌ No data to paste";
                    return;
                }

                bool skipHeader = rows[0].ToLower().Contains("glass") || rows[0].ToLower().Contains("width") || rows[0].ToLower().Contains("height");
                int startIndex = skipHeader ? 1 : 0;

                double defaultWidth1 = 0, defaultHeight1 = 0, defaultWidth2 = 0, defaultHeight2 = 0, defaultPrice = 0, defaultSurcharge = 20;
                if (spec.Items.Count > 0)
                {
                    var firstItem = spec.Items[0];
                    defaultWidth1 = firstItem.Width1;
                    defaultHeight1 = firstItem.Height1;
                    defaultWidth2 = firstItem.Width2;
                    defaultHeight2 = firstItem.Height2;
                    defaultPrice = firstItem.Price;
                    defaultSurcharge = firstItem.SurchargePercent;
                }

                spec.Items.Clear();
                int itemsAdded = 0;

                for (int i = startIndex; i < rows.Length; i++)
                {
                    var columns = rows[i].Split('\t').Select(c => c.Trim()).ToArray();
                    if (columns.Length < 6)
                    {
                        Debug.WriteLine($"[Import Skip] Invalid row: {rows[i]}");
                        continue;
                    }
                    if (columns.Length == 0 || string.IsNullOrWhiteSpace(string.Join("", columns))) continue;

                    var item = new InvoiceItemModel { SrNo = itemsAdded + 1, Specification = spec };

                    if (columns.Length > 0) item.GlassRef = columns[0].Trim();
                    if (columns.Length > 1 && TryParseNumber(columns[1], out double w1)) item.Width1 = Math.Max(0, w1); else item.Width1 = defaultWidth1;
                    if (columns.Length > 2 && TryParseNumber(columns[2], out double h1)) item.Height1 = Math.Max(0, h1); else item.Height1 = defaultHeight1;
                    if (columns.Length > 3 && TryParseNumber(columns[3], out double w2)) item.Width2 = Math.Max(0, w2); else item.Width2 = defaultWidth2;
                    if (columns.Length > 4 && TryParseNumber(columns[4], out double h2)) item.Height2 = Math.Max(0, h2); else item.Height2 = defaultHeight2;
                    if (columns.Length > 5 && int.TryParse(columns[5].Trim().Replace(",", ""), out int qty)) item.Qty = Math.Max(1, qty); else item.Qty = 1;
                    if (columns.Length > 6 && TryParseNumber(columns[6], out double price)) item.Price = Math.Max(0, price); else item.Price = defaultPrice;
                    if (columns.Length > 7 && TryParseNumber(columns[7].Replace("%", ""), out double surcharge)) item.SurchargePercent = Math.Clamp(surcharge, 0, 100); else item.SurchargePercent = defaultSurcharge;

                    item.PropertyChanged += (s, e) =>
                    {
                        spec.CalculateTotals();
                        Invoice.CalculateTotals();
                        OnPropertyChanged(nameof(Invoice));
                        OnPropertyChanged(nameof(SelectedTargetSpecification));
                        OnPropertyChanged(nameof(Invoice.Specifications));
                        Invoice.IsDirty = true;
                    };

                    spec.Items.Add(item);
                    itemsAdded++;
                }

                if (spec.Items.Count == 0)
                {
                    int nextSr = GetNextSrNo();
                    spec.Items.Add(new InvoiceItemModel
                    {
                        SrNo = nextSr,
                        Qty = 1,
                        SurchargePercent = defaultSurcharge,
                        Price = defaultPrice,
                        Width1 = defaultWidth1,
                        Height1 = defaultHeight1,
                        Width2 = defaultWidth2,
                        Height2 = defaultHeight2
                    });
                }

                RenumberAllSrNumbers();
                spec.CalculateTotals();
                Invoice.CalculateTotals();

                OnPropertyChanged(nameof(Invoice));
                OnPropertyChanged(nameof(SelectedTargetSpecification));
                OnPropertyChanged(nameof(Invoice.Specifications));
                OnPropertyChanged(nameof(SelectedSpecificationOtherCharges));

                Invoice.IsDirty = true;
                StatusMessage = $"✅ Pasted {itemsAdded} items from Excel";

                // Refresh auto-values for all charges
                RefreshAllChargeAutoValues();
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Paste failed: {ex.Message}";
                Debug.WriteLine($"[Paste Error] {ex}");
            }
        }

        private bool TryParseNumber(string input, out double result)
        {
            result = 0;
            if (string.IsNullOrWhiteSpace(input)) return false;
            string cleaned = input.Trim().Replace(",", "").Replace("AED", "").Replace("%", "");
            return double.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result);
        }

        // ==================== DEBUG & TEST METHODS ====================

        private void TestRoundTrip()
        {
            try
            {
                string testPath = @"C:\Temp\TestInvoice.csv";
                _excelCsvService.ExportToCsv(Invoice, testPath);
                StatusMessage = "✅ Exported to test file";

                var lines = File.ReadAllLines(testPath);
                MessageBox.Show($"Exported {lines.Length} lines:\n\n{string.Join("\n", lines.Take(20))}", "Exported Content");

                var modifiedLines = File.ReadAllLines(testPath).ToList();
                for (int i = 0; i < modifiedLines.Count; i++)
                {
                    if (modifiedLines[i].Contains(",1,") && modifiedLines[i].Contains("Clear"))
                    {
                        modifiedLines[i] = modifiedLines[i].Replace(",1,", ",99,");
                        break;
                    }
                }
                File.WriteAllLines(testPath, modifiedLines);
                StatusMessage = "✅ Modified test file (changed Qty to 99)";

                var imported = _excelCsvService.ImportFromCsvFile(testPath);
                if (imported != null)
                {
                    int totalItems = imported.Specifications.Sum(s => s.Items.Count);
                    StatusMessage = $"✅ Imported: {totalItems} items";
                    if (imported.Specifications.Count > 0 && imported.Specifications[0].Items.Count > 0)
                        MessageBox.Show($"First item Qty: {imported.Specifications[0].Items[0].Qty}\n\nIf Qty = 99, import works!", "Test Result");
                }
                else
                    MessageBox.Show("Import FAILED - returned null", "Error");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
        }

        private void DebugCsvImport()
        {
            try
            {
                var dialog = new OpenFileDialog { Filter = "CSV Files (*.csv)|*.csv", Title = "Select CSV to Debug" };
                if (dialog.ShowDialog() != true) return;

                string filePath = dialog.FileName;
                var lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);

                string report = $"File: {Path.GetFileName(filePath)}\nLines: {lines.Length}\n\n=== RAW CONTENT ===\n";
                for (int i = 0; i < Math.Min(lines.Length, 40); i++)
                    report += $"[{i:D2}] {lines[i]}\n";
                report += "\n=== IMPORT ATTEMPT ===\n";

                var imported = _excelCsvService.ImportFromCsvFile(filePath);
                if (imported == null)
                    report += "RESULT: NULL returned\n";
                else
                {
                    report += $"RESULT: {imported.Specifications.Count} specs\n";
                    foreach (var spec in imported.Specifications)
                    {
                        report += $"\nSpec: {spec.SpecificationName}\n  Items: {spec.Items.Count}\n";
                        foreach (var item in spec.Items)
                            report += $"    SR:{item.SrNo}, Glass:{item.GlassRef}, W:{item.Width1}, H:{item.Height1}, Qty:{item.Qty}\n";
                    }
                }
                MessageBox.Show(report, "CSV Debug Report", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n{ex.StackTrace}", "Error");
            }
        }

        private void TestCsvRoundTrip()
        {
            try
            {
                string testPath = @"C:\Temp\TestInvoice.csv";
                _excelCsvService.ExportToCsv(Invoice, testPath);

                var lines = File.ReadAllLines(testPath, System.Text.Encoding.UTF8);
                string rawContent = "=== EXPORTED CSV RAW CONTENT ===\n\n";
                for (int i = 0; i < lines.Length; i++)
                    rawContent += $"[{i:D2}] {lines[i]}\n";
                MessageBox.Show(rawContent, "RAW CSV Content", MessageBoxButton.OK, MessageBoxImage.Information);

                var imported = _excelCsvService.ImportFromCsvFile(testPath);
                if (imported == null)
                    MessageBox.Show("❌ IMPORT FAILED - Service returned NULL!", "Test Result", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                {
                    int specs = imported.Specifications.Count;
                    int items = imported.Specifications.Sum(s => s.Items.Count);
                    MessageBox.Show($"✅ IMPORT SUCCESS\n\nSpecs: {specs}\nItems: {items}", "Test Result", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==================== LOAD FROM DAILY WORK ====================

        public void LoadFromDailyWork(Data.Database.DailyWork dailyWork)
        {
            if (dailyWork == null) return;

            Invoice.InvoiceNo = $"PI-{DateTime.Now:yyyyMMdd}-{dailyWork.Id:D4}";
            Invoice.InvoiceDate = DateTime.Now;
            Invoice.ValidUntil = DateTime.Now.AddDays(30);
            Invoice.CustomerName = dailyWork.CustomerReference;
            Invoice.CustomerReference = dailyWork.CustomerReference;
            Invoice.Salesman = dailyWork.Salesman;
            Invoice.ProjectName = ExtractProjectName(dailyWork.Notes);
            Invoice.ProjectNo = dailyWork.PINumber;
            Invoice.ProjectLocation = "";
            Invoice.LPONo = "";
            Invoice.AttentionName = "";
            Invoice.ContactNo = "";
            Invoice.Color = dailyWork.Color;
            Invoice.Notes = dailyWork.Notes;

            Invoice.IsDirty = true;
            OnPropertyChanged(nameof(Invoice));
        }

        private string ExtractProjectName(string notes)
        {
            if (string.IsNullOrEmpty(notes)) return "";
            if (notes.Contains("Project:"))
            {
                var parts = notes.Split(new[] { "Project:", "|" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0) return parts[0].Trim();
            }
            return notes;
        }

        // ==================== JOB ORDER CONVERSION (PATCH 1) ====================

        public void CheckAndConvertToJobOrder()
        {
            // Check if status is "Confirmed" and not already converted
            if (Invoice != null &&
                Invoice.Status == "Confirmed" &&
                !IsJobOrder &&
                !string.IsNullOrEmpty(Invoice.InvoiceNo))
            {
                // Check if already converted (prevent double conversion)
                if (Invoice.IsConvertedToJobOrder)
                {
                    System.Diagnostics.Debug.WriteLine("[ProformaInvoice] Already converted to Job Order");
                    return;
                }

                // Mark as converted
                Invoice.IsConvertedToJobOrder = true;

                // Create Job Order
                if (_jobOrderVM != null)
                {
                    // Job Order creation is handled via MainViewModel.CreateJobOrderFromProformaInvoice

                    // Update UI to Job Order mode
                    IsJobOrder = true;

                    System.Diagnostics.Debug.WriteLine($"[ProformaInvoice] ✅ Converted to Job Order: {Invoice.InvoiceNo}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[ProformaInvoice] ❌ JobOrderVM not set!");
                }
            }
        }

        // ==================== LOAD FROM EXISTING PROFORMA INVOICE (PATCH 15) ====================

        public void LoadFromProformaInvoice(ProformaInvoiceModel pi)
        {
            if (pi == null) return;

            System.Diagnostics.Debug.WriteLine($"[PIViewModel] LoadFromProformaInvoice: {pi.InvoiceNo}");

            try
            {
                // PATCH 8: Use bulk update for better performance
                using (BulkUpdateScope())
                {
                    // Copy all properties from the loaded PI
                    Invoice.InvoiceNo = pi.InvoiceNo;
                    Invoice.InvoiceDate = pi.InvoiceDate;
                    Invoice.ValidUntil = pi.ValidUntil;
                    Invoice.CustomerName = pi.CustomerName ?? "";
                    Invoice.CustomerTRN = pi.CustomerTRN ?? "";
                    Invoice.CustomerReference = pi.CustomerReference ?? "";
                    Invoice.Salesman = pi.Salesman ?? "";
                    Invoice.CustomerAddress = pi.CustomerAddress ?? "";
                    Invoice.ProjectName = pi.ProjectName ?? "";
                    Invoice.ProjectNo = pi.ProjectNo ?? "";
                    Invoice.ProjectLocation = pi.ProjectLocation ?? "";
                    Invoice.LPONo = pi.LPONo ?? "";
                    Invoice.AttentionName = pi.AttentionName ?? "";
                    Invoice.ContactNo = pi.ContactNo ?? "";
                    Invoice.Color = pi.Color ?? "";
                    Invoice.Notes = pi.Notes ?? "";
                    Invoice.Status = pi.Status ?? "Pending";

                    // Copy specifications using DeepClone (PATCH 15)
                    Invoice.Specifications.Clear();
                    if (pi.Specifications != null)
                    {
                        foreach (var piSpec in pi.Specifications)
                        {
                            var newSpec = piSpec.DeepClone();
                            newSpec.Invoice = Invoice;

                            // PATCH 10: Ensure all items have proper parent reference
                            foreach (var item in newSpec.Items)
                            {
                                item.Specification = newSpec;
                            }

                            Invoice.Specifications.Add(newSpec);
                        }
                    }
                }

                // PATCH 3: Reconstruct after load (reattach handlers, rebuild relationships)
                ReconstructAfterLoad();

                // Set first spec as selected
                SelectedTargetSpecification = Invoice.Specifications.FirstOrDefault();
                SelectedSpecificationId = SelectedTargetSpecification?.Id ?? 0;

                Invoice.CalculateTotals();
                Invoice.IsDirty = false;
                CurrentFileName = pi.InvoiceNo ?? "Loaded Invoice";

                System.Diagnostics.Debug.WriteLine($"[PIViewModel] Loaded {Invoice.Specifications.Count} specs with full reconstruction");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PIViewModel] LoadFromProformaInvoice ERROR: {ex.Message}");
                MessageBox.Show($"Error loading invoice: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // ==================== VALIDATION (PATCH 17) ====================

        public ValidationResult ValidateInvoice()
        {
            return Invoice?.Validate() ?? new ValidationResult();
        }

        public bool CanSaveInvoice()
        {
            var result = ValidateInvoice();
            return result.IsValid;
        }

        public string GetValidationSummary()
        {
            var result = ValidateInvoice();
            return result.GetErrorSummary();
        }
    }
}