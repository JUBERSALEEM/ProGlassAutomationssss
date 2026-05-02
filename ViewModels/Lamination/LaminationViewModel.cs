using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Helpers;

namespace ProGlassAutomation.Views.Lamination
{
    public class LaminationViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        // Options
        public ObservableCollection<string> ThicknessOptions { get; } = new() { "4mm", "5mm", "6mm", "8mm", "10mm", "12mm", "15mm", "19mm" };
        public ObservableCollection<string> ColorOptions { get; } = new() { "Clear", "Extra Clear", "HD Grey", "HD Blue", "Green", "Bronze", "Dark Grey" };
        public ObservableCollection<string> PVBOptions { get; } = new() { "0.38 Clear", "0.76 Clear", "1.14 Clear", "1.52 Clear", "2.28 Clear", "3.04 Clear" };
        public ObservableCollection<string> ProfitOptions { get; } = new() { "0%", "5%", "10%", "15%", "20%", "25%", "30%", "35%", "40%", "50%" };
        public ObservableCollection<LaminationRecordUI> Records { get; } = new();

        // Properties
        private string _t1 = "6mm";
        public string Thickness1 { get => _t1; set { _t1 = value; OnPropertyChanged(); Recalc(); } }

        private string _t2 = "6mm";
        public string Thickness2 { get => _t2; set { _t2 = value; OnPropertyChanged(); Recalc(); } }

        private string _c1 = "Clear";
        public string Color1 { get => _c1; set { _c1 = value; OnPropertyChanged(); Recalc(); } }

        private string _c2 = "Clear";
        public string Color2 { get => _c2; set { _c2 = value; OnPropertyChanged(); Recalc(); } }

        private string _pvb = "1.52 Clear";
        public string PVBType { get => _pvb; set { _pvb = value; OnPropertyChanged(); Recalc(); } }

