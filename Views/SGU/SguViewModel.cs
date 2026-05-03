using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
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

        // Static arrays for dropdowns
        private static readonly string[] AllCategories = new string[]
        {
            "HD Clear", "HD Bronze", "HD Grey", "Belgium Clear", "PNA Clear",
            "Ramly Clear", "Sunlux Silver", "Reflite Silver", "Stopsol Classic",
            "Guardian Clear", "AGC Clear", "Şişecam Clear", "SGG Clear",
            "Pilkington Clear", "Tinted Bronze", "Low-E Clear", "Lacobel White"
        };

        private static readonly string[] AllThicknesses = new string[]
        {
            "2mm", "2.5mm", "3mm", "4mm", "5mm", "6mm", "8mm", "10mm", "12mm", "15mm", "19mm"
        };

        private static readonly string[] AllColors = new string[]
        {
            "Clear", "Bronze", "Dark Bronze", "Grey", "Dark Grey", "Green",
            "Blue", "Reflective Silver", "Reflective Gold", "Reflective Blue",
            "Mirror", "Mirror Silver", "White", "Black"
        };

        // Collections
        public ObservableCollection<string> CategoryOptions { get; } = new();
        public ObservableCollection<string> ThicknessOptions { get; } = new();
        public ObservableCollection<string> ColorOptions { get; } = new();
        public ObservableCollection<string> ProfitOptions { get; } = new() { "5%", "10%", "15%", "20%", "25%", "30%", "35%", "40%" };
        public ObservableCollection<RecordModel> Records { get; } = new();

        // Properties
        private string _cat;
        public string Category
        {
            get => _cat;
            set { _cat = value; OnPropertyChanged(); LoadColorsByCategory(); LoadPriceFromSheetStore(); }
        }

        private string _th = "6mm";
        public string Thickness
        {
            get => _th;
            set { _th = value; OnPropertyChanged(); LoadPriceFromSheetStore(); Calculate(); }
        }

        private string _c = "Clear";
        public string ColorName
        {
            get => _c;
            set { _c = value; OnPropertyChanged(); LoadPriceFromSheetStore(); Calculate(); }
        }

        private string _p = "15%";
        public string Profit
        {
            get => _p;
            set { _p = value; OnPropertyChanged(); OnPropertyChanged("PF"); Calculate(); }
        }

        private double _sh = 0;
        public double SheetPrice
        {
            get => _sh;
            set { _sh = value; OnPropertyChanged(); Calculate(); }
        }

        private double _cut = 5;
        public double Cutting
        {
            get => _cut;
            set { _cut = value; OnPropertyChanged(); Calculate(); }
        }

        private double _temp = 10;
        public double Tempering
        {
            get => _temp;
            set { _temp = value; OnPropertyChanged(); Calculate(); }
        }

        private double _result;
        public double Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(); }
        }

        private bool _histVis = true;
        public bool IsHistoryVisible
        {
            get => _histVis;
            set { _histVis = value; OnPropertyChanged(); }
        }

        public double PF => Profit switch
        {
            "5%" => 1.05,
            "10%" => 1.10,
            "15%" => 1.15,
            "20%" => 1.20,
            "25%" => 1.25,
            "30%" => 1.30,
            "35%" => 1.35,
            "40%" => 1.40,
            _ => 1.15
        };

        public string Spec => $"{Thickness} {ColorName}";

        public ICommand SaveCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand DeleteCommand { get; }

        public SguViewModel()
        {
            LoadOptions();
            LoadPriceFromSheetStore();

            SaveCommand = new RelayCommand(o =>
            {
                Records.Insert(0, new RecordModel
                {
                    DisplayText = $"{Category} | {Thickness} {ColorName} | {Result:F2} AED | {DateTime.Now:HH:mm}"
                });
                MessageBox.Show("Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            });

            ExportPdfCommand = new RelayCommand(o => ExportPdf());

            ClearCommand = new RelayCommand(o =>
            {
                SheetPrice = 0;
                Cutting = 5;
                Tempering = 10;
                Profit = "15%";
                Thickness = "6mm";
                ColorName = "Clear";
                if (CategoryOptions.Count > 0)
                    Category = CategoryOptions.First();
            });

            ClearAllCommand = new RelayCommand(o => Records.Clear());

            DeleteCommand = new RelayCommand(o =>
            {
                if (o is RecordModel r)
                    Records.Remove(r);
            });

            Calculate();
        }

        private void LoadOptions()
        {
            foreach (var cat in AllCategories)
                CategoryOptions.Add(cat);

            foreach (var t in AllThicknesses)
                ThicknessOptions.Add(t);

            foreach (var c in AllColors)
                ColorOptions.Add(c);

            if (CategoryOptions.Count > 0)
                Category = CategoryOptions.First();
        }

        private void LoadColorsByCategory()
        {
            ColorOptions.Clear();

            if (string.IsNullOrEmpty(Category))
            {
                foreach (var c in AllColors)
                    ColorOptions.Add(c);
                return;
            }

            switch (Category)
            {
                case "HD Clear":
                case "Belgium Clear":
                case "PNA Clear":
                case "Ramly Clear":
                case "Guardian Clear":
                case "AGC Clear":
                case "Şişecam Clear":
                case "SGG Clear":
                case "Pilkington Clear":
                case "Low-E Clear":
                    ColorOptions.Add("Clear");
                    break;

                case "HD Bronze":
                case "Tinted Bronze":
                    ColorOptions.Add("Bronze");
                    ColorOptions.Add("Dark Bronze");
                    break;

                case "HD Grey":
                    ColorOptions.Add("Grey");
                    ColorOptions.Add("Dark Grey");
                    break;

                case "Sunlux Silver":
                case "Reflite Silver":
                    ColorOptions.Add("Reflective Silver");
                    ColorOptions.Add("Mirror Silver");
                    break;

                case "Stopsol Classic":
                    ColorOptions.Add("Reflective Silver");
                    ColorOptions.Add("Reflective Gold");
                    ColorOptions.Add("Reflective Blue");
                    break;

                case "Lacobel White":
                    ColorOptions.Add("White");
                    break;

                default:
                    foreach (var c in AllColors)
                        ColorOptions.Add(c);
                    break;
            }

            ColorName = ColorOptions.FirstOrDefault();
        }

        private void LoadPriceFromSheetStore()
        {
            try
            {
                if (string.IsNullOrEmpty(Category) || string.IsNullOrEmpty(Thickness) || string.IsNullOrEmpty(ColorName))
                {
                    SheetPrice = 0;
                    return;
                }

                var sheets = Services.SheetStoreService.Instance.GetAllActive().ToList();

                var match = sheets.FirstOrDefault(s =>
                    s.Category == Category &&
                    s.Thickness == Thickness &&
                    s.Color == ColorName);

                if (match != null)
                {
                    SheetPrice = (double)match.PurchasePrice;
                    Calculate();
                    return;
                }

                var byThickness = sheets.FirstOrDefault(s =>
                    s.Category == Category &&
                    s.Thickness == Thickness);

                if (byThickness != null)
                {
                    SheetPrice = (double)byThickness.PurchasePrice;
                    Calculate();
                    return;
                }

                var byCategory = sheets.FirstOrDefault(s => s.Category == Category);
                if (byCategory != null)
                {
                    SheetPrice = (double)byCategory.PurchasePrice;
                    Calculate();
                    return;
                }

                SheetPrice = 0;
            }
            catch
            {
                SheetPrice = 0;
            }
        }

        public void Calculate()
        {
            Result = ((SheetPrice / 0.85) + Cutting + Tempering) * PF;
        }

        public void ExportPdf()
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    var visual = CreatePrintVisual();
                    printDialog.PrintVisual(visual, "SGU Quotation");
                    MessageBox.Show("PDF Exported!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        Visual CreatePrintVisual()
        {
            var grid = new Grid { Width = 600, Background = Brushes.White };

            var blueColor = Color.FromRgb(37, 99, 235);
            var orangeColor = Color.FromRgb(194, 65, 12);
            var yellowBgColor = Color.FromRgb(254, 243, 199);
            var greenColor = Color.FromRgb(16, 185, 129);
            var lightGreenColor = Color.FromRgb(187, 247, 208);
            var darkBlueColor = Color.FromRgb(30, 58, 95);

            var header = new Border { Background = new SolidColorBrush(blueColor), Padding = new Thickness(15) };
            var headerStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            headerStack.Children.Add(new TextBlock { Text = "PRO GLASS AUTOMATION", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Brushes.White, TextAlignment = TextAlignment.Center });
            headerStack.Children.Add(new TextBlock { Text = $"Date: {DateTime.Now:dd MMM yyyy}", FontSize = 10, Foreground = Brushes.White, TextAlignment = TextAlignment.Center });
            header.Child = headerStack;
            grid.Children.Add(header);

            var content = new StackPanel { Margin = new Thickness(20) };
            content.Children.Add(new TextBlock { Text = "SGU QUOTATION", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(blueColor), TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 20, 0, 20) });

            var specBox = new Border { Background = new SolidColorBrush(yellowBgColor), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 20) };
            specBox.Child = new TextBlock { Text = $"Specification: {Category} | {Spec}", FontSize = 12, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush(orangeColor) };
            content.Children.Add(specBox);

            content.Children.Add(new TextBlock { Text = "PRICE BREAKUP", FontSize = 14, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 10) });
            content.Children.Add(CreateDetailRow("Category:", Category));
            content.Children.Add(CreateDetailRow("Sheet Price:", $"{SheetPrice:F2} AED"));
            content.Children.Add(CreateDetailRow("Cutting Charge:", $"{Cutting:F2} AED"));
            content.Children.Add(CreateDetailRow("Tempering Charge:", $"{Tempering:F2} AED"));
            content.Children.Add(CreateDetailRow("Profit Margin:", Profit));

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