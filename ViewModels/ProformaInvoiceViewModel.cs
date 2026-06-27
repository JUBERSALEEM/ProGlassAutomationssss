using LiveChartsCore.SkiaSharpView.WPF;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Newtonsoft.Json;
using ProGlassAutomation.Models;
using ProGlassAutomation.Services;
using ProGlassAutomation.Views.ProformaInvoice;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using SpecModel = ProGlassAutomation.Models.SpecificationModel;

namespace ProGlassAutomation.ViewModels
{
    public class DimensionOption { public string Value { get; set; } = ""; public string Label { get; set; } = ""; }
    public class ChargeTypeOption { public string Value { get; set; } = ""; public string Label { get; set; } = ""; }

    public enum ChargeType
    {
        LM, LM1, LM2,
        SQM, SQM1, SQM2,
        QTY, MULTI2
    }

    public class AirSpacerOption { public string Thickness { get; set; } = ""; public string Type { get; set; } = ""; public double Price { get; set; } public string Display => $"{Thickness}mm {Type} - AED {Price:F2}"; }
    public class FileListItem { public string FilePath { get; set; } = ""; public string InvoiceNo { get; set; } = ""; public string CustomerName { get; set; } = ""; public DateTime InvoiceDate { get; set; } public double NetTotal { get; set; } public string FileName => Path.GetFileNameWithoutExtension(FilePath); public string DateDisplay => InvoiceDate.ToString("dd MMM yyyy"); public string TotalDisplay => $"AED {NetTotal:N2}"; }

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
        private const string LOG = "[ProformaVM]";

        public event PropertyChangedEventHandler PropertyChanged;
        private readonly ExcelCsvService _excelCsvService = new ExcelCsvService();
        private readonly GlassPriceCalculator _priceCalculator = new GlassPriceCalculator();

        private readonly Dictionary<InvoiceItemModel, SpecificationModel> _itemSpecMap = new();
        private readonly Dictionary<string, Func<List<SpecificationModel>, double>> _chargeCalcMap = new();

        private static readonly object _invoiceLock = new object();
        private static int _lastGeneratedNumber;

        private readonly Queue<string> _statusMessageQueue = new();
        private bool _isProcessingStatusQueue = false;

        private readonly HashSet<string> _attachedHandlers = new();

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        private int _currentPINumber = 0;
        private bool _isUpdatingASPPrice = false;
        private bool _isRecalculating = false;
        private bool _isChargeProcessing = false;
        private bool _isBulkUpdating = false;

        // ═══════════════════════════════════════════════════════
        // ✏️ UNDO / REDO STATE — Excel-style, whole-invoice snapshots
        // ═══════════════════════════════════════════════════════
        private const int MAX_UNDO_HISTORY = 50;
        private const int COALESCE_WINDOW_MS = 500; // typing within 500ms on same field = 1 snapshot
        private readonly Stack<UndoSnapshot> _undoStack = new();
        private readonly Stack<UndoSnapshot> _redoStack = new();
        private bool _isRestoringSnapshot = false;

        // Coalescing: track last snapshot's source field + timestamp
        private string _lastSnapshotKey = "";
        private DateTime _lastSnapshotTime = DateTime.MinValue;

        // Snapshot now captures the WHOLE invoice (Specifications + all top-level fields)
        private class UndoSnapshot
        {
            public string Description { get; set; } = "";
            public string InvoiceJson { get; set; } = "";          // full invoice (incl. specs, charges, all fields)
            public int SelectedSpecId { get; set; }
        }
        // ═══════════════════════════════════════════════════════

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
            _chargeCalcMap["lm"] = specs => CalculateTotalLMValue(specs, "w1h1");
            _chargeCalcMap["lm1"] = CalculateTotalLM1Value;
            _chargeCalcMap["lm2"] = CalculateTotalLM2Value;
            _chargeCalcMap["sqm"] = CalculateTotalSQMValue;
            _chargeCalcMap["sqm1"] = CalculateTotalSQM1Value;
            _chargeCalcMap["sqm2"] = CalculateTotalSQM2Value;
            _chargeCalcMap["qty"] = specs => (double)CalculateTotalQtyValue(specs);
            _chargeCalcMap["1x"] = specs => (double)CalculateTotalQtyValue(specs);
            _chargeCalcMap["2x"] = specs => CalculateTotalQtyValue(specs) * 2;

