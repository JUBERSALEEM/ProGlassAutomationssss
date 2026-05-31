using Newtonsoft.Json;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;
using System;
using DbJobOrder = ProGlassAutomation.Data.Database.JobOrderModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ProGlassAutomation.ViewModels
{
    public class JobOrderViewModel : ViewModelBase
    {
        #region Private Fields

        private string _jobOrderNumber = "";
        private string _clientName = "";
        private string _clientTRN = "";
        private string _clientReference = "";
        private string _salesman = "";
        private string _clientAddress = "";
        private string _projectName = "";
        private string _projectNo = "";
        private string _projectLocation = "";
        private string _lpoNo = "";
        private string _attentionName = "";
        private string _contactNo = "";
        private string _notes = "";
        private string _piNumber = "";
        private int _proformaInvoiceId = 0;
        private DateTime _jobOrderDate = DateTime.Now;
        private DateTime _requiredDate = DateTime.Today.AddDays(7);
        private string _status = "Pending";
        private bool _isSaving = false;

        private int _cachedTotalQty;
        private double _cachedTotalSQM;
        private double _cachedTotalSQM2;
        private double _cachedTotalAmount;
        private double _cachedTotalLM1;
        private double _cachedTotalLM2;
        private double _cachedAllOtherChargesTotal;
        private bool _totalsNeedRefresh = true;

        private string _companyName = "PROGLASS AUTOMATION";
        private string _companyTRN = "100458979400003";
        private string _companyLocation = "Dubai, UAE";

        private ObservableCollection<JobOrderSpecification> _specifications;
        private ObservableCollection<JobOrderOtherCharge> _allOtherCharges = new ObservableCollection<JobOrderOtherCharge>();
        private readonly DispatcherTimer _totalsRefreshTimer;

        public ObservableCollection<JobOrder> JobOrders { get; } = new ObservableCollection<JobOrder>();

        #endregion

        public JobOrderViewModel()
        {
            _specifications = new ObservableCollection<JobOrderSpecification>();
            Specifications.Add(new JobOrderSpecification { Id = 1, SpecificationName = "Specification 1" });

            _totalsRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _totalsRefreshTimer.Tick += (s, e) =>
            {
                _totalsRefreshTimer.Stop();
                PerformRefreshTotals();
            };

            Specifications.CollectionChanged += (s, e) => ScheduleTotalsRefresh();

            foreach (var spec in Specifications)
                SubscribeToSpecChanges(spec);

            SaveJobOrderCommand = new RelayCommand(async _ => await SaveJobOrderAsync());
            AddItemCommand = new RelayCommand(_ => ExecuteAddItem(null));
            DeleteJobOrderCommand = new RelayCommand(_ => DeleteJobOrder());
            AddSpecificationCommand = new RelayCommand(_ => AddSpecification());
            RemoveSpecificationCommand = new RelayCommand(_ => RemoveSpecification());
            AddOtherChargeCommand = new RelayCommand(o => ExecuteAddOtherCharge(o));
            DeleteOtherChargeCommand = new RelayCommand(o => ExecuteDeleteOtherCharge(o));

            RefreshTotals();
        }

        private void SubscribeToSpecChanges(JobOrderSpecification spec)
        {
            if (spec == null) return;
            spec.Items.CollectionChanged += (s, e) => ScheduleTotalsRefresh();
            spec.OtherCharges.CollectionChanged += (s, e) => ScheduleTotalsRefresh();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region Cached Total Properties

        public int TotalQty { get { EnsureTotalsRefreshed(); return _cachedTotalQty; } }
        public double TotalSQM { get { EnsureTotalsRefreshed(); return _cachedTotalSQM; } }
        public double TotalSQM2 { get { EnsureTotalsRefreshed(); return _cachedTotalSQM2; } }
        public double TotalAmount { get { EnsureTotalsRefreshed(); return _cachedTotalAmount; } }
        public double TotalLM1 { get { EnsureTotalsRefreshed(); return _cachedTotalLM1; } }
        public double TotalLM2 { get { EnsureTotalsRefreshed(); return _cachedTotalLM2; } }
        public double AllOtherChargesTotal { get { EnsureTotalsRefreshed(); return _cachedAllOtherChargesTotal; } }

        private void EnsureTotalsRefreshed()
        {
            if (_totalsNeedRefresh) { PerformRefreshTotals(); _totalsNeedRefresh = false; }
        }

        private void ScheduleTotalsRefresh()
        {
            _totalsNeedRefresh = true;
            _totalsRefreshTimer.Stop();
            _totalsRefreshTimer.Start();
        }

        private void PerformRefreshTotals()
        {
            _cachedTotalQty = 0; _cachedTotalSQM = 0; _cachedTotalSQM2 = 0;
            _cachedTotalAmount = 0; _cachedTotalLM1 = 0; _cachedTotalLM2 = 0;
            _cachedAllOtherChargesTotal = 0;

            foreach (var spec in Specifications)
            {
                if (spec == null) continue;
                _cachedTotalQty += spec.TotalQty;
                _cachedTotalSQM += spec.TotalSQM;
                _cachedTotalSQM2 += spec.TotalSQM2;
                _cachedTotalAmount += spec.TotalAmount;
                _cachedTotalLM1 += spec.TotalLM1;
                _cachedTotalLM2 += spec.TotalLM2;
                foreach (var charge in spec.OtherCharges)
                    _cachedAllOtherChargesTotal += charge?.Amount ?? 0;
            }

            OnPropertyChanged(nameof(TotalQty));
            OnPropertyChanged(nameof(TotalSQM));
            OnPropertyChanged(nameof(TotalSQM2));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(TotalLM1));
            OnPropertyChanged(nameof(TotalLM2));
            OnPropertyChanged(nameof(AllOtherChargesTotal));
        }

        public void InvalidateTotals() => ScheduleTotalsRefresh();

        #endregion

        #region Properties

        public string JobOrderNumber { get => _jobOrderNumber; set => SetProperty(ref _jobOrderNumber, value); }
        public string ClientName { get => _clientName; set => SetProperty(ref _clientName, value); }
        public string ClientTRN { get => _clientTRN; set => SetProperty(ref _clientTRN, value); }
        public string ClientReference { get => _clientReference; set => SetProperty(ref _clientReference, value); }
        public string Salesman { get => _salesman; set => SetProperty(ref _salesman, value); }
        public string ClientAddress { get => _clientAddress; set => SetProperty(ref _clientAddress, value); }
        public string ProjectName { get => _projectName; set => SetProperty(ref _projectName, value); }
        public string ProjectNo { get => _projectNo; set => SetProperty(ref _projectNo, value); }
        public string ProjectLocation { get => _projectLocation; set => SetProperty(ref _projectLocation, value); }
        public string LPONo { get => _lpoNo; set => SetProperty(ref _lpoNo, value); }
        public string AttentionName { get => _attentionName; set => SetProperty(ref _attentionName, value); }
        public string ContactNo { get => _contactNo; set => SetProperty(ref _contactNo, value); }
        public string Notes { get => _notes; set => SetProperty(ref _notes, value); }
        public string PINumber { get => _piNumber; set => SetProperty(ref _piNumber, value); }
        public int ProformaInvoiceId { get => _proformaInvoiceId; set => SetProperty(ref _proformaInvoiceId, value); }
        public DateTime JobOrderDate { get => _jobOrderDate; set => SetProperty(ref _jobOrderDate, value); }
        public DateTime RequiredDate { get => _requiredDate; set => SetProperty(ref _requiredDate, value); }
        public string Status { get => _status; set => SetProperty(ref _status, value); }
        public bool IsSaving { get => _isSaving; set => SetProperty(ref _isSaving, value); }
        public string CompanyName { get => _companyName; set => SetProperty(ref _companyName, value); }
        public string CompanyTRN { get => _companyTRN; set => SetProperty(ref _companyTRN, value); }
        public string CompanyLocation { get => _companyLocation; set => SetProperty(ref _companyLocation, value); }

        public ObservableCollection<JobOrderSpecification> Specifications
        {
            get => _specifications;
            set
            {
                if (_specifications != null) _specifications.CollectionChanged -= Specs_CollectionChanged;
                _specifications = value;
                OnPropertyChanged();
                if (_specifications != null) _specifications.CollectionChanged += Specs_CollectionChanged;
                InvalidateTotals();
            }
        }

        private void Specs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
                foreach (JobOrderSpecification spec in e.NewItems)
                    SubscribeToSpecChanges(spec);
            InvalidateTotals();
        }

        public ObservableCollection<JobOrderOtherCharge> AllOtherCharges => _allOtherCharges;

        #endregion

        #region Commands

        public ICommand SaveJobOrderCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand DeleteJobOrderCommand { get; }
        public ICommand AddSpecificationCommand { get; }
        public ICommand RemoveSpecificationCommand { get; }
        public ICommand AddOtherChargeCommand { get; }
        public ICommand DeleteOtherChargeCommand { get; }

        #endregion

        #region Load from Proforma Invoice

        public void LoadFromProformaInvoice(Models.ProformaInvoiceModel pi)
        {
            if (pi == null) return;
            try
            {
                CompanyName = pi.CompanyName ?? "PROGLASS AUTOMATION";
                CompanyTRN = pi.CompanyTRN ?? "100458979400003";
                CompanyLocation = pi.CompanyLocation ?? "Dubai, UAE";
                ClientName = pi.CustomerName ?? "";
                ClientTRN = pi.CustomerTRN ?? "";
                ClientReference = pi.CustomerReference ?? "";
                Salesman = string.IsNullOrWhiteSpace(pi.Salesman) ? "Unknown" : pi.Salesman;  // FIX HERE
                ClientAddress = pi.CustomerAddress ?? "";
                ProjectName = pi.ProjectName ?? "";
                ProjectNo = pi.ProjectNo ?? "";
                ProjectLocation = pi.ProjectLocation ?? "";
                LPONo = pi.LPONo ?? "";
                AttentionName = pi.AttentionName ?? "";
                ContactNo = pi.ContactNo ?? "";
                PINumber = pi.InvoiceNo ?? "";
                _proformaInvoiceId = pi.Id;
                Notes = pi.Notes ?? "";
                Status = "Pending";

                JobOrderNumber = DbHelper.GenerateNextJONumber();
                JobOrderDate = DateTime.Now;
                RequiredDate = DateTime.Today.AddDays(7);

                Specifications.Clear();

                if (pi.Specifications != null)
                {
                    int specIndex = 0;
                    foreach (var piSpec in pi.Specifications)
                    {
                        specIndex++;
                        var newSpec = new JobOrderSpecification
                        {
                            Id = specIndex,
                            SpecificationName = piSpec.SpecificationName ?? $"Specification {specIndex}",
                            ModuleType = piSpec.ModuleType ?? "SGU",
                            WorkType = piSpec.WorkType ?? "Annealed",
                            OuterThickness = piSpec.OuterThickness ?? "6mm",
                            OuterColor = piSpec.OuterColor ?? "Clear",
                            InnerThickness = piSpec.InnerThickness ?? "6mm",
                            InnerColor = piSpec.InnerColor ?? "Clear",
                            SpacerThickness = piSpec.SpacerThickness ?? "12mm",
                            PVBThickness = piSpec.PVBThickness ?? "0.76mm",
                            PVBColor = piSpec.PVBColor ?? "Clear",
                            IncludeInSpec = piSpec.IncludeInSpec,
                            BasePrice = piSpec.BasePrice,
                            SurchargePercent = piSpec.SurchargePercent
                        };

                        SubscribeToSpecChanges(newSpec);

                        if (piSpec.Items != null)
                        {
                            int itemIndex = 0;
                            foreach (var piItem in piSpec.Items)
                            {
                                itemIndex++;
                                var newItem = new JobOrderItem
                                {
                                    Id = itemIndex,
                                    SrNo = piItem.SrNo > 0 ? piItem.SrNo : itemIndex,
                                    GlassRef = piItem.GlassRef ?? "",
                                    Width1 = piItem.Width1,
                                    Height1 = piItem.Height1,
                                    Width2 = piItem.Width2,
                                    Height2 = piItem.Height2,
                                    Qty = piItem.Qty > 0 ? piItem.Qty : 1,
                                    Price = piItem.Price
                                };
                                newItem.Recalculate();
                                newSpec.Items.Add(newItem);
                            }
                        }

                        if (piSpec.OtherCharges != null)
                        {
                            foreach (var piCharge in piSpec.OtherCharges)
                            {
                                var newCharge = new JobOrderOtherCharge
                                {
                                    Name = piCharge.Name ?? $"Charge {newSpec.OtherCharges.Count + 1}",
                                    Type = piCharge.Type ?? "lm",
                                    Value = piCharge.Value,
                                    Rate = piCharge.Rate,
                                    Amount = piCharge.Amount,
                                    TargetsAllSpecs = true
                                };
                                newSpec.OtherCharges.Add(newCharge);
                            }
                        }

                        newSpec.CalculateTotals();
                        Specifications.Add(newSpec);
                    }
                }
                else
                {
                    var emptySpec = new JobOrderSpecification { Id = 1, SpecificationName = "Specification 1" };
                    SubscribeToSpecChanges(emptySpec);
                    Specifications.Add(emptySpec);
                }

                RefreshAllOtherCharges();
                RefreshTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading invoice: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshTotals()
        {
            _totalsNeedRefresh = true;
            EnsureTotalsRefreshed();
        }

        #endregion

        #region Specification Methods

        public void AddItemToSpecification(JobOrderSpecification spec)
        {
            if (spec == null) return;
            int nextSrNo = spec.Items.Count > 0 ? spec.Items.Max(i => i.SrNo) + 1 : 1;
            var newItem = new JobOrderItem { Id = spec.Items.Count + 1, SrNo = nextSrNo, Qty = 1 };
            spec.Items.Add(newItem);
            spec.CalculateTotals();
            InvalidateTotals();
        }

        public void RemoveItemFromSpecification(JobOrderItem item)
        {
            if (item == null) return;
            foreach (var spec in Specifications)
            {
                if (spec.Items.Contains(item))
                {
                    spec.Items.Remove(item);
                    int srNo = 1;
                    foreach (var i in spec.Items) i.SrNo = srNo++;
                    spec.CalculateTotals();
                    break;
                }
            }
            InvalidateTotals();
        }

        private void ExecuteAddItem(object parameter)
        {
            if (Specifications.Count > 0)
                AddItemToSpecification(Specifications[Specifications.Count - 1]);
        }

        public void AddSpecification()
        {
            var newSpec = new JobOrderSpecification
            {
                Id = Specifications.Count + 1,
                SpecificationName = $"Specification {Specifications.Count + 1}",
                ModuleType = "SGU",
                WorkType = "Annealed",
                SurchargePercent = 20
            };
            SubscribeToSpecChanges(newSpec);
            Specifications.Add(newSpec);
            InvalidateTotals();
        }

        public void RemoveSpecification()
        {
            if (Specifications.Count > 1)
            {
                Specifications.RemoveAt(Specifications.Count - 1);
                InvalidateTotals();
            }
        }

        public void RenumberAllItems()
        {
            int globalSrNo = 1;
            foreach (var spec in Specifications)
                foreach (var item in spec.Items)
                    item.SrNo = globalSrNo++;
        }

        #endregion

        #region Other Charges

        private void ExecuteAddOtherCharge(object parameter)
        {
            var targetSpec = parameter as JobOrderSpecification
                          ?? (Specifications.Count > 0 ? Specifications[Specifications.Count - 1] : null);
            if (targetSpec != null)
            {
                var newCharge = new JobOrderOtherCharge
                {
                    Name = $"Charge {targetSpec.OtherCharges.Count + 1}",
                    Type = "lm",
                    Value = 0,
                    Rate = 0,
                    Amount = 0,
                    TargetsAllSpecs = true
                };
                targetSpec.OtherCharges.Add(newCharge);
                targetSpec.CalculateTotals();
                RefreshAllOtherCharges();
                InvalidateTotals();
            }
        }

        private void ExecuteDeleteOtherCharge(object parameter)
        {
            if (parameter is JobOrderOtherCharge charge)
            {
                foreach (var spec in Specifications)
                    if (spec.OtherCharges.Remove(charge)) { spec.CalculateTotals(); break; }
                RefreshAllOtherCharges();
                InvalidateTotals();
            }
        }

        public void RefreshAllOtherCharges()
        {
            _allOtherCharges.Clear();
            foreach (var spec in Specifications)
                foreach (var charge in spec.OtherCharges)
                    _allOtherCharges.Add(charge);
            OnPropertyChanged(nameof(AllOtherCharges));
        }

        #endregion

        #region Save & Delete

        private async Task SaveJobOrderAsync()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => IsSaving = true);

                if (string.IsNullOrWhiteSpace(JobOrderNumber))
                    JobOrderNumber = await Task.Run(() => DbHelper.GenerateNextJONumber());

                RenumberAllItems();

                var jobOrderModel = new JobOrderModel
                {
                    JONumber = JobOrderNumber,
                    ProformaInvoiceId = _proformaInvoiceId,
                    ClientName = ClientName,
                    ClientTRN = ClientTRN,
                    ClientAddress = ClientAddress,
                    ClientReference = ClientReference,
                    Salesman = Salesman,
                    ContactPerson = AttentionName,
                    ContactNumber = ContactNo,
                    ProjectName = ProjectName,
                    ProjectNo = ProjectNo,            // ✅ ADD THIS
                    ProjectLocation = ProjectLocation,
                    LPONumber = LPONo,
                    JODate = JobOrderDate,
                    RequiredDate = RequiredDate,
                    Status = Status,
                    Notes = Notes,
                    TotalQty = TotalQty,
                    TotalAmount = TotalAmount
                };

                // Serialize specifications
                try
                {
                    var specsToSave = Specifications.Select(spec => new
                    {
                        spec.Id,
                        spec.SpecificationName,
                        spec.ModuleType,
                        spec.WorkType,
                        spec.OuterThickness,
                        spec.OuterColor,
                        spec.InnerThickness,
                        spec.InnerColor,
                        spec.SpacerThickness,
                        spec.PVBThickness,
                        spec.PVBColor,
                        spec.IncludeInSpec,
                        spec.BasePrice,
                        spec.SurchargePercent,
                        Items = spec.Items.Select(item => new
                        {
                            item.Id,
                            item.SrNo,
                            item.GlassRef,
                            item.Width1,
                            item.Height1,
                            item.Width2,
                            item.Height2,
                            item.Qty,
                            item.DeliveredQty,
                            item.Price
                        }).ToList(),
                        OtherCharges = spec.OtherCharges.Select(charge => new
                        {
                            charge.Name,
                            charge.Type,
                            charge.Value,
                            charge.Rate,
                            charge.Amount,
                            charge.LinkedSpecIndices,
                            charge.LinkedSpecIndex,
                            charge.TargetsAllSpecs,
                            charge.LmDimType,
                            charge.IsManualOverride
                        }).ToList()
                    }).ToList();

                    jobOrderModel.SpecificationsJson = JsonConvert.SerializeObject(specsToSave);
                }
                catch
                {
                    jobOrderModel.SpecificationsJson = "";
                }

                // Copy items
                int srNo = 1;
                foreach (var spec in Specifications)
                {
                    foreach (var item in spec.Items)
                    {
                        var itemModel = new JobOrderItemModel
                        {
                            SrNo = srNo++,
                            GlassRef = item.GlassRef ?? "",
                            Width = item.Width1,
                            Height = item.Height1,
                            OrderedQty = item.Qty,
                            ReleasedQty = item.DeliveredQty,
                            BalanceQty = item.RemainingQty,
                            Price = item.Price,
                            TotalAmount = item.TotalAmount
                        };
                        jobOrderModel.Items.Add(itemModel);
                    }
                }

                var saveModel = new JobOrder
                {
                    Id = jobOrderModel.Id,
                    JobNumber = jobOrderModel.JONumber,
                    PINumber = jobOrderModel.PINumber,
                    ClientName = jobOrderModel.ClientName,
                    ClientTRN = jobOrderModel.ClientTRN,
                    ClientAddress = jobOrderModel.ClientAddress,
                    ClientReference = jobOrderModel.ClientReference,
                    Salesman = jobOrderModel.Salesman,
                    ProjectNo = jobOrderModel.ProjectNo,
                    ProjectName = jobOrderModel.ProjectName,
                    ProjectLocation = jobOrderModel.ProjectLocation,
                    LPONumber = jobOrderModel.LPONumber,
                    Date = jobOrderModel.JODate,
                    RequiredDate = jobOrderModel.RequiredDate,
                    Status = jobOrderModel.Status,
                    Notes = jobOrderModel.Notes,
                    TotalQty = jobOrderModel.TotalQty,
                    ReleasedQty = jobOrderModel.ReleasedQty,
                    BalanceQty = jobOrderModel.BalanceQty,
                    TotalAmount = jobOrderModel.TotalAmount,
                    SpecificationsJson = jobOrderModel.SpecificationsJson
                };

                await Task.Run(() => DbHelper.SaveJobOrder(saveModel));

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var savedJob = new JobOrder
                    {
                        Id = jobOrderModel.Id,
                        JobNumber = jobOrderModel.JONumber,
                        ClientName = jobOrderModel.ClientName,
                        ClientTRN = jobOrderModel.ClientTRN,
                        ClientAddress = jobOrderModel.ClientAddress,
                        ProjectName = jobOrderModel.ProjectName,
                        ProjectLocation = jobOrderModel.ProjectLocation,
                        LPONumber = jobOrderModel.LPONumber,
                        Date = jobOrderModel.JODate,
                        RequiredDate = jobOrderModel.RequiredDate,
                        Status = jobOrderModel.Status,
                        Notes = jobOrderModel.Notes,
                        TotalQty = jobOrderModel.TotalQty,
                        TotalAmount = jobOrderModel.TotalAmount,
                        SpecificationsJson = jobOrderModel.SpecificationsJson
                    };

                    var existing = JobOrders.FirstOrDefault(x => x.Id == savedJob.Id);

                    if (existing != null)
                    {
                        existing.JobNumber = savedJob.JobNumber;
                        existing.PINumber = savedJob.PINumber;
                        existing.ClientName = savedJob.ClientName;
                        existing.ClientTRN = savedJob.ClientTRN;
                        existing.ClientAddress = savedJob.ClientAddress;
                        existing.ClientReference = savedJob.ClientReference;
                        existing.Salesman = savedJob.Salesman;
                        existing.ProjectNo = savedJob.ProjectNo;
                        existing.ProjectName = savedJob.ProjectName;
                        existing.ProjectLocation = savedJob.ProjectLocation;
                        existing.LPONumber = savedJob.LPONumber;
                        existing.Date = savedJob.Date;
                        existing.RequiredDate = savedJob.RequiredDate;
                        existing.Status = savedJob.Status;
                        existing.Notes = savedJob.Notes;
                        existing.TotalQty = savedJob.TotalQty;
                        existing.TotalAmount = savedJob.TotalAmount;
                        existing.SpecificationsJson = savedJob.SpecificationsJson;
                    }
                    else
                    {
                        JobOrders.Add(savedJob);
                    }

                    MessageBox.Show(
                        $"Job Order {JobOrderNumber} saved!\n\nDate: {JobOrderDate:dd MMM yyyy hh:mm:ss tt}\nTotal Items: {TotalQty}\nTotal SQM: {TotalSQM:F4}\nTotal Amount: AED {TotalAmount:N2}",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show($"Error saving: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
            finally
            {
                Application.Current.Dispatcher.Invoke(() => IsSaving = false);
            }
        }

        public void ClearAll(bool notifyUI = true)
        {
            JobOrderNumber = "";
            ClientName = "";
            ClientTRN = "";
            ClientReference = "";
            Salesman = "";
            ClientAddress = "";
            ProjectName = "";
            ProjectNo = "";
            ProjectLocation = "";
            LPONo = "";
            AttentionName = "";
            ContactNo = "";
            Notes = "";
            PINumber = "";
            ProformaInvoiceId = 0;
            Status = "Pending";
            JobOrderDate = DateTime.Now;
            RequiredDate = DateTime.Today.AddDays(7);

            Specifications.Clear();
            JobOrders.Clear();
            var newSpec = new JobOrderSpecification { Id = 1, SpecificationName = "Specification 1" };
            SubscribeToSpecChanges(newSpec);
            Specifications.Add(newSpec);

            _allOtherCharges.Clear();
            InvalidateTotals();

            if (notifyUI) OnPropertyChanged("");
        }

        private void DeleteJobOrder()
        {
            var result = MessageBox.Show(
                $"Delete Job Order {JobOrderNumber}?\n\nThis will clear all data.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes) ClearAll();
        }

        public JobOrder GetJobOrderById(int id) => JobOrders.FirstOrDefault(j => j.Id == id);

        #endregion

        #region Load From Existing

        public void LoadFromExistingJobOrder(ProGlassAutomation.Data.Database.JobOrderModel jo)
        {
            if (jo == null) return;

            try
            {
                ClearAll(notifyUI: false);

                JobOrderNumber = jo.JONumber ?? "";
                JobOrderDate = jo.JODate == DateTime.MinValue ? DateTime.Now : jo.JODate;
                RequiredDate = jo.RequiredDate == DateTime.MinValue ? DateTime.Today.AddDays(7) : jo.RequiredDate;
                Status = jo.Status ?? "Pending";
                PINumber = jo.PINumber ?? "";
                ClientName = jo.ClientName ?? "";
                ClientTRN = jo.ClientTRN ?? "";
                ClientAddress = jo.ClientAddress ?? "";
                ClientReference = jo.ClientReference ?? "";
                Salesman = jo.Salesman ?? "";
                ProjectName = jo.ProjectName ?? "";
                ProjectNo = jo.ProjectNo ?? "";            // ✅ ADD THIS
                ProjectLocation = jo.ProjectLocation ?? "";
                LPONo = jo.LPONumber ?? "";
                Notes = jo.Notes ?? "";
                ProformaInvoiceId = jo.ProformaInvoiceId;

                Specifications.Clear();

                if (!string.IsNullOrWhiteSpace(jo.SpecificationsJson) && jo.SpecificationsJson.Length > 10)
                {
                    try
                    {
                        var jsonSpecs = JsonConvert.DeserializeObject<List<JsonSpecModel>>(jo.SpecificationsJson);

                        if (jsonSpecs != null && jsonSpecs.Count > 0)
                        {
                            int specIndex = 0;
                            foreach (var js in jsonSpecs)
                            {
                                specIndex++;
                                var spec = new JobOrderSpecification
                                {
                                    Id = js.Id > 0 ? js.Id : specIndex,
                                    SpecificationName = string.IsNullOrWhiteSpace(js.SpecificationName) ? $"Specification {specIndex}" : js.SpecificationName,
                                    ModuleType = js.ModuleType ?? "SGU",
                                    WorkType = js.WorkType ?? "Annealed",
                                    OuterThickness = js.OuterThickness ?? "6mm",
                                    OuterColor = js.OuterColor ?? "Clear",
                                    InnerThickness = js.InnerThickness ?? "6mm",
                                    InnerColor = js.InnerColor ?? "Clear",
                                    SpacerThickness = js.SpacerThickness ?? "12mm",
                                    PVBThickness = js.PVBThickness ?? "0.76mm",
                                    PVBColor = js.PVBColor ?? "Clear",
                                    IncludeInSpec = js.IncludeInSpec,
                                    BasePrice = js.BasePrice,
                                    SurchargePercent = js.SurchargePercent
                                };

                                SubscribeToSpecChanges(spec);

                                if (js.Items != null)
                                {
                                    int itemIndex = 0;
                                    foreach (var ji in js.Items)
                                    {
                                        itemIndex++;
                                        var item = new JobOrderItem
                                        {
                                            Id = ji.Id > 0 ? ji.Id : itemIndex,
                                            SrNo = ji.SrNo > 0 ? ji.SrNo : itemIndex,
                                            GlassRef = ji.GlassRef ?? "",
                                            Width1 = ji.Width1,
                                            Height1 = ji.Height1,
                                            Width2 = ji.Width2,
                                            Height2 = ji.Height2,
                                            Qty = ji.Qty > 0 ? ji.Qty : 1,
                                            DeliveredQty = ji.DeliveredQty,
                                            Price = ji.Price
                                        };
                                        item.Recalculate();
                                        spec.Items.Add(item);
                                    }
                                }

                                if (js.OtherCharges != null)
                                {
                                    foreach (var jc in js.OtherCharges)
                                    {
                                        var charge = new JobOrderOtherCharge
                                        {
                                            Name = jc.Name ?? "Charge",
                                            Type = jc.Type ?? "lm",
                                            Value = jc.Value,
                                            Rate = jc.Rate,
                                            Amount = jc.Amount,
                                            TargetsAllSpecs = jc.TargetsAllSpecs,
                                            LinkedSpecIndices = jc.LinkedSpecIndices ?? "",
                                            LinkedSpecIndex = jc.LinkedSpecIndex,
                                            LmDimType = jc.LmDimType ?? "w1h1",
                                            IsManualOverride = jc.IsManualOverride
                                        };
                                        spec.OtherCharges.Add(charge);
                                    }
                                }

                                spec.CalculateTotals();
                                Specifications.Add(spec);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[JobOrderVM] JSON parse error: {ex.Message}");
                    }
                }

                // If no specifications from JSON, load from JobOrderModel.Items
                if (Specifications.Count == 0)
                {
                    var emptySpec = new JobOrderSpecification { Id = 1, SpecificationName = "Specification 1" };
                    SubscribeToSpecChanges(emptySpec);

                    // Load items from JobOrderModel.Items
                    if (jo.Items != null && jo.Items.Count > 0)
                    {
                        int itemIndex = 0;
                        foreach (var dbItem in jo.Items)
                        {
                            itemIndex++;
                            var item = new JobOrderItem
                            {
                                Id = dbItem.Id > 0 ? dbItem.Id : itemIndex,
                                SrNo = dbItem.SrNo > 0 ? dbItem.SrNo : itemIndex,
                                GlassRef = dbItem.GlassRef ?? "",
                                Width1 = dbItem.Width,
                                Height1 = dbItem.Height,
                                Qty = dbItem.OrderedQty > 0 ? dbItem.OrderedQty : 1,
                                DeliveredQty = dbItem.ReleasedQty,
                                Price = dbItem.Price
                            };
                            item.Recalculate();
                            emptySpec.Items.Add(item);
                        }
                        emptySpec.CalculateTotals();
                    }

                    Specifications.Add(emptySpec);
                }

                RefreshAllOtherCharges();
                RefreshTotals();
                OnPropertyChanged("");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Job Order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ClearForNewJobOrder()
        {
            ClearAll();
            JobOrderNumber = DbHelper.GenerateNextJONumber();
            JobOrderDate = DateTime.Now;
            RequiredDate = DateTime.Today.AddDays(7);
            Status = "Pending";
            PINumber = "";
            ProformaInvoiceId = 0;
            CompanyName = "PROGLASS AUTOMATION";
            CompanyTRN = "100458979400003";
            CompanyLocation = "Dubai, UAE";
            OnPropertyChanged("");
        }

        #endregion

        #region JSON Helper Classes

        public class JsonSpecModel
        {
            public int Id { get; set; }
            public string SpecificationName { get; set; }
            public string ModuleType { get; set; }
            public string WorkType { get; set; }
            public string OuterThickness { get; set; }
            public string OuterColor { get; set; }
            public string InnerThickness { get; set; }
            public string InnerColor { get; set; }
            public string SpacerThickness { get; set; }
            public string PVBThickness { get; set; }
            public string PVBColor { get; set; }
            public bool IncludeInSpec { get; set; } = true;
            public double BasePrice { get; set; }
            public double SurchargePercent { get; set; }
            public List<JsonItemModel> Items { get; set; }
            public List<JsonChargeModel> OtherCharges { get; set; }
        }

        public class JsonItemModel
        {
            public int Id { get; set; }
            public int SrNo { get; set; }
            public string GlassRef { get; set; }
            public double Width1 { get; set; }
            public double Height1 { get; set; }
            public double Width2 { get; set; }
            public double Height2 { get; set; }
            public int Qty { get; set; }
            public int DeliveredQty { get; set; }
            public double Price { get; set; }
        }

        public class JsonChargeModel
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public double Value { get; set; }
            public double Rate { get; set; }
            public double Amount { get; set; }
            public string LinkedSpecIndices { get; set; }
            public int LinkedSpecIndex { get; set; }
            public bool TargetsAllSpecs { get; set; }
            public string LmDimType { get; set; }
            public bool IsManualOverride { get; set; }
        }

        #endregion
    }
}