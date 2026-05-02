using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ProGlassAutomation.Views.SGU
{
    public class SguViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public ObservableCollection<string> ThicknessOptions { get; } = new() { "4mm", "5mm", "6mm", "8mm", "10mm", "12mm" };
        public ObservableCollection<string> ColorOptions { get; } = new() { "Clear", "Grey", "Green", "Blue", "Bronze" };
        public ObservableCollection<string> ProfitOptions { get; } = new() { "5%", "10%", "15%", "20%", "25%", "30%", "35%", "40%" };
        public ObservableCollection<RecordModel> Records { get; } = new();

        private string _t = "6mm"; public string Thickness { get => _t; set { _t = value; OnPropertyChanged(); Calculate(); } }
        private string _c = "Clear"; public string ColorName { get => _c; set { _c = value; OnPropertyChanged(); Calculate(); } }
        private string _p = "15%"; public string Profit { get => _p; set { _p = value; OnPropertyChanged(); OnPropertyChanged("PF"); Calculate(); } }
        private double _s = 46; public double Sheet { get => _s; set { _s = value; OnPropertyChanged(); Calculate(); } }
        private double _cut = 5; public double Cutting { get => _cut; set { _cut = value; OnPropertyChanged(); Calculate(); } }
        private double _temp = 10; public double Tempering { get => _temp; set { _temp = value; OnPropertyChanged(); Calculate(); } }
        private double _result; public double Result { get => _result; set { _result = value; OnPropertyChanged(); } }
        private bool _histVis = true; public bool IsHistoryVisible { get => _histVis; set { _histVis = value; OnPropertyChanged(); } }

        public double BasePrice => Sheet / 0.85;
        public double PF => Profit switch { "5%" => 1.05, "10%" => 1.10, "15%" => 1.15, "20%" => 1.20, "25%" => 1.25, "30%" => 1.30, "35%" => 1.35, "40%" => 1.40, _ => 1.15 };
        public string Spec => $"{Thickness} {ColorName}";

        public ICommand SaveCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand DeleteCommand { get; }

        public SguViewModel()
        {
            SaveCommand = new RelayCommand(o => { Records.Insert(0, new RecordModel { DisplayText = $"{Thickness} {ColorName} | {Result:F2} AED | {DateTime.Now:HH:mm}" }); MessageBox.Show("Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information); });
            ExportPdfCommand = new RelayCommand(o => ExportPdf());
            ClearCommand = new RelayCommand(o => { Sheet = 46; Cutting = 5; Tempering = 10; Profit = "15%"; Thickness = "6mm"; ColorName = "Clear"; });
            ClearAllCommand = new RelayCommand(o => Records.Clear());
            DeleteCommand = new RelayCommand(o => { if (o is RecordModel r) Records.Remove(r); });
            Calculate();
        }

        public void Calculate() { Result = ((Sheet / 0.85) + Cutting + Tempering) * PF; OnPropertyChanged("BasePrice"); }

        public void ExportPdf()
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var visual = CreatePrintVisual();
                    printDialog.PrintVisual(visual, "SGU Quotation");
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
            var greenColor = Color.FromRgb(16, 185, 129);
            var lightGreenColor = Color.FromRgb(187, 247, 208);
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
            content.Children.Add(new TextBlock { Text = "SGU QUOTATION", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(blueColor), TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 20, 0, 20) });

            var specBox = new Border { Background = new SolidColorBrush(yellowBgColor), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 20) };
            specBox.Child = new TextBlock { Text = $"Specification: {Spec}", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(orangeColor) };
            content.Children.Add(specBox);

            content.Children.Add(new TextBlock { Text = "PRICE BREAKUP", FontSize = 14, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            content.Children.Add(CreateDetailRow("Sheet Price:", $"{Sheet:F2} AED"));
            content.Children.Add(CreateDetailRow("Cutting Charge:", $"{Cutting:F2} AED"));
            content.Children.Add(CreateDetailRow("Tempering Charge:", $"{Tempering:F2} AED"));
            content.Children.Add(CreateDetailRow("Profit Margin:", Profit));
            content.Children.Add(CreateDetailRow("Base Price:", $"{BasePrice:F2} AED"));

            var finalBox = new Border { Background = new SolidColorBrush(greenColor), Padding = new Thickness(15), Margin = new Thickness(0, 20, 0, 20) };
            var finalStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            finalStack.Children.Add(new TextBlock { Text = "FINAL UNIT PRICE", FontSize = 10, Foreground = new SolidColorBrush(lightGreenColor), TextAlignment = TextAlignment.Center });
            finalStack.Children.Add(new TextBlock { Text = $"{Result:F2} AED", FontSize = 24, FontWeight = FontWeights.Bold, Foreground = Brushes.White, TextAlignment = TextAlignment.Center });
            finalBox.Child = finalStack;
            content.Children.Add(finalBox);

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

    public class RecordModel { public string DisplayText { get; set; } }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        public RelayCommand(Action<object> execute) => _execute = execute;
        public event EventHandler CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
        public bool CanExecute(object p) => true;
        public void Execute(object p) => _execute(p);
    }
}