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

        public ObservableCollection<string> SpacerOptions { get; } = new()
        {
            "6mm", "8mm", "10mm", "12mm", "14mm", "16mm", "18mm", "20mm"
        };

        public ObservableCollection<string> WastageOptions { get; }
            = new(Sheet.WastageOptions);

        public ObservableCollection<string> ProfitMarginOptions { get; }
            = new(Sheet.ProfitMarginOptions);

        public ObservableCollection<DguRecord> Records { get; } = new();

        // ═══════════════════════════════════════════════════════════
        // OUTER GLASS
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

        private double _outerPrice;
        public double OuterPrice
        {
            get => _outerPrice;
            set { _outerPrice = value; OnPropertyChanged(); Calculate(); }
        }

        // ═══════════════════════════════════════════════════════════
        // INNER GLASS
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

        private double _innerPrice;
        public double InnerPrice
        {
            get => _innerPrice;
            set { _innerPrice = value; OnPropertyChanged(); Calculate(); }
        }

        // ═══════════════════════════════════════════════════════════
        // SPACER & PROCESSING
        // ═══════════════════════════════════════════════════════════
        private string _spacer = "12mm";
        public string Spacer
        {
            get => _spacer;
            set { _spacer = value; OnPropertyChanged(); Calculate(); }
        }

        private double _spacerCharge = 10;
        public double SpacerCharge
        {
            get => _spacerCharge;
            set { _spacerCharge = value; OnPropertyChanged(); Calculate(); }
        }

        private double _sealantCharge = 15;
        public double SealantCharge
        {
            get => _sealantCharge;
            set { _sealantCharge = value; OnPropertyChanged(); Calculate(); }
        }

        private double _gasCharge = 5;
        public double GasCharge
        {
            get => _gasCharge;
            set { _gasCharge = value; OnPropertyChanged(); Calculate(); }
        }

        // ═══════════════════════════════════════════════════════════
        // WASTAGE & PROFIT
        // ═══════════════════════════════════════════════════════════
        private string _wastage = "15";
        public string Wastage
        {
            get => _wastage;
            set { _wastage = value; OnPropertyChanged(); Calculate(); }
        }

        private string _profitMargin = "20%";
        public string ProfitMargin
        {
            get => _profitMargin;
            set { _profitMargin = value; OnPropertyChanged(); Calculate(); }
        }

        // ═══════════════════════════════════════════════════════════
        // RESULT
        // ═══════════════════════════════════════════════════════════
        private double _result;
        public double Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        // Computed
        private double _wastageFactor = 0.85;
        private double _profitFactor = 1.20;

        public double TotalGlassCost => _outerPrice + _innerPrice;
        public double ProcessingCost => _sealantCharge + _spacerCharge + _gasCharge;
        public double BaseCost => _wastageFactor > 0 ? (TotalGlassCost + ProcessingCost) / _wastageFactor : TotalGlassCost + ProcessingCost;

        public double VatAmount => Result * 0.05;
        public double GrossTotal => Result + VatAmount;

        public string WastageFactorDisplay => $"÷ {1 - (double.Parse(_wastage) / 100.0):F2}";
        public string ProfitMarginDisplay => $"+ {_profitMargin}";

        public bool IsHistoryVisible => Records.Count > 0;

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
            // Set defaults - use Count not Length
            if (CategoryOptions.Count > 0)
            {
                _outerCategory = CategoryOptions[0];
                _innerCategory = CategoryOptions[0];
            }
            if (ThicknessOptions.Count > 5) _outerThickness = ThicknessOptions[5];
            if (ThicknessOptions.Count > 3) _innerThickness = ThicknessOptions[3];
            if (ColorOptions.Count > 0)
            {
                _outerColor = ColorOptions[0].Name;
                _innerColor = ColorOptions[0].Name;
            }
            if (SpacerOptions.Count > 3) _spacer = SpacerOptions[3];
            if (WastageOptions.Count > 2) _wastage = WastageOptions[2];
            if (ProfitMarginOptions.Count > 3) _profitMargin = ProfitMarginOptions[3];

            SaveCommand = new RelayCommand(o =>
            {
                Records.Insert(0, new DguRecord
                {
                    DisplayText = $"DGU: {_outerCategory} {_outerThickness} / {_spacer} / {_innerCategory} {_innerThickness}",
                    Wastage = $"{_wastage}%",
                    ProfitMargin = _profitMargin,
                    CreatedAt = DateTime.Now.ToString("HH:mm"),
                    Result = Result
                });
                OnPropertyChanged(nameof(IsHistoryVisible));
                MessageBox.Show("DGU saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ExportPdfCommand = new RelayCommand(o =>
            {
                MessageBox.Show("PDF export coming soon!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ClearCommand = new RelayCommand(o =>
            {
                OuterPrice = 0;
                InnerPrice = 0;
            });

            ClearAllCommand = new RelayCommand(o =>
            {
                Records.Clear();
                OnPropertyChanged(nameof(IsHistoryVisible));
            });

            DeleteCommand = new RelayCommand(o =>
            {
                if (o is DguRecord r) Records.Remove(r);
                OnPropertyChanged(nameof(IsHistoryVisible));
            });

            Calculate();
        }

        public void Calculate()
        {
            _wastageFactor = 1 - (double.Parse(_wastage) / 100.0);
            if (_wastageFactor <= 0) _wastageFactor = 1;

            _profitFactor = 1 + (double.Parse(_profitMargin.Replace("%", "")) / 100.0);

            double glass = _outerPrice + _innerPrice;
            double processing = _sealantCharge + _spacerCharge + _gasCharge;
            double baseCost = (glass + processing) / _wastageFactor;

            Result = baseCost * _profitFactor;

            OnPropertyChanged(nameof(TotalGlassCost));
            OnPropertyChanged(nameof(ProcessingCost));
            OnPropertyChanged(nameof(BaseCost));
            OnPropertyChanged(nameof(VatAmount));
            OnPropertyChanged(nameof(GrossTotal));
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