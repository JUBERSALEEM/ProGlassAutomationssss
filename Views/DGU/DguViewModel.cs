// ViewModels/DguViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.ViewModels
{
    public class DguViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        // ═══════════════════════════════════════════════════════════
        // LOAD DATA FROM SHEET.CS
        // ═══════════════════════════════════════════════════════════
        public ObservableCollection<string> CategoryOptions { get; }
            = new(Sheet.Categories);

        public ObservableCollection<string> ThicknessOptions { get; }
            = new(Sheet.Thicknesses);

        public ObservableCollection<GlassColorItem> ColorOptions { get; }
            = new(Sheet.ColorItems);

        public ObservableCollection<string> SpacerOptions { get; }
    = new(Sheet.Thicknesses);

        public ObservableCollection<string> WastageOptions { get; }
            = new(Sheet.WastageOptions);

        public ObservableCollection<string> ProfitMarginOptions { get; }
            = new(Sheet.ProfitMarginOptions);

        public ObservableCollection<DguRecord> Records { get; } = new();

        // ═══════════════════════════════════════════════════════════
        // INVENTORY SUMMARY
        // ═══════════════════════════════════════════════════════════
        public int CategoryTotal => Sheet.Categories.Count;
        public int ThicknessTotal => Sheet.Thicknesses.Length;
        public int ColorTotal => Sheet.ColorItems.Count;

        // ═══════════════════════════════════════════════════════════
        // OUTER GLASS PROPERTIES
        // ═══════════════════════════════════════════════════════════
        private string _outerCategory = "Clear Float";
        public string OuterCategory
        {
            get => _outerCategory;
            set { _outerCategory = value; OnPropertyChanged(); Calculate(); }
        }

        private string _outerThickness = "6mm";
        public string OuterThickness
        {
            get => _outerThickness;
            set { _outerThickness = value; OnPropertyChanged(); Calculate(); }
        }

        private string _outerColor = "Clear";
        public string OuterColor
        {
            get => _outerColor;
            set { _outerColor = value; OnPropertyChanged(); Calculate(); }
        }

        private double? _outerPrice;
        public double? OuterPrice
        {
            get => _outerPrice;
            set { _outerPrice = value; OnPropertyChanged(); Calculate(); }
        }

        // ═══════════════════════════════════════════════════════════
        // INNER GLASS PROPERTIES
        // ═══════════════════════════════════════════════════════════
        private string _innerCategory = "Clear Float";
        public string InnerCategory
        {
            get => _innerCategory;
            set { _innerCategory = value; OnPropertyChanged(); Calculate(); }
        }

        private string _innerThickness = "4mm";
        public string InnerThickness
        {
            get => _innerThickness;
            set { _innerThickness = value; OnPropertyChanged(); Calculate(); }
        }

        private string _innerColor = "Clear";
        public string InnerColor
        {
            get => _innerColor;
            set { _innerColor = value; OnPropertyChanged(); Calculate(); }
        }

        private double? _innerPrice;
        public double? InnerPrice
        {
            get => _innerPrice;
            set { _innerPrice = value; OnPropertyChanged(); Calculate(); }
        }

        // ═══════════════════════════════════════════════════════════
        // SPACER SETTINGS PROPERTIES
        // ═══════════════════════════════════════════════════════════
        private string _spacer = "6mm";
        public string Spacer
        {
            get => _spacer;
            set { _spacer = value; OnPropertyChanged(); Calculate(); }
        }

        private double? _spacerCharge;
        public double? SpacerCharge
        {
            get => _spacerCharge;
            set { _spacerCharge = value; OnPropertyChanged(); Calculate(); }
        }

        private double? _sealantCharge;
        public double? SealantCharge
        {
            get => _sealantCharge;
            set { _sealantCharge = value; OnPropertyChanged(); Calculate(); }
        }

        private double? _gasCharge;
        public double? GasCharge
        {
            get => _gasCharge;
            set { _gasCharge = value; OnPropertyChanged(); Calculate(); }
        }

        private double? _airspacePrice;
        public double? AirspacePrice
        {
            get => _airspacePrice;
            set { _airspacePrice = value; OnPropertyChanged(); Calculate(); }
        }

        // ═══════════════════════════════════════════════════════════
        // WASTAGE & PROFIT PROPERTIES
        // ═══════════════════════════════════════════════════════════
        private string _wastage = "15";
        public string Wastage
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

        // ═══════════════════════════════════════════════════════════
        // CALCULATION RESULTS
        // ═══════════════════════════════════════════════════════════
        private double _glassCost;
        public double GlassCost
        {
            get => _glassCost;
            set { _glassCost = value; OnPropertyChanged(); }
        }

        private double _baseCost;
        public double BaseCost
        {
            get => _baseCost;
            set { _baseCost = value; OnPropertyChanged(); }
        }

        private double _processingCost;
        public double ProcessingCost
        {
            get => _processingCost;
            set { _processingCost = value; OnPropertyChanged(); }
        }

        private double _result;
        public double Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        private double _vatAmount;
        public double VatAmount
        {
            get => _vatAmount;
            set { _vatAmount = value; OnPropertyChanged(); }
        }

        private double _grossTotal;
        public double GrossTotal
        {
            get => _grossTotal;
            set { _grossTotal = value; OnPropertyChanged(); }
        }

        // Display Properties
        public string WastageFactorDisplay => $"÷ {1 - (double.Parse(_wastage) / 100.0):F2}";
        public string ProfitMarginDisplay => $"× {1 + (double.Parse(_profitMargin.Replace("%", "")) / 100.0):F2}";

        // History
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
        public DguViewModel()
        {
            // Set defaults
            if (CategoryOptions.Count > 0)
            {
                _outerCategory = CategoryOptions[0];
                _innerCategory = CategoryOptions[0];
            }
            if (ThicknessOptions.Count > 0) _outerThickness = ThicknessOptions[0];
            if (ThicknessOptions.Count > 1) _innerThickness = ThicknessOptions[1];
            if (ColorOptions.Count > 0)
            {
                _outerColor = ColorOptions[0].Name;
                _innerColor = ColorOptions[0].Name;
            }
            if (SpacerOptions.Count > 0) _spacer = SpacerOptions[0];
            if (WastageOptions.Count > 1) _wastage = WastageOptions[1];
            if (ProfitMarginOptions.Count > 1) _profitMargin = ProfitMarginOptions[1];

            SaveCommand = new RelayCommand(o =>
            {
                Records.Insert(0, new DguRecord
                {
                    DisplayText = $"{OuterCategory} | {OuterThickness} | {OuterColor} + {InnerCategory} | {InnerThickness} | {InnerColor}",
                    Wastage = $"{_wastage}%",
                    ProfitMargin = _profitMargin,
                    CreatedAt = DateTime.Now.ToString("HH:mm"),
                    Result = Result
                });
                OnPropertyChanged(nameof(IsHistoryVisible));
                OnPropertyChanged(nameof(IsHistoryEmpty));
                MessageBox.Show("DGU saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ExportPdfCommand = new RelayCommand(o =>
            {
                MessageBox.Show("PDF export coming soon!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ClearCommand = new RelayCommand(o =>
            {
                OuterPrice = null;
                InnerPrice = null;
                SpacerCharge = null;
                SealantCharge = null;
                GasCharge = null;
                AirspacePrice = null;
            });

            ClearAllCommand = new RelayCommand(o =>
            {
                Records.Clear();
                OnPropertyChanged(nameof(IsHistoryVisible));
                OnPropertyChanged(nameof(IsHistoryEmpty));
            });

            DeleteCommand = new RelayCommand(o =>
            {
                if (o is DguRecord r) Records.Remove(r);
                OnPropertyChanged(nameof(IsHistoryVisible));
                OnPropertyChanged(nameof(IsHistoryEmpty));
            });

            Calculate();
        }

        // ═══════════════════════════════════════════════════════════
        // CORRECT FORMULA
        // ═══════════════════════════════════════════════════════════
        public void Calculate()
        {
            // STEP 1: Glass Cost = Sheet 1 + Sheet 2
            double sheet1 = OuterPrice ?? 0;
            double sheet2 = InnerPrice ?? 0;
            _glassCost = sheet1 + sheet2;

            // STEP 2: Base Cost = Glass Cost ÷ Wastage Factor
            double wastageFactor = 1 - (double.Parse(_wastage) / 100.0);
            _baseCost = wastageFactor > 0 ? _glassCost / wastageFactor : _glassCost;

            // STEP 3: Processing Cost = Base Cost + Airspace + Spacer + Sealant + Gas
            double airspace = AirspacePrice ?? 0;
            double spacer = SpacerCharge ?? 0;
            double sealant = SealantCharge ?? 0;
            double gas = GasCharge ?? 0;
            _processingCost = _baseCost + airspace + spacer + sealant + gas;

            // STEP 4: Unit Price = Processing Cost × Profit Factor
            double profitFactor = 1 + (double.Parse(_profitMargin.Replace("%", "")) / 100.0);
            _result = _processingCost * profitFactor;

            // VAT (5%) and Gross Total
            _vatAmount = _result * 0.05;
            _grossTotal = _result + _vatAmount;

            // Notify all changes
            OnPropertyChanged(nameof(GlassCost));
            OnPropertyChanged(nameof(BaseCost));
            OnPropertyChanged(nameof(ProcessingCost));
            OnPropertyChanged(nameof(Result));
            OnPropertyChanged(nameof(VatAmount));
            OnPropertyChanged(nameof(GrossTotal));
            OnPropertyChanged(nameof(WastageFactorDisplay));
            OnPropertyChanged(nameof(ProfitMarginDisplay));
        }
    }

    public class DguRecord
    {
        public string DisplayText { get; set; }
        public string Wastage { get; set; }
        public string ProfitMargin { get; set; }
        public string CreatedAt { get; set; }
        public double Result { get; set; }
    }
}