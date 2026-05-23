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

namespace ProGlassAutomation.ViewModels
{
    // ==================== HELPER CLASSES ====================
    public class DimensionOption
    {
        public string Value { get; set; }
        public string Label { get; set; }
    }

    public class ChargeTypeOption
    {
        public string Value { get; set; }
        public string Label { get; set; }
    }

    // ==================== FILE LIST ITEM ====================
    public class FileListItem
    {
        public string FilePath { get; set; }
        public string InvoiceNo { get; set; }
        public string CustomerName { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string FileName => Path.GetFileNameWithoutExtension(FilePath);
        public string DateDisplay => InvoiceDate.ToString("dd MMM yyyy");
    }

    // ==================== MAIN VIEWMODEL ====================
    public class ProformaInvoiceViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly ExcelCsvService _excelCsvService;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ==================== INVOICE NUMBER GENERATION ====================
        private int _currentPINumber = 0;
        public int CurrentPINumber
        {
            get => _currentPINumber;
            set { _currentPINumber = value; OnPropertyChanged(); }
        }

        private string GetNextInvoiceNo()
        {
            CurrentPINumber++;
            return $"PI-{DateTime.Now.Year}-{CurrentPINumber:D2}";
        }

        private ProformaInvoiceModel _invoice = new();
        public ProformaInvoiceModel Invoice
        {
            get => _invoice;
            set
            {
                _invoice = value;
                OnPropertyChanged();
                SubscribeToOtherChargeChanges();
            }
        }

