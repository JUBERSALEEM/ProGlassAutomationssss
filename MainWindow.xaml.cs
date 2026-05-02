using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using ProGlassAutomation.Views.Dashboard;
using ProGlassAutomation.Views.SGU;
using ProGlassAutomation.Views.DGU;
using ProGlassAutomation.Views.Lamination;
using ProGlassAutomation.Views.DGULamination;
using ProGlassAutomation.Views.GlassOptimization;
using ProGlassAutomation.Views.Subscription;
using ProGlassAutomation.Views.SheetStore;

namespace ProGlassAutomation
{
    public partial class MainWindow : Window
    {
        private bool _isActivated = false;
        private string _currentMachineId = "";
        private string _currentKey = "";
        private DispatcherTimer _clockTimer;

        private DashboardView _dashboardPage;
        private SubscriptionPlanView _subscriptionPage;
        private SguView _sguCalculatorPage;
        private DguView _dguCalculatorPage;
        private LaminationView _laminationCalculatorPage;
        private DGULaminationView _dguLaminationCalculatorPage;
        private GlassOptimizationView _glassOptimizationPage;
        private SheetStoreView _sheetStorePage;

        public MainWindow()
        {
            InitializeComponent();

            InitializePages();
            StartClock();

            _currentMachineId = Services.MachineIdService.Instance.GetMachineId();

            LockAllModules();
            CheckExistingActivation();

            ShowDashboard();
        }

        private void InitializePages()
        {
            _dashboardPage = new DashboardView();
            _subscriptionPage = new SubscriptionPlanView();
            _sguCalculatorPage = new SguView();
            _dguCalculatorPage = new DguView();
            _laminationCalculatorPage = new LaminationView();
            _dguLaminationCalculatorPage = new DGULaminationView();
            _glassOptimizationPage = new GlassOptimizationView();
            _sheetStorePage = new SheetStoreView();
        }

        // ==================== LIVE CLOCK ====================

        private void StartClock()
        {
            _clockTimer = new DispatcherTimer();
            _clockTimer.Interval = TimeSpan.FromSeconds(1);
            _clockTimer.Tick += ClockTimer_Tick;
            _clockTimer.Start();
            UpdateClockDisplay();
        }

        private void ClockTimer_Tick(object sender, EventArgs e)
        {
            UpdateClockDisplay();
        }

