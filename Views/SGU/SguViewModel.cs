using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Data.Database;

namespace ProGlassAutomation.Views.SGU
{
    public class SguViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> ThicknessOptions { get; set; }
        public ObservableCollection<string> ColorOptions { get; set; }
        public ObservableCollection<string> ProfitOptions { get; set; }

        public ObservableCollection<SguRecordUI> Records { get; set; }

        public ICommand SaveCommand { get; set; }

        public SguViewModel()
        {
            ThicknessOptions = new ObservableCollection<string>
            {
                "6mm","8mm","10mm","12mm","15mm","19mm"
            };

            ColorOptions = new ObservableCollection<string>
            {
                "Clear","HD Grey","HD Blue","HD Green","HD Bronze"
            };

            ProfitOptions = new ObservableCollection<string>
            {
                "15%","20%","25%","30%","35%"
            };

            Records = new ObservableCollection<SguRecordUI>();

            LoadHistory();

            SaveCommand = new RelayCommand(Save);

            Thickness = "6mm";
            Color = "Clear";
            Profit = "15%";

            Sheet = "";
            Cutting = "";
            Tempering = "";

            IsHistoryVisible = true;
        }

        private void LoadHistory()
        {
            try
            {
                var dbRecords = DbHelper.GetAllFormatted();

                Records.Clear();

                foreach (var r in dbRecords)
                {
                    Records.Add(new SguRecordUI
                    {
                        DisplayText = $"{r.Thickness} {r.Color} = {r.Result:0.00} AED"
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Load History Failed: " + ex.Message);
            }
        }

        private double _sheet;
        public string Sheet
        {
            get => _sheet == 0 ? "" : _sheet.ToString();
            set
            {
                double.TryParse(value, out _sheet);
                OnPropertyChanged();
                Recalculate();
            }
        }

        private double _cutting;
        public string Cutting
        {
            get => _cutting == 0 ? "" : _cutting.ToString();
            set
            {
                double.TryParse(value, out _cutting);
                OnPropertyChanged();
                Recalculate();
            }
        }

        private double _tempering;
        public string Tempering
        {
            get => _tempering == 0 ? "" : _tempering.ToString();
            set
            {
                double.TryParse(value, out _tempering);
                OnPropertyChanged();
                Recalculate();
            }
        }

        public string Thickness { get; set; }
        public string Color { get; set; }

        private string _profit;
        public string Profit
        {
            get => _profit;
            set
            {
                _profit = value;
                OnPropertyChanged();
                Recalculate();
            }
        }

        private string _result = "0.00";
        public string Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        private bool _isHistoryVisible = true;
        public bool IsHistoryVisible
        {
            get => _isHistoryVisible;
            set => SetProperty(ref _isHistoryVisible, value);
        }

        private double ParseProfit(string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return 15;
            p = p.Replace("%", "");
            return double.TryParse(p, out var r) ? r : 15;
        }

        private void Recalculate()
        {
            double baseSheet = _sheet / 0.85;
            double total = baseSheet + _cutting + _tempering;
            double profitValue = ParseProfit(Profit);
            double final = total * (1 + profitValue / 100);

            Result = final.ToString("0.00");
        }

        private void Save()
        {
            double final = 0;
            double.TryParse(Result, out final);

            try
            {
                DbHelper.Save(Thickness, Color, final);
                LoadHistory();

                MessageBox.Show("Saved Successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save Failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

    public class SguRecordUI
    {
        public string DisplayText { get; set; }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }
}