        private ObservableCollection<FileListItem> _savedFiles = new();
        public ObservableCollection<FileListItem> SavedFiles
        {
            get => _savedFiles;
            set { _savedFiles = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasSavedFiles)); }
        }

        public bool HasSavedFiles => SavedFiles?.Any() == true;

        private string _currentFileName = "Untitled";
        public string CurrentFileName
        {
            get => _currentFileName;
            set { _currentFileName = value; OnPropertyChanged(); }
        }

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private bool _isLMVisible = true;
        public bool IsLMVisible
        {
            get => _isLMVisible;
            set { _isLMVisible = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsLMToggleText)); }
        }

        public string IsLMToggleText => IsLMVisible ? "HIDE LM" : "SHOW LM";

        // ==================== COMPANY DETAILS ====================
        private string _companyName = "PROGLASS AUTOMATION";
        public string CompanyName
        {
            get => _companyName;
            set { _companyName = value; OnPropertyChanged(); }
        }

        private string _companyTRN = "100458979400003";
        public string CompanyTRN
        {
            get => _companyTRN;
            set { _companyTRN = value; OnPropertyChanged(); }
        }

        private string _companyLocation = "Dubai, UAE";
        public string CompanyLocation
        {
            get => _companyLocation;
            set { _companyLocation = value; OnPropertyChanged(); }
        }

        private string _companyPhone = "+971-50-123-4567";
        public string CompanyPhone
        {
            get => _companyPhone;
            set { _companyPhone = value; OnPropertyChanged(); }
        }

        // ==================== MODULE SELECTION ====================
        private bool _isSGUSelected = true;
        public bool IsSGUSelected
        {
            get => _isSGUSelected;
            set
            {
                _isSGUSelected = value;
                OnPropertyChanged();
                if (value) { IsDGUSelected = false; IsLAMSelected = false; }
            }
        }

        private bool _isDGUSelected;
        public bool IsDGUSelected
        {
            get => _isDGUSelected;
            set
            {
                _isDGUSelected = value;
                OnPropertyChanged();
                if (value) { IsSGUSelected = false; IsLAMSelected = false; }
            }
        }

        private bool _isLAMSelected;
        public bool IsLAMSelected
        {
            get => _isLAMSelected;
            set
            {
                _isLAMSelected = value;
                OnPropertyChanged();
                if (value) { IsSGUSelected = false; IsDGUSelected = false; }
            }
        }

        // ==================== DROPDOWN OPTIONS ====================
        public ObservableCollection<string> ThicknessOptions { get; } = new ObservableCollection<string>
        {
            "4", "5", "6", "8", "10", "12", "15", "19"
        };

        private ObservableCollection<string> _colorHistory = new ObservableCollection<string>
        {
            "Clear", "Grey", "Green", "Blue", "Bronze", "Black"
        };
        public ObservableCollection<string> ColorHistory
        {
            get => _colorHistory;
            set { _colorHistory = value; OnPropertyChanged(); }
        }

        // ==================== LM DIMENSION OPTIONS ====================
        public ObservableCollection<DimensionOption> LMDimensionOptions { get; } = new ObservableCollection<DimensionOption>
        {
            new DimensionOption { Value = "w1h1", Label = "2×(W1+H1) — Single Glass Polish" },
            new DimensionOption { Value = "4w1h1", Label = "4×(W1+H1) — DGU/LAM Polish" },
            new DimensionOption { Value = "2w1", Label = "2×W1 — Both Widths" },
            new DimensionOption { Value = "2h1", Label = "2×H1 — Both Heights" },
            new DimensionOption { Value = "w1_only", Label = "1×W1 — One Width" },
            new DimensionOption { Value = "h1_only", Label = "1×H1 — One Height" },
            new DimensionOption { Value = "w2h2", Label = "2×(W2+H2) — W2/H2 Perimeter" },
            new DimensionOption { Value = "2w2", Label = "2×W2" },
            new DimensionOption { Value = "2h2", Label = "2×H2" },
            new DimensionOption { Value = "w2_only", Label = "1×W2" },
            new DimensionOption { Value = "h2_only", Label = "1×H2" }
        };

        // ==================== CHARGE TYPE OPTIONS ====================
        public ObservableCollection<ChargeTypeOption> ChargeTypeOptions { get; } = new ObservableCollection<ChargeTypeOption>
        {
            new ChargeTypeOption { Value = "amount", Label = "amount" },
            new ChargeTypeOption { Value = "lm", Label = "lm" },
            new ChargeTypeOption { Value = "sqm", Label = "sqm" },
            new ChargeTypeOption { Value = "qty", Label = "qty" },
            new ChargeTypeOption { Value = "holes", Label = "holes (2X)" },
            new ChargeTypeOption { Value = "cutout", Label = "cutout (2X)" }
        };

        // ==================== SGU FIELDS ====================
        private string _selectedThickness = "6";
        public string SelectedThickness
        {
            get => _selectedThickness;
            set { _selectedThickness = value; OnPropertyChanged(); }
        }

        private string _selectedColor = "Clear";
        public string SelectedColor
        {
            get => _selectedColor;
            set { _selectedColor = value; OnPropertyChanged(); AddToColorHistory(value); }
        }

        private bool _isAnnealedSelected = true;
        public bool IsAnnealedSelected
        {
            get => _isAnnealedSelected;
            set { _isAnnealedSelected = value; OnPropertyChanged(); }
        }

        private bool _isFTSelected;
        public bool IsFTSelected
        {
            get => _isFTSelected;
            set { _isFTSelected = value; OnPropertyChanged(); }
        }

        public string WorkTypeText => IsFTSelected ? "FT Glass" : "Annealed";

        private double _sGUSheetPrice = 150;
        public double SGUSheetPrice
        {
            get => _sGUSheetPrice;
            set { _sGUSheetPrice = value; OnPropertyChanged(); }
        }

        private double _sGUCutting = 5;
        public double SGUCutting
        {
            get => _sGUCutting;
            set { _sGUCutting = value; OnPropertyChanged(); }
        }

        private double _sGUTempering = 10;
        public double SGUTempering
        {
            get => _sGUTempering;
            set { _sGUTempering = value; OnPropertyChanged(); }
        }

        private double _sGUWasteFactor = 1.1;
        public double SGUWasteFactor
        {
            get => _sGUWasteFactor;
            set { _sGUWasteFactor = value; OnPropertyChanged(); }
        }

        private double _sGUProfitPercent = 15;
        public double SGUProfitPercent
        {
            get => _sGUProfitPercent;
            set { _sGUProfitPercent = value; OnPropertyChanged(); }
        }

        // ==================== DGU FIELDS ====================
        private bool _isDGUAnnealedSelected = true;
        public bool IsDGUAnnealedSelected
        {
            get => _isDGUAnnealedSelected;
            set
            {
                _isDGUAnnealedSelected = value;
                OnPropertyChanged();
                if (value) IsDGUFTSelected = false;
            }
        }

        private bool _isDGUFTSelected;
        public bool IsDGUFTSelected
        {
            get => _isDGUFTSelected;
            set
            {
                _isDGUFTSelected = value;
                OnPropertyChanged();
                if (value) IsDGUAnnealedSelected = false;
            }
        }

        public string DGUWorkTypeText => IsDGUFTSelected ? "FT Glass" : "Annealed";

        private bool _isDGUIncludeInSpec = true;
        public bool IsDGUIncludeInSpec
        {
            get => _isDGUIncludeInSpec;
            set
            {
                _isDGUIncludeInSpec = value;
                OnPropertyChanged();
                if (value) IsDGUInternalOnly = false;
            }
        }

        private bool _isDGUInternalOnly;
        public bool IsDGUInternalOnly
        {
            get => _isDGUInternalOnly;
            set
            {
                _isDGUInternalOnly = value;
                OnPropertyChanged();
                if (value) IsDGUIncludeInSpec = false;
            }
        }

        private string _dGUOuterThickness = "6";
        public string DGUOuterThickness
        {
            get => _dGUOuterThickness;
            set { _dGUOuterThickness = value; OnPropertyChanged(); }
        }

        private string _dGUOuterColor = "Clear";
        public string DGUOuterColor
        {
            get => _dGUOuterColor;
            set { _dGUOuterColor = value; OnPropertyChanged(); AddToColorHistory(value); }
        }

        private double _dGUOuterPrice = 100;
        public double DGUOuterPrice
        {
            get => _dGUOuterPrice;
            set { _dGUOuterPrice = value; OnPropertyChanged(); }
        }

        private string _dGUSpacerThickness = "12";
        public string DGUSpacerThickness
        {
            get => _dGUSpacerThickness;
            set { _dGUSpacerThickness = value; OnPropertyChanged(); }
        }

        private double _dGUASPPrice = 15;
        public double DGUASPPrice
        {
            get => _dGUASPPrice;
            set { _dGUASPPrice = value; OnPropertyChanged(); }
        }

        private string _dGUInnerThickness = "6";
        public string DGUInnerThickness
        {
            get => _dGUInnerThickness;
            set { _dGUInnerThickness = value; OnPropertyChanged(); }
        }

        private string _dGUInnerColor = "Clear";
        public string DGUInnerColor
        {
            get => _dGUInnerColor;
            set { _dGUInnerColor = value; OnPropertyChanged(); AddToColorHistory(value); }
        }

        private double _dGUInnerPrice = 100;
        public double DGUInnerPrice
        {
            get => _dGUInnerPrice;
            set { _dGUInnerPrice = value; OnPropertyChanged(); }
        }

        private double _dGUWasteFactor = 1.1;
        public double DGUWasteFactor
        {
            get => _dGUWasteFactor;
            set { _dGUWasteFactor = value; OnPropertyChanged(); }
        }

        private double _dGUProfitPercent = 15;
        public double DGUProfitPercent
        {
            get => _dGUProfitPercent;
            set { _dGUProfitPercent = value; OnPropertyChanged(); }
        }

        // ==================== LAM FIELDS ====================
        private bool _isLAMAnnealedSelected = true;
        public bool IsLAMAnnealedSelected
        {
            get => _isLAMAnnealedSelected;
            set
            {
                _isLAMAnnealedSelected = value;
                OnPropertyChanged();
                if (value) IsLAMFTSelected = false;
            }
        }

        private bool _isLAMFTSelected;
        public bool IsLAMFTSelected
        {
            get => _isLAMFTSelected;
            set
            {
                _isLAMFTSelected = value;
                OnPropertyChanged();
                if (value) IsLAMAnnealedSelected = false;
            }
        }

        public string LAMWorkTypeText => IsLAMFTSelected ? "FT Glass" : "Annealed";

        private string _lAMOuterThickness = "6";
        public string LAMOuterThickness
        {
            get => _lAMOuterThickness;
            set { _lAMOuterThickness = value; OnPropertyChanged(); }
        }

        private string _lAMOuterColor = "Clear";
        public string LAMOuterColor
        {
            get => _lAMOuterColor;
            set { _lAMOuterColor = value; OnPropertyChanged(); AddToColorHistory(value); }
        }

        private double _lAMOuterPrice = 100;
        public double LAMOuterPrice
        {
            get => _lAMOuterPrice;
            set { _lAMOuterPrice = value; OnPropertyChanged(); }
        }

        private string _lAMPVBThickness = "0.76";
        public string LAMPVBThickness
        {
            get => _lAMPVBThickness;
            set { _lAMPVBThickness = value; OnPropertyChanged(); }
        }

        private string _lAMPVBColor = "Clear";
        public string LAMPVBColor
        {
            get => _lAMPVBColor;
            set { _lAMPVBColor = value; OnPropertyChanged(); }
        }

        private double _lAMPVBPrice = 25;
        public double LAMPVBPrice
        {
            get => _lAMPVBPrice;
            set { _lAMPVBPrice = value; OnPropertyChanged(); }
        }

        private string _lAMInnerThickness = "6";
        public string LAMInnerThickness
        {
            get => _lAMInnerThickness;
            set { _lAMInnerThickness = value; OnPropertyChanged(); }
        }

        private string _lAMInnerColor = "Clear";
        public string LAMInnerColor
        {
            get => _lAMInnerColor;
            set { _lAMInnerColor = value; OnPropertyChanged(); AddToColorHistory(value); }
        }

        private double _lAMInnerPrice = 100;
        public double LAMInnerPrice
        {
            get => _lAMInnerPrice;
            set { _lAMInnerPrice = value; OnPropertyChanged(); }
        }

        private double _lAMCutting = 5;
        public double LAMCutting
        {
            get => _lAMCutting;
            set { _lAMCutting = value; OnPropertyChanged(); }
        }

        private double _lAMTempering = 10;
        public double LAMTempering
        {
            get => _lAMTempering;
            set { _lAMTempering = value; OnPropertyChanged(); }
        }

        private double _lAMWasteFactor = 1.1;
        public double LAMWasteFactor
        {
            get => _lAMWasteFactor;
            set { _lAMWasteFactor = value; OnPropertyChanged(); }
        }

        private double _lAMProfitPercent = 15;
        public double LAMProfitPercent
        {
            get => _lAMProfitPercent;
            set { _lAMProfitPercent = value; OnPropertyChanged(); }
        }

        // ==================== GENERATED OUTPUT ====================
        private string _generatedDescription = "";
        public string GeneratedDescription
        {
            get => _generatedDescription;
            set { _generatedDescription = value; OnPropertyChanged(); }
        }

        private double _calculatedPrice = 0;
        public double CalculatedPrice
        {
            get => _calculatedPrice;
            set { _calculatedPrice = value; OnPropertyChanged(); }
        }

        private string _priceCalculationSummary = "";
        public string PriceCalculationSummary
        {
            get => _priceCalculationSummary;
            set { _priceCalculationSummary = value; OnPropertyChanged(); }
        }

        // ==================== TARGET SPECIFICATION ====================
        private SpecificationModel _selectedTargetSpecification;
        public SpecificationModel SelectedTargetSpecification
        {
            get => _selectedTargetSpecification;
            set
            {
                _selectedTargetSpecification = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedSpecificationOtherCharges));
            }
        }

        public ObservableCollection<OtherChargeModel> SelectedSpecificationOtherCharges
        {
            get
            {
                if (SelectedTargetSpecification?.OtherCharges == null)
                    return new ObservableCollection<OtherChargeModel>();
                return SelectedTargetSpecification.OtherCharges;
            }
        }

        private int _selectedSpecificationId;
        public int SelectedSpecificationId
        {
            get => _selectedSpecificationId;
            set
            {
                _selectedSpecificationId = value;
                OnPropertyChanged();

                if (Invoice?.Specifications != null)
                {
                    SelectedTargetSpecification = Invoice.Specifications
                        .FirstOrDefault(s => s.Id == value);
                }
            }
        }

        // ==================== COMMANDS ====================
        public ICommand NewInvoiceCommand { get; }
        public ICommand SaveInvoiceCommand { get; }
        public ICommand OpenInvoiceCommand { get; }
        public ICommand DeleteInvoiceCommand { get; }
        public ICommand AddSpecificationCommand { get; }
        public ICommand RemoveSpecificationCommand { get; }
        public ICommand ToggleLMCommand { get; }
        public ICommand CalculatePriceCommand { get; }
        public ICommand IncludeInSpecificationCommand { get; }
        public ICommand PrintCommand { get; }

        // Module Selection Commands
        public ICommand SelectSGUCommand { get; }
        public ICommand SelectDGUCommand { get; }
        public ICommand SelectLAMCommand { get; }

        // Import/Export Commands
        public ICommand ExportCsvCommand { get; }
        public ICommand ImportCsvCommand { get; }
        public ICommand ImportItemsCommand { get; }

        // ==================== OTHER CHARGES COMMANDS ====================
        public ICommand AddOtherChargeCommand { get; }
        public ICommand RemoveOtherChargeCommand { get; }
        public ICommand CalculateOtherChargeCommand { get; }

        // Constructor
        public ProformaInvoiceViewModel()
        {
            _excelCsvService = new ExcelCsvService();
            _excelCsvService.StatusChanged += status =>
            {
                Application.Current.Dispatcher.Invoke(() => StatusMessage = status);
            };

            // Initialize with auto-generated invoice number (PI-2026-01 format)
            Invoice = new ProformaInvoiceModel
            {
                InvoiceNo = GetNextInvoiceNo(),
                InvoiceDate = DateTime.Now,
                ValidUntil = DateTime.Now.AddDays(30)
            };

            AddSpecification();

            // Standard Commands
            NewInvoiceCommand = new RelayCommand(_ => NewInvoice());
            SaveInvoiceCommand = new RelayCommand(_ => SaveInvoice());
            OpenInvoiceCommand = new RelayCommand(_ => OpenInvoice());
            DeleteInvoiceCommand = new RelayCommand(_ => DeleteInvoice());
            AddSpecificationCommand = new RelayCommand(_ => AddSpecification());
            RemoveSpecificationCommand = new RelayCommand(_ => RemoveSpecification(), _ => Invoice.Specifications.Count > 0);
            ToggleLMCommand = new RelayCommand(_ => ToggleLM());
            CalculatePriceCommand = new RelayCommand(_ => CalculatePrice());
            IncludeInSpecificationCommand = new RelayCommand(_ => IncludeInSpecification(), _ => CanIncludeInSpecification());
            PrintCommand = new RelayCommand(_ => PrintInvoice());

            // Module Selection Commands
            SelectSGUCommand = new RelayCommand(_ => SelectSGU());
            SelectDGUCommand = new RelayCommand(_ => SelectDGU());
            SelectLAMCommand = new RelayCommand(_ => SelectLAM());

            // Import/Export Commands
            ExportCsvCommand = new RelayCommand(_ => ExportToCsv());
            ImportCsvCommand = new RelayCommand(_ => ImportFromCsv());
            ImportItemsCommand = new RelayCommand(_ => ImportItemsFromCsv());

            // Other Charges Commands
            AddOtherChargeCommand = new RelayCommand(_ => AddOtherCharge());
            RemoveOtherChargeCommand = new RelayCommand(param => RemoveOtherCharge(param as OtherChargeModel));
            CalculateOtherChargeCommand = new RelayCommand(_ => CalculateAllOtherCharges());

            LoadSavedFiles();
        }

        private bool CanIncludeInSpecification()
        {
            return CalculatedPrice > 0 &&
                   SelectedTargetSpecification != null &&
                   Invoice?.Specifications?.Contains(SelectedTargetSpecification) == true;
        }

        // ==================== MODULE SELECTION METHODS ====================
        private void SelectSGU()
        {
            IsSGUSelected = true;
            IsDGUSelected = false;
            IsLAMSelected = false;
        }

        private void SelectDGU()
        {
            IsDGUSelected = true;
            IsSGUSelected = false;
            IsLAMSelected = false;
        }

        private void SelectLAM()
        {
            IsLAMSelected = true;
            IsSGUSelected = false;
            IsDGUSelected = false;
        }

        // ==================== COLOR HISTORY ====================
        private void AddToColorHistory(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) return;
            if (!ColorHistory.Contains(color))
            {
                ColorHistory.Add(color);
            }
        }

        // ==================== METHODS ====================

        private void NewInvoice()
        {
            if (Invoice.IsDirty)
            {
                var result = MessageBox.Show("Save changes before creating new invoice?",
                    "Save Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) SaveInvoice();
                else if (result == MessageBoxResult.Cancel) return;
            }

            // Create new invoice with auto-generated number (PI-2026-01 format)
            Invoice = new ProformaInvoiceModel
            {
                InvoiceNo = GetNextInvoiceNo(),
                InvoiceDate = DateTime.Now,
                ValidUntil = DateTime.Now.AddDays(30)
            };
            CurrentFileName = "Untitled";

            AddSpecification();

            LoadSavedFiles();
        }

        private void SaveInvoice()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    InitialDirectory = GetDataFolder(),
                    FileName = $"{Invoice.InvoiceNo}.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    Invoice.CalculateTotals();
                    string json = JsonConvert.SerializeObject(Invoice, Formatting.Indented);
                    File.WriteAllText(dialog.FileName, json);
                    Invoice.IsDirty = false;
                    CurrentFileName = Path.GetFileNameWithoutExtension(dialog.FileName);
                    LoadSavedFiles();
                    StatusMessage = $"✅ Invoice saved: {CurrentFileName}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenInvoice()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    InitialDirectory = GetDataFolder()
                };

                if (dialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var invoice = JsonConvert.DeserializeObject<ProformaInvoiceModel>(json);
                    if (invoice != null)
                    {
                        Invoice = invoice;
                        CurrentFileName = Path.GetFileNameWithoutExtension(dialog.FileName);
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
            try
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete '{CurrentFileName}'?\nThis action cannot be undone.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    string filePath = Path.Combine(GetDataFolder(), $"{CurrentFileName}.json");
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        LoadSavedFiles();
                        StatusMessage = $"✅ Deleted: {CurrentFileName}";
                        NewInvoice();
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Delete failed: {ex.Message}";
                MessageBox.Show($"Delete failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintInvoice()
        {
            try
            {
                var window = Application.Current.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.DataContext == this);

                if (window != null)
                {
                    var printDialog = new System.Windows.Controls.PrintDialog();
                    if (printDialog.ShowDialog() == true)
                    {
                        printDialog.PrintVisual(window.Content as System.Windows.Media.Visual, "ProForma Invoice");
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
                MessageBox.Show($"Print failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetDataFolder()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return folder;
        }

        private void LoadSavedFiles()
        {
            SavedFiles.Clear();
            string folder = GetDataFolder();

            if (Directory.Exists(folder))
            {
                foreach (var file in Directory.GetFiles(folder, "*.json").OrderByDescending(f => new FileInfo(f).LastWriteTime))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var invoice = JsonConvert.DeserializeObject<ProformaInvoiceModel>(json);
                        if (invoice != null)
                        {
                            SavedFiles.Add(new FileListItem
                            {
                                FilePath = file,
                                InvoiceNo = invoice.InvoiceNo,
                                CustomerName = invoice.CustomerName,
                                InvoiceDate = invoice.InvoiceDate
                            });
                        }
                    }
                    catch { }
                }
            }
            OnPropertyChanged(nameof(HasSavedFiles));
        }

        private void AddSpecification()
        {
            var spec = new SpecificationModel
            {
                SpecificationName = $"Specification {Invoice.Specifications.Count + 1}",
                Id = Invoice.Specifications.Count
            };

            var firstItem = new InvoiceItemModel
            {
                SrNo = 1,
                SurchargePercent = 20
            };
            spec.Items.Add(firstItem);

            Invoice.Specifications.Add(spec);
            SelectedTargetSpecification = spec;

            SubscribeToOtherChargeChanges();
        }

        private void RemoveSpecification()
        {
            if (Invoice.Specifications.Count > 0)
            {
                Invoice.Specifications.RemoveAt(Invoice.Specifications.Count - 1);
                if (Invoice.Specifications.Count > 0)
                    SelectedTargetSpecification = Invoice.Specifications.Last();
                else
                    SelectedTargetSpecification = null;
            }
        }

        public void AddItemWithPrice(SpecificationModel spec)
        {
            if (spec == null) return;

            var newItem = new InvoiceItemModel { SrNo = spec.Items.Count + 1 };

            if (spec.Items.Count > 0)
            {
                var firstRow = spec.Items[0];
                newItem.Price = firstRow.Price;
                newItem.SurchargePercent = firstRow.SurchargePercent;
            }

            spec.Items.Add(newItem);
            Invoice.IsDirty = true;
        }

        public void RemoveItem(InvoiceItemModel item)
        {
            if (item == null) return;
            var spec = Invoice.Specifications.FirstOrDefault(s => s.Items.Contains(item));
            if (spec != null && spec.Items.Count > 1)
            {
                spec.Items.Remove(item);
                for (int i = 0; i < spec.Items.Count; i++)
                    spec.Items[i].SrNo = i + 1;
                Invoice.IsDirty = true;
            }
        }

        private void ToggleLM()
        {
            IsLMVisible = !IsLMVisible;
        }

        // ==================== OTHER CHARGES METHODS ====================
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
                        charge.PropertyChanged -= OtherCharge_PropertyChanged;
                        charge.PropertyChanged += OtherCharge_PropertyChanged;
                    }
                }
            }
        }

        private void OtherCharge_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is not OtherChargeModel charge) return;

            // When Type changes, recalculate Value
            if (e.PropertyName == nameof(OtherChargeModel.Type))
            {
                UpdateChargeValue(charge);
            }

            // When LinkedSpecIndex changes, recalculate Value
            if (e.PropertyName == nameof(OtherChargeModel.LinkedSpecIndex))
            {
                UpdateChargeValue(charge);
            }

            // Update totals when any property changes
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

            var charge = new OtherChargeModel
            {
                Name = "New Charge",
                Type = "amount",
                Value = 1,
                Rate = 0,
                Amount = 0,
                LmDimType = "w1h1",
                LinkedSpecIndex = SelectedTargetSpecification.Id
            };

            SelectedTargetSpecification.OtherCharges.Add(charge);
            Invoice.IsDirty = true;
            OnPropertyChanged(nameof(SelectedSpecificationOtherCharges));

            // Subscribe to property changes
            charge.PropertyChanged += OtherCharge_PropertyChanged;

            // Auto-calculate value based on type
            UpdateChargeValue(charge);
        }

        private void RemoveOtherCharge(OtherChargeModel charge)
        {
            if (charge == null || SelectedTargetSpecification == null) return;

            charge.PropertyChanged -= OtherCharge_PropertyChanged;
            SelectedTargetSpecification.OtherCharges.Remove(charge);
            Invoice.IsDirty = true;
            SelectedTargetSpecification.CalculateOtherChargesTotal();
            Invoice.CalculateTotals();
            OnPropertyChanged(nameof(SelectedSpecificationOtherCharges));
        }

        private void CalculateAllOtherCharges()
        {
            if (SelectedTargetSpecification == null) return;

            foreach (var charge in SelectedTargetSpecification.OtherCharges)
            {
                CalculateOtherChargeValue(charge, SelectedTargetSpecification);
            }

            SelectedTargetSpecification.CalculateOtherChargesTotal();
            Invoice.CalculateTotals();
            Invoice.IsDirty = true;
        }

        private void UpdateChargeValue(OtherChargeModel charge)
        {
            if (charge == null || SelectedTargetSpecification == null) return;

            switch (charge.Type?.ToLower())
            {
                case "lm":
                    charge.Value = CalculateLMValue(SelectedTargetSpecification, charge.LmDimType);
                    break;
                case "sqm":
                    charge.Value = CalculateSQMValue(SelectedTargetSpecification);
                    break;
                case "qty":
                    charge.Value = CalculateQtyValue(SelectedTargetSpecification);
                    break;
                case "holes":
                    charge.Value = CalculateQtyValue(SelectedTargetSpecification) * 2;
                    break;
                case "cutout":
                    charge.Value = CalculateQtyValue(SelectedTargetSpecification) * 2;
                    break;
                case "amount":
                default:
                    charge.Value = 1;
                    break;
            }
        }

        private void CalculateOtherChargeValue(OtherChargeModel charge, SpecificationModel spec)
        {
            if (charge == null || spec == null) return;

            switch (charge.Type?.ToLower())
            {
                case "lm":
                    charge.Value = CalculateLMValue(spec, charge.LmDimType);
                    break;
                case "sqm":
                    charge.Value = CalculateSQMValue(spec);
                    break;
                case "qty":
                    charge.Value = CalculateQtyValue(spec);
                    break;
                case "holes":
                    charge.Value = CalculateQtyValue(spec) * 2;
                    break;
                case "cutout":
                    charge.Value = CalculateQtyValue(spec) * 2;
                    break;
                case "amount":
                default:
                    charge.Value = 1;
                    break;
            }

            charge.Amount = Math.Round(charge.Value * charge.Rate, 2);
        }

        private double CalculateLMValue(SpecificationModel spec, string dimType)
        {
            double totalLM = 0;
            int multiplier = GetModuleMultiplier(spec.ModuleType);

            foreach (var item in spec.Items)
            {
                double lm = CalculateRowLM(item, dimType);
                totalLM += lm * item.Qty * multiplier;
            }

            return Math.Round(totalLM, 4);
        }

        private double CalculateRowLM(InvoiceItemModel item, string dimType)
        {
            double w1 = item.Width1 / 1000.0;
            double h1 = item.Height1 / 1000.0;
            double w2 = item.Width2 / 1000.0;
            double h2 = item.Height2 / 1000.0;

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

        private double CalculateSQMValue(SpecificationModel spec)
        {
            double totalSQM = 0;

            foreach (var item in spec.Items)
            {
                totalSQM += item.TotalSQM;
            }

            return Math.Round(totalSQM, 4);
        }

        private double CalculateQtyValue(SpecificationModel spec)
        {
            int totalQty = 0;
            int multiplier = GetModuleMultiplier(spec.ModuleType);

            foreach (var item in spec.Items)
            {
                totalQty += item.Qty * multiplier;
            }

            return totalQty;
        }

        private int GetModuleMultiplier(string moduleType)
        {
            return moduleType switch
            {
                "DGU" => 2,
                "LAM" => 2,
                _ => 1
            };
        }

        // ==================== CALCULATE PRICE ====================
        private void CalculatePrice()
        {
            if (IsSGUSelected)
                CalculateSGUPrice();
            else if (IsDGUSelected)
                CalculateDGUPrice();
            else if (IsLAMSelected)
                CalculateLAMPrice();
        }

        private void CalculateSGUPrice()
        {
            double step1 = SGUSheetPrice / SGUWasteFactor;
            double step2 = step1 + SGUCutting;
            double step3 = step2 + SGUTempering;
            double final = step3 * (1 + SGUProfitPercent / 100);

            CalculatedPrice = Math.Round(final, 2);
            GeneratedDescription = $"{SelectedThickness}mm {SelectedColor} {WorkTypeText}";
            PriceCalculationSummary = $"({SGUSheetPrice} / {SGUWasteFactor:F2}) + {SGUCutting} + {SGUTempering} = {final:F2} × {1 + SGUProfitPercent / 100:F2} = {CalculatedPrice:F2}";
        }

        private void CalculateDGUPrice()
        {
            double glassTotal = DGUOuterPrice + DGUInnerPrice;
            double step1 = glassTotal / DGUWasteFactor;
            double step2 = step1 + DGUASPPrice;
            double final = step2 * (1 + DGUProfitPercent / 100);

            CalculatedPrice = Math.Round(final, 2);
            string uInsertText = IsDGUIncludeInSpec ? "with U-Insert" : "";
            GeneratedDescription = $"{DGUOuterThickness}mm {DGUOuterColor} {DGUWorkTypeText} + {DGUSpacerThickness}mm ASP {uInsertText} + {DGUInnerThickness}mm {DGUInnerColor} {DGUWorkTypeText}";
            PriceCalculationSummary = $"(({DGUOuterPrice} + {DGUInnerPrice}) / {DGUWasteFactor:F2}) + {DGUASPPrice} = {step2:F2} × {1 + DGUProfitPercent / 100:F2} = {CalculatedPrice:F2}";
        }

        private void CalculateLAMPrice()
        {
            double glassTotal = LAMOuterPrice + LAMPVBPrice + LAMInnerPrice;
            double step1 = glassTotal / LAMWasteFactor;
            double step2 = step1 + LAMCutting + LAMTempering;
            double final = step2 * (1 + LAMProfitPercent / 100);

            CalculatedPrice = Math.Round(final, 2);
            GeneratedDescription = $"{LAMOuterThickness}mm {LAMOuterColor} {LAMWorkTypeText} + {LAMPVBThickness}mm PVB ({LAMPVBColor}) + {LAMInnerThickness}mm {LAMInnerColor} {LAMWorkTypeText}";
            PriceCalculationSummary = $"({LAMOuterPrice} + {LAMPVBPrice} + {LAMInnerPrice}) / {LAMWasteFactor:F2} + {LAMCutting} + {LAMTempering} = {step2:F2} × {1 + LAMProfitPercent / 100:F2} = {CalculatedPrice:F2}";
        }

        // ==================== INCLUDE IN SPECIFICATION ====================
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
                SelectedTargetSpecification.SpacerThickness = DGUSpacerThickness;
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
                SelectedTargetSpecification.PVBThickness = LAMPVBThickness;
                SelectedTargetSpecification.PVBColor = LAMPVBColor;
                SelectedTargetSpecification.PVBPrice = LAMPVBPrice.ToString();
                SelectedTargetSpecification.InnerThickness = LAMInnerThickness;
                SelectedTargetSpecification.InnerColor = LAMInnerColor;
                SelectedTargetSpecification.InnerPrice = LAMInnerPrice.ToString();
            }

            SelectedTargetSpecification.SpecificationName = GeneratedDescription;
            SelectedTargetSpecification.BasePrice = CalculatedPrice;

            if (SelectedTargetSpecification.Items.Count > 0)
            {
                SelectedTargetSpecification.SurchargePercent = SelectedTargetSpecification.Items[0].SurchargePercent;
            }

            Invoice.CalculateTotals();
            Invoice.IsDirty = true;

            StatusMessage = $"✅ Applied '{GeneratedDescription}' @ AED {CalculatedPrice:N2}";
        }

        // ==================== IMPORT/EXPORT METHODS ====================
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

                if (importedInvoice != null)
                {
                    Invoice.Specifications.Clear();

                    foreach (var spec in importedInvoice.Specifications)
                    {
                        var newSpec = new SpecificationModel
                        {
                            SpecificationName = spec.SpecificationName,
                            BasePrice = spec.BasePrice,
                            ModuleType = spec.ModuleType,
                            WorkType = spec.WorkType,
                            IncludeInSpec = spec.IncludeInSpec,
                            OuterThickness = spec.OuterThickness,
                            OuterColor = spec.OuterColor,
                            OuterPrice = spec.OuterPrice,
                            SpacerThickness = spec.SpacerThickness,
                            ASPPrice = spec.ASPPrice,
                            InnerThickness = spec.InnerThickness,
                            InnerColor = spec.InnerColor,
                            InnerPrice = spec.InnerPrice,
                            PVBThickness = spec.PVBThickness,
                            PVBColor = spec.PVBColor,
                            PVBPrice = spec.PVBPrice
                        };

                        foreach (var item in spec.Items)
                        {
                            var newItem = new InvoiceItemModel
                            {
                                SrNo = item.SrNo,
                                GlassRef = item.GlassRef,
                                Qty = item.Qty,
                                Price = item.Price,
                                SurchargePercent = item.SurchargePercent
                            };

                            newItem.Width1 = item.Width1;
                            newItem.Height1 = item.Height1;
                            newItem.Width2 = item.Width2;
                            newItem.Height2 = item.Height2;

                            newSpec.Items.Add(newItem);
                        }

                        Invoice.Specifications.Add(newSpec);
                    }

                    Invoice.InvoiceNo = importedInvoice.InvoiceNo;
                    Invoice.InvoiceDate = importedInvoice.InvoiceDate;
                    Invoice.ValidUntil = importedInvoice.ValidUntil;
                    Invoice.CustomerName = importedInvoice.CustomerName;
                    Invoice.CustomerTRN = importedInvoice.CustomerTRN;
                    Invoice.CustomerAddress = importedInvoice.CustomerAddress;
                    Invoice.ProjectName = importedInvoice.ProjectName;
                    Invoice.ProjectLocation = importedInvoice.ProjectLocation;
                    Invoice.LPONo = importedInvoice.LPONo;
                    Invoice.AttentionName = importedInvoice.AttentionName;
                    Invoice.ContactNo = importedInvoice.ContactNo;

                    Invoice.CalculateTotals();
                    Invoice.IsDirty = true;

                    CurrentFileName = "Imported";
                    StatusMessage = $"✅ Imported {importedInvoice.Specifications.Sum(s => s.Items.Count)} items";
                }
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

                if (items != null && items.Count > 0)
                {
                    if (Invoice.Specifications.Count == 0)
                        AddSpecification();

                    var targetSpec = Invoice.Specifications[0];
                    int startSrNo = targetSpec.Items.Count + 1;

                    foreach (var item in items)
                    {
                        var newItem = new InvoiceItemModel
                        {
                            SrNo = startSrNo++,
                            GlassRef = item.GlassRef,
                            Qty = item.Qty,
                            Price = item.Price,
                            SurchargePercent = 20
                        };

                        newItem.Width1 = item.Width1;
                        newItem.Height1 = item.Height1;
                        newItem.Width2 = item.Width2;
                        newItem.Height2 = item.Height2;

                        targetSpec.Items.Add(newItem);
                    }

                    Invoice.CalculateTotals();
                    Invoice.IsDirty = true;

                    StatusMessage = $"✅ Imported {items.Count} items";
                }
                else
                {
                    StatusMessage = "❌ No items found in file";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Item import failed: {ex.Message}";
            }
        }
    }
}