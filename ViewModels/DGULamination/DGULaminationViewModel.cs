using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ProGlassAutomation.Views.DGULamination
{
    public class DGULaminationViewModel : INotifyPropertyChanged
    {
        // =====================================================
        // EVENT HANDLER
        // =====================================================
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        // =====================================================
        // COLLECTIONS
        // =====================================================
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
            "15%", "20%", "25%", "30%", "35%"
        };

        public ObservableCollection<RecordModel> Records { get; } = new ObservableCollection<RecordModel>();

        // =====================================================
        // PRICE PROPERTIES
        // =====================================================
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

        private string _outsourcePrice = "100";
        public string OutsourcePrice
        {
            get => _outsourcePrice;
            set { _outsourcePrice = value; OnPropertyChanged(); Calculate(); }
        }

        // =====================================================
        // GLASS PROPERTIES
        // =====================================================
        private string _thickness1 = "6mm";
        public string Thickness1
        {
            get => _thickness1;
            set { _thickness1 = value; OnPropertyChanged(); Calculate(); }
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
            set { _thickness2 = value; OnPropertyChanged(); Calculate(); }
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
            set { _thickness3 = value; OnPropertyChanged(); Calculate(); }
        }

        private string _color3 = "Clear";
        public string Color3
        {
            get => _color3;
            set { _color3 = value; OnPropertyChanged(); }
        }

        // =====================================================
        // ASP SETTINGS
        // =====================================================
        private string _aspThickness = "12mm";
        public string AspThickness
        {
            get => _aspThickness;
            set { _aspThickness = value; OnPropertyChanged(); Calculate(); }
        }

        private string _aspType = "Normal";
        public string AspType
        {
            get => _aspType;
            set { _aspType = value; OnPropertyChanged(); Calculate(); }
        }

        // =====================================================
        // PROFIT MARGIN
        // =====================================================
        private string _profitMargin = "20%";
        public string ProfitMargin
        {
            get => _profitMargin;
            set { _profitMargin = value; OnPropertyChanged(); Calculate(); }
        }

        // =====================================================
        // RESULT
        // =====================================================
        private string _result = "0.00";
        public string Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        // =====================================================
        // HISTORY VISIBILITY
        // =====================================================
        private bool _isHistoryVisible = false;
        public bool IsHistoryVisible
        {
            get => _isHistoryVisible;
            set { _isHistoryVisible = value; OnPropertyChanged(); }
        }

        // =====================================================
        // SAVE COMMAND
        // =====================================================
        public ICommand SaveCommand { get; }

        // =====================================================
        // CONSTRUCTOR
        // =====================================================
        public DGULaminationViewModel()
        {
            SaveCommand = new RelayCommand(SaveRecord);
            Calculate();
        }

        // =====================================================
        // 🧠 LIVE CALCULATION ENGINE
        // =====================================================
        private void Calculate()
        {
            try
            {
                double s1 = ParseDouble(Sheet1);
                double s2 = ParseDouble(Sheet2);
                double s3 = ParseDouble(Sheet3);
                double asp = ParseDouble(AspPrice);
                double outsource = ParseDouble(OutsourcePrice);

                double glassTotal = s1 + s2 + s3;
                double factor = GetProfitFactor();
                double step1 = glassTotal / factor;
                double aspValue = GetAspValue();
                double baseValue = step1 + aspValue + outsource;
                double profitPercent = GetProfitPercent();
                double final = baseValue * (1 + profitPercent);

                Result = final.ToString("0.00");
            }
            catch
            {
                Result = "0.00";
            }
        }

        // =====================================================
        // 💾 SAVE RECORD (COMPACT FORMAT)
        // =====================================================
        private void SaveRecord()
        {
            try
            {
                string timestamp = DateTime.Now.ToString("dd-MMMM-yyyy - hh:mmtt");
                double final = ParseDouble(Result);

                // Build compact specification string
                string spec = $"{Thickness1} {Color1} FT Glass + " +
                             $"1.52mm {Color3} PVB + " +
                             $"{Thickness3} {Color3} FT Glass";

                // Final compact format
                string record = $"{spec} - {final:0.00} AED - {timestamp}";

                Records.Insert(0, new RecordModel { DisplayText = record });

                while (Records.Count > 10)
                    Records.RemoveAt(Records.Count - 1);
            }
            catch { }
        }

        // =====================================================
        // HELPER: Parse Double Safely
        // =====================================================
        private double ParseDouble(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            if (double.TryParse(value, out double result)) return result;
            return 0;
        }

        // =====================================================
        // ASP VALUE LOGIC
        // =====================================================
        private double GetAspValue()
        {
            double baseAsp = AspThickness switch
            {
                "6mm" => 45,
                "8mm" => 45,
                "10mm" => 45,
                "12mm" => 45,
                "14mm" => 48,
                "16mm" => 50,
                "18mm" => 52,
                "20mm" => 55,
                "22mm" => 58,
                "24mm" => 60,
                _ => 45
            };

            if (AspType == "Black")
                baseAsp += 10;

            return baseAsp;
        }

        // =====================================================
        // PROFIT FACTOR
        // =====================================================
        private double GetProfitFactor()
        {
            return ProfitMargin switch
            {
                "15%" => 0.85,
                "20%" => 0.80,
                "25%" => 0.75,
                "30%" => 0.70,
                "35%" => 0.65,
                _ => 0.80
            };
        }

        // =====================================================
        // PROFIT PERCENTAGE
        // =====================================================
        private double GetProfitPercent()
        {
            return ProfitMargin switch
            {
                "15%" => 0.15,
                "20%" => 0.20,
                "25%" => 0.25,
                "30%" => 0.30,
                "35%" => 0.35,
                _ => 0.20
            };
        }
    }

    // =====================================================
    // RECORD MODEL
    // =====================================================
    public class RecordModel
    {
        public string DisplayText { get; set; } = "";
        public override string ToString() => DisplayText;
    }

    // =====================================================
    // RELAY COMMAND
    // =====================================================
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute();
    }
}