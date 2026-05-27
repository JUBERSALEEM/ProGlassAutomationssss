using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using ProGlassAutomation.Views;
using ProGlassAutomation.Models;
using ProGlassAutomation.Data.Database;
using System.Collections.ObjectModel;

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
                "DailyWorks" => new Views.DailyWorksView(),
                "Deliveries" => new Views.Delivery.DeliveryView(),
                "Profile" => new Views.Profile.ProfileView(),
                "Users" => CreatePlaceholder("Users - Coming Soon!"),
                "ProformaInvoice" => new Views.ProformaInvoice.ProformaInvoiceView(),
                "JobOrders" => CreateJobOrdersListView(),      // Changed to List View
                "JobOrderEdit" => CreateJobOrderEditView(),     // Single Job Order Edit
                "JobOrderDetails" => new Views.JobOrder.JobOrderDetailsView(),
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
            var view = new Views.JobOrderListView();
            view.DataContext = JobOrderListVM;  // Use shared List VM

            // Connect events for navigation (only once)
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
            // Check if JobOrderView exists in the Views folder
            // If not, use JobOrder.JobOrderView (in the JobOrder subfolder)
            var view = new Views.JobOrder.JobOrderView();
            view.DataContext = JobOrderVM;  // Use shared Edit VM
            return view;
        }

        // ═══════════════════════════════════════════════════════
        // JOB ORDER LIST EVENT HANDLERS
        // ═══════════════════════════════════════════════════════

        private void OnOpenJobOrderRequested(JobOrder jo)
        {
            try
            {
                // Load the job order data into the edit VM
                JobOrderVM.LoadFromExistingJobOrder(jo);

                // Navigate to edit view
                Navigate("JobOrderEdit");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Job Order: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnOpenProformaInvoiceRequested(JobOrder jo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jo.PINumber))
                {
                    MessageBox.Show("No linked Proforma Invoice found.", "Info",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var allPIs = DbHelper.GetAllProformaInvoices();
                var pi = allPIs.FirstOrDefault(p => p.InvoiceNo == jo.PINumber);

                if (pi != null)
                {
                    // Navigate to PI view and load
                    Navigate("ProformaInvoice");

                    if (GetOrCreateView("ProformaInvoice") is Views.ProformaInvoice.ProformaInvoiceView piView)
                    {
                        if (piView.DataContext is ProformaInvoiceViewModel piVm)
                        {
                            try
                            {
                                // Convert database model to JSON and back to UI model
                                var json = Newtonsoft.Json.JsonConvert.SerializeObject(pi);
                                var uiModel = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.ProformaInvoiceModel>(json);

                                if (uiModel != null)
                                {
                                    piVm.LoadFromProformaInvoice(uiModel);
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[MainVM] Error loading PI: {ex.Message}");
                                System.Windows.MessageBox.Show($"Error loading invoice: {ex.Message}", "Error",
                                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Proforma Invoice not found.", "Info",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening Proforma Invoice: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnNewJobOrderRequested()
        {
            // Clear the edit VM for new job order
            JobOrderVM.ClearForNewJobOrder();

            // Navigate to edit view
            Navigate("JobOrderEdit");
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

            // Special handling for Job Order Edit - don't cache
            if (viewName == "JobOrderEdit")
            {
                var view = CreateJobOrderEditView();
                CurrentView = view;
                CurrentViewName = viewName;
                return;
            }

            // Get cached view and inject
            var newView = GetOrCreateView(viewName);

            // Prevent reloading same module
            if (CurrentView == newView)
            {
                // TOGGLE OFF (close module)
                CurrentView = null;
                CurrentViewName = "";
                return;
            }

            CurrentView = newView;
            CurrentViewName = viewName;
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

        // ═══════════════════════════════════════════════════════
        // LICENSE CALLBACKS
        // ═══════════════════════════════════════════════════════

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
        // PUBLIC NAVIGATION HELPERS
        // ═══════════════════════════════════════════════════════

        public void ShowDashboard() => Navigate("Dashboard");
        public void ShowSubscriptionPlan() => Navigate("Subscription");
        public void ShowSheetStore() => Navigate("SheetStore");
        public void ShowDailyWorks() => Navigate("DailyWorks");
        public void ShowDeliveries() => Navigate("Deliveries");
        public void ShowProfile() => Navigate("Profile");
        public void ShowUsers() => Navigate("Users");
        public void ShowBalanceReports() => Navigate("BalanceReports");
        public void ShowProformaInvoice() => Navigate("ProformaInvoice");
        public void ShowJobOrders() => Navigate("JobOrders");

        // ═══════════════════════════════════════════════════════
        // PROFORMA INVOICE TO JOB ORDER CONVERSION
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

                System.Diagnostics.Debug.WriteLine($"[MainVM] CreateJobOrderFromProformaInvoice: {invoice.InvoiceNo}");

                // Load data into shared Job Order VM
                JobOrderVM.LoadFromProformaInvoice(invoice);

                // Navigate to edit view (not list)
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

        public void ShowJobOrderDetails(int jobOrderId)
        {
            // Clear cache to get fresh instance
            _viewCache.Remove("JobOrderDetails");

            // Create view and set the job order ID
            var view = new Views.JobOrder.JobOrderDetailsView();
            var vm = new ViewModels.JobOrderDetailsViewModel();
            vm.LoadJobOrder(jobOrderId);
            view.DataContext = vm;

            CurrentView = view;
            CurrentViewName = "JobOrderDetails";
        }

        // Calculator methods
        public void ShowSGUCalculator() => Navigate("SGU");
        public void ShowDGUCalculator() => Navigate("DGU");
        public void ShowLaminationCalculator() => Navigate("Lamination");
        public void ShowDGULaminationCalculator() => Navigate("DguLam");
        public void ShowGlassOptimization() => Navigate("Optimization");

        // ═══════════════════════════════════════════════════════
        // JOB ORDER TO DELIVERY CONVERSION
        // ═══════════════════════════════════════════════════════

        public void CreateDeliveryFromJobOrder(JobOrder jobOrder)
        {
            if (jobOrder == null) return;

            // Clear cache to get fresh instance
            _viewCache.Remove("Deliveries");

            // Navigate to deliveries view
            Navigate("Deliveries");

            // Get the delivery view and set up from job order
            if (GetOrCreateView("Deliveries") is Views.Delivery.DeliveryView deliveryView)
            {
                if (deliveryView.DataContext is ViewModels.DeliveryViewModel deliveryVM)
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
        public ICommand JobOrderDetailsCommand => new RelayCommand(o =>
        {
            if (o is int jobOrderId)
                ShowJobOrderDetails(jobOrderId);
        });
    }
}