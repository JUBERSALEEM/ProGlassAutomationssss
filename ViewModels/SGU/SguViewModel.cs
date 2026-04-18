using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ProGlassAutomation.Data.Database;

namespace ProGlassAutomation.ViewModels.SGU
{
    public class SguViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        double sheet, cutting, tempering, profit;

        public string Thickness { get; set; }
        public string Color { get; set; }

        public string Sheet { get => sheet == 0 ? "" : sheet.ToString(); set { double.TryParse(value, out sheet); Calc(); } }
        public string Cutting { get => cutting == 0 ? "" : cutting.ToString(); set { double.TryParse(value, out cutting); Calc(); } }
        public string Tempering { get => tempering == 0 ? "" : tempering.ToString(); set { double.TryParse(value, out tempering); Calc(); } }
        public string Profit { get => profit == 0 ? "" : profit.ToString(); set { double.TryParse(value, out profit); Calc(); } }

        double result;
        public string Result => result.ToString("0.00");

        public ObservableCollection<SguRecord> Records { get; set; }

        public SguViewModel()
        {
            DbHelper.Init();
            Records = new ObservableCollection<SguRecord>(DbHelper.GetAllFormatted());
        }

        void Calc()
        {
            double baseSheet = sheet / 0.85;
            double total = baseSheet + cutting + tempering;
            result = total * (1 + profit / 100);

            OnPropertyChanged(nameof(Result));
        }

        public void Save()
        {
            DbHelper.Save(Thickness, Color, result);

            Records.Clear();
            foreach (var r in DbHelper.GetAllFormatted())
                Records.Add(r);
        }
    }

    public class SguRecord
    {
        public string Thickness { get; set; }
        public string Color { get; set; }
        public double Result { get; set; }
        public string CreatedAt { get; set; }

        // FINAL DISPLAY STRING
        public string DisplayText { get; set; }
    }
}