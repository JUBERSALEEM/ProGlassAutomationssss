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

// Disambiguation aliases
using DbProformaInvoice = ProGlassAutomation.Data.Database.ProformaInvoiceModel;
using InvoiceModel = ProGlassAutomation.Models.ProformaInvoiceModel;

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

        // Helper method to set CurrentView and notify UI
        private void SetCurrentView(UserControl view)
        {
            _currentView = view;
            Notify(nameof(CurrentView));
            System.Diagnostics.Debug.WriteLine($"[MainVM] SetCurrentView: {view?.GetType().Name ?? "null"}");
        }

        // ═══════════════════════════════════════════════════════
        // VIEW CACHE - Instantiate once, reuse forever
        // ═══════════════════════════════════════════════
        private readonly Dictionary<string, UserControl> _viewCache = new();

        // Job Order ViewModel - shared across views
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

        // Proforma Invoice Main ViewModel - for save on exit
        private ProformaInvoiceMainViewModel _proformaInvoiceMainVM;
        public ProformaInvoiceMainViewModel ProformaInvoiceMainViewModel => _proformaInvoiceMainVM;

        private UserControl GetOrCreateView(string viewName)
        {
            System.Diagnostics.Debug.WriteLine($"[MainVM] GetOrCreateView called with: {viewName}");

            if (_viewCache.TryGetValue(viewName, out var cached))
            {
                System.Diagnostics.Debug.WriteLine($"[MainVM] Returning cached view: {viewName}");
                return cached;
            }

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
                "ProformaInvoice" => new Views.ProformaInvoice.ProformaInvoiceMainView(),
                "JobOrders" => CreateJobOrdersListView(),
                "JobOrderEdit" => CreateJobOrderEditView(),
                _ => null
            };

            if (view != null)
            {
                _viewCache[viewName] = view;
                System.Diagnostics.Debug.WriteLine($"[MainVM] Created and cached view: {viewName}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MainVM] WARNING: GetOrCreateView returned NULL for: {viewName}");
            }

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

            // Wire up events
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

        // Create DailyWorks view and wire up event connection
        private UserControl CreateDailyWorksView()
        {
            System.Diagnostics.Debug.WriteLine("[MainVM] CreateDailyWorksView STARTING");

            try
            {
                var view = new Views.DailyWorksView();
                System.Diagnostics.Debug.WriteLine("[MainVM] DailyWorksView created");

                // Create or get DailyWorksViewModel
                if (_dailyWorksViewModel == null)
                {
                    _dailyWorksViewModel = new DailyWorksViewModel();
                    System.Diagnostics.Debug.WriteLine("[MainVM] DailyWorksViewModel created");
                }

                view.DataContext = _dailyWorksViewModel;
                System.Diagnostics.Debug.WriteLine("[MainVM] DataContext set");

                // Wire up InvoiceSaved event from MainViewModel to DailyWorksViewModel
                InvoiceSaved -= _dailyWorksViewModel.OnProformaInvoiceSaved;
                InvoiceSaved += _dailyWorksViewModel.OnProformaInvoiceSaved;

                System.Diagnostics.Debug.WriteLine("[MainVM] DailyWorks view created and event wired");
                System.Diagnostics.Debug.WriteLine("[MainVM] CreateDailyWorksView COMPLETE");

                return view;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainVM] CreateDailyWorksView ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════
        // JOB ORDER LIST EVENT HANDLERS
        // ═══════════════════════════════════════════════

        private void OnOpenJobOrderRequested(Data.Database.JobOrderModel jo)
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

        private void OnOpenProformaInvoiceRequested(Data.Database.JobOrderModel jo)
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
        // NAVIGATION ENGINE - Central hub
        // ═══════════════════════════════════════════════
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
            System.Diagnostics.Debug.WriteLine($"[MainVM] Navigate called with: {viewName}");

            if (string.IsNullOrEmpty(viewName))
            {
                System.Diagnostics.Debug.WriteLine("[MainVM] Navigate: viewName is null/empty, returning");
                return;
            }

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
                System.Diagnostics.Debug.WriteLine($"[MainVM] Navigated to JobOrderEdit");
                return;
            }

            var newView = GetOrCreateView(viewName);

            System.Diagnostics.Debug.WriteLine($"[MainVM] GetOrCreateView returned: {newView?.GetType().Name ?? "NULL"}");

            if (newView == null)
            {
                System.Diagnostics.Debug.WriteLine($"[MainVM] ERROR: Navigate - newView is NULL for: {viewName}");
                MessageBox.Show($"View '{viewName}' not found.", "Navigation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CurrentView == newView)
            {
                System.Diagnostics.Debug.WriteLine("[MainVM] Same view, toggling off");
                SetCurrentView(null);
                CurrentViewName = "";
                return;
            }

            SetCurrentView(newView);
            CurrentViewName = viewName;
            System.Diagnostics.Debug.WriteLine($"[MainVM] Navigated successfully to: {viewName}");
        }

        private bool IsCalculatorView(string name) =>
            name is "SGU" or "DGU" or "Lamination" or "DguLam" or "Optimization";

        // ═══════════════════════════════════════════════
        // LICENSE STATE
        // ═══════════════════════════════════════════════
        private bool _isLicensed = false;
        private string _currentKey = "";

        public bool IsLicensed
        {
            get => _isLicensed;
            set => Set(ref _isLicensed, value, nameof(IsLicensed));
        }

        public bool CalculatorsEnabled => _isLicensed;
        public bool IsLocked => !_isLicensed;

        // Track if data needs saving
        private bool _isDirty = false;
        public bool IsDirty
        {
            get => _isDirty;
            set => Set(ref _isDirty, value);
        }

        // ═══════════════════════════════════════════════
        // INIT
        // ═══════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════
        // PUBLIC NAVIGATION HELPERS
        // ═══════════════════════════════════════════════

        public void ShowDashboard()
        {
            System.Diagnostics.Debug.WriteLine("[MainVM] ShowDashboard called");
            Navigate("Dashboard");
        }

        public void ShowSubscriptionPlan() => Navigate("Subscription");
        public void ShowSheetStore() => Navigate("SheetStore");
        public void ShowDeliveries() => Navigate("Deliveries");
        public void ShowProfile() => Navigate("Profile");
        public void ShowUsers() => Navigate("Users");
        public void ShowBalanceReports() => Navigate("BalanceReports");
        public void ShowJobOrders() => Navigate("JobOrders");

        // ═══════════════════════════════════════════════
        // DAILY WORKS - WITH DEBUG
        // ═══════════════════════════════════════════════

        public void ShowDailyWorks()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainVM] ShowDailyWorks called - START");

                // Clear cache to force recreation
                if (_viewCache.ContainsKey("DailyWorks"))
                {
                    System.Diagnostics.Debug.WriteLine("[MainVM] Removing DailyWorks from cache");
                    _viewCache.Remove("DailyWorks");
                }

                Navigate("DailyWorks");

                System.Diagnostics.Debug.WriteLine("[MainVM] ShowDailyWorks called - END");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainVM] ShowDailyWorks ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                MessageBox.Show($"Error loading Daily Works: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════
        // PROFORMA INVOICE - WITH DEBUG
        // ═══════════════════════════════════════════════

        public void ShowProformaInvoice()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainVM] ShowProformaInvoice called - START");

                System.Diagnostics.Debug.WriteLine("[MainVM] About to create ProformaInvoiceMainView");
                var mainView = new Views.ProformaInvoice.ProformaInvoiceMainView();
                System.Diagnostics.Debug.WriteLine("[MainVM] Created ProformaInvoiceMainView");

                // Create editor ViewModel FIRST, then pass to ProformaInvoiceMainViewModel
                System.Diagnostics.Debug.WriteLine("[MainVM] Creating ProformaInvoiceViewModel");
                var editorViewModel = new ProformaInvoiceViewModel();

                System.Diagnostics.Debug.WriteLine("[MainVM] Creating ProformaInvoiceMainViewModel");
                _proformaInvoiceMainVM = new ProformaInvoiceMainViewModel(editorViewModel);

                // Subscribe InvoiceSaved to update DailyWorks
                System.Diagnostics.Debug.WriteLine("[MainVM] Subscribing to InvoiceSaved");
                editorViewModel.InvoiceSaved += OnProformaInvoiceSaved;

                // SUBSCRIBE TO OpenPIEditor EVENT
                System.Diagnostics.Debug.WriteLine("[MainVM] Subscribing to OpenPIEditor event");
                _proformaInvoiceMainVM.OpenPIEditor += invoice =>
                {
                    System.Diagnostics.Debug.WriteLine($"[MainVM] OpenPIEditor event triggered with: {invoice?.InvoiceNo ?? "NEW"}");

                    try
                    {
                        if (invoice != null)
                        {
                            editorViewModel.LoadFromProformaInvoice(invoice);
                        }
                        else
                        {
                            editorViewModel.CreateNewInvoice();
                        }

                        var editorView = new Views.ProformaInvoice.ProformaInvoiceView { DataContext = editorViewModel };
                        SetCurrentView(editorView);
                        System.Diagnostics.Debug.WriteLine("[MainVM] Set editor view as CurrentView");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainVM] OpenPIEditor error: {ex.Message}");
                        MessageBox.Show($"Error opening invoice: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                mainView.DataContext = _proformaInvoiceMainVM;

                System.Diagnostics.Debug.WriteLine("[MainVM] Setting CurrentView to ProformaInvoiceMainView");
                SetCurrentView(mainView);
                CurrentViewName = "ProformaInvoice";

                System.Diagnostics.Debug.WriteLine($"[MainVM] CurrentView now: {CurrentView?.GetType().Name}");
                System.Diagnostics.Debug.WriteLine("[MainVM] ShowProformaInvoice called - END");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainVM] ShowProformaInvoice ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                MessageBox.Show($"Error loading Proforma Invoice: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event for forwarding InvoiceSaved to DailyWorksViewModel
        public event Action<InvoiceModel>? InvoiceSaved;

        // Event handler to forward InvoiceSaved to DailyWorksViewModel
        public void OnProformaInvoiceSaved(InvoiceModel invoice)
        {
            System.Diagnostics.Debug.WriteLine($"[MainVM] OnProformaInvoiceSaved received: {invoice?.InvoiceNo}");

            // Forward directly to DailyWorksViewModel's handler
            if (DailyWorksViewModel != null)
            {
                System.Diagnostics.Debug.WriteLine($"[MainVM] Forwarding to DailyWorksViewModel");
                DailyWorksViewModel.OnProformaInvoiceSaved(invoice);
            }

            // Also raise the InvoiceSaved event for any other subscribers
            InvoiceSaved?.Invoke(invoice);
        }

        // Reference to DailyWorksViewModel
        private DailyWorksViewModel _dailyWorksViewModel;
        public DailyWorksViewModel DailyWorksViewModel
        {
            get => _dailyWorksViewModel;
            set => Set(ref _dailyWorksViewModel, value);
        }

        // Handle invoice being saved
        public void OnInvoiceToBeAdded(Models.ProformaInvoiceModel invoice)
        {
            if (invoice == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[MainVM] OnInvoiceToBeAdded: {invoice.InvoiceNo}");

                // Tell MainViewModel to save on exit
                IsDirty = true;

                System.Diagnostics.Debug.WriteLine($"[MainVM] Marked as dirty for save");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainVM] OnInvoiceToBeAdded error: {ex.Message}");
            }
        }

        // ═══════════════════════════════════════════════
        // PROFORMA INVOICE METHODS
        // ═══════════════════════════════════════════════

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
                System.Diagnostics.Debug.WriteLine($"[MainVM] ShowProformaInvoiceByNumber: {invoiceNo}");

                var allPIs = DbHelper.GetAllProformaInvoices();
                var pi = allPIs.FirstOrDefault(p => p.InvoiceNo == invoiceNo);

                if (pi != null)
                {
                    var editorViewModel = new ProformaInvoiceViewModel();

                    try
                    {
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
                            var view = new Views.ProformaInvoice.ProformaInvoiceView { DataContext = editorViewModel };
                            SetCurrentView(view);
                            CurrentViewName = "ProformaInvoice";
                            System.Diagnostics.Debug.WriteLine($"[MainVM] Loaded PI: {invoiceNo}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainVM] Error loading PI: {ex.Message}");
                        MessageBox.Show($"Error loading invoice: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
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

        // ═══════════════════════════════════════════════
        // PROFORMA INVOICE TO JOB ORDER CONVERSION
        // ═══════════════════════════════════════════════

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

                System.Diagnostics.Debug.WriteLine($"[MainVM] CreateJobOrderFromProformaInvoice: {invoice.InvoiceNo}");

                JobOrderVM.LoadFromProformaInvoice(invoice);
                Navigate("JobOrderEdit");

                System.Diagnostics.Debug.WriteLine($"[MainVM] Job Order loaded successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainVM] CreateJobOrderFromProformaInvoice ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
                MessageBox.Show($"Failed to create job order: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ═══════════════════════════════════════════════
        // CALCULATOR METHODS
        // ═══════════════════════════════════════════════

        public void ShowSGUCalculator() => Navigate("SGU");
        public void ShowDGUCalculator() => Navigate("DGU");
        public void ShowLaminationCalculator() => Navigate("Lamination");
        public void ShowDGULaminationCalculator() => Navigate("DguLam");
        public void ShowGlassOptimization() => Navigate("Optimization");

        // ═══════════════════════════════════════════════
        // JOB ORDER TO DELIVERY CONVERSION
        // ═══════════════════════════════════════════════

        public void CreateDeliveryFromJobOrder(JobOrder jobOrder)
        {
            if (jobOrder == null) return;

            _viewCache.Remove("Deliveries");
            Navigate("Deliveries");

            if (GetOrCreateView("Deliveries") is Views.Delivery.DeliveryView deliveryView)
            {
                if (deliveryView.DataContext is ViewModels.DeliveryViewModel deliveryVM)
                {
                    deliveryVM.CreateFromJobOrder(jobOrder);
                }
            }
        }

        // ═══════════════════════════════════════════════
        // CLICK COMMANDS
        // ═══════════════════════════════════════════════

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

        // ═══════════════════════════════════════════════
        // SAVE ALL DATA (FOR APP CLOSE)
        // ═══════════════════════════════════════════════

        public void SaveAllData()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[MainViewModel] SaveAllData called");

                // Save ProformaInvoice data
                if (_proformaInvoiceMainVM != null)
                {
                    _proformaInvoiceMainVM.SaveOnExit();
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] ProformaInvoice data saved");
                }

                System.Diagnostics.Debug.WriteLine("[MainViewModel] All data saved successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Error saving data: {ex.Message}");
            }
        }
    }
}