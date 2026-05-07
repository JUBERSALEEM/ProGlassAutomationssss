using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.ViewModels
{
    public class SguViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        // ═══════════════════════════════════════════════════════════
        // LOAD DATA FROM SHEET.CS (Static Properties)
        // ═══════════════════════════════════════════════════════════

        public ObservableCollection<string> CategoryOptions { get; }
            = new(Sheet.Categories);

        public ObservableCollection<string> ThicknessOptions { get; }
            = new(Sheet.Thicknesses);

        public ObservableCollection<GlassColorItem> ColorOptions { get; }
            = new(Sheet.ColorItems);

        public ObservableCollection<string> WastageOptions { get; }
            = new(Sheet.WastageOptions);

        public ObservableCollection<string> ProfitMarginOptions { get; }
            = new(Sheet.ProfitMarginOptions);

        public ObservableCollection<SguRecord> Records { get; } = new();

        // ═══════════════════════════════════════════════════════════
        // INVENTORY TOTALS FROM SHEET.CS
        // ═══════════════════════════════════════════════════════════

        public int CategoryTotalQty => Sheet.Categories.Count;
        public int ThicknessTotalQty => Sheet.Thicknesses.Length;
        public int ColorTotalQty => Sheet.ColorItems.Count;

        // ═══════════════════════════════════════════════════════════
        // PROPERTIES - NO DEFAULT VALUES
        // ═══════════════════════════════════════════════════════════
        private string _category;
        public string Category
        {
            get => _category;
            set { _category = value; OnPropertyChanged(); Calculate(); }
        }

        private string _thickness;
        public string Thickness
        {
            get => _thickness;
            set { _thickness = value; OnPropertyChanged(); Calculate(); }
        }

        private string _colorName;
        public string ColorName
        {
            get => _colorName;
            set { _colorName = value; OnPropertyChanged(); Calculate(); }
        }

        private string _wastage = "15";
        public string WastageConsider
        {
            get => _wastage;
            set { _wastage = value; OnPropertyChanged(); Calculate(); }
        }

        private string _profitMargin = "15%";
        public string ProfitMargin
        {
            get => _profitMargin;
            set { _profitMargin = value; OnPropertyChanged(); Calculate(); }
        }

        // NO DEFAULT VALUE - Start empty
        private double? _sheetPrice;
        public double? SheetPrice
        {
            get => _sheetPrice;
            set { _sheetPrice = value; OnPropertyChanged(); OnPropertyChanged(nameof(SheetPriceDisplay)); Calculate(); }
        }

        public string SheetPriceDisplay => _sheetPrice?.ToString() ?? "";

        // NO DEFAULT VALUE - Start empty
        private double? _cutting;
        public double? Cutting
        {
            get => _cutting;
            set { _cutting = value; OnPropertyChanged(); OnPropertyChanged(nameof(CuttingDisplay)); Calculate(); }
        }

        public string CuttingDisplay => _cutting?.ToString() ?? "";

        // NO DEFAULT VALUE - Start empty
        private double? _tempering;
        public double? Tempering
        {
            get => _tempering;
            set { _tempering = value; OnPropertyChanged(); OnPropertyChanged(nameof(TemperingDisplay)); Calculate(); }
        }

        public string TemperingDisplay => _tempering?.ToString() ?? "";

        private double _result;
        public double Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        // Computed values
        private double _wastageFactor = 0.85;
        private double _profitFactor = 1.15;

        public double BaseCost => _wastageFactor > 0 && _sheetPrice.HasValue ? _sheetPrice.Value / _wastageFactor : 0;
        public double ProcessingCost => (_cutting ?? 0) + (_tempering ?? 0);
        public double Subtotal => BaseCost + ProcessingCost;
        public double FinalPrice => Subtotal * _profitFactor;

        public bool IsHistoryVisible => Records.Count > 0;
        public bool IsHistoryEmpty => Records.Count == 0;

        // ═══════════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════════
        public ICommand SaveCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand DeleteCommand { get; }

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════
        public SguViewModel()
        {
            // Set dropdown defaults (not text fields)
            if (CategoryOptions.Count > 0) _category = CategoryOptions[0];
            if (ThicknessOptions.Count > 5) _thickness = ThicknessOptions[5];
            if (ColorOptions.Count > 0) _colorName = ColorOptions[0].Name;
            if (WastageOptions.Count > 2) _wastage = WastageOptions[2];
            if (ProfitMarginOptions.Count > 2) _profitMargin = ProfitMarginOptions[2];

            // Text fields start empty (no default values)
            _sheetPrice = null;
            _cutting = null;
            _tempering = null;

            SaveCommand = new RelayCommand(o =>
            {
                if (!_sheetPrice.HasValue)
                {
                    MessageBox.Show("Please enter sheet price", "Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Records.Insert(0, new SguRecord
                {
                    DisplayText = $"{Category} | {Thickness} | {ColorName}",
                    SheetPrice = _sheetPrice ?? 0,
                    Cutting = _cutting ?? 0,
                    Tempering = _tempering ?? 0,
                    Wastage = $"{_wastage}%",
                    ProfitMargin = _profitMargin,
                    CreatedAt = DateTime.Now.ToString("HH:mm"),
                    Result = Result
                });
                OnPropertyChanged(nameof(IsHistoryVisible));
                OnPropertyChanged(nameof(IsHistoryEmpty));
                MessageBox.Show("SGU saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ExportPdfCommand = new RelayCommand(o =>
            {
                MessageBox.Show("PDF export coming soon!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ClearCommand = new RelayCommand(o =>
            {
                SheetPrice = null;
                Cutting = null;
                Tempering = null;
                Result = 0;
            });

            ClearAllCommand = new RelayCommand(o =>
            {
                Records.Clear();
                OnPropertyChanged(nameof(IsHistoryVisible));
                OnPropertyChanged(nameof(IsHistoryEmpty));
            });

            DeleteCommand = new RelayCommand(o =>
            {
                if (o is SguRecord r) Records.Remove(r);
                OnPropertyChanged(nameof(IsHistoryVisible));
                OnPropertyChanged(nameof(IsHistoryEmpty));
            });

            // Initialize with default wastage/profit
            Calculate();
        }

        public void Calculate()
        {
            // Parse wastage
            if (double.TryParse(_wastage, out double w))
            {
                _wastageFactor = 1 - (w / 100.0);
            }

            // Parse profit margin
            if (!string.IsNullOrEmpty(_profitMargin))
            {
                var marginValue = _profitMargin.Replace("%", "").Trim();
                if (double.TryParse(marginValue, out double m))
                {
                    _profitFactor = 1 + (m / 100.0);
                }
            }

            // Calculate
            double baseCost = _wastageFactor > 0 && _sheetPrice.HasValue ? _sheetPrice.Value / _wastageFactor : 0;
            double processing = (_cutting ?? 0) + (_tempering ?? 0);
            Result = (baseCost + processing) * _profitFactor;

            OnPropertyChanged(nameof(BaseCost));
            OnPropertyChanged(nameof(ProcessingCost));
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(FinalPrice));
        }
    }

    public class SguRecord
    {
        public string DisplayText { get; set; }
        public double SheetPrice { get; set; }
        public double Cutting { get; set; }
        public double Tempering { get; set; }
        public string Wastage { get; set; }
        public string ProfitMargin { get; set; }
        public string CreatedAt { get; set; }
        public double Result { get; set; }
    }
}