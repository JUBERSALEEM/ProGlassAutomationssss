using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ProGlassAutomation.Views.Lamination
{
    public class LaminationViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> ThicknessOptions { get; set; }
        public ObservableCollection<string> ColorOptions { get; set; }
        public ObservableCollection<string> PVBOptions { get; set; }
        public ObservableCollection<string> ProfitOptions { get; set; }
        public ObservableCollection<LaminationRecord> Records { get; set; }

        public ICommand SaveCommand { get; set; }

        public LaminationViewModel()
        {
            ThicknessOptions = new ObservableCollection<string>
            {
                "6mm","8mm","10mm","12mm","15mm","19mm"
            };

            ColorOptions = new ObservableCollection<string>
            {
                "Clear","HD Grey","HD Blue","Green","Bronze"
            };

            PVBOptions = new ObservableCollection<string>
            {
                "1.14 Clear","1.52 Clear","2.28 Clear"
            };

            ProfitOptions = new ObservableCollection<string>
            {
                "15%","20%","25%","30%","35%"
            };

            Records = new ObservableCollection<LaminationRecord>();

            SaveCommand = new RelayCommand(Save);

            Thickness1 = "6mm";
            Thickness2 = "6mm";
            Color1 = "Clear";
            Color2 = "Clear";

            PVBType = "1.52 Clear";
            PVBPrice = 100;

            Profit = "15%";

            Cutting = 10;
            Tempering = 10;

            // ✅ FIX: default unchecked state
            IncludeCutting = false;
            IncludeTempering = false;
        }

        // ================= GLASS =================

        public double Sheet1
        {
            get => _sheet1;
            set { _sheet1 = value; OnPropertyChanged(); Recalculate(); }
        }
        private double _sheet1;

        public double Sheet2
        {
            get => _sheet2;
            set { _sheet2 = value; OnPropertyChanged(); Recalculate(); }
        }
        private double _sheet2;

        public string Thickness1 { get; set; }
        public string Thickness2 { get; set; }
        public string Color1 { get; set; }
        public string Color2 { get; set; }

        // ================= PVB =================

        public string PVBType
        {
            get => _pvbType;
            set { _pvbType = value; OnPropertyChanged(); Recalculate(); }
        }
        private string _pvbType;

        public double PVBPrice
        {
            get => _pvbPrice;
            set { _pvbPrice = value; OnPropertyChanged(); Recalculate(); }
        }
        private double _pvbPrice;

        // ================= PROFIT =================

        public string Profit
        {
            get => _profit;
            set { _profit = value; OnPropertyChanged(); Recalculate(); }
        }
        private string _profit;

        // ================= CHARGES =================

        public double Cutting
        {
            get => _cutting;
            set { _cutting = value; OnPropertyChanged(); Recalculate(); }
        }
        private double _cutting;

        public double Tempering
        {
            get => _tempering;
            set { _tempering = value; OnPropertyChanged(); Recalculate(); }
        }
        private double _tempering;

        // ================= FIX: TOGGLE FLAGS =================

        public bool IncludeCutting
        {
            get => _includeCutting;
            set
            {
                _includeCutting = value;
                OnPropertyChanged();
                Recalculate();
            }
        }
        private bool _includeCutting;

        public bool IncludeTempering
        {
            get => _includeTempering;
            set
            {
                _includeTempering = value;
                OnPropertyChanged();
                Recalculate();
            }
        }
        private bool _includeTempering;

        // ================= RESULT =================

        public string Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }
        private string _result;

        // ================= ENGINE =================

        private void Recalculate()
        {
            double baseGlass = Sheet1 + Sheet2;

            double profit = ParseProfit(Profit);
            double factor = GetFactor(profit);

            double stage1 = baseGlass / factor;

            double subtotal = stage1 + PVBPrice;

            // ✅ FIXED LOGIC (TICK BASED)
            if (IncludeCutting)
                subtotal += Cutting;

            if (IncludeTempering)
                subtotal += Tempering;

            double final = subtotal * (1 + profit / 100.0);

            Result = final.ToString("0.00");
        }

        private double GetFactor(double profit)
        {
            if (profit == 15) return 0.85;
            if (profit == 20) return 0.80;
            if (profit == 25) return 0.75;
            if (profit == 30) return 0.70;
            return 1 - profit / 100.0;
        }

        private double ParseProfit(string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return 15;
            p = p.Replace("%", "");
            return double.TryParse(p, out var r) ? r : 15;
        }

        // ================= SAVE =================

        private void Save()
        {
            double baseGlass = Sheet1 + Sheet2;

            double profit = ParseProfit(Profit);
            double factor = GetFactor(profit);

            double stage1 = baseGlass / factor;

            double subtotal = stage1 + PVBPrice;

            // ✅ FIXED SAVE LOGIC TOO
            if (IncludeCutting)
                subtotal += Cutting;

            if (IncludeTempering)
                subtotal += Tempering;

            double final = subtotal * (1 + profit / 100.0);

            string outer = $"{Thickness1} {Color1} FT Glass";
            string inner = $"{Thickness2} {Color2} FT Glass";
            string pvb = $"{PVBType}";

            Records.Add(new LaminationRecord
            {
                DisplayText =
                    $"{outer} + {pvb} + {inner} - {final:0.00} - {DateTime.Now:dd/MM/yyyy HH:mm}"
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class LaminationRecord
    {
        public string DisplayText { get; set; }
    }
}