using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ProGlassAutomation.ViewModels
{
    public class JobOrderViewModel : ViewModelBase
    {
        private string _jobOrderNumber = "";
        private string _customerName = "";
        private string _customerTRN = "";
        private string _customerReference = "";
        private string _salesman = "";
        private string _customerAddress = "";
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
        private ObservableCollection<JobOrderSpecification> _specifications;

        public ObservableCollection<JobOrder> JobOrders { get; set; } = new ObservableCollection<JobOrder>();

        public JobOrderViewModel()
        {
            _specifications = new ObservableCollection<JobOrderSpecification>();
            Specifications.Add(new JobOrderSpecification { Id = 1, SpecificationName = "Specification 1" });

            SaveJobOrderCommand = new RelayCommand(_ => SaveJobOrder());
            AddItemCommand = new RelayCommand(_ => ExecuteAddItem(null));
            DeleteJobOrderCommand = new RelayCommand(_ => DeleteJobOrder());
            AddSpecificationCommand = new RelayCommand(_ => AddSpecification());
            RemoveSpecificationCommand = new RelayCommand(_ => RemoveSpecification());

            AddOtherChargeCommand = new RelayCommand(o => ExecuteAddOtherCharge(o));
            DeleteOtherChargeCommand = new RelayCommand(o => ExecuteDeleteOtherCharge(o));
        }

        #region INotifyPropertyChanged Support

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

        #region Properties

        public string JobOrderNumber
        {
            get => _jobOrderNumber;
            set => SetProperty(ref _jobOrderNumber, value);
        }

        public string CustomerName
        {
            get => _customerName;
            set => SetProperty(ref _customerName, value);
        }

        public string CustomerTRN
        {
            get => _customerTRN;
            set => SetProperty(ref _customerTRN, value);
        }

        public string CustomerReference
        {
            get => _customerReference;
            set => SetProperty(ref _customerReference, value);
        }

        public string Salesman
        {
            get => _salesman;
            set => SetProperty(ref _salesman, value);
        }

        public string CustomerAddress
        {
            get => _customerAddress;
            set => SetProperty(ref _customerAddress, value);
        }

        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        public string ProjectNo
        {
            get => _projectNo;
            set => SetProperty(ref _projectNo, value);
        }

        public string ProjectLocation
        {
            get => _projectLocation;
            set => SetProperty(ref _projectLocation, value);
        }

        public string LPONo
        {
            get => _lpoNo;
            set => SetProperty(ref _lpoNo, value);
        }

        public string AttentionName
        {
            get => _attentionName;
            set => SetProperty(ref _attentionName, value);
        }

        public string ContactNo
        {
            get => _contactNo;
            set => SetProperty(ref _contactNo, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string PINumber
        {
            get => _piNumber;
            set => SetProperty(ref _piNumber, value);
        }

        public int ProformaInvoiceId
        {
            get => _proformaInvoiceId;
            set => SetProperty(ref _proformaInvoiceId, value);
        }

        public DateTime JobOrderDate
        {
            get => _jobOrderDate;
            set => SetProperty(ref _jobOrderDate, value);
        }

        public DateTime RequiredDate
        {
            get => _requiredDate;
            set => SetProperty(ref _requiredDate, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _companyName = "PROGLASS AUTOMATION";
        public string CompanyName
        {
            get => _companyName;
            set => SetProperty(ref _companyName, value);
        }

        private string _companyTRN = "100458979400003";
        public string CompanyTRN
        {
            get => _companyTRN;
            set => SetProperty(ref _companyTRN, value);
        }

        private string _companyLocation = "Dubai, UAE";
        public string CompanyLocation
        {
            get => _companyLocation;
            set => SetProperty(ref _companyLocation, value);
        }

        public ObservableCollection<JobOrderSpecification> Specifications
        {
            get => _specifications;
            set => SetProperty(ref _specifications, value);
        }

        public int TotalQty => Specifications.Sum(s => s.TotalQty);
        public double TotalSQM => Specifications.Sum(s => s.TotalSQM);
        public double TotalAmount => Specifications.Sum(s => s.TotalAmount);
        public double TotalLM1 => Specifications.Sum(s => s.TotalLM1);
        public double TotalLM2 => Specifications.Sum(s => s.TotalLM2);

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

        #region Other Charges Methods

        private void ExecuteAddOtherCharge(object parameter)
        {
            JobOrderSpecification targetSpec = parameter as JobOrderSpecification;

            if (targetSpec == null && Specifications.Count > 0)
                targetSpec = Specifications[Specifications.Count - 1];

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
                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(AllOtherCharges));
                OnPropertyChanged(nameof(AllOtherChargesTotal));
            }
        }

        private void ExecuteDeleteOtherCharge(object parameter)
        {
            if (parameter is JobOrderOtherCharge charge)
            {
                foreach (var spec in Specifications)
                {
                    if (spec.OtherCharges.Contains(charge))
                    {
                        spec.OtherCharges.Remove(charge);
                        spec.CalculateTotals();
                        OnPropertyChanged(nameof(TotalAmount));
                        OnPropertyChanged(nameof(AllOtherCharges));
                        OnPropertyChanged(nameof(AllOtherChargesTotal));
                        break;
                    }
                }
            }
        }

        private ObservableCollection<JobOrderOtherCharge> _allOtherCharges = new ObservableCollection<JobOrderOtherCharge>();

        public ObservableCollection<JobOrderOtherCharge> AllOtherCharges
        {
            get => _allOtherCharges;
        }

        public void RefreshAllOtherCharges()
        {
            _allOtherCharges.Clear();
            foreach (var spec in Specifications)
            {
                foreach (var charge in spec.OtherCharges)
                {
                    _allOtherCharges.Add(charge);
                }
            }
            OnPropertyChanged(nameof(AllOtherCharges));
            OnPropertyChanged(nameof(AllOtherChargesTotal));
        }

        public double AllOtherChargesTotal => _allOtherCharges.Sum(c => c.Amount);

        #endregion

        #region Import from Proforma Invoice

        public void LoadFromProformaInvoice(Models.ProformaInvoiceModel pi)
        {
            System.Diagnostics.Debug.WriteLine("[JobOrderVM] LoadFromProformaInvoice START");

            if (pi == null)
            {
                System.Diagnostics.Debug.WriteLine("[JobOrderVM] pi is NULL");
                MessageBox.Show("Invoice is null!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                CompanyName = pi.CompanyName ?? "PROGLASS AUTOMATION";
                CompanyTRN = pi.CompanyTRN ?? "100458979400003";
                CompanyLocation = pi.CompanyLocation ?? "Dubai, UAE";

                CustomerName = pi.CustomerName ?? "";
                CustomerTRN = pi.CustomerTRN ?? "";
                CustomerReference = pi.CustomerReference ?? "";
                Salesman = pi.Salesman ?? "";
                CustomerAddress = pi.CustomerAddress ?? "";
                ProjectName = pi.ProjectName ?? "";
                ProjectNo = pi.ProjectNo ?? "";
                ProjectLocation = pi.ProjectLocation ?? "";
                LPONo = pi.LPONo ?? "";
                AttentionName = pi.AttentionName ?? "";
                ContactNo = pi.ContactNo ?? "";
                PINumber = pi.InvoiceNo ?? "";
                _proformaInvoiceId = pi.Id; // Store for saving
                Notes = pi.Notes ?? "";
                Status = "Pending";

                JobOrderNumber = DbHelper.GenerateNextJONumber();
                JobOrderDate = DateTime.Now;
                RequiredDate = DateTime.Today.AddDays(7);

                Specifications.Clear();

                if (pi.Specifications != null && pi.Specifications.Count > 0)
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

                        if (piSpec.Items != null && piSpec.Items.Count > 0)
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
                            newSpec.CalculateTotals();
                        }

                        // PATCH: Import other charges from PI
                        if (piSpec.OtherCharges != null && piSpec.OtherCharges.Count > 0)
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
                                    TargetsAllSpecs = true,
                                    LinkedSpecIndices = piCharge.LinkedSpecIndices ?? "",
                                    LinkedSpecIndex = piCharge.LinkedSpecIndex,
                                    LmDimType = piCharge.LmDimType ?? "w1h1",
                                    IsManualOverride = piCharge.IsManualOverride
                                };
                                newSpec.OtherCharges.Add(newCharge);
                            }
                            newSpec.CalculateTotals();
                        }

                        Specifications.Add(newSpec);
                    }
                }
                else
                {
                    Specifications.Add(new JobOrderSpecification { Id = 1, SpecificationName = "Specification 1" });
                }

                // PATCH: Refresh aggregated collections
                RefreshAllOtherCharges();

                NotifyAllPropertiesChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading invoice: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Specification Methods

        public void AddItemToSpecification(JobOrderSpecification spec)
        {
            if (spec == null) return;

            int nextSrNo = 1;
            if (spec.Items.Count > 0)
            {
                nextSrNo = spec.Items.Max(i => i.SrNo) + 1;
            }

            spec.Items.Add(new JobOrderItem
            {
                Id = spec.Items.Count + 1,
                SrNo = nextSrNo,
                Qty = 1
            });

            spec.CalculateTotals();
            NotifyTotalsChanged();
        }

        public void RemoveItemFromSpecification(JobOrderItem item)
        {
            if (item == null) return;

            foreach (var spec in Specifications)
            {
                if (spec.Items.Contains(item))
                {
                    spec.Items.Remove(item);

                    for (int i = 0; i < spec.Items.Count; i++)
                    {
                        spec.Items[i].SrNo = i + 1;
                    }

                    spec.CalculateTotals();
                    break;
                }
            }

            NotifyTotalsChanged();
        }

        private void ExecuteAddItem(object parameter)
        {
            if (Specifications.Count > 0)
            {
                AddItemToSpecification(Specifications[Specifications.Count - 1]);
            }
        }

        public void AddSpecification()
        {
            Specifications.Add(new JobOrderSpecification
            {
                Id = Specifications.Count + 1,
                SpecificationName = $"Specification {Specifications.Count + 1}",
                ModuleType = "SGU",
                WorkType = "Annealed",
                SurchargePercent = 20
            });

            NotifyTotalsChanged();
        }

        public void RemoveSpecification()
        {
            if (Specifications.Count > 1)
            {
                Specifications.RemoveAt(Specifications.Count - 1);
                NotifyTotalsChanged();
            }
        }

        public void RenumberAllItems()
        {
            int globalSrNo = 1;
            foreach (var spec in Specifications)
            {
                foreach (var item in spec.Items)
                {
                    item.SrNo = globalSrNo;
                    globalSrNo++;
                }
            }
        }

        private void NotifyTotalsChanged()
        {
            OnPropertyChanged(nameof(TotalQty));
            OnPropertyChanged(nameof(TotalSQM));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(TotalLM1));
            OnPropertyChanged(nameof(TotalLM2));
            OnPropertyChanged(nameof(AllOtherCharges));
            OnPropertyChanged(nameof(AllOtherChargesTotal));
        }

        private void NotifyAllPropertiesChanged()
        {
            OnPropertyChanged(nameof(CompanyName));
            OnPropertyChanged(nameof(CompanyTRN));
            OnPropertyChanged(nameof(CompanyLocation));
            OnPropertyChanged(nameof(JobOrderNumber));
            OnPropertyChanged(nameof(JobOrderDate));
            OnPropertyChanged(nameof(RequiredDate));
            OnPropertyChanged(nameof(CustomerName));
            OnPropertyChanged(nameof(CustomerTRN));
            OnPropertyChanged(nameof(CustomerAddress));
            OnPropertyChanged(nameof(ProjectName));
            OnPropertyChanged(nameof(ProjectLocation));
            OnPropertyChanged(nameof(LPONo));
            OnPropertyChanged(nameof(AttentionName));
            OnPropertyChanged(nameof(ContactNo));
            OnPropertyChanged(nameof(PINumber));
            OnPropertyChanged(nameof(Notes));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(Specifications));
            OnPropertyChanged(nameof(TotalQty));
            OnPropertyChanged(nameof(TotalSQM));
            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(TotalLM1));
            OnPropertyChanged(nameof(TotalLM2));
            OnPropertyChanged(nameof(AllOtherCharges));
            OnPropertyChanged(nameof(AllOtherChargesTotal));
        }

        #endregion

        #region Save & Delete

        private void SaveJobOrder()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(JobOrderNumber))
                    JobOrderNumber = DbHelper.GenerateNextJONumber();

                RenumberAllItems();

                var jobOrderModel = new JobOrderModel
                {
                    JONumber = JobOrderNumber,
                    ProformaInvoiceId = _proformaInvoiceId,
                    ClientName = CustomerName,
                    ClientTRN = CustomerTRN,
                    ClientAddress = CustomerAddress,
                    ContactPerson = AttentionName,
                    ContactNumber = ContactNo,
                    ProjectName = ProjectName,
                    ProjectLocation = ProjectLocation,
                    LPONumber = LPONo,
                    JODate = JobOrderDate,
                    RequiredDate = RequiredDate,
                    Status = Status,
                    Notes = Notes,
                    TotalQty = TotalQty,
                    TotalAmount = TotalAmount
                };

                // Serialize specifications to JSON (PATCH: Include all charge properties)
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
                    System.Diagnostics.Debug.WriteLine($"[JobOrderVM] JSON Length: {jobOrderModel.SpecificationsJson.Length}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[JobOrderVM] JSON Serialize Error: {ex.Message}");
                    jobOrderModel.SpecificationsJson = "";
                }

                // Copy items to database model
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

                // Save to database
                DbHelper.SaveJobOrder(jobOrderModel);

                // Create UI model for local collection
                var savedJob = new JobOrder
                {
                    Id = jobOrderModel.Id,
                    JobNumber = jobOrderModel.JONumber,
                    CustomerName = jobOrderModel.ClientName,
                    CustomerTRN = jobOrderModel.ClientTRN,
                    CustomerAddress = jobOrderModel.ClientAddress,
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

                JobOrders.Add(savedJob);

                MessageBox.Show($"Job Order {JobOrderNumber} saved!\n\nDate: {JobOrderDate:dd MMM yyyy hh:mm:ss tt}\nTotal Items: {TotalQty}\nTotal SQM: {TotalSQM:F4}\nTotal Amount: AED {TotalAmount:N2}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ClearAll(bool notifyUI = true)
        {
            JobOrderNumber = "";
            CustomerName = "";
            CustomerTRN = "";
            CustomerReference = "";
            Salesman = "";
            CustomerAddress = "";
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
            Specifications.Add(new JobOrderSpecification { Id = 1, SpecificationName = "Specification 1" });

            RefreshAllOtherCharges();

            if (notifyUI)
            {
                NotifyAllPropertiesChanged();
            }
        }

        #endregion

        #region Helper Methods

        public JobOrder GetJobOrderById(int id) => JobOrders.FirstOrDefault(j => j.Id == id);

        private void DeleteJobOrder()
        {
            var result = MessageBox.Show($"Delete Job Order {JobOrderNumber}?\n\nThis will clear all data.", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                ClearAll();
            }
        }

        #endregion

        #region Load From Existing Job Order

        public void LoadFromExistingJobOrder(JobOrder jo)
        {
            if (jo == null)
            {
                System.Diagnostics.Debug.WriteLine("[JobOrderVM] LoadFromExistingJobOrder: jo is NULL");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[JobOrderVM] LoadFromExistingJobOrder: {jo.JobNumber}");
            System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Date: {jo.Date}");
            System.Diagnostics.Debug.WriteLine($"[JobOrderVM] SpecificationsJson Length: {jo.SpecificationsJson?.Length ?? 0}");

            try
            {
                // Clear all fields first (don't notify UI yet)
                ClearAll(notifyUI: false);

                // Load basic info
                JobOrderNumber = jo.JobNumber ?? "";
                JobOrderDate = jo.Date == DateTime.MinValue ? DateTime.Now : jo.Date;
                RequiredDate = jo.RequiredDate == DateTime.MinValue ? DateTime.Today.AddDays(7) : jo.RequiredDate;
                Status = jo.Status ?? "Pending";
                PINumber = jo.PINumber ?? "";

                // Load customer info
                CustomerName = jo.CustomerName ?? "";
                CustomerTRN = jo.CustomerTRN ?? "";
                CustomerAddress = jo.CustomerAddress ?? "";

                // Load project info
                ProjectName = jo.ProjectName ?? "";
                ProjectLocation = jo.ProjectLocation ?? "";
                LPONo = jo.LPONumber ?? "";

                // Load notes
                Notes = jo.Notes ?? "";

                System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Basic info loaded. Date: {JobOrderDate}");

                // Load specifications from JSON
                Specifications.Clear();

                if (!string.IsNullOrWhiteSpace(jo.SpecificationsJson) && jo.SpecificationsJson.Length > 10)
                {
                    try
                    {
                        var jsonSpecs = JsonConvert.DeserializeObject<List<JsonSpecModel>>(jo.SpecificationsJson);

                        if (jsonSpecs != null && jsonSpecs.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Found {jsonSpecs.Count} specs in JSON");

                            for (int specIndex = 0; specIndex < jsonSpecs.Count; specIndex++)
                            {
                                var js = jsonSpecs[specIndex];

                                var spec = new JobOrderSpecification
                                {
                                    Id = js.Id > 0 ? js.Id : specIndex + 1,
                                    SpecificationName = string.IsNullOrWhiteSpace(js.SpecificationName) ? $"Specification {specIndex + 1}" : js.SpecificationName,
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

                                // Load items for this specification
                                if (js.Items != null && js.Items.Count > 0)
                                {
                                    for (int itemIndex = 0; itemIndex < js.Items.Count; itemIndex++)
                                    {
                                        var ji = js.Items[itemIndex];

                                        var item = new JobOrderItem
                                        {
                                            Id = ji.Id > 0 ? ji.Id : itemIndex + 1,
                                            SrNo = ji.SrNo > 0 ? ji.SrNo : itemIndex + 1,
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

                                // PATCH: Load other charges for this specification
                                if (js.OtherCharges != null && js.OtherCharges.Count > 0)
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

                                // Recalculate spec totals
                                spec.CalculateTotals();

                                // Add spec to collection
                                Specifications.Add(spec);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Error parsing specs JSON: {ex.Message}");
                    }
                }

                // If no specs loaded, create empty spec
                if (Specifications.Count == 0)
                {
                    Specifications.Add(new JobOrderSpecification { Id = 1, SpecificationName = "Specification 1" });
                }

                // PATCH: Refresh aggregated collections
                RefreshAllOtherCharges();

                // Notify ALL property changes for UI refresh
                NotifyAllPropertiesChanged();

                System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Load complete. Date: {JobOrderDate:dd MMM yyyy hh:mm:ss tt}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderVM] LoadFromExistingJobOrder ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Stack: {ex.StackTrace}");
                MessageBox.Show($"Error loading Job Order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Clear For New Job Order

        public void ClearForNewJobOrder()
        {
            ClearAll();

            // Generate new job order number
            JobOrderNumber = DbHelper.GenerateNextJONumber();
            JobOrderDate = DateTime.Now;
            RequiredDate = DateTime.Today.AddDays(7);
            Status = "Pending";
            PINumber = "";
            ProformaInvoiceId = 0;

            // Reset company info
            CompanyName = "PROGLASS AUTOMATION";
            CompanyTRN = "100458979400003";
            CompanyLocation = "Dubai, UAE";

            NotifyAllPropertiesChanged();
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

        // PATCH: Updated JsonChargeModel with all properties
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