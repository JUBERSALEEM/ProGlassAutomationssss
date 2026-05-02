using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ProGlassAutomation.Views.DGULamination
{
    public class DGULaminationViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // Collections
        public ObservableCollection<string> GlassThicknessOptions { get; } = new ObservableCollection<string>
        {
            "3mm", "4mm", "5mm", "6mm", "8mm", "10mm", "12mm", "15mm", "19mm"
        };

        public ObservableCollection<string> ColorOptions { get; } = new ObservableCollection<string>
        {
            "Clear", "HD Grey", "Green", "Blue", "Grey", "Bronze", "Tinted", "Reflective"
        };

        public ObservableCollection<string> AspThicknessOptions { get; } = new ObservableCollection<string>
        {
            "6mm", "8mm", "10mm", "12mm", "14mm", "16mm", "18mm", "20mm", "22mm", "24mm"
        };

        public ObservableCollection<string> AspTypeOptions { get; } = new ObservableCollection<string>
        {
            "Normal", "Black"
        };

        public ObservableCollection<string> ProfitOptions { get; } = new ObservableCollection<string>
        {
            "5%", "10%", "15%", "17%", "20%", "25%", "30%", "35%", "40%", "50%", "97%"
        };

        public ObservableCollection<RecordModel> Records { get; } = new ObservableCollection<RecordModel>();

        // Price Properties
        private string _sheet1 = "46";
        public string Sheet1
        {
            get => _sheet1;
            set { _sheet1 = value; OnPropertyChanged(); Calculate(); }
        }

        private string _sheet2 = "29";
        public string Sheet2
        {
            get => _sheet2;
            set { _sheet2 = value; OnPropertyChanged(); Calculate(); }
        }

        private string _sheet3 = "29";
        public string Sheet3
        {
            get => _sheet3;
            set { _sheet3 = value; OnPropertyChanged(); Calculate(); }
        }

        private string _aspPrice = "45";
        public string AspPrice
        {
            get => _aspPrice;
            set { _aspPrice = value; OnPropertyChanged(); Calculate(); }
        }

        private string _outsourcePrice = "110";
        public string OutsourcePrice
        {
            get => _outsourcePrice;
            set { _outsourcePrice = value; OnPropertyChanged(); Calculate(); }
        }

        private string _cuttingCharge = "10";
        public string CuttingCharge
        {
            get => _cuttingCharge;
            set { _cuttingCharge = value; OnPropertyChanged(); Calculate(); }
        }

        private string _temperingCharge = "20";
        public string TemperingCharge
        {
            get => _temperingCharge;
            set { _temperingCharge = value; OnPropertyChanged(); Calculate(); }
        }

        // Glass Properties
        private string _thickness1 = "6mm";
        public string Thickness1
        {
            get => _thickness1;
            set { _thickness1 = value; OnPropertyChanged(); }
        }

        private string _color1 = "Clear";
        public string Color1
        {
            get => _color1;
            set { _color1 = value; OnPropertyChanged(); }
        }

        private string _thickness2 = "6mm";
        public string Thickness2
        {
            get => _thickness2;
            set { _thickness2 = value; OnPropertyChanged(); }
        }

        private string _color2 = "Clear";
        public string Color2
        {
            get => _color2;
            set { _color2 = value; OnPropertyChanged(); }
        }

        private string _thickness3 = "6mm";
        public string Thickness3
        {
            get => _thickness3;
            set { _thickness3 = value; OnPropertyChanged(); }
        }

        private string _color3 = "Clear";
        public string Color3
        {
            get => _color3;
            set { _color3 = value; OnPropertyChanged(); }
        }

        // ASP Properties
        private string _aspThickness = "12mm";
        public string AspThickness
        {
            get => _aspThickness;
            set { _aspThickness = value; OnPropertyChanged(); }
        }

        private string _aspType = "Normal";
        public string AspType
        {
            get => _aspType;
            set { _aspType = value; OnPropertyChanged(); }
        }

        // PROFIT MARGIN - ONLY THIS IS SHOWN IN UI
        private string _profitMargin = "15%";
        public string ProfitMargin
        {
            get => _profitMargin;
            set { _profitMargin = value; OnPropertyChanged(); Calculate(); }
        }

        // PROFIT FACTOR - AUTO CALCULATED (NOT SHOWN IN UI INPUT)
        private string _profitFactor = "0.85";
        public string ProfitFactor
        {
            get => _profitFactor;
            private set { _profitFactor = value; OnPropertyChanged(); }
        }

        // Calculation Preview Properties
        private string _result1 = "0.00";
        public string Result1
        {
            get => _result1;
            set { _result1 = value; OnPropertyChanged(); }
        }

        private string _result2 = "0.00";
        public string Result2
        {
            get => _result2;
            set { _result2 = value; OnPropertyChanged(); }
        }

        private string _result3 = "0.00";
        public string Result3
        {
            get => _result3;
            set { _result3 = value; OnPropertyChanged(); }
        }

        private string _result4 = "0.00";
        public string Result4
        {
            get => _result4;
            set { _result4 = value; OnPropertyChanged(); }
        }

        // Final Result
        private string _result = "0.00";
        public string Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        // History Visibility
        private bool _isHistoryVisible = false;
        public bool IsHistoryVisible
        {
            get => _isHistoryVisible;
            set { _isHistoryVisible = value; OnPropertyChanged(); }
        }

        // Commands
        public ICommand SaveCommand { get; }

        // Constructor
        public DGULaminationViewModel()
        {
            SaveCommand = new RelayCommand(SaveRecord);
            Calculate();
        }

        // Parse profit margin percentage to double
        private double ParseProfitPercentage(string profit)
        {
            if (string.IsNullOrEmpty(profit)) return 0.15;
            string clean = profit.Replace("%", "").Trim();
            if (double.TryParse(clean, out double value))
                return value / 100.0;
            return 0.15;
        }

        // Calculate Profit Factor from Profit Margin
        private void UpdateProfitFactor()
        {
            double margin = ParseProfitPercentage(ProfitMargin);
            double factor = 1 - margin;
            ProfitFactor = factor.ToString("0.00");
        }

        // Calculation Engine
        private void Calculate()
        {
            try
            {
                double s1 = ParseDouble(Sheet1);
                double s2 = ParseDouble(Sheet2);
                double s3 = ParseDouble(Sheet3);
                double asp = ParseDouble(AspPrice);
                double outsource = ParseDouble(OutsourcePrice);
                double cutting = ParseDouble(CuttingCharge);
                double tempering = ParseDouble(TemperingCharge);

                // UPDATE PROFIT FACTOR BASED ON PROFIT MARGIN
                UpdateProfitFactor();

                double factor = ParseDouble(ProfitFactor);

                // Step 1: Sum of all glass prices
                double glassTotal = s1 + s2 + s3;
                Result1 = glassTotal.ToString("0.00");

                // Step 2: Divide by factor
                double step1 = glassTotal / factor;
                Result2 = step1.ToString("0.00");

                // Step 3: Add ASP + Outsource + Cutting + Tempering
                double charges = asp + outsource + cutting + tempering;
                double step2 = step1 + charges;
                Result3 = step2.ToString("0.00");

                // Step 4: Apply profit margin (FINAL RESULT)
                double margin = 1 + ParseProfitPercentage(ProfitMargin);
                double final = step2 * margin;
                Result4 = final.ToString("0.00");
                Result = final.ToString("0.00");
            }
            catch
            {
                Result1 = "0.00";
                Result2 = "0.00";
                Result3 = "0.00";
                Result4 = "0.00";
                Result = "0.00";
            }
        }

        // Save Record
        private void SaveRecord()
        {
            try
            {
                DateTime now = DateTime.Now;
                string timestamp = now.ToString("yyyy-MM-dd HH:mm");
                double final = ParseDouble(Result);

                string spec = Thickness1 + " " + Color1 + " Outer + " +
                             Thickness2 + " " + Color2 + " Inner + " +
                             Thickness3 + " " + Color3 + " Lami";

                string detail = "ASP: " + AspThickness + " " + AspType + " | " +
                               "PVB: " + OutsourcePrice + " AED | " +
                               "Cut: " + CuttingCharge + " AED | " +
                               "Temp: " + TemperingCharge + " AED | " +
                               "Profit: " + ProfitMargin;

                Records.Insert(0, new RecordModel
                {
                    DisplayText = spec + " - " + final.ToString("0.00") + " AED - " + timestamp,
                    DetailText = detail,
                    Timestamp = now,
                    Result = final
                });

                while (Records.Count > 50)
                    Records.RemoveAt(Records.Count - 1);
            }
            catch { }
        }

        // Helper: Parse Double
        private double ParseDouble(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            if (double.TryParse(value, out double result)) return result;
            return 0;
        }
    }

    // Record Model
    public class RecordModel
    {
        public string DisplayText { get; set; } = "";
        public string DetailText { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public double Result { get; set; }
    }

    // Relay Command
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute();
    }
}