        private string _profit = "15%";
        public string Profit
        {
            get => _profit;
            set
            {
                _profit = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProfitFactor));   // e.g., 0.85 for 15%
                OnPropertyChanged(nameof(ProfitMargin));  // e.g., 1.15 for 15%
                Recalc();
            }
        }

        private double _s1 = 36;
        public double Sheet1 { get => _s1; set { _s1 = value; OnPropertyChanged(); Recalc(); } }

        private double _s2 = 36;
        public double Sheet2 { get => _s2; set { _s2 = value; OnPropertyChanged(); Recalc(); } }

        private double _pvbPrice = 100;
        public double PVBPrice { get => _pvbPrice; set { _pvbPrice = value; OnPropertyChanged(); Recalc(); } }

        private double _cutting = 10;
        public double Cutting { get => _cutting; set { _cutting = value; OnPropertyChanged(); Recalc(); } }

        private double _tempering = 16;
        public double Tempering { get => _tempering; set { _tempering = value; OnPropertyChanged(); Recalc(); } }

        private double _result;
        public double Result { get => _result; set { _result = value; OnPropertyChanged(); } }

        // Step-by-step calculation results
        private double _result1;
        public double Result1 { get => _result1; private set { _result1 = value; OnPropertyChanged(); } }

        private double _result2;
        public double Result2 { get => _result2; private set { _result2 = value; OnPropertyChanged(); } }

        private double _result3;
        public double Result3 { get => _result3; private set { _result3 = value; OnPropertyChanged(); } }

        private double _result4;
        public double Result4 { get => _result4; private set { _result4 = value; OnPropertyChanged(); } }

        // Profit Factor (Divisor) - e.g., 0.85 for 15%
        private double _profitFactor;
        public double ProfitFactor { get => _profitFactor; private set { _profitFactor = value; OnPropertyChanged(); } }

        // Profit Margin (Multiplier) - e.g., 1.15 for 15%
        private double _profitMargin;
        public double ProfitMargin { get => _profitMargin; private set { _profitMargin = value; OnPropertyChanged(); } }

        private bool _isHistoryVisible = true;
        public bool IsHistoryVisible { get => _isHistoryVisible; set { _isHistoryVisible = value; OnPropertyChanged(); } }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand CopyCommand { get; }

        public LaminationViewModel()
        {
            SaveCommand = new RelayCommand(p => Save());
            ClearCommand = new RelayCommand(p => Clear());
            DeleteCommand = new RelayCommand(p => DeleteRecord(p));
            ClearAllCommand = new RelayCommand(p => Records.Clear());
            CopyCommand = new RelayCommand(p => CopyToClipboard());
            Recalc();
        }

        // ================= CALCULATION =================
        void Recalc()
        {
            // Get profit values based on profit percentage
            var (factor, margin) = GetProfitValues(Profit);
            ProfitFactor = factor;
            ProfitMargin = margin;

            // Step 1: Sheet1 + Sheet2
            Result1 = Sheet1 + Sheet2;

            // Step 2: Result1 / ProfitFactor
            Result2 = Result1 / ProfitFactor;

            // Step 3: Result2 + PVBPrice + Cutting + Tempering
            Result3 = Result2 + PVBPrice + Cutting + Tempering;

            // Step 4: Result3 * ProfitMargin
            Result4 = Result3 * ProfitMargin;

            // Final Result
            Result = Result4;
        }

        // Get ProfitFactor and ProfitMargin from Profit percentage
        (double Factor, double Margin) GetProfitValues(string profit) => profit switch
        {
            "0%" => (1.00, 1.00),
            "5%" => (0.95, 1.05),
            "10%" => (0.90, 1.10),
            "15%" => (0.85, 1.15),
            "20%" => (0.80, 1.20),
            "25%" => (0.75, 1.25),
            "30%" => (0.70, 1.30),
            "35%" => (0.65, 1.35),
            "40%" => (0.60, 1.40),
            "50%" => (0.50, 1.50),
            _ => (0.85, 1.15)
        };

        // ================= ACTIONS =================
        void Save()
        {
            var record = new LaminationRecordUI
            {
                DisplayText = $"{Thickness1} {Color1} + {PVBType} + {Thickness2} {Color2}",
                DetailText = $"Profit: {Profit} | Final: {Result:F2} AED",
                Timestamp = DateTime.Now,
                Result = Result
            };

            Records.Insert(0, record);

            while (Records.Count > 50)
                Records.RemoveAt(Records.Count - 1);

            MessageBox.Show($"Saved!\nTotal: {Result:F2} AED", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        void Clear()
        {
            Sheet1 = 36;
            Sheet2 = 36;
            PVBPrice = 100;
            Cutting = 10;
            Tempering = 16;
            Thickness1 = "6mm";
            Thickness2 = "6mm";
            Color1 = "Clear";
            Color2 = "Clear";
            PVBType = "1.52 Clear";
            Profit = "15%";
            Recalc();
        }

        void DeleteRecord(object p)
        {
            if (p is LaminationRecordUI r)
                Records.Remove(r);
        }

        void CopyToClipboard()
        {
            string text = $"LAMINATION QUOTATION\n" +
                         $"========================\n" +
                         $"Outer Glass: {Thickness1} {Color1}\n" +
                         $"Inner Glass: {Thickness2} {Color2}\n" +
                         $"PVB Layer: {PVBType}\n" +
                         $"========================\n" +
                         $"Step 1: Sheet1 + Sheet2\n" +
                         $"        {Sheet1:F2} + {Sheet2:F2} = {Result1:F2}\n" +
                         $"------------------------\n" +
                         $"Step 2: / ProfitFactor ({ProfitFactor:F2})\n" +
                         $"        {Result1:F2} / {ProfitFactor:F2} = {Result2:F2}\n" +
                         $"------------------------\n" +
                         $"Step 3: + PVB + Cutting + Tempering\n" +
                         $"        {Result2:F2} + {PVBPrice:F2} + {Cutting:F2} + {Tempering:F2} = {Result3:F2}\n" +
                         $"------------------------\n" +
                         $"Step 4: x ProfitMargin ({ProfitMargin:F2})\n" +
                         $"        {Result3:F2} x {ProfitMargin:F2} = {Result4:F2}\n" +
                         $"========================\n" +
                         $"FINAL PRICE: {Result:F2} AED\n" +
                         $"========================\n" +
                         $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}";

            Clipboard.SetText(text);
            MessageBox.Show("Copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public class LaminationRecordUI
    {
        public string DisplayText { get; set; }
        public string DetailText { get; set; }
        public DateTime Timestamp { get; set; }
        public double Result { get; set; }
    }
}