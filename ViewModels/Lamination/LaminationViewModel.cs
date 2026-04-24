using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Models;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Views.SGU;
using ProGlassAutomation.Helpers;

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

            Records = new ObservableCollection<LaminationRecordUI>();

            // Load history from database
            LoadHistory();

            SaveCommand = new RelayCommand(Save);

            // DEFAULTS
            Thickness1 = "6mm";
            Thickness2 = "6mm";
            Color1 = "Clear";
            Color2 = "Clear";
            PVBType = "1.52 Clear";

            Profit = "15%";

            Cutting = "";
            Tempering = "";
            PVBPrice = 0;

            Sheet1 = 0;
            Sheet2 = 0;

            // History is OPEN by default
            IsHistoryVisible = true;
        }

        private void LoadHistory()
        {
            try
            {
                var dbRecords = DbHelper.GetAllLamination();

                Records.Clear();

                foreach (var r in dbRecords)
                {
                    Records.Add(new LaminationRecordUI
                    {
                        DisplayText = $"{r.Thickness1} {r.Color1} + {r.PVBType} + {r.Thickness2} {r.Color2} = {r.Result:0.00} AED"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load History Failed: " + ex.Message);
            }
        }

        // ================= PRICES =================

        private double _sheet1;
        public double Sheet1
        {
            get => _sheet1;
            set { _sheet1 = value; OnPropertyChanged(); Recalculate(); }
        }

        private double _sheet2;
        public double Sheet2
        {
            get => _sheet2;
            set { _sheet2 = value; OnPropertyChanged(); Recalculate(); }
        }

        public string Thickness1 { get; set; }
        public string Thickness2 { get; set; }
        public string Color1 { get; set; }
        public string Color2 { get; set; }

        private string _pvbType;
        public string PVBType
        {
            get => _pvbType;
            set { _pvbType = value; OnPropertyChanged(); Recalculate(); }
        }

        private double _pvbPrice;
        public double PVBPrice
        {
            get => _pvbPrice;
            set { _pvbPrice = value; OnPropertyChanged(); Recalculate(); }
        }

        private string _profit;
        public string Profit
        {
            get => _profit;
            set { _profit = value; OnPropertyChanged(); Recalculate(); }
        }

        // ================= CUTTING AND TEMPERING =================

        private string _cutting;
        public string Cutting
        {
            get => _cutting;
            set { _cutting = value; OnPropertyChanged(); Recalculate(); }
        }

        private string _tempering;
        public string Tempering
        {
            get => _tempering;
            set { _tempering = value; OnPropertyChanged(); Recalculate(); }
        }

        // ================= RESULT =================

        private string _result;
        public string Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        // ================= HISTORY VISIBILITY (BOOL FOR TOGGLE) =================

        private bool _isHistoryVisible = true;  // CHANGED: true = history open by default

        public bool IsHistoryVisible
        {
            get => _isHistoryVisible;
            set => SetProperty(ref _isHistoryVisible, value);
        }

        // ================= PARSER =================

        private double ParseSafe(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return 0;

            return double.TryParse(input, out var v) ? v : 0;
        }

        private double ParseProfit(string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return 15;

            p = p.Replace("%", "");
            return double.TryParse(p, out var r) ? r : 15;
        }

        // ================= CALCULATION =================

        private void Recalculate()
        {
            double baseGlass = Sheet1 + Sheet2;

            double profit = ParseProfit(Profit);
            double factor = 1 - (profit / 100.0);
            double stage1 = factor > 0 ? baseGlass / factor : baseGlass;

            double stage2 = stage1 + PVBPrice;

            double cutting = ParseSafe(Cutting);
            double tempering = ParseSafe(Tempering);
            double stage3 = stage2 + cutting + tempering;

            double final = stage3 + (stage3 * profit / 100.0);

            Result = final.ToString("0.00");
        }

        // ================= SAVE =================

        private void Save()
        {
            double final = 0;
            double.TryParse(Result, out final);

            var dbRecord = new LaminationRecord
            {
                Thickness1 = Thickness1,
                Color1 = Color1,
                Thickness2 = Thickness2,
                Color2 = Color2,
                PVBType = PVBType,
                Result = final,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Cutting = ParseSafe(Cutting),
                Tempering = ParseSafe(Tempering)
            };

            try
            {
                DbHelper.SaveLamination(dbRecord);

                // Add to top of list
                Records.Insert(0, new LaminationRecordUI
                {
                    DisplayText = $"{Thickness1} {Color1} + {PVBType} + {Thickness2} {Color2} = {final:0.00} AED"
                });

                MessageBox.Show("Saved Successfully!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save Failed: " + ex.Message);
            }
        }

        // ================= MVVM =================

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, newValue))
            {
                field = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }

            return false;
        }
    }

    public class LaminationRecordUI
    {
        public string DisplayText { get; set; }
    }
}