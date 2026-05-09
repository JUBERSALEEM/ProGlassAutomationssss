using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProGlassAutomation.Views;

namespace ProGlassAutomation.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(params string[] props)
        {
            foreach (var p in props)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }

        protected bool Set<T>(ref T field, T value, params string[] props)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            Notify(props);
            return true;
        }

        // ═══════════════════════════════════════════════════════
        // VIEW CACHE - Instantiate once, reuse forever
        // ═══════════════════════════════════════════════════════
        private readonly Dictionary<string, UserControl> _viewCache = new();

        private UserControl GetOrCreateView(string viewName)
        {
            if (_viewCache.TryGetValue(viewName, out var cached))
                return cached;

            UserControl view = viewName switch
            {
                "SheetStore" => new Views.SheetStore.SheetStoreView(),
                "DGU" => new Views.DGU.DguView(),
                "SGU" => new Views.SGU.SguView(),
                "Lamination" => new Views.Lamination.LaminationView(),
                "DguLam" => new Views.DGULamination.DGULaminationView(),
                "Optimization" => new Views.GlassOptimization.GlassOptimizationView(),
                "Dashboard" => new Views.Dashboard.DashboardView(),
                "Subscription" => new Views.Subscription.SubscriptionPlanView(),
                "AluminumStore" => CreatePlaceholder("Aluminum Store - Coming Soon!"),
                "SpacerStore" => CreatePlaceholder("Spacer Store - Coming Soon!"),
                "DailyWorks" => new Views.DailyWorksView(),
                "Profile" => new Views.Profile.ProfileView(),
                "Users" => CreatePlaceholder("Users - Coming Soon!"),
                _ => null
            };

            if (view != null)
                _viewCache[viewName] = view;

            return view;
        }

        private UserControl CreatePlaceholder(string message)
        {
            var grid = new Grid();
            var textBlock = new TextBlock
            {
                Text = message,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6B7280")),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            grid.Children.Add(textBlock);
            var placeholder = new UserControl { Content = grid };
            return placeholder;
        }

        // ═══════════════════════════════════════════════════════
        // NAVIGATION ENGINE - Central hub
        // ═══════════════════════════════════════════════════════
        private UserControl _currentView;
        public UserControl CurrentView
        {
            get => _currentView;
            private set => Set(ref _currentView, value, nameof(CurrentView));
        }

        private string _currentViewName = "";
        public string CurrentViewName
        {
            get => _currentViewName;
            private set => Set(ref _currentViewName, value, nameof(CurrentViewName));
        }

        public ICommand NavigateCommand { get; }

        private void Navigate(string viewName)
        {
            if (string.IsNullOrEmpty(viewName)) return;

            // Check license for calculators
            if (IsCalculatorView(viewName) && !IsLicensed)
            {
                MessageBox.Show("Please activate your license to access calculators",
                    "License Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                Navigate("Subscription");
                return;
            }

            // Clear cache for fresh instance (matching original behavior)
            _viewCache.Clear();

            // Get cached view and inject
            CurrentView = GetOrCreateView(viewName);
            CurrentViewName = viewName;
        }

        private bool IsCalculatorView(string name) =>
            name is "SGU" or "DGU" or "Lamination" or "DguLam" or "Optimization";

        // ═══════════════════════════════════════════════════════
        // LICENSE STATE - Matching original MainWindow pattern
        // ═══════════════════════════════════════════════════════
        private bool _isLicensed = false;
        private string _currentKey = "";

        public bool IsLicensed
        {
            get => _isLicensed;
            set => Set(ref _isLicensed, value, nameof(IsLicensed));
        }

        public bool CalculatorsEnabled => _isLicensed;

        // ═══════════════════════════════════════════════════════
        // UI STATE BINDINGS - For XAML overlays and status
        // ═══════════════════════════════════════════════════════
        public bool IsLocked => !_isLicensed;

        // ═══════════════════════════════════════════════════════
        // INIT
        // ═══════════════════════════════════════════════════════
        public MainViewModel()
        {
            NavigateCommand = new RelayCommand(o => Navigate(o?.ToString() ?? ""));

            // Check existing activation
            CheckExistingActivation();

            // Default view
            Navigate("Dashboard");
        }

        private void CheckExistingActivation()
        {
            try
            {
                _isLicensed = false;
                string storedKey = GetStoredKey();
                if (string.IsNullOrEmpty(storedKey)) return;

                string machineId = Services.MachineIdService.Instance.GetMachineId();
                string errorMsg = "";
                bool isValid = Services.KeyGeneratorService.Instance.ValidateKeyWithActivation(
                    storedKey, machineId, out errorMsg);

                if (isValid)
                {
                    _currentKey = storedKey;
                    _isLicensed = true;
                    Notify(nameof(IsLocked));
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
                    return System.IO.File.ReadAllText(keyFile).Trim();
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
                    System.IO.File.Delete(keyFile);
            }
            catch { }
        }

        // ═══════════════════════════════════════════════════════
        // LICENSE CALLBACKS - Exact signature matching original
        // ═══════════════════════════════════════════════════════

        public void OnLicenseActivated(string key)
        {
            _currentKey = key;
            _isLicensed = true;
            SaveKey(key);

            Notify(nameof(IsLicensed));
            Notify(nameof(CalculatorsEnabled));
            Notify(nameof(IsLocked));

            _viewCache.Clear();
            InitializePages();
            ShowDashboard();
        }

        public void OnLicenseDeactivated()
        {
            _currentKey = "";
            _isLicensed = false;
            DeleteKey();

            Notify(nameof(IsLicensed));
            Notify(nameof(CalculatorsEnabled));
            Notify(nameof(IsLocked));

            _viewCache.Clear();
            InitializePages();
            ShowDashboard();
        }

        private void InitializePages()
        {
            _viewCache.Clear();
        }

        // ═══════════════════════════════════════════════════════
        // NAVIGATION HELPERS
        // ═══════════════════════════════════════════════════════

        public void ShowDashboard() => Navigate("Dashboard");
        public void ShowSubscriptionPlan() => Navigate("Subscription");
        public void ShowSheetStore() => Navigate("SheetStore");
        public void ShowDailyWorks() => Navigate("DailyWorks");
        public void ShowProfile() => Navigate("Profile");
        public void ShowUsers() => Navigate("Users");

        private void ShowSGUCalculator() => Navigate("SGU");
        private void ShowDGUCalculator() => Navigate("DGU");
        private void ShowLaminationCalculator() => Navigate("Lamination");
        private void ShowDGULaminationCalculator() => Navigate("DguLam");
        private void ShowGlassOptimization() => Navigate("Optimization");

        // ═══════════════════════════════════════════════════════
        // CLICK COMMANDS
        // ═══════════════════════════════════════════════════════

        public ICommand SGUCommand => new RelayCommand(o =>
        {
            if (!_isLicensed) { MessageBox.Show("Please activate your license to access calculators", "License Required", MessageBoxButton.OK, MessageBoxImage.Warning); ShowSubscriptionPlan(); return; }
            ShowSGUCalculator();
        });

        public ICommand DGUCommand => new RelayCommand(o =>
        {
            if (!_isLicensed) { MessageBox.Show("Please activate your license to access calculators", "License Required", MessageBoxButton.OK, MessageBoxImage.Warning); ShowSubscriptionPlan(); return; }
            ShowDGUCalculator();
        });

        public ICommand LaminationCommand => new RelayCommand(o =>
        {
            if (!_isLicensed) { MessageBox.Show("Please activate your license to access calculators", "License Required", MessageBoxButton.OK, MessageBoxImage.Warning); ShowSubscriptionPlan(); return; }
            ShowLaminationCalculator();
        });

        public ICommand DguLamCommand => new RelayCommand(o =>
        {
            if (!_isLicensed) { MessageBox.Show("Please activate your license to access calculators", "License Required", MessageBoxButton.OK, MessageBoxImage.Warning); ShowSubscriptionPlan(); return; }
            ShowDGULaminationCalculator();
        });

        public ICommand GlassOptCommand => new RelayCommand(o =>
        {
            if (!_isLicensed) { MessageBox.Show("Please activate your license to access calculators", "License Required", MessageBoxButton.OK, MessageBoxImage.Warning); ShowSubscriptionPlan(); return; }
            ShowGlassOptimization();
        });

        public ICommand SheetStoreCommand => new RelayCommand(o => ShowSheetStore());
        public ICommand DashboardCommand => new RelayCommand(o => ShowDashboard());
        public ICommand SubscriptionPlanCommand => new RelayCommand(o => ShowSubscriptionPlan());
        public ICommand LogoCommand => new RelayCommand(o => ShowDashboard());
        public ICommand DailyWorksCommand => new RelayCommand(o => ShowDailyWorks());
        public ICommand ProfileCommand => new RelayCommand(o => ShowProfile());
        public ICommand UsersCommand => new RelayCommand(o => ShowUsers());
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _exec;
        private readonly Func<object, bool> _canExec;

        public RelayCommand(Action<object> exec, Func<object, bool> canExec = null)
        {
            _exec = exec ?? throw new ArgumentNullException(nameof(exec));
            _canExec = canExec;
        }

        public RelayCommand(Action exec, Func<bool> canExec = null)
            : this(_ => exec(), canExec != null ? _ => canExec() : null) { }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object p) => _canExec?.Invoke(p) ?? true;
        public void Execute(object p) => _exec(p);
    }
}