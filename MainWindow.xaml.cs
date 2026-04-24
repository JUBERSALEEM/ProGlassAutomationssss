using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ProGlassAutomation.Views.SGU;
using ProGlassAutomation.Views.DGU;
using ProGlassAutomation.Views.Lamination;
using ProGlassAutomation.Views.DGULamination;

namespace ProGlassAutomation
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

        public string CurrentDateText { get; set; }
        public string ClockText { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            CurrentDateText = DateTime.Now.ToString("dd MMM yyyy");
            ClockText = DateTime.Now.ToString("HH:mm:ss");
            _timer.Tick += (s, e) => { CurrentDateText = DateTime.Now.ToString("dd MMM yyyy"); ClockText = DateTime.Now.ToString("HH:mm:ss"); OnPropertyChanged(nameof(CurrentDateText)); OnPropertyChanged(nameof(ClockText)); };
            _timer.Start();
            LoadWelcome();
        }

        private void LoadWelcome() => MainPanel.Content = new Grid
        {
            Children =
            {
                new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Children =
                {
                    new TextBlock { Text = "🏭 WELCOME TO GLASS ERP SYSTEM", FontSize = 26, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center },
                    new TextBlock { Text = "Select module from sidebar to continue", FontSize = 14, Margin = new Thickness(0,10,0,25), HorizontalAlignment = HorizontalAlignment.Center },
                    new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Children =
                    {
                        CreateStatusCard("📊 SGU MODULE", "LIVE READY", System.Windows.Media.Brushes.LimeGreen),
                        CreateStatusCard("🧮 DGU MODULE", "LIVE READY", System.Windows.Media.Brushes.LimeGreen),
                        CreateStatusCard("🧪 LAMINATION", "LIVE READY", System.Windows.Media.Brushes.LimeGreen),
                        CreateStatusCard("🧩 DGU+LAMINATION", "LIVE READY", System.Windows.Media.Brushes.LimeGreen)
                    }},
                    new TextBlock { Text = "🟢 ALL SYSTEMS ONLINE", Margin = new Thickness(0,25,0,0), Foreground = System.Windows.Media.Brushes.Green, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center }
                }}
            }
        };

        private UIElement CreateStatusCard(string title, string status, System.Windows.Media.Brush color) => new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)),
            CornerRadius = new CornerRadius(12),
            BorderBrush = System.Windows.Media.Brushes.Gray,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(15),
            Margin = new Thickness(10),
            Width = 180,
            Height = 90,
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = title, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center },
                    new TextBlock { Text = status, Foreground = color, FontWeight = FontWeights.Bold, FontSize = 14, TextAlignment = TextAlignment.Center }
                }
            }
        };

        private void LoadModule(UserControl view) { if (view != null) MainPanel.Content = view; else MessageBox.Show("Module not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning); }

        private void SGU_Click(object sender, MouseButtonEventArgs e) => LoadModule(new SguView());
        private void DGU_Click(object sender, MouseButtonEventArgs e) => LoadModule(new DguView());
        private void LAM_Click(object sender, MouseButtonEventArgs e) => LoadModule(new LaminationView());
        private void DguLam_Click(object sender, MouseButtonEventArgs e) => LoadModule(new DGULaminationView());

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}