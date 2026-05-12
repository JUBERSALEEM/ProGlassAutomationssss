using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
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

        // ═══════════════════════════════════════════════════════════
        // COLLECTIONS
        // ═══════════════════════════════════════════════════════════
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

        public ObservableCollection<string> WastageOptions { get; } = new ObservableCollection<string>
        {
            "10", "15", "20", "25", "30"
        };

        public ObservableCollection<string> ProfitOptions { get; } = new ObservableCollection<string>
        {
            "5%", "10%", "15%", "17%", "20%", "25%", "30%", "35%", "40%", "50%", "97%"
        };

        public ObservableCollection<RecordModel> Records { get; } = new ObservableCollection<RecordModel>();

        // ═══════════════════════════════════════════════════════════
        // INVENTORY TOTALS
        // ═══════════════════════════════════════════════════════════
        public int GlassThicknessTotal => GlassThicknessOptions.Count;
        public int ColorTotal => ColorOptions.Count;
        public int AspTypeTotal => AspTypeOptions.Count;

        // ═══════════════════════════════════════════════════════════
        // PRICE PROPERTIES
        // ═══════════════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════════════
        // GLASS PROPERTIES
        // ═══════════════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════════════
        // ASP PROPERTIES
        // ═══════════════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════════════
        // WASTAGE (separate from Profit Margin)
        // ═══════════════════════════════════════════════════════════
        private string _wastage = "15";
        public string Wastage
        {
            get => _wastage;
            set { _wastage = value; OnPropertyChanged(); Calculate(); }
        }

        private string _wastageFactor = "0.85";
        public string WastageFactor
        {
            get => _wastageFactor;
            private set { _wastageFactor = value; OnPropertyChanged(); }
        }

        // ═══════════════════════════════════════════════════════════
        // PROFIT MARGIN
        // ═══════════════════════════════════════════════════════════
        private string _profitMargin = "15%";
        public string ProfitMargin
        {
            get => _profitMargin;
            set { _profitMargin = value; OnPropertyChanged(); Calculate(); }
        }

        private string _profitFactor = "1.15";
        public string ProfitFactor
        {
            get => _profitFactor;
            private set { _profitFactor = value; OnPropertyChanged(); }
        }

        // ═══════════════════════════════════════════════════════════
        // CALCULATION PREVIEW PROPERTIES
        // ═══════════════════════════════════════════════════════════
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

        private string _result = "0.00";
        public string Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        // ═══════════════════════════════════════════════════════════
        // HISTORY
        // ═══════════════════════════════════════════════════════════
        // ═══════════════════════════════════════════════════════════
        // HISTORY
        // ═══════════════════════════════════════════════════════════
        public bool IsHistoryVisible => Records.Count > 0;
        public bool IsHistoryEmpty => Records.Count == 0;

        // ═══════════════════════════════════════════════════════════
        // COMMANDS
        // ═══════════════════════════════════════════════════════════
        public ICommand SaveCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ExportPdfCommand { get; }

        // ═══════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════
        public DGULaminationViewModel()
        {
            SaveCommand = new RelayCommand(_ => SaveRecord());
            ClearCommand = new RelayCommand(_ => ClearFields());
            ClearAllCommand = new RelayCommand(_ => ClearAllRecords());
            DeleteCommand = new RelayCommand(DeleteRecord);
            ExportPdfCommand = new RelayCommand(_ => ExportPdf());
            Calculate();
        }

        // ═══════════════════════════════════════════════════════════
        // PARSE WASTAGE PERCENTAGE
        // ═══════════════════════════════════════════════════════════
        private double ParseWastagePercentage(string wastage)
        {
            if (string.IsNullOrEmpty(wastage)) return 0.15;
            if (double.TryParse(wastage, out double value))
                return value / 100.0;
            return 0.15;
        }

        // ═══════════════════════════════════════════════════════════
        // PARSE PROFIT PERCENTAGE
        // ═══════════════════════════════════════════════════════════
        private double ParseProfitPercentage(string profit)
        {
            if (string.IsNullOrEmpty(profit)) return 0.15;
            string clean = profit.Replace("%", "").Trim();
            if (double.TryParse(clean, out double value))
                return value / 100.0;
            return 0.15;
        }

        // ═══════════════════════════════════════════════════════════
        // UPDATE WASTAGE FACTOR
        // ═══════════════════════════════════════════════════════════
        private void UpdateWastageFactor()
        {
            double wastagePercent = ParseWastagePercentage(Wastage);
            double factor = 1 - wastagePercent;
            WastageFactor = factor.ToString("0.00");
        }

        // ═══════════════════════════════════════════════════════════
        // UPDATE PROFIT FACTOR
        // ═══════════════════════════════════════════════════════════
        private void UpdateProfitFactor()
        {
            double profitPercent = ParseProfitPercentage(ProfitMargin);
            double factor = 1 + profitPercent;
            ProfitFactor = factor.ToString("0.00");
        }

        // ═══════════════════════════════════════════════════════════
        // CALCULATE (Matching DguViewModel Formula)
        // ═══════════════════════════════════════════════════════════
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

                // Update factors
                UpdateWastageFactor();
                UpdateProfitFactor();

                double wastageFactor = ParseDouble(WastageFactor);
                double profitFactor = ParseDouble(ProfitFactor);

                // Step 1: Sum of all glass prices
                double glassTotal = s1 + s2 + s3;
                Result1 = glassTotal.ToString("0.00");

                // Step 2: Divide by wastage factor (÷ 0.85, ÷ 0.80, etc.)
                double baseCost = wastageFactor > 0 ? glassTotal / wastageFactor : glassTotal;
                Result2 = baseCost.ToString("0.00");

                // Step 3: Add ASP + Outsource + Cutting + Tempering
                double charges = asp + outsource + cutting + tempering;
                double processingCost = baseCost + charges;
                Result3 = processingCost.ToString("0.00");

                // Step 4: Multiply by profit factor (× 1.15, × 1.20, etc.)
                double final = processingCost * profitFactor;
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

        // ═══════════════════════════════════════════════════════════
        // SAVE RECORD
        // ═══════════════════════════════════════════════════════════
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
                               "Wastage: " + Wastage + "% | " +
                               "Profit: " + ProfitMargin;

                Records.Insert(0, new RecordModel
                {
                    DisplayText = spec + " - " + final.ToString("0.00") + " AED - " + timestamp,
                    DetailText = detail,
                    Timestamp = now,
                    CreatedAt = now.ToString("dd/MM HH:mm"),
                    Result = final,
                    Wastage = Wastage + "%",
                    ProfitMargin = ProfitMargin
                });

                while (Records.Count > 50)
                    Records.RemoveAt(Records.Count - 1);

                OnPropertyChanged(nameof(IsHistoryVisible));
                OnPropertyChanged(nameof(IsHistoryEmpty));
                MessageBox.Show($"Record saved!\n\nResult: {final:F2} AED", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════════════
        // CLEAR FIELDS
        // ═══════════════════════════════════════════════════════════
        private void ClearFields()
        {
            Sheet1 = "46";
            Sheet2 = "29";
            Sheet3 = "29";
            AspPrice = "45";
            OutsourcePrice = "110";
            CuttingCharge = "10";
            TemperingCharge = "20";
        }

        // ═══════════════════════════════════════════════════════════
        // CLEAR ALL RECORDS
        // ═══════════════════════════════════════════════════════════
        private void ClearAllRecords()
        {
            Records.Clear();
            OnPropertyChanged(nameof(IsHistoryVisible));
            OnPropertyChanged(nameof(IsHistoryEmpty));
        }

        // ═══════════════════════════════════════════════════════════
        // DELETE RECORD
        // ═══════════════════════════════════════════════════════════
        private void DeleteRecord(object parameter)
        {
            if (parameter is RecordModel record)
                Records.Remove(record);
            OnPropertyChanged(nameof(IsHistoryVisible));
            OnPropertyChanged(nameof(IsHistoryEmpty));
        }

        // ═══════════════════════════════════════════════════════════
        // EXPORT PDF
        // ═══════════════════════════════════════════════════════════
        private void ExportPdf()
        {
            MessageBox.Show("PDF export coming soon!", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ═══════════════════════════════════════════════════════════
        // PARSE DOUBLE HELPER
        // ═══════════════════════════════════════════════════════════
        private double ParseDouble(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            if (double.TryParse(value, out double result)) return result;
            return 0;
        }
    }

    // ═══════════════════════════════════════════════════════════
    // RECORD MODEL
    // ═══════════════════════════════════════════════════════════
    public class RecordModel
    {
        public string DisplayText { get; set; } = "";
        public string DetailText { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public double Result { get; set; }
        public string CreatedAt { get; set; } = "";
        public string Wastage { get; set; } = "";
        public string ProfitMargin { get; set; } = "";
    }

    // ═══════════════════════════════════════════════════════════
    // RELAY COMMAND
    // ═══════════════════════════════════════════════════════════
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public RelayCommand(Action<object> execute) => _execute = execute;
        public event EventHandler CanExecuteChanged;
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute(parameter);
    }
}