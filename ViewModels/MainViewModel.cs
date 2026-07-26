using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;
using ProGlassAutomation.Views;
using ProGlassAutomation.Views.ProformaInvoice;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using DbJobOrder = ProGlassAutomation.Data.Database.JobOrderModel;
using ModelsInvoice = ProGlassAutomation.Models.ProformaInvoiceModel;

// Disambiguation aliases
using DbProformaInvoice = ProGlassAutomation.Data.Database.ProformaInvoiceModel;
using InvoiceModel = ProGlassAutomation.Models.ProformaInvoiceModel;

namespace ProGlassAutomation.ViewModels
{
    /// <summary>
    /// Main ViewModel - Navigation + Factory
    /// PATCH: Updated for refactored DailyWorksViewModel
    /// </summary>
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

        // Helper method to set CurrentView and notify UI
        private void SetCurrentView(UserControl view)
        {
            _currentView = view;
            Notify(nameof(CurrentView));
        }

        // Get MainWindow for content setting
        private MainWindow GetMainWindow()
        {
            return Application.Current.MainWindow as MainWindow;
        }

        // ═══════════════════════════════════════════════════════
        // VIEW CACHE
        // ═══════════════════════════════════════════════════════
        private readonly Dictionary<string, UserControl> _viewCache = new();

        // Job Order ViewModel
        private JobOrderViewModel _jobOrderVM;
        public JobOrderViewModel JobOrderVM
        {
            get
            {
                if (_jobOrderVM == null)
                    _jobOrderVM = new JobOrderViewModel();
                return _jobOrderVM;
            }
        }

        // Job Order List ViewModel
        private JobOrderListViewModel _jobOrderListVM;
        public JobOrderListViewModel JobOrderListVM
        {
            get
            {
                if (_jobOrderListVM == null)
                    _jobOrderListVM = new JobOrderListViewModel();
                return _jobOrderListVM;
            }
        }

        // Proforma Invoice List ViewModel
        private ProformaInvoiceListViewModel _proformaInvoiceListVM;
        public ProformaInvoiceListViewModel ProformaInvoiceListVM
        {
            get
            {
                if (_proformaInvoiceListVM == null)
                    _proformaInvoiceListVM = new ProformaInvoiceListViewModel();
                return _proformaInvoiceListVM;
            }
        }

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
                "DailyWorks" => CreateDailyWorksView(),
                "Deliveries" => new Views.Delivery.DeliveryView(),
                "Profile" => new Views.Profile.ProfileView(),
                "Users" => CreatePlaceholder("Users - Coming Soon!"),
                "ProformaInvoice" => CreateProformaInvoiceListView(),
                "JobOrders" => CreateJobOrdersListView(),
                "JobOrderEdit" => CreateJobOrderEditView(),
                "TaxInvoice" => new Views.TaxInvoice.TaxInvoiceView { DataContext = new ViewModels.TaxInvoice.TaxInvoiceViewModel() },
                "Analytics" => new Views.Analytics.AnalyticsView { DataContext = new ViewModels.Analytics.AnalyticsViewModel() },
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

        private UserControl CreateJobOrdersListView()
        {
            var view = new Views.JobOrder.JobOrderListView();
            view.DataContext = JobOrderListVM;

            JobOrderListVM.OpenJobOrderRequested -= OnOpenJobOrderRequested;
            JobOrderListVM.OpenJobOrderRequested += OnOpenJobOrderRequested;

            JobOrderListVM.OpenProformaInvoiceRequested -= OnOpenProformaInvoiceRequested;
            JobOrderListVM.OpenProformaInvoiceRequested += OnOpenProformaInvoiceRequested;

            JobOrderListVM.NewJobOrderRequested -= OnNewJobOrderRequested;
            JobOrderListVM.NewJobOrderRequested += OnNewJobOrderRequested;

            return view;
        }

        private UserControl CreateJobOrderEditView()
        {
            var view = new Views.JobOrder.JobOrderView();
            view.DataContext = JobOrderVM;
            return view;
        }

        private UserControl CreateProformaInvoiceListView()
        {
            var view = new ProformaInvoiceListView();
            view.DataContext = SharedViewModels.ProformaInvoiceListVM;
            return view;
        }

