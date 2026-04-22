using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ProGlassAutomation.Views.DGULamination
{
    public class DGULaminationViewModel : INotifyPropertyChanged
    {
        // =====================================================
        // 🚨 SINGLE PROPERTY CHANGED EVENT (NO AMBIGUITY)
        // =====================================================
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool _lock;

        // =====================================================
        // 🪟 GLASS PROPERTIES (NEW)
        // =====================================================
        private string _thickness1 = "6mm";
        public string Thickness1
        {
            get => _thickness1;
            set { _thickness1 = value; Recalc(); OnPropertyChanged(); }
        }

        private string _color1 = "Clear";
        public string Color1
        {
            get => _color1;
            set { _color1 = value; Recalc(); OnPropertyChanged(); }
        }

        private string _thickness2 = "6mm";
        public string Thickness2
        {
            get => _thickness2;
            set { _thickness2 = value; Recalc(); OnPropertyChanged(); }
        }

        private string _color2 = "Clear";
        public string Color2
        {
            get => _color2;
            set { _color2 = value; Recalc(); OnPropertyChanged(); }
        }

        private string _thickness3 = "6mm";
        public string Thickness3
        {
            get => _thickness3;
            set { _thickness3 = value; Recalc(); OnPropertyChanged(); }
        }

        private string _color3 = "Clear";
        public string Color3
        {
            get => _color3;
            set { _color3 = value; Recalc(); OnPropertyChanged(); }
        }

        // =====================================================
        // 💰 PRICE INPUTS (Keep existing)
        // =====================================================
        private double _sheet1 = 46;
        public double Sheet1
        {
            get => _sheet1;
            set { _sheet1 = value; Recalc(); }
        }

        private double _sheet2 = 29;
        public double Sheet2
        {
            get => _sheet2;
            set { _sheet2 = value; Recalc(); }
        }

        private double _sheet3 = 29;
        public double Sheet3
        {
            get => _sheet3;
            set { _sheet3 = value; Recalc(); }
        }

        // =====================================================
        // 📏 ASP SETTINGS
        // =====================================================
        private string _aspThickness = "12mm";
        public string AspThickness
        {
            get => _aspThickness;
            set { _aspThickness = value; Recalc(); }
        }

        private string _aspType = "Normal";
        public string AspType
        {
            get => _aspType;
            set { _aspType = value; Recalc(); }
        }

        private double _aspPrice = 45;
        public double AspPrice
        {
            get => _aspPrice;
            set { _aspPrice = value; Recalc(); }
        }

        // =====================================================
        // 🚚 OUTSOURCE
        // =====================================================
        private double _outsource = 100;
        public double OutsourcePrice
        {
            get => _outsource;
            set { _outsource = value; Recalc(); }
        }

        // =====================================================
        // 📈 PROFIT
        // =====================================================
        private string _profit = "15%";
        public string ProfitMargin
        {
            get => _profit;
            set { _profit = value; Recalc(); }
        }

        // =====================================================
        // 📊 RESULT
        // =====================================================
        private string _result = "0.00";
        public string Result
        {
            get => _result;
            set
            {
                _result = value;
                OnPropertyChanged();
            }
        }

        // =====================================================
        // 🟢 STATUS INDICATORS (For colorful balls)
        // =====================================================
        private bool _isOuterActive = true;
        public bool IsOuterActive
        {
            get => _isOuterActive;
            set { _isOuterActive = value; OnPropertyChanged(); }
        }

        private bool _isInnerActive = true;
        public bool IsInnerActive
        {
            get => _isInnerActive;
            set { _isInnerActive = value; OnPropertyChanged(); }
        }

        private bool _isLaminationActive = false;
        public bool IsLaminationActive
        {
            get => _isLaminationActive;
            set { _isLaminationActive = value; OnPropertyChanged(); }
        }

        private bool _isAspActive = true;
        public bool IsAspActive
        {
            get => _isAspActive;
            set { _isAspActive = value; OnPropertyChanged(); }
        }

        // =====================================================
        // 📦 COLLECTIONS
        // =====================================================
        public ObservableCollection<string> GlassThicknessOptions { get; set; }
        public ObservableCollection<string> AspThicknessOptions { get; set; }
        public ObservableCollection<string> AspTypeOptions { get; set; }
        public ObservableCollection<string> ColorOptions { get; set; }

        public ObservableCollection<RecordModel> Records { get; set; }

        // =====================================================
        // 🚀 CONSTRUCTOR
        // =====================================================
        public DGULaminationViewModel()
        {
            GlassThicknessOptions = new()
            {
                "6mm","8mm","10mm","12mm","15mm","19mm"
            };

            AspThicknessOptions = new()
            {
                "6mm","8mm","10mm","12mm","14mm","16mm",
                "18mm","20mm","22mm","24mm"
            };

            AspTypeOptions = new() { "Normal", "Black" };

            ColorOptions = new()
            {
                "Clear", "HD Grey", "Green", "Blue", "Grey", "Bronze"
            };

            Records = new ObservableCollection<RecordModel>();

            Recalc();
        }

        // =====================================================
        // 🧠 MAIN CALC ENGINE + CLEAN HISTORY FORMAT
        // =====================================================
        private void Recalc()
        {
            if (_lock) return;

            try
            {
                _lock = true;

                // 🔹 GLASS TOTAL
                double glassTotal = Sheet1 + Sheet2 + Sheet3;

                // 🔹 PROFIT FACTOR
                double factor = GetProfitFactor();

                double step1 = glassTotal / factor;

                // 🔹 ASP CALC
                double asp = GetAsp();

                // 🔹 BASE
                double baseValue = step1 + asp + OutsourcePrice;

                // 🔹 FINAL PROFIT
                double profit = GetProfitPercent();

                double final = baseValue * (1 + profit);

                Result = final.ToString("0.00");

                // 🎯 CLEAN PROFESSIONAL HISTORY FORMAT WITH DATE/TIME
                string history = BuildCleanHistoryString(final);

                Records.Insert(0, new RecordModel
                {
                    DisplayText = history
                });

                // Limit history to 10 items
                if (Records.Count > 10)
                    Records.RemoveAt(Records.Count - 1);

                // Update status indicators
                UpdateStatusIndicators();

            }
            catch (Exception ex)
            {
                MessageBox.Show("CALC ERROR: " + ex.Message);
            }
            finally
            {
                _lock = false;
            }
        }

        // =====================================================
        // 📝 BUILD CLEAN HISTORY STRING WITH DATE/TIME & RESULT
        // =====================================================
        private string BuildCleanHistoryString(double finalPrice)
        {
            string timestamp = DateTime.Now.ToString("dd MMM yyyy | HH:mm:ss");

            return $"📋 {timestamp}\n" +
                   $"{Thickness1} {Color1} FT Glass +\n" +
                   $"{AspThickness} {AspType} ASP +\n" +
                   $"{Thickness2} {Color2} FT Glass +\n" +
                   $"1.52 {Color3} PVB +\n" +
                   $"{Thickness3} {Color3} FT Glass\n" +
                   $"━━━━━━━━━━━━━━━\n" +
                   $"💰 TOTAL: {finalPrice:0.00}";
        }

        // =====================================================
        // 🟢 UPDATE STATUS INDICATORS
        // =====================================================
        private void UpdateStatusIndicators()
        {
            IsOuterActive = !string.IsNullOrEmpty(Thickness1) && Sheet1 > 0;
            IsInnerActive = !string.IsNullOrEmpty(Thickness2) && Sheet2 > 0;
            IsLaminationActive = !string.IsNullOrEmpty(Thickness3) && Sheet3 > 0;
            IsAspActive = !string.IsNullOrEmpty(AspThickness) && AspPrice > 0;
        }

        // =====================================================
        // 🧪 ASP LOGIC (NORMAL + BLACK +10)
        // =====================================================
        private double GetAsp()
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
        // 📊 PROFIT FACTOR
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
                _ => 0.85
            };
        }

        // =====================================================
        // 📈 PROFIT %
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
                _ => 0.15
            };
        }
    }

    // =====================================================
    // 📜 ENHANCED HISTORY MODEL
    // =====================================================
    public class RecordModel
    {
        public string DisplayText { get; set; }

        public override string ToString()
        {
            return DisplayText;
        }
    }
}