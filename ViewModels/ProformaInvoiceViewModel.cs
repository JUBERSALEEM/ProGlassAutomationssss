using Microsoft.Win32;
using Newtonsoft.Json;
using ProGlassAutomation.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ProformaInvoiceModel _invoice = new();
        public ProformaInvoiceModel Invoice
        {
            get => _invoice;
            set { _invoice = value; OnPropertyChanged(); }
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

        private bool _isLMVisible = true;
        public bool IsLMVisible
        {
            get => _isLMVisible;
            set { _isLMVisible = value; OnPropertyChanged(); }
        }

        public string IsLMToggleText => IsLMVisible ? "HIDE LM" : "SHOW LM";

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

        // Commands
        public ICommand NewInvoiceCommand { get; }
        public ICommand SaveInvoiceCommand { get; }
        public ICommand OpenInvoiceCommand { get; }
        public ICommand AddSpecificationCommand { get; }
        public ICommand RemoveSpecificationCommand { get; }
        public ICommand ToggleLMCommand { get; }
        public ICommand CalculatePriceCommand { get; }
        public ICommand UseCalculatedPriceCommand { get; }

        // Constructor
        public ProformaInvoiceViewModel()
        {
            Invoice = new ProformaInvoiceModel();
            Invoice.InvoiceNo = Invoice.GenerateInvoiceNo();

            NewInvoiceCommand = new RelayCommand(_ => NewInvoice());
            SaveInvoiceCommand = new RelayCommand(_ => SaveInvoice());
            OpenInvoiceCommand = new RelayCommand(_ => OpenInvoice());
            AddSpecificationCommand = new RelayCommand(_ => AddSpecification());
            RemoveSpecificationCommand = new RelayCommand(_ => RemoveSpecification(), _ => Invoice.Specifications.Count > 0);
            ToggleLMCommand = new RelayCommand(_ => ToggleLM());
            CalculatePriceCommand = new RelayCommand(_ => CalculatePrice());
            UseCalculatedPriceCommand = new RelayCommand(_ => UseCalculatedPrice());

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
                    MessageBox.Show("Invoice saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
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
                    }
                }
            }
            catch (Exception ex)
            {
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
            Invoice.Specifications.Add(spec);
        }

        private void RemoveSpecification()
        {
            if (Invoice.Specifications.Count > 0)
            {
                Invoice.Specifications.RemoveAt(Invoice.Specifications.Count - 1);
            }
        }

        public void AddItem(SpecificationModel spec)
        {
            if (spec == null) return;
            var item = new InvoiceItemModel { SrNo = spec.Items.Count + 1 };
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
                CalculatedPrice = 200; // Placeholder
            }
            else if (IsLAMSelected)
            {
                CalculatedPrice = 250; // Placeholder
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
            MessageBox.Show($"Applied AED {CalculatedPrice:N2} to all items!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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