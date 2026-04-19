using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProGlassAutomation.Models;
using ProGlassAutomation.Data.Database; // 🔥 DATABASE FIX ADDED

namespace ProGlassAutomation.Views.DGU
{
    public class DguViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // ================= LISTS =================

        public List<string> ThicknessList { get; } = new()
        {
            "6mm","8mm","10mm","12mm","15mm","19mm"
        };

        public List<string> ColorList { get; } = new()
        {
            "Clear","Green","Blue","Grey"
        };

        public List<string> AspList { get; } = new()
        {
            "6mm","8mm","10mm","12mm","14mm","16mm","18mm","20mm","22mm","24mm"
        };

        public List<string> ProfitList { get; } = new()
        {
            "15%","20%","25%","30%","35%"
        };

        // ================= HISTORY =================

        public ObservableCollection<DguRecord> Records { get; set; }
            = new ObservableCollection<DguRecord>();

        public ObservableCollection<DguRecord> FilteredRecords => Records;

        // ================= CONSTRUCTOR (DB LOAD ADDED) =================

        public DguViewModel()
        {
            // 🔥 INIT DATABASE
            DbHelper.Init();

            // 🔥 LOAD HISTORY FROM SQLITE
            var data = DbHelper.GetAllDgu();
            foreach (var item in data)
                Records.Add(item);

            // ✔ DEFAULT BLANK INPUTS
            Sheet1 = string.Empty;
            Sheet2 = string.Empty;

            Thickness1 = "6mm";
            Thickness2 = "6mm";

            Color1 = "Clear";
            Color2 = "Clear";

            AspType = "12mm";
            Profit = "15%";

            Calculate();
        }

        // ================= INPUTS =================

        private string thickness1;
        public string Thickness1
        {
            get => thickness1;
            set { thickness1 = value; OnChange(); Calculate(); }
        }

        private string color1;
        public string Color1
        {
            get => color1;
            set { color1 = value; OnChange(); }
        }

        private string sheet1;
        public string Sheet1
        {
            get => sheet1;
            set { sheet1 = value; OnChange(); Calculate(); }
        }

        private string thickness2;
        public string Thickness2
        {
            get => thickness2;
            set { thickness2 = value; OnChange(); Calculate(); }
        }

        private string color2;
        public string Color2
        {
            get => color2;
            set { color2 = value; OnChange(); }
        }

        private string sheet2;
        public string Sheet2
        {
            get => sheet2;
            set { sheet2 = value; OnChange(); Calculate(); }
        }

        private string aspType;
        public string AspType
        {
            get => aspType;
            set { aspType = value; OnChange(); Calculate(); }
        }

        private string profit;
        public string Profit
        {
            get => profit;
            set { profit = value; OnChange(); Calculate(); }
        }

        // ================= RESULT =================

        private string result = "0.00";
        public string Result
        {
            get => result;
            set { result = value; OnChange(); }
        }

        // ================= SAVE (DB ADDED) =================

        public void Save()
        {
            Calculate();

            var record = new DguRecord
            {
                Thickness1 = Thickness1,
                Color1 = Color1,
                Thickness2 = Thickness2,
                Color2 = Color2,
                Spacer = AspType,
                Result = double.TryParse(Result, out var r) ? r : 0,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // 🔥 UI HISTORY
            Records.Add(record);

            // 🔥 DATABASE SAVE
            DbHelper.SaveDgu(
                record.Thickness1,
                record.Color1,
                record.Thickness2,
                record.Color2,
                record.Spacer,
                record.Result
            );

            OnChange(nameof(FilteredRecords));
        }

        // ================= CALCULATION =================

        public void Calculate()
        {
            double baseValue = Parse(Sheet1) + Parse(Sheet2);

            double factor = Profit switch
            {
                "15%" => 0.85,
                "20%" => 0.80,
                "25%" => 0.75,
                "30%" => 0.70,
                "35%" => 0.65,
                _ => 1
            };

            double step2 = baseValue / factor;

            double asp = AspType switch
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

            double step3 = step2 + asp;

            double margin = Profit switch
            {
                "15%" => 0.15,
                "20%" => 0.20,
                "25%" => 0.25,
                "30%" => 0.30,
                "35%" => 0.35,
                _ => 0
            };

            double final = step3 + (step3 * margin);

            Result = final.ToString("0.00");
        }

        // ================= PARSE =================

        private double Parse(string v)
        {
            return double.TryParse(v, out var x) ? x : 0;
        }

        // ================= NOTIFY =================

        private void OnChange([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}