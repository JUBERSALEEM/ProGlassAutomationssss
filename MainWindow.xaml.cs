using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ProGlassAutomation
{
    public partial class MainWindow : Window
    {
        public string CurrentDateText { get; set; } = "";
        public string ClockText { get; set; } = "";
        public string CompanyName { get; set; } = "PROGLASS AUTOMATION";
        public string CompanyTRN { get; set; } = "100001234500003";

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            var t = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            t.Tick += (s, e) => { ClockText = DateTime.Now.ToString("HH:mm:ss"); CurrentDateText = DateTime.Now.ToString("dd/MM/yyyy"); };
            t.Start();
        }

        void LogoButton_Click(object sender, MouseButtonEventArgs e)
        {
            var d = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp" };
            if (d.ShowDialog() == true)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(d.FileName);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    LogoEllipse.Fill = new ImageBrush(bitmap) { Stretch = Stretch.UniformToFill };
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to load image: " + ex.Message, "Error");
                }
            }
        }

        void SGU_Click(object sender, MouseButtonEventArgs e) => LoadModule("SGU");
        void DGU_Click(object sender, MouseButtonEventArgs e) => LoadModule("DGU");
        void LAM_Click(object sender, MouseButtonEventArgs e) => LoadModule("LAMINATION");
        void DguLam_Click(object sender, MouseButtonEventArgs e) => LoadModule("DGU_LAM");
        void GlassOpt_Click(object sender, MouseButtonEventArgs e) => LoadModule("OPTIMIZATION");

        void LoadModule(string name)
        {
            WelcomeScreen.Visibility = Visibility.Collapsed;
            try
            {
                MainPanel.Content = name switch
                {
                    "SGU" => new Views.SGU.SguView(),
                    "DGU" => new Views.DGU.DguView(),
                    "LAMINATION" => new Views.Lamination.LaminationView(),
                    "DGU_LAM" => new Views.DGULamination.DGULaminationView(),
                    "OPTIMIZATION" => new Views.GlassOptimization.GlassOptimizationView(),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading {name}: {ex.Message}", "Error");
            }
        }
    }
}