using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ProGlassAutomation.Views.GlassOptimization
{
    public class GlassOptimizationViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<SheetItem> Sheets { get; } = new ObservableCollection<SheetItem>();
        public ObservableCollection<string> Colors { get; } = new ObservableCollection<string> { "Clear", "Grey", "Blue", "Green", "Bronze", "Reflective", "F Green", "Dark Grey" };
        public ObservableCollection<string> ProfitOptions { get; } = new ObservableCollection<string> { "15%", "20%", "25%", "30%", "35%" };
        public ICommand AddCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand ClearAllCommand { get; }

        SheetItem _sel;
        string _n = "Sheet 1", _c = "Clear", _t = "5mm", _w = "3210", _h = "2250", _q = "2", _u = "65.80", _p = "47", _pm = "15%";

        public GlassOptimizationViewModel()
        {
            AddCommand = new RelayCommand(() => { Sheets.Add(new SheetItem { Name = $"Sheet {Sheets.Count + 1}", Color = _c, Thickness = _t, Width = D(_w), Height = D(_h), Qty = D(_q), Util = D(_u), Price = D(_p) }); RefreshAll(); });
            RemoveCommand = new RelayCommand<SheetItem>(s => { if (s != null) { Sheets.Remove(s); RefreshAll(); } });
            UpdateCommand = new RelayCommand(() => { if (_sel != null) { _sel.Name = _n; _sel.Color = _c; _sel.Thickness = _t; _sel.Width = D(_w); _sel.Height = D(_h); _sel.Qty = D(_q); _sel.Util = D(_u); _sel.Price = D(_p); if (!Colors.Contains(_c)) Colors.Add(_c); RefreshAll(); } });
            ClearAllCommand = new RelayCommand(() => { Sheets.Clear(); RefreshAll(); });
            Sheets.Add(new SheetItem { Name = "Sheet 1", Color = "Clear", Thickness = "5mm", Width = 3210, Height = 2250, Qty = 2, Util = 65.80, Price = 47 });
            Sheets.Add(new SheetItem { Name = "Sheet 2", Color = "Grey", Thickness = "6mm", Width = 3210, Height = 2250, Qty = 2, Util = 65.80, Price = 27 });
            RefreshAll();
        }

        public SheetItem Selected { get => _sel; set { _sel = value; if (value != null) { _n = value.Name; _c = value.Color; _t = value.Thickness; _w = value.Width.ToString(); _h = value.Height.ToString(); _q = value.Qty.ToString(); _u = value.Util.ToString(); _p = value.Price.ToString(); OnPropertyChanged(""); } } }

        public string EditName { get => _n; set { _n = value; OnPropertyChanged(); } }
        public string EditColor { get => _c; set { _c = value; OnPropertyChanged(); } }
        public string EditThickness { get => _t; set { _t = value; OnPropertyChanged(); } }
        public string EditWidth { get => _w; set { _w = value; OnPropertyChanged(); } }
        public string EditHeight { get => _h; set { _h = value; OnPropertyChanged(); } }
        public string EditQty { get => _q; set { _q = value; OnPropertyChanged(); } }
        public string EditUtil { get => _u; set { _u = value; OnPropertyChanged(); } }
        public string EditPrice { get => _p; set { _p = value; OnPropertyChanged(); } }
        public string ProfitMargin { get => _pm; set { _pm = value; OnPropertyChanged(); RefreshAll(); } }

        double Factor => ProfitMargin switch { "15%" => 0.85, "20%" => 0.80, "25%" => 0.75, "30%" => 0.70, "35%" => 0.65, _ => 0.80 };
        public string TotalSec1 { get; set; } = "0.00";
        public string TotalSec2 { get; set; } = "0.00";
        public string TotalResult { get; set; } = "0.00";

        double D(string v) => double.TryParse(v, out var r) ? r : 0;

        void RefreshAll()
        {
            double ts1 = 0, ts2 = 0, tr = 0;
            foreach (var s in Sheets)
            {
                s.Section1 = s.Util > 0 ? s.Price / (s.Util / 100) : 0;
                s.Section2 = Factor > 0 ? s.Price / Factor : 0;
                s.FinalResult = s.Section1 - s.Section2;
                ts1 += s.Section1; ts2 += s.Section2; tr += s.FinalResult;
                s.NotifyAll();
            }
            TotalSec1 = ts1.ToString("0.00"); TotalSec2 = ts2.ToString("0.00"); TotalResult = tr.ToString("0.00");
            OnPropertyChanged("TotalSec1"); OnPropertyChanged("TotalSec2"); OnPropertyChanged("TotalResult");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class SheetItem : INotifyPropertyChanged
    {
        string _n = "Sheet", _c = "Clear", _t = "5mm";
        public string Name { get => _n; set { _n = value; OnPropertyChanged(); } }
        public string Color { get => _c; set { _c = value; OnPropertyChanged(); } }
        public string Thickness { get => _t; set { _t = value; OnPropertyChanged(); } }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Qty { get; set; } = 1;
        public double Util { get; set; } = 65.80;
        public double Price { get; set; }
        public double Section1 { get; set; }
        public double Section2 { get; set; }
        public double FinalResult { get; set; }
        public string SizeText => $"{Width:0} × {Height:0} cm";
        public string DetailsText => $"Qty: {Qty:0} | Util: {Util:0.00}% | Price: {Price:0.00} AED";
        public string Section1Text => Section1.ToString("0.00");
        public string Section2Text => Section2.ToString("0.00");
        public string ResultText => FinalResult.ToString("0.00");
        public void NotifyAll() { OnPropertyChanged("SizeText"); OnPropertyChanged("DetailsText"); OnPropertyChanged("Section1Text"); OnPropertyChanged("Section2Text"); OnPropertyChanged("ResultText"); }
        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class RelayCommand : ICommand { readonly Action _e; public RelayCommand(Action e) => _e = e; public event EventHandler CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; } public bool CanExecute(object p) => true; public void Execute(object p) => _e(); }
    public class RelayCommand<T> : ICommand { readonly Action<T> _e; public RelayCommand(Action<T> e) => _e = e; public event EventHandler CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; } public bool CanExecute(object p) => true; public void Execute(object p) => _e((T)p); }
}