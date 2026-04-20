using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Models;
using ProGlassAutomation.Data.Database;

namespace ProGlassAutomation.Views.Lamination
{
    public class LaminationViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> ThicknessOptions { get; set; }
        public ObservableCollection<string> ColorOptions { get; set; }
        public ObservableCollection<string> PVBOptions { get; set; }
        public ObservableCollection<string> ProfitOptions { get; set; }

        public ObservableCollection<LaminationRecordUI> Records { get; set; }

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

            // ================= LOAD HISTORY =================
            Records = new ObservableCollection<LaminationRecordUI>();

            try
            {
                var dbRecords = DbHelper.GetAllLamination();

                foreach (var r in dbRecords)
                {
                    Records.Add(new LaminationRecordUI
                    {
                        DisplayText =
                            $"{r.Thickness1} {r.Color1} FT Glass + " +
                            $"{r.PVBType} PVB + " +
                            $"{r.Thickness2} {r.Color2} FT Glass - " +
                            $"{r.Result:0.00} AED - {r.CreatedAt}"
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Load History Failed: " + ex.Message);
            }

            SaveCommand = new RelayCommand(Save);

            // DEFAULT VALUES
            Thickness1 = "6mm";
            Thickness2 = "6mm";
            Color1 = "Clear";
            Color2 = "Clear";

            PVBType = "1.52 Clear";
            PVBPrice = 100;

            Profit = "15%";

            Cutting = 10;
            Tempering = 10;

            IncludeCutting = false;
            IncludeTempering = false;
        }

        // ================= INPUT =================

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

        public string Profit
        {
            get => _profit;
            set { _profit = value; OnPropertyChanged(); Recalculate(); }
        }
        private string _profit;

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

        public bool IncludeCutting
        {
            get => _includeCutting;
            set { _includeCutting = value; OnPropertyChanged(); Recalculate(); }
        }
        private bool _includeCutting;

        public bool IncludeTempering
        {
            get => _includeTempering;
            set { _includeTempering = value; OnPropertyChanged(); Recalculate(); }
        }
        private bool _includeTempering;

        public string Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }
        private string _result;

        // ================= CALCULATION =================

        private void Recalculate()
        {
            double baseGlass = Sheet1 + Sheet2;

            double profit = ParseProfit(Profit);
            double factor = GetFactor(profit);

            double stage1 = baseGlass / factor;
            double stage2 = stage1 + PVBPrice;

            double stage3 = stage2;

            if (IncludeCutting)
                stage3 += Cutting;

            if (IncludeTempering)
                stage3 += Tempering;

            double final = stage3 + (stage3 * profit / 100.0);

            Result = final.ToString("0.00",
                System.Globalization.CultureInfo.InvariantCulture);
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

        // ================= SAVE (CRASH FREE) =================

        private void Save()
        {
            double final;

            if (!double.TryParse(Result, out final))
                final = 0;

            var dbRecord = new LaminationRecord
            {
                Thickness1 = Thickness1,
                Color1 = Color1,
                Thickness2 = Thickness2,
                Color2 = Color2,
                PVBType = PVBType,
                Result = final,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Cutting = IncludeCutting ? Cutting : 0,
                Tempering = IncludeTempering ? Tempering : 0
            };

            try
            {
                DbHelper.SaveLamination(dbRecord);

                Records.Insert(0, new LaminationRecordUI
                {
                    DisplayText =
                        $"{Thickness1} {Color1} FT Glass + {PVBType} PVB + {Thickness2} {Color2} FT Glass - {final:0.00} AED - {DateTime.Now:dd/MM/yyyy HH:mm}"
                });

                MessageBox.Show("Lamination Saved Successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save Failed: " + ex.Message,
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        // ================= MVVM =================

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    // ================= UI MODEL =================

    public class LaminationRecordUI
    {
        public string DisplayText { get; set; }
    }
}