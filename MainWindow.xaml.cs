using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private DispatcherTimer _clockTimer;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            StartClock();
        }

        private void StartClock()
        {
            _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clockTimer.Tick += (s, e) => {
                ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
                CurrentDateText.Text = DateTime.Now.ToString("dd-MMM-yyyy");
            };
            _clockTimer.Start();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var aboutDialog = new Views.About.AboutDialog { Owner = this };
                aboutDialog.ShowDialog();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void AnimateButton(Button btn, Border border)
        {
            if (border == null) return;
            border.RenderTransformOrigin = new Point(0.5, 0.5);
            if (!(border.RenderTransform is ScaleTransform)) border.RenderTransform = new ScaleTransform(1, 1);
            var scale = (ScaleTransform)border.RenderTransform;

            DoubleAnimation grow = new DoubleAnimation(1.04, TimeSpan.FromMilliseconds(100));
            DoubleAnimation shrink = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(100)) { BeginTime = TimeSpan.FromMilliseconds(100) };

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
        }

        private bool CheckLicense()
        {
            if (!_viewModel.IsLicensed)
            {
                MessageBox.Show("Security Warning: Enterprise License Required.", "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                _viewModel.ShowSubscriptionPlan();
                return false;
            }
            return true;
        }

        // NAVIGATION HANDLERS
        private void BtnDashboard_Click(object sender, RoutedEventArgs e) { _viewModel.ShowDashboard(); AnimateButton(BtnDashboard, BorderDash); }
        private void BtnSubscription_Click(object sender, RoutedEventArgs e) { _viewModel.ShowSubscriptionPlan(); AnimateButton(BtnSubscription, BorderSub); }
        private void BtnSGU_Click(object sender, RoutedEventArgs e) { if (CheckLicense()) { _viewModel.ShowSGUCalculator(); AnimateButton(BtnSGU, BorderSGU); } }
        private void BtnDGU_Click(object sender, RoutedEventArgs e) { if (CheckLicense()) { _viewModel.ShowDGUCalculator(); AnimateButton(BtnDGU, BorderDGU); } }
        private void BtnLamination_Click(object sender, RoutedEventArgs e) { if (CheckLicense()) { _viewModel.ShowLaminationCalculator(); AnimateButton(BtnLam, BorderLam); } }
        private void BtnDguLam_Click(object sender, RoutedEventArgs e) { if (CheckLicense()) { _viewModel.ShowDGULaminationCalculator(); AnimateButton(BtnDguLam, BorderDguLam); } }
        private void BtnOptimization_Click(object sender, RoutedEventArgs e) { if (CheckLicense()) { _viewModel.ShowGlassOptimization(); AnimateButton(BtnOpt, BorderOpt); } }

        // OPERATIONS HANDLERS
        private void BtnSheetStore_Click(object sender, RoutedEventArgs e) => _viewModel.ShowSheetStore();
        private void BtnDailyWorks_Click(object sender, RoutedEventArgs e) => _viewModel.ShowDailyWorks();
        private void BtnDeliveries_Click(object sender, RoutedEventArgs e) => _viewModel.ShowDeliveries();
        private void BtnProfile_Click(object sender, RoutedEventArgs e) => _viewModel.ShowProfile();
        private void BtnUsers_Click(object sender, RoutedEventArgs e) => _viewModel.ShowUsers();

        private void ScreenshotBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Safely hide buttons using the restored x:Names
                if (ScreenshotBtn != null) ScreenshotBtn.Visibility = Visibility.Collapsed;
                if (AboutBtn != null) AboutBtn.Visibility = Visibility.Collapsed;

                RenderTargetBitmap rtb = new RenderTargetBitmap((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(this);

                if (ScreenshotBtn != null) ScreenshotBtn.Visibility = Visibility.Visible;
                if (AboutBtn != null) AboutBtn.Visibility = Visibility.Visible;

                var sfd = new Microsoft.Win32.SaveFileDialog { Filter = "JPG|*.jpg", FileName = $"ProGlass_Capture_{DateTime.Now:HHmm}.jpg" };
                if (sfd.ShowDialog() == true)
                {
                    var encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(rtb));
                    using (var fs = System.IO.File.OpenWrite(sfd.FileName)) encoder.Save(fs);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}