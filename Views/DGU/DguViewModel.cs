using Microsoft.Win32;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProGlassAutomation.Views.DGU
{
    public class DguViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public ObservableCollection<string> ThicknessList { get; } = new() { "6mm", "8mm", "10mm", "12mm", "15mm", "19mm" };
        public ObservableCollection<string> ColorList { get; } = new() { "Clear", "Green", "Blue", "Grey" };
        public ObservableCollection<string> AspList { get; } = new() { "6mm", "8mm", "10mm", "12mm", "14mm", "16mm", "18mm", "20mm", "22mm", "24mm" };
        public ObservableCollection<string> ProfitList { get; } = new() { "15%", "20%", "25%", "30%", "35%" };
        public ObservableCollection<DguRecord> Records { get; } = new();

        private string _t1 = "6mm"; public string Thickness1 { get => _t1; set { _t1 = value; OnPropertyChanged(); Calculate(); } }
        private string _t2 = "6mm"; public string Thickness2 { get => _t2; set { _t2 = value; OnPropertyChanged(); Calculate(); } }
        private string _c1 = "Clear"; public string Color1 { get => _c1; set { _c1 = value; OnPropertyChanged(); } }
        private string _c2 = "Clear"; public string Color2 { get => _c2; set { _c2 = value; OnPropertyChanged(); } }
        private string _s1 = "0"; public string Sheet1 { get => _s1; set { _s1 = value; OnPropertyChanged(); Calculate(); } }
        private string _s2 = "0"; public string Sheet2 { get => _s2; set { _s2 = value; OnPropertyChanged(); Calculate(); } }
        private string _asp = "12mm"; public string AspType { get => _asp; set { _asp = value; OnPropertyChanged(); Calculate(); } }
        private string _profit = "15%"; public string Profit { get => _profit; set { _profit = value; OnPropertyChanged(); Calculate(); } }
        private string _result = "0.00"; public string Result { get => _result; set { _result = value; OnPropertyChanged(); } }
        private string _w = "1000"; public string Width { get => _w; set { _w = value; OnPropertyChanged(); Calculate(); } }
        private string _h = "1000"; public string Height { get => _h; set { _h = value; OnPropertyChanged(); Calculate(); } }
        private string _q = "1"; public string Qty { get => _q; set { _q = value; OnPropertyChanged(); Calculate(); } }
        private string _totSqm = "0.00"; public string TotalSqm { get => _totSqm; set { _totSqm = value; OnPropertyChanged(); } }
        private string _totPrice = "0.00"; public string TotalPrice { get => _totPrice; set { _totPrice = value; OnPropertyChanged(); } }
        private string _vat = "0.00"; public string VatAmount { get => _vat; set { _vat = value; OnPropertyChanged(); } }
        private string _gross = "0.00"; public string GrossTotal { get => _gross; set { _gross = value; OnPropertyChanged(); } }
        private bool _histVis = true; public bool IsHistoryVisible { get => _histVis; set { _histVis = value; OnPropertyChanged(); } }

        public string SpecificationSummary => $"{Thickness1} {Color1} FT Glass + {AspType} ASP + {Thickness2} {Color2} FT Glass";

        public ICommand SaveCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ClearAllCommand { get; }

        public DguViewModel()
        {
            DbHelper.Init();
            foreach (var item in DbHelper.GetAllDgu()) Records.Add(item);
            SaveCommand = new RelayCommand(o => Save());
            ExportPdfCommand = new RelayCommand(o => ExportPdf());
            DeleteCommand = new RelayCommand(o => { if (o is DguRecord r) { Records.Remove(r); DbHelper.DeleteDgu(r.Id); } });
            ClearCommand = new RelayCommand(o => { Thickness1 = "6mm"; Thickness2 = "6mm"; Color1 = "Clear"; Color2 = "Clear"; AspType = "12mm"; Profit = "15%"; Sheet1 = "0"; Sheet2 = "0"; Width = "1000"; Height = "1000"; Qty = "1"; Calculate(); });
            ClearAllCommand = new RelayCommand(o => Records.Clear());
            Calculate();
        }

        double GetAspPrice() => AspType switch
        {
            "6mm" or "8mm" or "10mm" or "12mm" => 45,
            "14mm" => 48,
            "16mm" => 50,
            "18mm" => 52,
            "20mm" => 55,
            "22mm" => 58,
            "24mm" => 60,
            _ => 45
        };

        double P(string v) => double.TryParse(v, out var x) ? x : 0;

        /// <summary>
        /// CORRECTED FORMULA:
        /// Step 1: baseValue = Sheet1 + Sheet2
        /// Step 2: step2 = baseValue / factor (where factor = 1 - profit%)
        /// Step 3: step3 = step2 + AspPrice
        /// Step 4: final = step3 + (step3 * margin)
        /// </summary>
        public void Calculate()
        {
            // Step 1: Get base value from sheets
            double baseValue = P(Sheet1) + P(Sheet2);

            // Step 2: Divide by factor (inverse of profit margin)
            double factor = Profit switch
            {
                "15%" => 0.85,
                "20%" => 0.80,
                "25%" => 0.75,
                "30%" => 0.70,
                "35%" => 0.65,
                _ => 1.0
            };

            double step2 = baseValue / factor;

            // Step 3: Add ASP price
            double step3 = step2 + GetAspPrice();

            // Step 4: Apply margin percentage
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

            // Set unit price result
            Result = final.ToString("0.00");

            // Calculate totals
            double sqm = (P(Width) / 1000 * P(Height) / 1000) * P(Qty);
            TotalSqm = sqm.ToString("0.00");
            double tot = final * sqm;
            TotalPrice = tot.ToString("0.00");
            VatAmount = (tot * 0.05).ToString("0.00");
            GrossTotal = (tot * 1.05).ToString("0.00");
        }

        public void Save()
        {
            Calculate();
            double resultValue = double.TryParse(Result, out var r) ? r : 0;
            var record = new DguRecord { Thickness1 = Thickness1, Color1 = Color1, Thickness2 = Thickness2, Color2 = Color2, Spacer = AspType, Result = resultValue, CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            Records.Insert(0, record);
            DbHelper.SaveDgu(Thickness1, Color1, Thickness2, Color2, AspType, resultValue);
            MessageBox.Show("Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ExportPdf()
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var visual = CreatePrintVisual();
                    printDialog.PrintVisual(visual, "DGU Quotation");
                    MessageBox.Show("PDF Exported via Print!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        Visual CreatePrintVisual()
        {
            var grid = new Grid();
            grid.Width = 600;
            grid.Background = Brushes.White;

            var blueColor = Color.FromRgb(37, 99, 235);
            var orangeColor = Color.FromRgb(194, 65, 12);
            var yellowBgColor = Color.FromRgb(254, 243, 199);
            var darkBlueColor = Color.FromRgb(30, 58, 95);

            // Header
            var header = new Border { Background = new SolidColorBrush(blueColor), Padding = new Thickness(15) };
            var headerStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            headerStack.Children.Add(new TextBlock { Text = "PRO GLASS AUTOMATION", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Brushes.White, TextAlignment = TextAlignment.Center });
            headerStack.Children.Add(new TextBlock { Text = $"Date: {DateTime.Now:dd MMM yyyy}", FontSize = 10, Foreground = Brushes.White, TextAlignment = TextAlignment.Center });
            header.Child = headerStack;
            grid.Children.Add(header);

            // Content
            var content = new StackPanel { Margin = new Thickness(20) };
            content.Children.Add(new TextBlock { Text = "DGU QUOTATION", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(blueColor), TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 20, 0, 20) });

            var specBox = new Border { Background = new SolidColorBrush(yellowBgColor), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 20) };
            specBox.Child = new TextBlock { Text = $"Specification: {SpecificationSummary}", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(orangeColor) };
            content.Children.Add(specBox);

            content.Children.Add(new TextBlock { Text = "DETAILS", FontSize = 14, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            content.Children.Add(CreateDetailRow("Size:", $"{Width} x {Height} mm"));
            content.Children.Add(CreateDetailRow("Quantity:", Qty));
            content.Children.Add(CreateDetailRow("Total SQM:", TotalSqm));

            content.Children.Add(new TextBlock { Text = "PRICE", FontSize = 14, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 20, 0, 10) });
            content.Children.Add(CreateDetailRow("Unit Price:", $"{Result} AED"));
            content.Children.Add(CreateDetailRow("Sub Total:", $"{TotalPrice} AED"));
            content.Children.Add(CreateDetailRow("5% VAT:", $"{VatAmount} AED"));

            var totalBox = new Border { Background = new SolidColorBrush(blueColor), Padding = new Thickness(15), Margin = new Thickness(0, 20, 0, 20) };
            totalBox.Child = new TextBlock { Text = $"GROSS TOTAL: {GrossTotal} AED", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = Brushes.White, TextAlignment = TextAlignment.Center };
            content.Children.Add(totalBox);

            var footer = new Border { Background = new SolidColorBrush(darkBlueColor), Padding = new Thickness(10) };
            footer.Child = new TextBlock { Text = "PRO GLASS AUTOMATION | Dubai, UAE | jubersaleem01@gmail.com", FontSize = 9, Foreground = Brushes.White, TextAlignment = TextAlignment.Center };
            content.Children.Add(footer);

            grid.Children.Add(content);
            return grid;
        }

        StackPanel CreateDetailRow(string label, string value)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
            row.Children.Add(new TextBlock { Text = label, Width = 150 });
            row.Children.Add(new TextBlock { Text = value, FontWeight = FontWeights.Bold });
            return row;
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public RelayCommand(Action<object> execute) => _execute = execute;
        public event EventHandler CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
        public bool CanExecute(object p) => true;
        public void Execute(object p) => _execute(p);
    }
}