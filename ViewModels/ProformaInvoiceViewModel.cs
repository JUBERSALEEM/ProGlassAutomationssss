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
    public class ProformaInvoiceViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly ExcelCsvService _excelCsvService;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ProformaInvoiceModel _invoice = new();
        public ProformaInvoiceModel Invoice
        {
            get => _invoice;
            set
            {
                _invoice = value;
                OnPropertyChanged();
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

        // NEW: DGU Work Type
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

        // NEW: DGU U-Insert
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

        // Outer Glass
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

        // Air Space (Spacer)
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

        // Inner Glass
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

        // NEW: LAM Work Type
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

        // Outer Glass
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

        // PVB Layer
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

        // Inner Glass
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
            set { _selectedTargetSpecification = value; OnPropertyChanged(); }
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

        // Constructor
        public ProformaInvoiceViewModel()
        {
            _excelCsvService = new ExcelCsvService();
            _excelCsvService.StatusChanged += status =>
            {
                Application.Current.Dispatcher.Invoke(() => StatusMessage = status);
            };

            Invoice = new ProformaInvoiceModel();
            Invoice.InvoiceNo = Invoice.GenerateInvoiceNo();

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

            Invoice = new ProformaInvoiceModel();
            Invoice.InvoiceNo = Invoice.GenerateInvoiceNo();
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
                SpecificationName = $"Specification {Invoice.Specifications.Count + 1}"
            };

            var firstItem = new InvoiceItemModel
            {
                SrNo = 1,
                SurchargePercent = 20
            };
            spec.Items.Add(firstItem);

            Invoice.Specifications.Add(spec);
            SelectedTargetSpecification = spec;
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

        // ==================== CALCULATE PRICE ====================
        private void CalculatePrice()
        {
            if (IsSGUSelected)
            {
                CalculateSGUPrice();
            }
            else if (IsDGUSelected)
            {
                CalculateDGUPrice();
            }
            else if (IsLAMSelected)
            {
                CalculateLAMPrice();
            }
        }

        private void CalculateSGUPrice()
        {
            // Formula: (SheetPrice / WasteFactor + Cutting + Tempering) * ProfitFactor
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
            // Formula: ((Outer + Inner) / WasteFactor) + ASP) * ProfitFactor
            double glassTotal = DGUOuterPrice + DGUInnerPrice;
            double step1 = glassTotal / DGUWasteFactor;
            double step2 = step1 + DGUASPPrice;
            double final = step2 * (1 + DGUProfitPercent / 100);

            CalculatedPrice = Math.Round(final, 2);

            // Generate description with Work Type and U-Insert
            string workTypeText = DGUWorkTypeText;
            string uInsertText = IsDGUIncludeInSpec ? "with U-Insert" : "";

            GeneratedDescription = $"{DGUOuterThickness}mm {DGUOuterColor} {workTypeText} + {DGUSpacerThickness}mm ASP {uInsertText} + {DGUInnerThickness}mm {DGUInnerColor} {workTypeText}";

            PriceCalculationSummary = $"(({DGUOuterPrice} + {DGUInnerPrice}) / {DGUWasteFactor:F2}) + {DGUASPPrice} = {step1:F2} + {DGUASPPrice} = {step2:F2} × {1 + DGUProfitPercent / 100:F2} = {CalculatedPrice:F2}";
        }

        private void CalculateLAMPrice()
        {
            // Formula: ((Outer + PVB + Inner) / WasteFactor + Cutting + Tempering) * ProfitFactor
            double glassTotal = LAMOuterPrice + LAMPVBPrice + LAMInnerPrice;
            double step1 = glassTotal / LAMWasteFactor;
            double step2 = step1 + LAMCutting + LAMTempering;
            double final = step2 * (1 + LAMProfitPercent / 100);

            CalculatedPrice = Math.Round(final, 2);

            // Generate description with Work Type
            string workTypeText = LAMWorkTypeText;
            GeneratedDescription = $"{LAMOuterThickness}mm {LAMOuterColor} {workTypeText} + {LAMPVBThickness}mm PVB ({LAMPVBColor}) + {LAMInnerThickness}mm {LAMInnerColor} {workTypeText}";

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

            // Set Module Type
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

            // Update specification name with generated description
            SelectedTargetSpecification.SpecificationName = GeneratedDescription;

            // Update base price for all items in this specification
            SelectedTargetSpecification.BasePrice = CalculatedPrice;

            // Update surcharge from first item if exists
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
                Debug.WriteLine("[ImportFromCsv] Starting import...");

                var importedInvoice = _excelCsvService.ImportFromCsv();

                if (importedInvoice != null)
                {
                    Debug.WriteLine($"[ImportFromCsv] Imported invoice: {importedInvoice.InvoiceNo}");
                    Debug.WriteLine($"[ImportFromCsv] Specifications: {importedInvoice.Specifications.Count}");

                    int totalItems = importedInvoice.Specifications.Sum(s => s.Items.Count);
                    Debug.WriteLine($"[ImportFromCsv] Total items: {totalItems}");

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
                            Debug.WriteLine($"[ImportFromCsv] Item: {newItem.GlassRef}, W={newItem.Width1}, H={newItem.Height1}, SQM={newItem.SQM}");
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
                    StatusMessage = $"✅ Imported {totalItems} items";
                    Debug.WriteLine($"[ImportFromCsv] Complete - {totalItems} items");
                }
                else
                {
                    StatusMessage = "❌ Import returned null";
                    Debug.WriteLine("[ImportFromCsv] Returned null");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Import failed: {ex.Message}";
                Debug.WriteLine($"[ImportFromCsv] Error: {ex}");
                MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportItemsFromCsv()
        {
            try
            {
                Debug.WriteLine("[ImportItemsFromCsv] Starting import...");

                var items = _excelCsvService.ImportItemsFromCsv();

                if (items != null && items.Count > 0)
                {
                    Debug.WriteLine($"[ImportItemsFromCsv] Found {items.Count} items");

                    if (Invoice.Specifications.Count == 0)
                    {
                        AddSpecification();
                    }

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
                        Debug.WriteLine($"[ImportItemsFromCsv] Added: {newItem.GlassRef}, W={newItem.Width1}, H={newItem.Height1}, SQM={newItem.SQM}");
                    }

                    Invoice.CalculateTotals();
                    Invoice.IsDirty = true;

                    StatusMessage = $"✅ Imported {items.Count} items";
                    Debug.WriteLine($"[ImportItemsFromCsv] Complete");
                }
                else
                {
                    StatusMessage = "❌ No items found in file";
                    Debug.WriteLine("[ImportItemsFromCsv] No items found");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Item import failed: {ex.Message}";
                Debug.WriteLine($"[ImportItemsFromCsv] Error: {ex}");
            }
        }
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
}