            _excelCsvService.StatusChanged += status => EnqueueStatus(status);
            InitializeCommands();
            LoadSavedFiles();
            CreateNewInvoice();
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null) { if (Equals(field, value)) return false; field = value; OnPropertyChanged(propertyName); return true; }

        private void EnqueueStatus(string message)
        {
            lock (_statusMessageQueue)
            {
                _statusMessageQueue.Enqueue(message);
            }
            ProcessStatusQueue();
        }

        private async void ProcessStatusQueue()
        {
            if (_isProcessingStatusQueue) return;
            _isProcessingStatusQueue = true;

            try
            {
                while (_statusMessageQueue.Count > 0)
                {
                    lock (_statusMessageQueue)
                    {
                        if (_statusMessageQueue.Count > 0)
                            _statusMessage = _statusMessageQueue.Dequeue();
                    }
                    OnPropertyChanged(nameof(StatusMessage));
                    await Task.Delay(50);
                }
            }
            finally
            {
                _isProcessingStatusQueue = false;
            }
        }

        // ═══════════════════════════════════════════════════════
        // ✏️ UNDO — Invoice property listener
        // Catches changes to: Customer*, Project*, LPO, Attention,
        // Contact, Notes, Color, Status, InvoiceNo, InvoiceDate, ValidUntil
        // ═══════════════════════════════════════════════════════
        private static readonly HashSet<string> _trackedInvoiceFields = new()
        {
            nameof(ProformaInvoiceModel.InvoiceNo),
            nameof(ProformaInvoiceModel.InvoiceDate),
            nameof(ProformaInvoiceModel.ValidUntil),
            nameof(ProformaInvoiceModel.Status),
            nameof(ProformaInvoiceModel.CustomerName),
            nameof(ProformaInvoiceModel.CustomerTRN),
            nameof(ProformaInvoiceModel.CustomerReference),
            nameof(ProformaInvoiceModel.Salesman),
            nameof(ProformaInvoiceModel.CustomerAddress),
            nameof(ProformaInvoiceModel.ProjectName),
            nameof(ProformaInvoiceModel.ProjectNo),
            nameof(ProformaInvoiceModel.ProjectLocation),
            nameof(ProformaInvoiceModel.LPONo),
            nameof(ProformaInvoiceModel.AttentionName),
            nameof(ProformaInvoiceModel.ContactNo),
            nameof(ProformaInvoiceModel.Notes),
            nameof(ProformaInvoiceModel.Color),
        };

        private void OnInvoicePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProformaInvoiceModel.IsDirty))
                OnPropertyChanged(nameof(HasUnsavedChanges));

            string propName = e.PropertyName;

            // ✏️ UNDO — auto-snapshot when tracked invoice fields change
            if (_trackedInvoiceFields.Contains(propName))
            {
                TakeSnapshotCoalesced($"Edit {FriendlyName(propName)}", $"Invoice.{propName}");
            }

            if (propName == nameof(ProformaInvoiceModel.GrandTotal) ||
                propName == nameof(ProformaInvoiceModel.VatAmount) ||
                propName == nameof(ProformaInvoiceModel.NetTotal) ||
                propName == nameof(ProformaInvoiceModel.OtherChargesTotal) ||
                propName == nameof(ProformaInvoiceModel.TotalSQM) ||
                propName == nameof(ProformaInvoiceModel.TotalQty) ||
                propName == nameof(ProformaInvoiceModel.TotalSQM1) ||
                propName == nameof(ProformaInvoiceModel.TotalSQM2) ||
                propName == nameof(ProformaInvoiceModel.TotalLM) ||
                propName == nameof(ProformaInvoiceModel.TotalLM1) ||
                propName == nameof(ProformaInvoiceModel.TotalLM2))
            {
                RaiseAllInvoiceTotalsChanged();
            }
        }

        // Map raw property names to user-friendly labels for status bar
        private static string FriendlyName(string propName) => propName switch
        {
            "InvoiceNo" => "Invoice No",
            "InvoiceDate" => "Invoice Date",
            "ValidUntil" => "Valid Until",
            "CustomerName" => "Customer Name",
            "CustomerTRN" => "Customer TRN",
            "CustomerReference" => "Customer Reference",
            "Salesman" => "Salesman",
            "CustomerAddress" => "Customer Address",
            "ProjectName" => "Project Name",
            "ProjectNo" => "Project No",
            "ProjectLocation" => "Project Location",
            "LPONo" => "LPO No",
            "AttentionName" => "Attention",
            "ContactNo" => "Contact No",
            "SpecificationName" => "Specification Name",
            "BasePrice" => "Base Price",
            "SurchargePercent" => "Surcharge %",
            _ => propName
        };

        private void RaiseAllInvoiceTotalsChanged()
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

        public bool HasNoCharges => SelectedSpecificationOtherCharges == null || SelectedSpecificationOtherCharges.Count == 0;

        public string CompanyName { get; set; } = "PROGLASS AUTOMATION";

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

        public bool IsPriceSectionVisible => !IsJobOrder;
        public bool IsNetTotalSectionVisible => !IsJobOrder;

        private JobOrderViewModel _jobOrderVM;
        public JobOrderViewModel JobOrderVM { get => _jobOrderVM; set => SetProperty(ref _jobOrderVM, value); }

        public string CompanyTRN { get; set; } = "100458979400003";
        public string CompanyLocation { get; set; } = "Dubai, UAE";
        public string CompanyPhone { get; set; } = "+971-50-123-4567";

        private bool _isFilePanelOpen;
        public bool IsFilePanelOpen { get => _isFilePanelOpen; set => SetProperty(ref _isFilePanelOpen, value); }

        private bool _isOptimizationPanelOpen;
        public bool IsOptimizationPanelOpen { get => _isOptimizationPanelOpen; set => SetProperty(ref _isOptimizationPanelOpen, value); }

        private bool _isSGUSelected = true;
        public bool IsSGUSelected
        {
            get => _isSGUSelected;
            set
            {
                if (SetProperty(ref _isSGUSelected, value) && value)
                {
                    _isDGUSelected = false;
                    OnPropertyChanged(nameof(IsDGUSelected));
                    _isLAMSelected = false;
                    OnPropertyChanged(nameof(IsLAMSelected));
                }
            }
        }

        private bool _isDGUSelected;
        public bool IsDGUSelected
        {
            get => _isDGUSelected;
            set
            {
                if (SetProperty(ref _isDGUSelected, value) && value)
                {
                    _isSGUSelected = false;
                    OnPropertyChanged(nameof(IsSGUSelected));
                    _isLAMSelected = false;
                    OnPropertyChanged(nameof(IsLAMSelected));
                }
            }
        }

        private bool _isLAMSelected;
        public bool IsLAMSelected
        {
            get => _isLAMSelected;
            set
            {
                if (SetProperty(ref _isLAMSelected, value) && value)
                {
                    _isSGUSelected = false;
                    OnPropertyChanged(nameof(IsSGUSelected));
                    _isDGUSelected = false;
                    OnPropertyChanged(nameof(IsDGUSelected));
                }
            }
        }

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
            new() { Value = "lm", Label = "LM (W1+H1)" },
            new() { Value = "lm1", Label = "LM1 (W1+H1)" },
            new() { Value = "lm2", Label = "LM2 (W2+H2)" },
            new() { Value = "sqm", Label = "SQM (W1+H1)" },
            new() { Value = "sqm1", Label = "SQM1 (W1+H1)" },
            new() { Value = "sqm2", Label = "SQM2 (W2+H2)" },
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

        private OtherChargeModel _selectedCharge;
        public OtherChargeModel SelectedCharge { get => _selectedCharge; set => SetProperty(ref _selectedCharge, value); }

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
        public ICommand PrintPreviewCommand { get; private set; } = null!;
        public ICommand PrintInvoiceCommand { get; private set; } = null!;
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
        public ICommand RefreshChargesCommand { get; private set; } = null!;

        public ICommand UndoCommand { get; private set; } = null!;
        public ICommand RedoCommand { get; private set; } = null!;

        public event Action<ProformaInvoiceModel>? InvoiceSaved;
        public event Action<ProformaInvoiceModel>? InvoiceToBeAdded;

        private void InitializeCommands()
        {
            NewInvoiceCommand = new RelayCommand(_ => NewInvoice());
            SaveInvoiceCommand = new RelayCommand(_ => SaveInvoice(), _ => CanExecuteSaveInvoice());
            OpenInvoiceCommand = new RelayCommand(_ => OpenInvoice());
            CreateJobOrderCommand = new RelayCommand(_ => ExecuteCreateJobOrder());
            DeleteInvoiceCommand = new RelayCommand(param => DeleteInvoice(param));
            AddSpecificationCommand = new RelayCommand(_ => AddSpecification());
            RemoveSpecificationCommand = new RelayCommand(_ => RemoveSpecification(), _ => Invoice?.Specifications?.Count > 0);
            ToggleLMCommand = new RelayCommand(_ => ToggleLM());
            CalculatePriceCommand = new RelayCommand(_ => CalculatePrice());
            IncludeInSpecificationCommand = new RelayCommand(_ => IncludeInSpecification(), _ => CanIncludeInSpecification());
            PrintCommand = new RelayCommand(_ => PrintInvoice());
            PrintPreviewCommand = new RelayCommand(_ => ShowPrintPreview(), _ => CanShowPrintPreview());
            PrintInvoiceCommand = new RelayCommand(_ => ShowPrintPreview(), _ => CanShowPrintPreview());
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

            UndoCommand = new RelayCommand(_ => Undo(), _ => CanUndo);
            RedoCommand = new RelayCommand(_ => Redo(), _ => CanRedo);
        }

        public IDisposable BulkUpdateScope() => new ViewModelBulkUpdateScope(this);

        private class ViewModelBulkUpdateScope : IDisposable
        {
            private readonly ProformaInvoiceViewModel _vm;
            public ViewModelBulkUpdateScope(ProformaInvoiceViewModel vm) { _vm = vm; _vm.IsBulkUpdating = true; _vm.Invoice?.BeginBulkUpdate(); }
            public void Dispose() { _vm.Invoice?.EndBulkUpdate(); _vm.IsBulkUpdating = false; }
        }

        // ==================== METHODS ====================

        private void ResetASPPriceToAuto()
        {
            IsASPPriceManual = false;
            UpdateAirSpacerPrice();
        }

        private bool CanExecuteSaveInvoice() => Invoice != null;

        private void ExecuteCreateJobOrder()
        {
            try
            {
                Debug.WriteLine("[PIViewModel] ExecuteCreateJobOrder called");
                if (Invoice == null)
                {
                    MessageBox.Show("Please create or load an invoice first.", "No Invoice", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (Invoice.Specifications == null || Invoice.Specifications.Count == 0)
                {
                    MessageBox.Show("Please add at least one specification before creating job order.", "No Specifications", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                if (string.IsNullOrWhiteSpace(Invoice.CustomerName))
                    Invoice.CustomerName = "New Customer";

                var mainWindow = Application.Current.MainWindow;
                if (mainWindow?.DataContext is MainViewModel mainVM)
                {
                    Debug.WriteLine("[PIViewModel] Calling mainVM.CreateJobOrderFromProformaInvoice");
                    mainVM.CreateJobOrderFromProformaInvoice(Invoice);
                }
                else
                {
                    Debug.WriteLine("[PIViewModel] MainWindow or DataContext is null!");
                    MessageBox.Show($"Creating Job Order from PI: {Invoice.InvoiceNo}\nCustomer: {Invoice.CustomerName}", "Create Job Order", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PIViewModel] CreateJobOrder error: {ex.Message}");
                MessageBox.Show($"Failed to create job order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            ClearUndoHistory();

            Invoice = new ProformaInvoiceModel
            {
                InvoiceNo = GetNextSequentialInvoiceNo(),
                InvoiceDate = DateTime.Now,
                ValidUntil = DateTime.Now.AddDays(2),
                CompanyName = CompanyName,
                CompanyTRN = CompanyTRN,
                CompanyLocation = CompanyLocation,
                CompanyPhone = CompanyPhone
            };
            CurrentFileName = "Untitled";
            AddSpecification();
            if (Invoice.Specifications.Count > 0)
                Invoice.Specifications[0].Invoice = Invoice;
            Invoice.IsDirty = false;

            ClearUndoHistory();

            Debug.WriteLine($"[ProformaInvoiceVM] Created new invoice: {Invoice.InvoiceNo}");
        }

        public void LoadFromExistingInvoice(ProformaInvoiceModel invoice)
        {
            if (invoice == null)
            {
                Debug.WriteLine("[ProformaInvoiceVM] LoadFromExistingInvoice: invoice is null!");
                return;
            }
            Debug.WriteLine($"[ProformaInvoiceVM] Loading invoice: {invoice.InvoiceNo}");

            ClearUndoHistory();

            using (BulkUpdateScope())
            {
                Invoice.InvoiceNo = invoice.InvoiceNo;
                Invoice.InvoiceDate = invoice.InvoiceDate;
                Invoice.ValidUntil = invoice.ValidUntil;
                Invoice.CustomerName = invoice.CustomerName ?? "";
                Invoice.CustomerTRN = invoice.CustomerTRN ?? "";
                Invoice.CustomerReference = invoice.CustomerReference ?? "";
                Invoice.Salesman = invoice.Salesman ?? "";
                Invoice.CustomerAddress = invoice.CustomerAddress ?? "";
                Invoice.ProjectName = invoice.ProjectName ?? "";
                Invoice.ProjectNo = invoice.ProjectNo ?? "";
                Invoice.ProjectLocation = invoice.ProjectLocation ?? "";
                Invoice.LPONo = invoice.LPONo ?? "";
                Invoice.AttentionName = invoice.AttentionName ?? "";
                Invoice.ContactNo = invoice.ContactNo ?? "";
                Invoice.Color = invoice.Color ?? "";
                Invoice.Notes = invoice.Notes ?? "";
                Invoice.Status = invoice.Status ?? "Pending";
                Invoice.IsConvertedToJobOrder = invoice.IsConvertedToJobOrder;
                CompanyName = invoice.CompanyName ?? "PROGLASS AUTOMATION";
                CompanyTRN = invoice.CompanyTRN ?? "100458979400003";
                CompanyLocation = invoice.CompanyLocation ?? "Dubai, UAE";
                CompanyPhone = invoice.CompanyPhone ?? "+971-50-123-4567";
                Invoice.Specifications.Clear();
                if (invoice.Specifications != null)
                {
                    foreach (var srcSpec in invoice.Specifications)
                    {
                        var newSpec = srcSpec.DeepClone();
                        newSpec.Invoice = Invoice;
                        foreach (var item in newSpec.Items)
                            item.Specification = newSpec;
                        Invoice.Specifications.Add(newSpec);
                    }
                }
            }
            ReconstructAfterLoad();
            SelectedTargetSpecification = Invoice.Specifications.FirstOrDefault();
            SelectedSpecificationId = SelectedTargetSpecification?.Id ?? 0;
            Invoice.CalculateTotals();
            Invoice.IsDirty = false;
            CurrentFileName = invoice.InvoiceNo ?? "Loaded Invoice";

            ClearUndoHistory();
            Debug.WriteLine($"[ProformaInvoiceVM] Loaded: {Invoice.InvoiceNo}");
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
            SharedViewModels.RequestInvoiceListRefresh();
        }
        private void SaveInvoice()
        {
            try
            {
                Debug.WriteLine("[ProformaInvoice] ===== SAVE STARTED =====");
                if (string.IsNullOrWhiteSpace(Invoice?.CustomerName))
                {
                    Invoice!.CustomerName = "New Customer";
                    Debug.WriteLine("[ProformaInvoice] Set default customer name");
                }
                Invoice!.CompanyName = CompanyName;
                Invoice.CompanyTRN = CompanyTRN;
                Invoice.CompanyLocation = CompanyLocation;
                Invoice.CompanyPhone = CompanyPhone;
                Invoice.CalculateTotals();
                Debug.WriteLine($"[ProformaInvoice] Calculated totals: SQM={Invoice.TotalSQM}, Qty={Invoice.TotalQty}");

                AddToMainViewModelList();
                Debug.WriteLine("[ProformaInvoice] Added to main list");

                string folder = GetDataFolder();
                Debug.WriteLine($"[ProformaInvoice] Data folder: {folder}");

                string filePath = Path.Combine(folder, $"{Invoice.InvoiceNo}.json");

                if (File.Exists(filePath))
                {
                    Debug.WriteLine($"[ProformaInvoice] File EXISTS: {filePath}");
                    var result = MessageBox.Show(
                        $"Invoice '{Invoice.InvoiceNo}' already exists.\n\nDo you want to overwrite it?",
                        "Confirm Overwrite", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                    {
                        Debug.WriteLine("[ProformaInvoice] Save cancelled by user");
                        return;
                    }
                }
                else
                {
                    Debug.WriteLine($"[ProformaInvoice] File is NEW: {filePath}");
                }

                string json = JsonConvert.SerializeObject(Invoice, Formatting.Indented, _jsonSettings);
                Debug.WriteLine($"[ProformaInvoice] JSON length: {json.Length}");

                File.WriteAllText(filePath, json);
                Debug.WriteLine($"[ProformaInvoice] File saved: {filePath}");

                CurrentFileName = Invoice.InvoiceNo;
                LoadSavedFiles();

                Invoice.IsDirty = false;
                // ✏️ UNDO — Excel KEEPS history after save, so we don't clear it
                // (uncomment next line if you want to clear history on save instead)
                // ClearUndoHistory();
                StatusMessage = $"✅ Saved: {CurrentFileName}";

                InvoiceSaved?.Invoke(Invoice);
                SharedViewModels.RequestInvoiceListRefresh();

                string extractedColor = "";
                if (!string.IsNullOrWhiteSpace(Invoice?.Notes))
                    extractedColor = ProGlassAutomation.Helpers.ColorExtractor.ExtractColors(Invoice.Notes);

                if (string.IsNullOrEmpty(extractedColor) && Invoice?.Specifications != null)
                {
                    foreach (var spec in Invoice.Specifications)
                    {
                        if (!string.IsNullOrWhiteSpace(spec.SpecificationName))
                        {
                            extractedColor = ProGlassAutomation.Helpers.ColorExtractor.ExtractColors(spec.SpecificationName);
                            if (!string.IsNullOrEmpty(extractedColor)) break;
                        }
                    }
                }

                var finalColor = !string.IsNullOrEmpty(extractedColor) ? extractedColor : (Invoice?.Color ?? "Clear");

                Debug.WriteLine("[PI] Starting DailyWorks save...");
                try
                {
                    var piNumber = Invoice?.InvoiceNo ?? "";
                    var totalSqm = Invoice?.TotalSQM ?? 0;
                    var totalQty = Invoice?.TotalQty ?? 0;

                    if (string.IsNullOrEmpty(piNumber))
                        Debug.WriteLine("[PI] ERROR: PI Number is empty!");

                    var existingWork = ProGlassAutomation.Data.Database.DbHelper.GetDailyWorkByPINumber(piNumber);

                    if (existingWork != null)
                    {
                        var result = MessageBox.Show(
                            $"PI Number '{piNumber}' already exists in DailyWorks.\n\nDo you want to overwrite the existing record?",
                            "Duplicate PI Number", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result != MessageBoxResult.Yes)
                        {
                            Debug.WriteLine($"[PI] DailyWorks save skipped by user: {piNumber}");
                        }
                        else
                        {
                            double calculatedSQM = Invoice?.TotalSQM ?? 0;
                            int calculatedQty = Invoice?.TotalQty ?? 0;

                            if (calculatedSQM == 0 && Invoice?.Specifications != null)
                            {
                                foreach (var spec in Invoice.Specifications)
                                {
                                    if (spec?.Items != null)
                                    {
                                        foreach (var item in spec.Items)
                                        {
                                            calculatedSQM += item.TotalSQM;
                                            calculatedQty += item.Qty;
                                        }
                                    }
                                }
                            }

                            existingWork.Date = DateTime.Now;
                            existingWork.UpdateDate = DateTime.Now;
                            existingWork.Company = Invoice?.CustomerName ?? "";
                            existingWork.CustomerReference = Invoice?.CustomerReference ?? "";
                            existingWork.TypeOfWork = "Installation";
                            existingWork.ProductionStatus = "Pending";
                            existingWork.Qty = calculatedQty > 0 ? calculatedQty : (Invoice?.TotalQty ?? 0);
                            existingWork.SQM = calculatedSQM > 0 ? calculatedSQM : (Invoice?.TotalSQM ?? 0);
                            existingWork.Status = "Draft";
                            existingWork.Salesman = Invoice?.Salesman ?? "";
                            existingWork.Color = finalColor;
                            existingWork.Notes = Invoice?.Notes ?? "";

                            ProGlassAutomation.Data.Database.DbHelper.UpdateDailyWork(existingWork);
                            Debug.WriteLine($"[PI] Updated DailyWorks: {piNumber}");
                        }
                    }
                    else
                    {
                        double calculatedSQM = Invoice?.TotalSQM ?? 0;
                        int calculatedQty = Invoice?.TotalQty ?? 0;

                        if (calculatedSQM == 0 && Invoice?.Specifications != null)
                        {
                            foreach (var spec in Invoice.Specifications)
                            {
                                if (spec?.Items != null)
                                {
                                    foreach (var item in spec.Items)
                                    {
                                        calculatedSQM += item.TotalSQM;
                                        calculatedQty += item.Qty;
                                    }
                                }
                            }
                        }

                        var dailyWork = new ProGlassAutomation.Data.Database.DailyWork
                        {
                            Date = DateTime.Now,
                            UpdateDate = DateTime.Now,
                            Company = Invoice?.CustomerName ?? "",
                            PINumber = piNumber,
                            CustomerReference = Invoice?.CustomerReference ?? "",
                            TypeOfWork = "Installation",
                            ProductionStatus = "Pending",
                            Qty = calculatedQty > 0 ? calculatedQty : (Invoice?.TotalQty ?? 0),
                            SQM = calculatedSQM > 0 ? calculatedSQM : (Invoice?.TotalSQM ?? 0),
                            Status = "Draft",
                            Salesman = Invoice?.Salesman ?? "",
                            Color = finalColor,
                            Notes = Invoice?.Notes ?? "",
                            CreatedDate = DateTime.Now
                        };

                        ProGlassAutomation.Data.Database.DbHelper.SaveDailyWork(dailyWork);
                        Debug.WriteLine($"[PI] Saved to DailyWorks: {piNumber}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PI] DailyWorks save error: {ex.Message}");
                }

                CheckAndConvertToJobOrder();
                Debug.WriteLine("[ProformaInvoice] ===== SAVE COMPLETE =====");

                MessageBox.Show($"Saved successfully!\n\n{filePath}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ProformaInvoice] ERROR: {ex.Message}");
                Debug.WriteLine($"[ProformaInvoice] STACK: {ex.StackTrace}");
                StatusMessage = $"❌ Error: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddToMainViewModelList()
        {
            try
            {
                Debug.WriteLine($"[PIViewModel] About to raise InvoiceToBeAdded event for: {Invoice?.InvoiceNo}");
                InvoiceToBeAdded?.Invoke(Invoice);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PIViewModel] AddToMainViewModelList error: {ex.Message}");
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
                        CompanyName = invoice.CompanyName ?? "PROGLASS AUTOMATION";
                        CompanyTRN = invoice.CompanyTRN ?? "100458979400003";
                        CompanyLocation = invoice.CompanyLocation ?? "Dubai, UAE";
                        CompanyPhone = invoice.CompanyPhone ?? "+971-50-123-4567";
                        AddToMainViewModelList();
                        ReconstructAfterLoad();
                        Invoice.CalculateTotals();
                        ClearUndoHistory();
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

        private void DeleteInvoice(object? param)
        {
            try
            {
                ProformaInvoiceModel? invoiceToDelete = param as ProformaInvoiceModel;
                if (invoiceToDelete == null) invoiceToDelete = Invoice;
                if (invoiceToDelete == null)
                {
                    MessageBox.Show("No invoice selected to delete.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string invoiceInfo = $"{invoiceToDelete.InvoiceNo} - {invoiceToDelete.CustomerName}";
                var result = MessageBox.Show(
                    $"Are you sure you want to delete this invoice?\n\n{invoiceInfo}\n\nThis action cannot be undone!",
                    "⚠️ Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    string filePath = Path.Combine(GetDataFolder(), $"{invoiceToDelete.InvoiceNo}.json");
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        Debug.WriteLine($"[PIViewModel] Deleted: {filePath}");
                    }

                    if (Invoice?.InvoiceNo == invoiceToDelete.InvoiceNo)
                        CreateNewInvoice();

                    LoadSavedFiles();
                    StatusMessage = $"✅ Deleted: {invoiceToDelete.InvoiceNo}";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PIViewModel] Delete error: {ex.Message}");
                StatusMessage = $"❌ Delete failed: {ex.Message}";
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
                    {
                        invoice.CalculateTotals();
                        SavedFiles.Add(new FileListItem
                        {
                            FilePath = file,
                            InvoiceNo = invoice.InvoiceNo,
                            CustomerName = invoice.CustomerName,
                            InvoiceDate = invoice.InvoiceDate,
                            NetTotal = invoice.NetTotal
                        });
                    }
                }
                catch { }
            }
            OnPropertyChanged(nameof(HasSavedFiles));
        }

        private bool CanShowPrintPreview() => Invoice?.Specifications?.Any() == true;

        private void ShowPrintPreview()
        {
            try
            {
                if (Invoice == null || Invoice.Specifications == null || Invoice.Specifications.Count == 0)
                {
                    MessageBox.Show("No invoice data to preview!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var previewWindow = new Window
                {
                    Title = $"Print Preview - {Invoice.InvoiceNo}",
                    Width = 900,
                    Height = 700,
                    MinWidth = 800,
                    MinHeight = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = System.Windows.Media.Brushes.White,
                    Content = new ProformaInvoicePrintPreviewView { DataContext = this }
                };

                previewWindow.Show();
                StatusMessage = "✅ Preview opened";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Preview failed: {ex.Message}";
                MessageBox.Show($"Preview error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintInvoice() => ShowPrintPreview();

        // ==================== SPECIFICATIONS ====================

        private void AddSpecification()
        {
            TakeSnapshot("Add specification");

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
                    SurchargePercent = spec.SurchargePercent,
                    Specification = spec
                };
                spec.Items.Add(firstItem);
            }

            Invoice.Specifications.Add(spec);

            if (SelectedTargetSpecification == null)
                SelectedTargetSpecification = spec;

            SubscribeToOtherChargeChanges();
            RefreshAllChargeAutoValues();
        }

        private void RemoveSpecification()
        {
            if (Invoice.Specifications.Count <= 0) return;
            TakeSnapshot($"Remove specification '{Invoice.Specifications[Invoice.Specifications.Count - 1].SpecificationName}'");

            Invoice.Specifications.RemoveAt(Invoice.Specifications.Count - 1);
            SelectedTargetSpecification = Invoice.Specifications.LastOrDefault();
            RenumberAllSrNumbers();
            RefreshAllChargeAutoValues();
        }

        public int GetNextSrNo()
        {
            int maxSr = 0;
            if (Invoice?.Specifications == null) return 1;
            foreach (var spec in Invoice.Specifications)
            {
                foreach (var item in spec.Items)
                {
                    if (item.SrNo > maxSr) maxSr = item.SrNo;
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

        public void ReconstructAfterLoad()
        {
            try
            {
                Debug.WriteLine("[PIViewModel] Reconstructing after load...");

                if (Invoice?.Specifications != null)
                {
                    foreach (var spec in Invoice.Specifications)
                        spec.Invoice = Invoice;
                }

                if (Invoice?.Specifications != null)
                {
                    foreach (var spec in Invoice.Specifications)
                    {
                        foreach (var item in spec.Items)
                            item.Specification = spec;
                    }
                }

                AttachAllEventHandlers();

                if (Invoice?.Specifications != null)
                {
                    foreach (var spec in Invoice.Specifications)
                    {
                        if (spec.OtherCharges != null)
                        {
                            foreach (var charge in spec.OtherCharges)
                                charge.BoundSpecs = Invoice.Specifications.ToList();
                        }
                    }
                }

                RefreshAllChargeAutoValues();
                RenumberAllSrNumbers();
                if (!_isRecalculating)
                {
                    _isRecalculating = true;
                    Invoice?.CalculateTotals();
                    _isRecalculating = false;
                }

                Debug.WriteLine("[PIViewModel] Reconstruction complete");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PIViewModel] Reconstruction error: {ex.Message}");
            }
        }

        private void AttachAllEventHandlers()
        {
            DetachAllEventHandlers();
            if (Invoice == null) return;

            Invoice.PropertyChanged -= OnInvoicePropertyChanged;
            Invoice.PropertyChanged += OnInvoicePropertyChanged;

            if (Invoice.Specifications != null)
            {
                foreach (var spec in Invoice.Specifications)
                    AttachSpecificationHandlers(spec);

                Invoice.Specifications.CollectionChanged -= Specifications_CollectionChanged;
                Invoice.Specifications.CollectionChanged += Specifications_CollectionChanged;
            }
        }

        private void AttachSpecificationHandlers(SpecificationModel spec)
        {
            if (spec == null) return;

            string handlerKey = $"Spec_{spec.Id}";
            if (_attachedHandlers.Contains(handlerKey)) return;
            _attachedHandlers.Add(handlerKey);

            spec.PropertyChanged -= Spec_PropertyChanged;
            spec.PropertyChanged += Spec_PropertyChanged;

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

            _attachedHandlers.Clear();

            Invoice.PropertyChanged -= OnInvoicePropertyChanged;

            if (Invoice.Specifications != null)
            {
                Invoice.Specifications.CollectionChanged -= Specifications_CollectionChanged;
                foreach (var spec in Invoice.Specifications)
                    DetachSpecificationHandlers(spec);
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
                    item.PropertyChanged -= Item_PropertyChanged;
            }

            if (spec.OtherCharges != null)
            {
                spec.OtherCharges.CollectionChanged -= OtherCharges_CollectionChanged;
                foreach (var charge in spec.OtherCharges)
                    charge.PropertyChanged -= Charge_PropertyChanged;
            }
        }

        private void Specifications_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (SpecificationModel spec in e.NewItems)
                    AttachSpecificationHandlers(spec);
            }

            if (e.OldItems != null)
            {
                foreach (SpecificationModel spec in e.OldItems)
                    DetachSpecificationHandlers(spec);
            }

            Invoice?.CalculateTotals();
            Invoice.IsDirty = true;
        }

        private void Spec_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is SpecificationModel spec)
            {
                string propName = e.PropertyName;

                // ✏️ UNDO — snapshot when user-editable spec fields change
                if (propName == nameof(SpecificationModel.SpecificationName) ||
                    propName == nameof(SpecificationModel.BasePrice) ||
                    propName == nameof(SpecificationModel.SurchargePercent))
                {
                    TakeSnapshotCoalesced(
                        $"Edit {FriendlyName(propName)} in '{spec.SpecificationName}'",
                        $"Spec_{spec.Id}.{propName}");
                }

                if (propName == "SpecTotalSQM" ||
                    propName == "SpecTotalQty" ||
                    propName == "SpecTotalSQM1" ||
                    propName == "SpecTotalSQM2" ||
                    propName == "SpecTotalLM" ||
                    propName == "SpecTotalPrice" ||
                    propName == "OtherChargesTotal")
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
                    item.PropertyChanged += Item_PropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (InvoiceItemModel item in e.OldItems)
                    item.PropertyChanged -= Item_PropertyChanged;
            }

            Invoice?.CalculateTotals();
            Invoice.IsDirty = true;
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is InvoiceItemModel item && item.Specification != null)
            {
                item.Specification.Recalculate();
                Invoice.IsDirty = true;
            }
        }

        private void OtherCharges_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (OtherChargeModel charge in e.NewItems)
                    charge.PropertyChanged += Charge_PropertyChanged;
            }

            if (e.OldItems != null)
            {
                foreach (OtherChargeModel charge in e.OldItems)
                    charge.PropertyChanged -= Charge_PropertyChanged;
            }

            Invoice?.CalculateTotals();
            Invoice.IsDirty = true;
        }

        private void Charge_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_isChargeProcessing) return;
            if (sender is not OtherChargeModel charge) return;

            try
            {
                _isChargeProcessing = true;

                if (e.PropertyName == nameof(OtherChargeModel.Type) ||
                    e.PropertyName == nameof(OtherChargeModel.Rate) ||
                    e.PropertyName == nameof(OtherChargeModel.LinkedSpecIndices) ||
                    e.PropertyName == nameof(OtherChargeModel.Value) ||
                    e.PropertyName == nameof(OtherChargeModel.Name))
                {
                    // ✏️ UNDO — snapshot charge edits
                    TakeSnapshotCoalesced(
                        $"Edit charge '{charge.Name}' ({e.PropertyName})",
                        $"Charge_{charge.GetHashCode()}.{e.PropertyName}");

                    UpdateChargeValue(charge);
                    CalculateOtherChargeValue(charge, null);

                    if (SelectedTargetSpecification != null)
                    {
                        SelectedTargetSpecification.CalculateOtherChargesTotal();
                        Invoice?.CalculateTotals();
                        Invoice.IsDirty = true;
                        RaiseAllInvoiceTotalsChanged();
                    }
                }
            }
            finally
            {
                _isChargeProcessing = false;
            }
        }

        public void AddItemWithPrice(SpecificationModel spec)
        {
            if (spec == null) return;
            TakeSnapshot($"Add row to '{spec.SpecificationName}'");

            int nextSrNo = GetNextSrNo();
            var newItem = spec.AddItem(nextSrNo);
            newItem.Price = spec.BasePrice;
            newItem.SurchargePercent = spec.SurchargePercent;
            spec.RenumberItems();
            Invoice.IsDirty = true;
        }

        public void RemoveItem(InvoiceItemModel item)
        {
            if (item == null) return;
            var spec = Invoice.Specifications.FirstOrDefault(s => s.Items.Contains(item));
            if (spec != null && spec.Items.Count > 1)
            {
                TakeSnapshot($"Delete row from '{spec.SpecificationName}'");

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

            TakeSnapshot($"Apply calculated price to '{SelectedTargetSpecification.SpecificationName}'");

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
                SelectedTargetSpecification.PVBThickness = LAMPVBThickness;
                SelectedTargetSpecification.PVBColor = LAMPVBColor;
                SelectedTargetSpecification.PVBPrice = LAMPVBPrice.ToString();
                OnPropertyChanged(nameof(GeneratedDescription));
            }

            SelectedTargetSpecification.SpecificationName = GeneratedDescription;
            SelectedTargetSpecification.BasePrice = CalculatedPrice;

            if (SelectedTargetSpecification.Items.Count > 0)
            {
                foreach (var item in SelectedTargetSpecification.Items)
                {
                    item.SurchargePercent = SelectedTargetSpecification.SurchargePercent;
                    if (item.Price == 0) item.Price = CalculatedPrice;
                }
                SelectedTargetSpecification.Items[0].Price = CalculatedPrice;
            }

            Invoice.CalculateTotals();
            Invoice.IsDirty = true;
            int itemsCount = SelectedTargetSpecification?.Items?.Count ?? 0;
            StatusMessage = $"✅ Applied specification with {itemsCount} items";
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
                        charge.PropertyChanged -= Charge_PropertyChanged;
                        charge.PropertyChanged += Charge_PropertyChanged;
                        charge.BoundSpecs = Invoice.Specifications.ToList();
                    }
                }
            }
        }

        private void AddOtherCharge()
        {
            if (SelectedTargetSpecification == null)
            {
                MessageBox.Show("Please select a specification first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Invoice?.Specifications == null || !Invoice.Specifications.Any())
            {
                MessageBox.Show("Invalid selection - no specifications available", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int specIndex = Invoice.Specifications.IndexOf(SelectedTargetSpecification);
            if (specIndex < 0)
            {
                MessageBox.Show("Invalid specification selection", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TakeSnapshot($"Add charge to '{SelectedTargetSpecification.SpecificationName}'");

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

            charge.BoundSpecs = Invoice.Specifications.ToList();
            SelectedTargetSpecification.OtherCharges.Add(charge);
            charge.PropertyChanged += Charge_PropertyChanged;
            Invoice.IsDirty = true;
            OnPropertyChanged(nameof(SelectedSpecificationOtherCharges));
            SelectedCharge = charge;
            UpdateChargeValue(charge);
        }

        private void RemoveOtherCharge(OtherChargeModel? charge)
        {
            if (charge == null || SelectedTargetSpecification == null) return;

            TakeSnapshot($"Remove charge '{charge.Name}' from '{SelectedTargetSpecification.SpecificationName}'");

            charge.PropertyChanged -= Charge_PropertyChanged;
            SelectedTargetSpecification.OtherCharges.Remove(charge);
            SelectedTargetSpecification.CalculateOtherChargesTotal();
            Invoice?.CalculateTotals();
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
            Invoice?.CalculateTotals();
            Invoice.IsDirty = true;
            RaiseAllInvoiceTotalsChanged();
        }

        private void UpdateChargeValue(OtherChargeModel charge)
        {
            if (charge == null) return;
            var linkedSpecs = GetLinkedSpecifications(charge);
            if (linkedSpecs.Count == 0) return;

            CalculateChargeValue(charge, linkedSpecs);

            Application.Current.Dispatcher.Invoke(() =>
            {
                charge.Amount = Math.Round(charge.Value * charge.Rate, 2);
            });
        }

        private void CalculateChargeValue(OtherChargeModel charge, List<SpecificationModel> linkedSpecs)
        {
            if (charge == null) return;

            var type = charge.Type?.ToLower() ?? "";
            charge.Value = type switch
            {
                "lm" => CalculateTotalLMValue(linkedSpecs, "w1h1"),
                "lm1" => CalculateTotalLM1Value(linkedSpecs),
                "lm2" => CalculateTotalLM2Value(linkedSpecs),
                "sqm" => CalculateTotalSQMValue(linkedSpecs),
                "sqm1" => CalculateTotalSQM1Value(linkedSpecs),
                "sqm2" => CalculateTotalSQM2Value(linkedSpecs),
                "qty" or "1x" => CalculateTotalQtyValue(linkedSpecs),
                "2x" => CalculateTotalQtyValue(linkedSpecs) * 2,
                _ => 0
            };
        }

        private void CalculateOtherChargeValue(OtherChargeModel charge, SpecificationModel? spec)
        {
            if (charge == null) return;
            charge.Amount = Math.Round(charge.Value * charge.Rate, 2);
        }

        private List<SpecificationModel> GetLinkedSpecifications(OtherChargeModel charge)
        {
            var specs = new List<SpecificationModel>();
            if (Invoice?.Specifications == null) return specs;

            if (!string.IsNullOrEmpty(charge.LinkedSpecIndices))
            {
                var indices = charge.LinkedSpecIndices
                    .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out int idx) ? idx : -1)
                    .Where(idx => idx >= 0 && idx < Invoice.Specifications.Count)
                    .Distinct()
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

        private double CalculateTotalLM1Value(List<SpecificationModel> specs)
        {
            double totalLM1 = 0;
            foreach (var spec in specs)
            {
                foreach (var item in spec.Items)
                    totalLM1 += item.LM1 * item.Qty;
            }
            return Math.Round(totalLM1, 4);
        }

        private double CalculateTotalLM2Value(List<SpecificationModel> specs)
        {
            double totalLM2 = 0;
            foreach (var spec in specs)
            {
                foreach (var item in spec.Items)
                    totalLM2 += item.LM2 * item.Qty;
            }
            return Math.Round(totalLM2, 4);
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

        public double SpecTotalLM1 => SelectedTargetSpecification?.Items?.Sum(x => x.LM1 * x.Qty) ?? 0;
        public double SpecTotalLM2 => SelectedTargetSpecification?.Items?.Sum(x => x.LM2 * x.Qty) ?? 0;

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

                foreach (var spec in Invoice.Specifications)
                {
                    spec.Invoice = Invoice;
                    foreach (var item in spec.Items)
                        item.SrNo = 0;
                    spec.Recalculate();
                }

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
                ClearUndoHistory();

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

                TakeSnapshot($"Import {items.Count} items from CSV");

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
                        SurchargePercent = targetSpec.SurchargePercent,
                        Specification = targetSpec
                    };
                    newItem.Width1 = item.Width1;
                    newItem.Height1 = item.Height1;
                    newItem.Width2 = item.Width2;
                    newItem.Height2 = item.Height2;
                    targetSpec.Items.Add(newItem);
                }

                RenumberAllSrNumbers();
                Invoice.CalculateTotals();
                Invoice.IsDirty = true;
                RaiseAllInvoiceTotalsChanged();

                int importedCount = items.Count;
                StatusMessage = $"✅ Imported {importedCount} items successfully";
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

                TakeSnapshot($"Paste {rows.Length} rows from Excel");

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
                int totalItemsAdded = 0;

                for (int i = startIndex; i < rows.Length; i++)
                {
                    var columns = rows[i].Split('\t').Select(c => c.Trim()).ToArray();
                    if (columns.Length < 6) continue;
                    if (columns.Length == 0 || string.IsNullOrWhiteSpace(string.Join("", columns))) continue;

                    var item = new InvoiceItemModel { SrNo = totalItemsAdded + 1, Specification = spec };
                    if (columns.Length > 0) item.GlassRef = columns[0].Trim();
                    if (columns.Length > 1 && TryParseNumber(columns[1], out double w1)) item.Width1 = Math.Max(0, w1); else item.Width1 = defaultWidth1;
                    if (columns.Length > 2 && TryParseNumber(columns[2], out double h1)) item.Height1 = Math.Max(0, h1); else item.Height1 = defaultHeight1;
                    if (columns.Length > 3 && TryParseNumber(columns[3], out double w2)) item.Width2 = Math.Max(0, w2); else item.Width2 = defaultWidth2;
                    if (columns.Length > 4 && TryParseNumber(columns[4], out double h2)) item.Height2 = Math.Max(0, h2); else item.Height2 = defaultHeight2;
                    if (columns.Length > 5 && int.TryParse(columns[5].Trim().Replace(",", ""), out int qty)) item.Qty = Math.Max(1, qty); else item.Qty = 1;
                    if (columns.Length > 6 && TryParseNumber(columns[6], out double price)) item.Price = Math.Max(0, price); else item.Price = defaultPrice;
                    if (columns.Length > 7 && TryParseNumber(columns[7].Replace("%", ""), out double surcharge)) item.SurchargePercent = Math.Clamp(surcharge, 0, 100); else item.SurchargePercent = defaultSurcharge;

                    spec.Items.Add(item);
                    totalItemsAdded++;
                }

                RenumberAllSrNumbers();
                spec.Recalculate();
                Invoice.CalculateTotals();
                OnPropertyChanged(nameof(Invoice));
                OnPropertyChanged(nameof(SelectedTargetSpecification));
                OnPropertyChanged(nameof(Invoice.Specifications));
                OnPropertyChanged(nameof(SelectedSpecificationOtherCharges));
                Invoice.IsDirty = true;
                StatusMessage = $"✅ Pasted {totalItemsAdded} items from Excel";
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

        // ==================== DEBUG & TEST ====================
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
                    MessageBox.Show($"First item Qty: {imported.Specifications[0].Items[0].Qty}", "Test Result");
                }
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
                        report += $"\nSpec: {spec.SpecificationName}\n  Items: {spec.Items.Count}\n";
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
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadFromDailyWork(Data.Database.DailyWork dailyWork)
        {
            if (dailyWork == null) return;

            Invoice.InvoiceNo = string.IsNullOrEmpty(dailyWork.PINumber)
                ? $"PI-{DateTime.Now:yyyyMMdd}-{dailyWork.Id:D4}"
                : dailyWork.PINumber;

            Invoice.InvoiceDate = DateTime.Now;
            Invoice.ValidUntil = DateTime.Now.AddDays(30);
            Invoice.CustomerName = dailyWork.Company ?? "New Customer";
            Invoice.CustomerReference = dailyWork.CustomerReference ?? "";
            Invoice.Salesman = dailyWork.Salesman ?? "";
            Invoice.ProjectName = ExtractProjectName(dailyWork.Notes);
            Invoice.ProjectNo = dailyWork.PINumber ?? "";
            Invoice.Color = dailyWork.Color ?? "";
            Invoice.Notes = dailyWork.Notes ?? "";

            if (dailyWork.SQM > 0 || dailyWork.Qty > 0)
            {
                Invoice.Specifications.Clear();
                var spec = new SpecificationModel
                {
                    SpecificationName = "Load from DailyWork",
                    Id = 0,
                    Invoice = Invoice
                };

                double qty = dailyWork.Qty > 0 ? dailyWork.Qty : 1;
                double sqm = dailyWork.SQM > 0 ? dailyWork.SQM : 1;
                double widthMm = 1000;
                double denom = widthMm * qty;
                double heightMm = denom > 0 ? (sqm * 1000000) / denom : 1000;

                if (double.IsNaN(heightMm) || double.IsInfinity(heightMm) || heightMm <= 0)
                    heightMm = 1000;

                var item = new InvoiceItemModel
                {
                    SrNo = 1,
                    GlassRef = dailyWork.Color ?? "Clear",
                    Width1 = widthMm,
                    Height1 = heightMm,
                    Width2 = 0,
                    Height2 = 0,
                    Qty = (int)qty,
                    SurchargePercent = spec.SurchargePercent,
                    Specification = spec
                };

                item.Recalculate();
                spec.Items.Add(item);
                Invoice.Specifications.Add(spec);

                SubscribeToOtherChargeChanges();
            }

            Invoice.CalculateTotals();
            Invoice.IsDirty = true;
            ClearUndoHistory();
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

        public void CheckAndConvertToJobOrder()
        {
            if (Invoice != null &&
                Invoice.Status == "Confirmed" &&
                !IsJobOrder &&
                !string.IsNullOrEmpty(Invoice.InvoiceNo))
            {
                if (Invoice.IsConvertedToJobOrder)
                {
                    Debug.WriteLine("[ProformaInvoice] Already converted to Job Order");
                    return;
                }

                Invoice.IsConvertedToJobOrder = true;

                if (_jobOrderVM != null)
                {
                    IsJobOrder = true;
                    Debug.WriteLine($"[ProformaInvoice] ✅ Converted to Job Order: {Invoice.InvoiceNo}");
                }
                else
                {
                    Debug.WriteLine("[ProformaInvoice] ❌ JobOrderVM not set!");
                }
            }
        }

        public void LoadFromProformaInvoice(ProformaInvoiceModel pi)
        {
            if (pi == null) return;
            Debug.WriteLine($"[PIViewModel] LoadFromProformaInvoice: {pi.InvoiceNo}");

            try
            {
                ClearUndoHistory();

                using (BulkUpdateScope())
                {
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

                    Invoice.Specifications.Clear();
                    if (pi.Specifications != null)
                    {
                        foreach (var piSpec in pi.Specifications)
                        {
                            var newSpec = piSpec.DeepClone();
                            newSpec.Invoice = Invoice;
                            foreach (var item in newSpec.Items)
                                item.Specification = newSpec;
                            Invoice.Specifications.Add(newSpec);
                        }
                    }
                }

                ReconstructAfterLoad();
                SelectedTargetSpecification = Invoice.Specifications.FirstOrDefault();
                SelectedSpecificationId = SelectedTargetSpecification?.Id ?? 0;
                Invoice.CalculateTotals();
                Invoice.IsDirty = false;
                CurrentFileName = pi.InvoiceNo ?? "Loaded Invoice";

                ClearUndoHistory();
                Debug.WriteLine($"[PIViewModel] Loaded {Invoice.Specifications.Count} specs with full reconstruction");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PIViewModel] LoadFromProformaInvoice ERROR: {ex.Message}");
                MessageBox.Show($"Error loading invoice: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public ModelValidationResult ValidateInvoice()
        {
            return Invoice?.Validate() ?? new ModelValidationResult();
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

        // ═══════════════════════════════════════════════════════
        // ✏️ UNDO / REDO IMPLEMENTATION — Excel-style
        // ═══════════════════════════════════════════════════════

        public bool CanUndo => _undoStack.Count > 0 && !_isRestoringSnapshot;
        public bool CanRedo => _redoStack.Count > 0 && !_isRestoringSnapshot;

        /// <summary>
        /// Captures the current WHOLE invoice state as a snapshot.
        /// Excel-style: every committed change = one snapshot.
        /// </summary>
        public void TakeSnapshot(string description)
        {
            if (_isRestoringSnapshot) return;
            if (_isBulkUpdating) return; // skip during bulk updates (e.g., paste, load)
            if (Invoice == null) return;

            try
            {
                var snapshot = new UndoSnapshot
                {
                    Description = description,
                    SelectedSpecId = SelectedTargetSpecification?.Id ?? 0,
                    InvoiceJson = JsonConvert.SerializeObject(Invoice, Formatting.None, _jsonSettings)
                };

                _undoStack.Push(snapshot);

                while (_undoStack.Count > MAX_UNDO_HISTORY)
                {
                    var arr = _undoStack.ToArray();
                    _undoStack.Clear();
                    for (int i = arr.Length - 2; i >= 0; i--)
                        _undoStack.Push(arr[i]);
                }

                _redoStack.Clear();

                _lastSnapshotKey = description;
                _lastSnapshotTime = DateTime.Now;

                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));

                Debug.WriteLine($"{LOG} Snapshot: '{description}' (stack: {_undoStack.Count})");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} TakeSnapshot error: {ex.Message}");
            }
        }

        /// <summary>
        /// Like TakeSnapshot, but coalesces rapid changes on the same field
        /// within COALESCE_WINDOW_MS into a single snapshot.
        /// Excel uses this for typing — one snapshot per field per "edit session".
        /// </summary>
        public void TakeSnapshotCoalesced(string description, string fieldKey)
        {
            if (_isRestoringSnapshot) return;
            if (_isBulkUpdating) return;

            // If same field was just snapshotted within the window, skip
            if (_lastSnapshotKey == fieldKey &&
                (DateTime.Now - _lastSnapshotTime).TotalMilliseconds < COALESCE_WINDOW_MS)
            {
                Debug.WriteLine($"{LOG} Snapshot coalesced (skipped): '{fieldKey}'");
                return;
            }

            TakeSnapshot(description);
            _lastSnapshotKey = fieldKey; // remember field-level key for next coalesce check
        }

        public void Undo()
        {
            if (!CanUndo) return;

            try
            {
                var currentSnapshot = new UndoSnapshot
                {
                    Description = "(current)",
                    SelectedSpecId = SelectedTargetSpecification?.Id ?? 0,
                    InvoiceJson = JsonConvert.SerializeObject(Invoice, Formatting.None, _jsonSettings)
                };
                _redoStack.Push(currentSnapshot);

                var snapshot = _undoStack.Pop();
                RestoreSnapshot(snapshot);

                StatusMessage = $"↶ Undone: {snapshot.Description}";
                Debug.WriteLine($"{LOG} Undo: '{snapshot.Description}' (undo:{_undoStack.Count}, redo:{_redoStack.Count})");

                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} Undo error: {ex.Message}");
                StatusMessage = $"❌ Undo failed: {ex.Message}";
            }
        }

        public void Redo()
        {
            if (!CanRedo) return;

            try
            {
                var currentSnapshot = new UndoSnapshot
                {
                    Description = "(current)",
                    SelectedSpecId = SelectedTargetSpecification?.Id ?? 0,
                    InvoiceJson = JsonConvert.SerializeObject(Invoice, Formatting.None, _jsonSettings)
                };
                _undoStack.Push(currentSnapshot);

                var snapshot = _redoStack.Pop();
                RestoreSnapshot(snapshot);

                StatusMessage = $"↷ Redone: {snapshot.Description}";
                Debug.WriteLine($"{LOG} Redo: '{snapshot.Description}' (undo:{_undoStack.Count}, redo:{_redoStack.Count})");

                OnPropertyChanged(nameof(CanUndo));
                OnPropertyChanged(nameof(CanRedo));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} Redo error: {ex.Message}");
                StatusMessage = $"❌ Redo failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Restore the WHOLE Invoice from snapshot JSON.
        /// Covers Customer/Project/LPO/Notes/Specs/Charges — everything.
        /// </summary>
        private void RestoreSnapshot(UndoSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.InvoiceJson)) return;

            _isRestoringSnapshot = true;
            try
            {
                DetachAllEventHandlers();

                var restored = JsonConvert.DeserializeObject<ProformaInvoiceModel>(snapshot.InvoiceJson, _jsonSettings);
                if (restored == null) return;

                using (BulkUpdateScope())
                {
                    // Copy scalar invoice fields
                    Invoice.InvoiceNo = restored.InvoiceNo;
                    Invoice.InvoiceDate = restored.InvoiceDate;
                    Invoice.ValidUntil = restored.ValidUntil;
                    Invoice.Status = restored.Status;
                    Invoice.CustomerName = restored.CustomerName;
                    Invoice.CustomerTRN = restored.CustomerTRN;
                    Invoice.CustomerReference = restored.CustomerReference;
                    Invoice.Salesman = restored.Salesman;
                    Invoice.CustomerAddress = restored.CustomerAddress;
                    Invoice.ProjectName = restored.ProjectName;
                    Invoice.ProjectNo = restored.ProjectNo;
                    Invoice.ProjectLocation = restored.ProjectLocation;
                    Invoice.LPONo = restored.LPONo;
                    Invoice.AttentionName = restored.AttentionName;
                    Invoice.ContactNo = restored.ContactNo;
                    Invoice.Color = restored.Color;
                    Invoice.Notes = restored.Notes;
                    Invoice.IsConvertedToJobOrder = restored.IsConvertedToJobOrder;

                    // Replace specifications wholesale
                    Invoice.Specifications.Clear();
                    if (restored.Specifications != null)
                    {
                        foreach (var spec in restored.Specifications)
                        {
                            spec.Invoice = Invoice;
                            if (spec.Items != null)
                            {
                                foreach (var item in spec.Items)
                                    item.Specification = spec;
                            }
                            Invoice.Specifications.Add(spec);
                        }
                    }
                }

                AttachAllEventHandlers();
                RefreshAllChargeAutoValues();
                RenumberAllSrNumbers();
                Invoice.CalculateTotals();
                Invoice.IsDirty = true;

                SelectedTargetSpecification = Invoice.Specifications.FirstOrDefault(s => s.Id == snapshot.SelectedSpecId)
                                              ?? Invoice.Specifications.FirstOrDefault();
                SelectedSpecificationId = SelectedTargetSpecification?.Id ?? 0;

                // Notify everything that may have changed
                OnPropertyChanged(nameof(Invoice));
                OnPropertyChanged(nameof(SelectedTargetSpecification));
                OnPropertyChanged(nameof(SelectedSpecificationOtherCharges));
                RaiseAllInvoiceTotalsChanged();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{LOG} RestoreSnapshot error: {ex.Message}");
            }
            finally
            {
                _isRestoringSnapshot = false;
            }
        }

        public void ClearUndoHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _lastSnapshotKey = "";
            _lastSnapshotTime = DateTime.MinValue;
            OnPropertyChanged(nameof(CanUndo));
            OnPropertyChanged(nameof(CanRedo));
            Debug.WriteLine($"{LOG} Undo/Redo history cleared");
        }
    }
}