        // ═══════════════════════════════════════════════════════
        // CREATE DAILY WORKS VIEW - PATCHED
        // Now uses refactored DailyWorksViewModel
        // ═══════════════════════════════════════════════════════════════

        private UserControl CreateDailyWorksView()
        {
            // Create new DailyWorksViewModel
            _dailyWorksViewModel = new DailyWorksViewModel();

            var view = new Views.DailyWorksView();
            view.DataContext = _dailyWorksViewModel;

            // Connect navigation event from DailyWorks back to main
            _dailyWorksViewModel.RequestNavigateToInvoice -= OnNavigateToInvoiceFromDailyWorks;
            _dailyWorksViewModel.RequestNavigateToInvoice += OnNavigateToInvoiceFromDailyWorks;

            // Connect invoice saved event
            InvoiceSaved -= _dailyWorksViewModel.OnProformaInvoiceSaved;
            InvoiceSaved += _dailyWorksViewModel.OnProformaInvoiceSaved;

            return view;
        }

        private void OnNavigateToInvoiceFromDailyWorks()
        {
            // Navigate to Proforma Invoice when requested from DailyWorks
            ShowProformaInvoice();
        }

        // ═══════════════════════════════════════════════════════
        // DAILY WORKS VIEWMODEL - PATCHED
        // Now uses refactored version with Repository
        // ═══════════════════════════════════════════════════════

        private DailyWorksViewModel _dailyWorksViewModel;

        public DailyWorksViewModel DailyWorksViewModel
        {
            get
            {
                if (_dailyWorksViewModel == null)
                {
                    // Lazy load - will be created when Navigate("DailyWorks") is called
                }
                return _dailyWorksViewModel;
            }
            set => Set(ref _dailyWorksViewModel, value);
        }

        // ═══════════════════════════════════════════════════════
        // JOB ORDER LIST EVENT HANDLERS
        // ═══════════════════════════════════════════════════════