        private void UpdateClockDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
                CurrentDateText.Text = DateTime.Now.ToString("dd-MMM-yyyy");
            });
        }

        // ==================== LICENSE CHECK ====================

        private void CheckExistingActivation()
        {
            try
            {
                _isActivated = false;

                string storedKey = GetStoredKey();
                if (string.IsNullOrEmpty(storedKey)) return;

                string errorMsg = "";
                bool isValid = Services.KeyGeneratorService.Instance.ValidateKeyWithActivation(storedKey, _currentMachineId, out errorMsg);

                if (isValid)
                {
                    _currentKey = storedKey;
                    _isActivated = true;
                    Dispatcher.Invoke(() => UnlockAllModules());
                }
                else
                {
                    DeleteKey();
                }
            }
            catch
            {
                DeleteKey();
            }
        }

        private string GetStoredKey()
        {
            try
            {
                string keyFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.key");
                if (System.IO.File.Exists(keyFile))
                {
                    return System.IO.File.ReadAllText(keyFile).Trim();
                }
            }
            catch { }
            return "";
        }

        private void SaveKey(string key)
        {
            try
            {
                string keyFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.key");
                System.IO.File.WriteAllText(keyFile, key);
            }
            catch { }
        }

        private void DeleteKey()
        {
            try
            {
                string keyFile = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.key");
                if (System.IO.File.Exists(keyFile))
                {
                    System.IO.File.Delete(keyFile);
                }
            }
            catch { }
        }

        // ==================== LOCK/UNLOCK MODULES ====================

        private void LockAllModules()
        {
            _isActivated = false;

            CalculatorsSection.Opacity = 0.5;
            LockedIcon.Visibility = Visibility.Visible;

            SGUOverlay.Visibility = Visibility.Visible;
            DGUOverlay.Visibility = Visibility.Visible;
            LAMOverlay.Visibility = Visibility.Visible;
            DGULamOverlay.Visibility = Visibility.Visible;
            GlassOptOverlay.Visibility = Visibility.Visible;

            CalculatorsStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            CalculatorsStatusText.Text = "CALCULATORS LOCKED";
            CalculatorsStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D97706"));

            ActivationStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            ActivationStatusText.Text = "ACTIVATION REQUIRED";
            ActivationStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));

            SystemActiveDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
            SystemStatusText.Text = "LICENSE INACTIVE";
            SystemStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DC2626"));
        }

        private void UnlockAllModules()
        {
            _isActivated = true;

            CalculatorsSection.Opacity = 1.0;
            LockedIcon.Visibility = Visibility.Collapsed;

            SGUOverlay.Visibility = Visibility.Collapsed;
            DGUOverlay.Visibility = Visibility.Collapsed;
            LAMOverlay.Visibility = Visibility.Collapsed;
            DGULamOverlay.Visibility = Visibility.Collapsed;
            GlassOptOverlay.Visibility = Visibility.Collapsed;

            CalculatorsStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            CalculatorsStatusText.Text = "CALCULATORS UNLOCKED";
            CalculatorsStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));

            ActivationStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            ActivationStatusText.Text = "SYSTEM ACTIVE";
            ActivationStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));

            SystemActiveDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
            SystemStatusText.Text = "LICENSE ACTIVE";
            SystemStatusText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#059669"));
        }

        // ==================== PUBLIC METHODS ====================

        public void OnLicenseActivated(string key)
        {
            _currentKey = key;
            _isActivated = true;
            SaveKey(key);
            UnlockAllModules();
            InitializePages();
            ShowDashboard();
        }

        public void OnLicenseDeactivated()
        {
            _currentKey = "";
            _isActivated = false;
            DeleteKey();
            LockAllModules();
            InitializePages();
            ShowDashboard();
        }

        // ==================== NAVIGATION METHODS ====================

        public void ShowDashboard()
        {
            _dashboardPage = new DashboardView();
            MainPanel.Content = _dashboardPage;
        }

        public void ShowSubscriptionPlan()
        {
            _subscriptionPage = new SubscriptionPlanView();
            MainPanel.Content = _subscriptionPage;
        }

        public void ShowSheetStore()
        {
            _sheetStorePage = new SheetStoreView();
            MainPanel.Content = _sheetStorePage;
        }

        private void ShowSGUCalculator()
        {
            _sguCalculatorPage = new SguView();
            MainPanel.Content = _sguCalculatorPage;
        }

        private void ShowDGUCalculator()
        {
            _dguCalculatorPage = new DguView();
            MainPanel.Content = _dguCalculatorPage;
        }

        private void ShowLaminationCalculator()
        {
            _laminationCalculatorPage = new LaminationView();
            MainPanel.Content = _laminationCalculatorPage;
        }

        private void ShowDGULaminationCalculator()
        {
            _dguLaminationCalculatorPage = new DGULaminationView();
            MainPanel.Content = _dguLaminationCalculatorPage;
        }

        private void ShowGlassOptimization()
        {
            _glassOptimizationPage = new GlassOptimizationView();
            MainPanel.Content = _glassOptimizationPage;
        }

        // ==================== CLICK HANDLERS ====================

        private void SGU_Click(object sender, MouseButtonEventArgs e)
        {
            if (!_isActivated) { MessageBox.Show("Please activate your license to access calculators", "License Required", MessageBoxButton.OK, MessageBoxImage.Warning); ShowSubscriptionPlan(); return; }
            ShowSGUCalculator();
        }

        private void DGU_Click(object sender, MouseButtonEventArgs e)
        {
            if (!_isActivated) { MessageBox.Show("Please activate your license to access calculators", "License Required", MessageBoxButton.OK, MessageBoxImage.Warning); ShowSubscriptionPlan(); return; }
            ShowDGUCalculator();
        }

        private void LAM_Click(object sender, MouseButtonEventArgs e)
        {
            if (!_isActivated) { MessageBox.Show("Please activate your license to access calculators", "License Required", MessageBoxButton.OK, MessageBoxImage.Warning); ShowSubscriptionPlan(); return; }
            ShowLaminationCalculator();
        }

        private void DguLam_Click(object sender, MouseButtonEventArgs e)
        {
            if (!_isActivated) { MessageBox.Show("Please activate your license to access calculators", "License Required", MessageBoxButton.OK, MessageBoxImage.Warning); ShowSubscriptionPlan(); return; }
            ShowDGULaminationCalculator();
        }

        private void GlassOpt_Click(object sender, MouseButtonEventArgs e)
        {
            if (!_isActivated) { MessageBox.Show("Please activate your license to access calculators", "License Required", MessageBoxButton.OK, MessageBoxImage.Warning); ShowSubscriptionPlan(); return; }
            ShowGlassOptimization();
        }

        private void SheetStore_Click(object sender, MouseButtonEventArgs e)
        {
            ShowSheetStore();
        }

        private void AluminumStore_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Aluminum Store - Coming Soon!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SpacerStore_Click(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Spacer Store - Coming Soon!", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Dashboard_Click(object sender, MouseButtonEventArgs e)
        {
            ShowDashboard();
        }

        private void SubscriptionPlan_Click(object sender, MouseButtonEventArgs e)
        {
            ShowSubscriptionPlan();
        }

        private void LogoButton_Click(object sender, MouseButtonEventArgs e)
        {
            ShowDashboard();
        }
    }
}