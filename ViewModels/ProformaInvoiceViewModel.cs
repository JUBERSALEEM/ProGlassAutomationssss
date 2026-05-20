// ViewModels/ProformaInvoiceViewModel.cs
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
            set { _savedFiles = value; OnPropertyChanged(); }
        }

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
            set { _isLMVisible = value; OnPropertyChanged(); }
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

        // Calculator Properties
        private bool _isSGUSelected = true;
        public bool IsSGUSelected
        {
            get => _isSGUSelected;
            set { _isSGUSelected = value; OnPropertyChanged(); }
        }

        private bool _isDGUSelected;
        public bool IsDGUSelected
        {
            get => _isDGUSelected;
            set { _isDGUSelected = value; OnPropertyChanged(); }
        }

        private bool _isLAMSelected;
        public bool IsLAMSelected
        {
            get => _isLAMSelected;
            set { _isLAMSelected = value; OnPropertyChanged(); }
        }

        private double _calculatedPrice = 0;
        public double CalculatedPrice
        {
            get => _calculatedPrice;
            set { _calculatedPrice = value; OnPropertyChanged(); }
        }

        // SGU Calculator
        private double _sGUSheetPrice = 150;
        public double SGUSheetPrice
        {
            get => _sGUSheetPrice;
            set { _sGUSheetPrice = value; OnPropertyChanged(); }
        }

        private double _sGUWasteFactor = 1.1;
        public double SGUWasteFactor
        {
            get => _sGUWasteFactor;
            set { _sGUWasteFactor = value; OnPropertyChanged(); }
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

        private double _sGUProfitPercent = 15;
        public double SGUProfitPercent
        {
            get => _sGUProfitPercent;
            set { _sGUProfitPercent = value; OnPropertyChanged(); }
        }

        // Surcharge Settings
        private double _surchargeThreshold = 4;
        public double SurchargeThreshold
        {
            get => _surchargeThreshold;
            set { _surchargeThreshold = value; OnPropertyChanged(); }
        }

        private double _surchargePercent = 20;
        public double SurchargePercent
        {
            get => _surchargePercent;
            set { _surchargePercent = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand NewInvoiceCommand { get; }
        public ICommand SaveInvoiceCommand { get; }
        public ICommand OpenInvoiceCommand { get; }
        public ICommand AddSpecificationCommand { get; }
        public ICommand RemoveSpecificationCommand { get; }
        public ICommand ToggleLMCommand { get; }
        public ICommand CalculatePriceCommand { get; }
        public ICommand UseCalculatedPriceCommand { get; }

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

            // ADD DEFAULT SPECIFICATION WITH 1 ROW
            AddSpecification();

            // Standard Commands
            NewInvoiceCommand = new RelayCommand(_ => NewInvoice());
            SaveInvoiceCommand = new RelayCommand(_ => SaveInvoice());
            OpenInvoiceCommand = new RelayCommand(_ => OpenInvoice());
            AddSpecificationCommand = new RelayCommand(_ => AddSpecification());
            RemoveSpecificationCommand = new RelayCommand(_ => RemoveSpecification(), _ => Invoice.Specifications.Count > 0);
            ToggleLMCommand = new RelayCommand(_ => ToggleLM());
            CalculatePriceCommand = new RelayCommand(_ => CalculatePrice());
            UseCalculatedPriceCommand = new RelayCommand(_ => UseCalculatedPrice());

            // Import/Export Commands
            ExportCsvCommand = new RelayCommand(_ => ExportToCsv());
            ImportCsvCommand = new RelayCommand(_ => ImportFromCsv());
            ImportItemsCommand = new RelayCommand(_ => ImportItemsFromCsv());

            LoadSavedFiles();
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

            // ADD DEFAULT SPECIFICATION WITH 1 ROW
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
        }

        private void AddSpecification()
        {
            var spec = new SpecificationModel
            {
                SpecificationName = $"Specification {Invoice.Specifications.Count + 1}"
            };

            // ADD 1 EMPTY ROW BY DEFAULT
            var firstItem = new InvoiceItemModel
            {
                SrNo = 1,
                SurchargePercent = SurchargePercent,
                SurchargeThreshold = SurchargeThreshold
            };
            spec.Items.Add(firstItem);

            Invoice.Specifications.Add(spec);
        }

        private void RemoveSpecification()
        {
            if (Invoice.Specifications.Count > 0)
            {
                Invoice.Specifications.RemoveAt(Invoice.Specifications.Count - 1);
            }
        }

        // Add item with price copied from first row (base price, not calculated)
        public void AddItemWithPrice(SpecificationModel spec)
        {
            if (spec == null) return;

            var newItem = new InvoiceItemModel { SrNo = spec.Items.Count + 1 };

            if (spec.Items.Count > 0)
            {
                var firstRow = spec.Items[0];
                // Copy base price, not the calculated Price getter
                newItem.Price = firstRow.BasePrice;
                newItem.SurchargePercent = firstRow.SurchargePercent;
                newItem.SurchargeThreshold = firstRow.SurchargeThreshold;
            }
            else
            {
                newItem.SurchargePercent = SurchargePercent;
                newItem.SurchargeThreshold = SurchargeThreshold;
            }

            spec.Items.Add(newItem);
            Invoice.IsDirty = true;
        }

        // Regular add item
        public void AddItem(SpecificationModel spec)
        {
            if (spec == null) return;
            var item = new InvoiceItemModel
            {
                SrNo = spec.Items.Count + 1,
                SurchargePercent = SurchargePercent,
                SurchargeThreshold = SurchargeThreshold
            };
            spec.Items.Add(item);
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

        private void CalculatePrice()
        {
            if (IsSGUSelected)
            {
                double answer1 = SGUSheetPrice / SGUWasteFactor;
                double answer2 = answer1 + SGUCutting;
                double answer3 = answer2 + SGUTempering;
                CalculatedPrice = Math.Round(answer3 * (1 + SGUProfitPercent / 100), 2);
            }
            else if (IsDGUSelected)
            {
                CalculatedPrice = 200;
            }
            else if (IsLAMSelected)
            {
                CalculatedPrice = 250;
            }
        }

        private void UseCalculatedPrice()
        {
            if (CalculatedPrice <= 0)
            {
                MessageBox.Show("Calculate price first!", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var spec in Invoice.Specifications)
            {
                foreach (var item in spec.Items)
                {
                    item.Price = CalculatedPrice;
                }
            }
            Invoice.CalculateTotals();
            StatusMessage = $"✅ Applied AED {CalculatedPrice:N2} to all items";
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

                    // CRITICAL FIX: Clear existing specifications first
                    Invoice.Specifications.Clear();

                    // Copy specifications one by one
                    foreach (var spec in importedInvoice.Specifications)
                    {
                        var newSpec = new SpecificationModel
                        {
                            SpecificationName = spec.SpecificationName,
                            BasePrice = spec.BasePrice
                        };

                        // Copy items - set dimensions AFTER creation to trigger CalculateAll()
                        foreach (var item in spec.Items)
                        {
                            var newItem = new InvoiceItemModel
                            {
                                SrNo = item.SrNo,
                                GlassRef = item.GlassRef,
                                Qty = item.Qty,
                                Price = item.Price,
                                SurchargePercent = item.SurchargePercent,
                                SurchargeThreshold = item.SurchargeThreshold
                            };

                            // Set dimensions AFTER to trigger CalculateAll() in InvoiceItemModel
                            newItem.Width1 = item.Width1;
                            newItem.Height1 = item.Height1;
                            newItem.Width2 = item.Width2;
                            newItem.Height2 = item.Height2;

                            newSpec.Items.Add(newItem);
                            Debug.WriteLine($"[ImportFromCsv] Item: {newItem.GlassRef}, W={newItem.Width1}, H={newItem.Height1}, SQM={newItem.SQM}");
                        }

                        Invoice.Specifications.Add(newSpec);
                    }

                    // Update invoice properties
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

                    // Calculate totals and mark as dirty
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

                    // Ensure we have a specification
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
                            SurchargePercent = SurchargePercent,
                            SurchargeThreshold = SurchargeThreshold
                        };

                        // Set dimensions AFTER to trigger CalculateAll()
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