        private void OnOpenJobOrderRequested(DbJobOrder jo)
        {
            try
            {
                JobOrderVM.LoadFromExistingJobOrder(jo);
                Navigate("JobOrderEdit");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Job Order: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenProformaInvoiceRequested(DbJobOrder jo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jo.PINumber))
                {
                    MessageBox.Show("No linked Proforma Invoice found.", "Info",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                ShowProformaInvoiceByNumber(jo.PINumber);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Proforma Invoice: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnNewJobOrderRequested()
        {
            JobOrderVM.ClearForNewJobOrder();
            Navigate("JobOrderEdit");
        }

        // ═══════════════════════════════════════════════════════
        // NAVIGATION ENGINE
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

            if (IsCalculatorView(viewName) && !IsLicensed)
            {
                MessageBox.Show("Please activate your license to access calculators",
                    "License Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                Navigate("Subscription");
                return;
            }

            if (viewName == "JobOrderEdit")
            {
                var view = CreateJobOrderEditView();
                SetCurrentView(view);
                CurrentViewName = viewName;
                GetMainWindow()?.SetContent(view);
                return;
            }

            var newView = GetOrCreateView(viewName);

            if (newView == null)
            {
                MessageBox.Show($"View '{viewName}' not found.", "Navigation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CurrentView == newView)
            {
                SetCurrentView(null);
                CurrentViewName = "";
                GetMainWindow()?.SetContent(null);
                return;
            }

            SetCurrentView(newView);
            CurrentViewName = viewName;
            GetMainWindow()?.SetContent(newView);
        }

        private bool IsCalculatorView(string name) =>
            name is "SGU" or "DGU" or "Lamination" or "DguLam" or "Optimization";

        // ═══════════════════════════════════════════════════════
        // LICENSE STATE
        // ═══════════════════════════════════════════════════════
        private bool _isLicensed = false;
        private string _currentKey = "";

        public bool IsLicensed
        {
            get => _isLicensed;
            set => Set(ref _isLicensed, value, nameof(IsLicensed));
        }

        public bool CalculatorsEnabled => _isLicensed;
        public bool IsLocked => !_isLicensed;

        private bool _isDirty = false;
        public bool IsDirty
        {
            get => _isDirty;
            set => Set(ref _isDirty, value);
        }

        // ═══════════════════════════════════════════════════════
        // INIT
        // ═══════════════════════════════════════════════════════
        public MainViewModel()
        {
            NavigateCommand = new RelayCommand(o => Navigate(o?.ToString() ?? ""));
            CheckExistingActivation();
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
                    Notify(nameof(IsLicensed));
                    Notify(nameof(IsLocked));
                    Notify(nameof(CalculatorsEnabled));
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

        public void OnLicenseActivated(string key)
        {
            _currentKey = key;
            _isLicensed = true;
            SaveKey(key);
            Notify(nameof(IsLicensed));
            Notify(nameof(CalculatorsEnabled));
            Notify(nameof(IsLocked));
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
        public void ShowDeliveries() => Navigate("Deliveries");
        public void ShowProfile() => Navigate("Profile");
        public void ShowUsers() => Navigate("Users");
        public void ShowBalanceReports() => Navigate("BalanceReports");
        public void ShowJobOrders() => Navigate("JobOrders");
        public void ShowTaxInvoice() => Navigate("TaxInvoice");
        public void ShowAnalytics() => Navigate("Analytics");

        public void ShowDailyWorks()
        {
            try
            {
                if (_viewCache.ContainsKey("DailyWorks"))
                    _viewCache.Remove("DailyWorks");
                Navigate("DailyWorks");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Daily Works: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════
        // PROFORMA INVOICE
        // ═══════════════════════════════════════════════════════

        public void ShowProformaInvoice()
        {
            try
            {
                var listView = new ProformaInvoiceListView();

                // 🔴 FIX: Use SHARED instance from SharedViewModels!
                listView.DataContext = SharedViewModels.ProformaInvoiceListVM;

                // Subscribe to refresh event for when returning from Editor
                SharedViewModels.InvoiceListRefreshRequested -= OnInvoiceListRefreshRequested;
                SharedViewModels.InvoiceListRefreshRequested += OnInvoiceListRefreshRequested;

                SetCurrentView(listView);
                CurrentViewName = "ProformaInvoice";
                GetMainWindow()?.SetContent(listView);

                Debug.WriteLine("[MainViewModel] ShowProformaInvoice using SharedViewModels");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔴 Handle refresh when returning from Editor
        private void OnInvoiceListRefreshRequested()
        {
            SharedViewModels.ProformaInvoiceListVM.ApplyFilters();
            Debug.WriteLine("[MainViewModel] InvoiceListRefreshRequested - ApplyFilters called");
        }

        // Event for forwarding InvoiceSaved to DailyWorksViewModel
        public event Action<InvoiceModel>? InvoiceSaved;

        public void OnProformaInvoiceSaved(ProGlassAutomation.Models.ProformaInvoiceModel invoice)
        {
            if (DailyWorksViewModel != null)
            {
                // Create the proper InvoiceModel type
                var invoiceModel = new ProGlassAutomation.Models.ProformaInvoiceModel
                {
                    InvoiceNo = invoice.InvoiceNo,
                    // Copy other properties as needed
                };
                DailyWorksViewModel.OnProformaInvoiceSaved(invoiceModel);
            }
            InvoiceSaved?.Invoke(invoice);
        }

        public void OnInvoiceToBeAdded(Models.ProformaInvoiceModel invoice)
        {
            if (invoice == null) return;
            IsDirty = true;
        }

        // ═══════════════════════════════════════════════════════
        // PROFORMA INVOICE BY NUMBER
        // ═══════════════════════════════════════════════════════

        public void ShowProformaInvoiceByNumber(string invoiceNo)
        {
            if (string.IsNullOrWhiteSpace(invoiceNo))
            {
                MessageBox.Show("Invoice number is empty.", "Info",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var allPIs = DbHelper.GetAllProformaInvoices();
                var pi = allPIs.FirstOrDefault(p => p.InvoiceNo == invoiceNo);

                if (pi != null)
                {
                    var editorViewModel = new ProformaInvoiceViewModel();

                    var settings = new Newtonsoft.Json.JsonSerializerSettings
                    {
                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                    };
                    var json = Newtonsoft.Json.JsonConvert.SerializeObject(pi, settings);
                    var uiModel = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.ProformaInvoiceModel>(json, settings);

                    if (uiModel != null)
                    {
                        editorViewModel.LoadFromProformaInvoice(uiModel);
                        // Event will be handled by MainViewModel's InvoiceSaved event
                        var view = new Views.ProformaInvoice.ProformaInvoiceView { DataContext = editorViewModel };
                        SetCurrentView(view);
                        CurrentViewName = "ProformaInvoice";
                        GetMainWindow()?.SetContent(view);
                    }
                }
                else
                {
                    MessageBox.Show($"Proforma Invoice '{invoiceNo}' not found.", "Info",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Proforma Invoice: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════
        // PROFORMA TO JOB ORDER
        // ═══════════════════════════════════════════════════════

        public void CreateJobOrderFromProformaInvoice(Models.ProformaInvoiceModel invoice)
        {
            try
            {
                if (invoice == null)
                {
                    MessageBox.Show("Invoice is null. Cannot create job order.", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                JobOrderVM.LoadFromProformaInvoice(invoice);
                Navigate("JobOrderEdit");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create job order: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════════════
        // CALCULATOR METHODS
        // ═══════════════════════════════════════════════════════

        public void ShowSGUCalculator() => Navigate("SGU");
        public void ShowDGUCalculator() => Navigate("DGU");
        public void ShowLaminationCalculator() => Navigate("Lamination");
        public void ShowDGULaminationCalculator() => Navigate("DguLam");
        public void ShowGlassOptimization() => Navigate("Optimization");

        // ═══════════════════════════════════════════════════════
        // JOB ORDER TO DELIVERY
        // ═══════════════════════════════════════════════════════

        public void CreateDeliveryFromJobOrder(Models.JobOrder jobOrder)
        {
            if (jobOrder == null) return;

            _viewCache.Remove("Deliveries");
            Navigate("Deliveries");

            if (GetOrCreateView("Deliveries") is Views.Delivery.DeliveryView deliveryView)
            {
                if (deliveryView.DataContext is DeliveryViewModel deliveryVM)
                {
                    deliveryVM.CreateFromJobOrder(jobOrder);
                }
            }
        }

        // ═══════════════════════════════════════════════════════
        // CLICK COMMANDS
        // ═══════════════════════════════════════════════════════

        public ICommand SGUCommand => new RelayCommand(o =>
        {
            if (!_isLicensed) { ShowSubscriptionPlan(); return; }
            ShowSGUCalculator();
        });

        public ICommand DGUCommand => new RelayCommand(o =>
        {
            if (!_isLicensed) { ShowSubscriptionPlan(); return; }
            ShowDGUCalculator();
        });

        public ICommand LaminationCommand => new RelayCommand(o =>
        {
            if (!_isLicensed) { ShowSubscriptionPlan(); return; }
            ShowLaminationCalculator();
        });

        public ICommand DguLamCommand => new RelayCommand(o =>
        {
            if (!_isLicensed) { ShowSubscriptionPlan(); return; }
            ShowDGULaminationCalculator();
        });

        public ICommand GlassOptCommand => new RelayCommand(o =>
        {
            if (!_isLicensed) { ShowSubscriptionPlan(); return; }
            ShowGlassOptimization();
        });

        public ICommand SheetStoreCommand => new RelayCommand(o => ShowSheetStore());
        public ICommand DashboardCommand => new RelayCommand(o => ShowDashboard());
        public ICommand SubscriptionPlanCommand => new RelayCommand(o => ShowSubscriptionPlan());
        public ICommand LogoCommand => new RelayCommand(o => ShowDashboard());
        public ICommand DailyWorksCommand => new RelayCommand(o => ShowDailyWorks());
        public ICommand DeliveriesCommand => new RelayCommand(o => ShowDeliveries());
        public ICommand ProfileCommand => new RelayCommand(o => ShowProfile());
        public ICommand UsersCommand => new RelayCommand(o => ShowUsers());
        public ICommand ProformaInvoiceCommand => new RelayCommand(o => ShowProformaInvoice());
        public ICommand JobOrdersCommand => new RelayCommand(o => ShowJobOrders());
        public ICommand TaxInvoiceCommand => new RelayCommand(o => ShowTaxInvoice());
        public ICommand AnalyticsCommand => new RelayCommand(o => ShowAnalytics());

        // ═══════════════════════════════════════════════════════
        // SAVE ALL DATA
        // ═══════════════════════════════════════════════════════

        public void SaveAllData()
        {
            try
            {
                // Save any pending data before exit
                System.Diagnostics.Debug.WriteLine("[MainViewModel] SaveAllData called");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Error saving data: {ex.Message}");
            }
        }
    }
}