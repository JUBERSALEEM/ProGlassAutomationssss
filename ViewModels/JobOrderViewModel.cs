using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;

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
        private DateTime _jobOrderDate = DateTime.Today;
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

            // Other Charges Commands
            AddOtherChargeCommand = new RelayCommand(o => ExecuteAddOtherCharge(o));
            DeleteOtherChargeCommand = new RelayCommand(o => ExecuteDeleteOtherCharge(o));
        }

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

        public string CompanyName { get; set; } = "PROGLASS AUTOMATION";
        public string CompanyTRN { get; set; } = "100458979400003";
        public string CompanyLocation { get; set; } = "Dubai, UAE";

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
                        break;
                    }
                }
            }
        }

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
                // STEP 0: Import Company Info
                CompanyName = pi.CompanyName ?? "PROGLASS AUTOMATION";
                CompanyTRN = pi.CompanyTRN ?? "100458979400003";
                CompanyLocation = pi.CompanyLocation ?? "Dubai, UAE";

                // STEP 1: Copy basic properties
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
                Notes = pi.Notes ?? "";
                Status = "Pending";

                System.Diagnostics.Debug.WriteLine($"[JobOrderVM] CustomerName: {CustomerName}");
                System.Diagnostics.Debug.WriteLine($"[JobOrderVM] ProjectNo: {ProjectNo}");

                // Generate new job order number
                JobOrderNumber = DbHelper.GenerateNextJONumber();
                JobOrderDate = DateTime.Today;
                RequiredDate = DateTime.Today.AddDays(7);

                // STEP 2: Copy specifications and items
                Specifications.Clear();

                if (pi.Specifications != null && pi.Specifications.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Found {pi.Specifications.Count} specifications");

                    int specIndex = 0;
                    foreach (var piSpec in pi.Specifications)
                    {
                        specIndex++;
                        System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Processing spec {specIndex}: {piSpec.SpecificationName}");

                        // Create new JobOrderSpecification
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

                        // Copy items
                        if (piSpec.Items != null && piSpec.Items.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Spec {specIndex} has {piSpec.Items.Count} items");

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

                                // Recalculate SQM after properties are set (object initializer runs after constructor)
                                newItem.Recalculate();

                                newSpec.Items.Add(newItem);

                                System.Diagnostics.Debug.WriteLine($"[JobOrderVM]   Item {itemIndex}: GlassRef={newItem.GlassRef}, W1={newItem.Width1}, H1={newItem.Height1}, Qty={newItem.Qty}, Price={newItem.Price}");
                            }

                            // Calculate totals
                            newSpec.CalculateTotals();
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Spec {specIndex} has NO items");
                        }

                        // Copy Other Charges
                        if (piSpec.OtherCharges != null && piSpec.OtherCharges.Count > 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Spec {specIndex} has {piSpec.OtherCharges.Count} other charges");

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
                                    LinkedSpecIndices = piCharge.LinkedSpecIndices ?? ""
                                };
                                newSpec.OtherCharges.Add(newCharge);
                            }

                            // Recalculate spec totals after adding charges
                            newSpec.CalculateTotals();
                        }

                        Specifications.Add(newSpec);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[JobOrderVM] No specifications found, creating default");
                    Specifications.Add(new JobOrderSpecification { Id = 1, SpecificationName = "Specification 1" });
                }

                System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Loaded {Specifications.Count} specs");
                System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Total items: {Specifications.Sum(s => s.Items.Count)}");
                System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Total other charges: {Specifications.Sum(s => s.OtherCharges.Count)}");
                System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Total Qty: {TotalQty}");
                System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Total SQM: {TotalSQM:F4}");
                System.Diagnostics.Debug.WriteLine($"[JobOrderVM] Total Amount: {TotalAmount:F2}");
                System.Diagnostics.Debug.WriteLine("[JobOrderVM] LoadFromProformaInvoice END");

                // Notify property changes for totals and company info
                OnPropertyChanged(nameof(CompanyName));
                OnPropertyChanged(nameof(CompanyTRN));
                OnPropertyChanged(nameof(CompanyLocation));
                OnPropertyChanged(nameof(TotalQty));
                OnPropertyChanged(nameof(TotalSQM));
                OnPropertyChanged(nameof(TotalAmount));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderVM] ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack: {ex.StackTrace}");
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
            OnPropertyChanged(nameof(TotalQty));
            OnPropertyChanged(nameof(TotalSQM));
            OnPropertyChanged(nameof(TotalAmount));
        }

        // Added this property
        public ObservableCollection<JobOrderOtherCharge> AllOtherCharges
        {
            get
            {
                var all = new ObservableCollection<JobOrderOtherCharge>();
                foreach (var spec in Specifications)
                {
                    foreach (var charge in spec.OtherCharges)
                    {
                        all.Add(charge);
                    }
                }
                return all;
            }
        }

        public double AllOtherChargesTotal => AllOtherCharges.Sum(c => c.Amount);

        public void RemoveItemFromSpecification(JobOrderItem item)
        {
            if (item == null) return;

            foreach (var spec in Specifications)
            {
                if (spec.Items.Contains(item))
                {
                    spec.Items.Remove(item);

                    // Renumber items
                    for (int i = 0; i < spec.Items.Count; i++)
                    {
                        spec.Items[i].SrNo = i + 1;
                    }

                    spec.CalculateTotals();
                    break;
                }
            }

            OnPropertyChanged(nameof(TotalQty));
            OnPropertyChanged(nameof(TotalSQM));
            OnPropertyChanged(nameof(TotalAmount));
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
        }

        public void RemoveSpecification()
        {
            if (Specifications.Count > 1)
            {
                Specifications.RemoveAt(Specifications.Count - 1);
                OnPropertyChanged(nameof(TotalQty));
                OnPropertyChanged(nameof(TotalSQM));
                OnPropertyChanged(nameof(TotalAmount));
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

        #endregion

        #region Save & Delete

        private void SaveJobOrder()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(JobOrderNumber))
                    JobOrderNumber = DbHelper.GenerateNextJONumber();

                // Renumber all items before saving
                RenumberAllItems();

                // Create JobOrderModel (Database model) from ViewModel data
                var jobOrderModel = new JobOrderModel
                {
                    JONumber = JobOrderNumber,
                    ClientName = CustomerName,
                    ProjectName = ProjectName,
                    ProjectLocation = ProjectLocation,
                    JODate = JobOrderDate,
                    RequiredDate = RequiredDate,
                    Status = Status,
                    Notes = Notes,
                    TotalQty = TotalQty,
                    TotalAmount = TotalAmount
                };

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
                    ProjectName = jobOrderModel.ProjectName,
                    ProjectLocation = jobOrderModel.ProjectLocation,
                    Date = jobOrderModel.JODate,
                    RequiredDate = jobOrderModel.RequiredDate,
                    Status = jobOrderModel.Status,
                    Notes = jobOrderModel.Notes,
                    TotalQty = jobOrderModel.TotalQty,
                    TotalAmount = jobOrderModel.TotalAmount
                };

                // Add to local collection
                JobOrders.Add(savedJob);

                MessageBox.Show($"Job Order {JobOrderNumber} saved!\n\nTotal Items: {TotalQty}\nTotal SQM: {TotalSQM:F4}\nTotal Amount: AED {TotalAmount:N2}",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteJobOrder()
        {
            var result = MessageBox.Show($"Delete Job Order {JobOrderNumber}?\n\nThis will clear all data.", "Confirm Delete",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
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
                Status = "Pending";
                Specifications.Clear();
                Specifications.Add(new JobOrderSpecification { Id = 1, SpecificationName = "Specification 1" });

                OnPropertyChanged(nameof(TotalQty));
                OnPropertyChanged(nameof(TotalSQM));
                OnPropertyChanged(nameof(TotalAmount));
            }
        }

        #endregion

        #region Helper Methods

        public JobOrder GetJobOrderById(int id) => JobOrders.FirstOrDefault(j => j.Id == id);

        public void ClearAll()
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
            Status = "Pending";
            JobOrderDate = DateTime.Today;
            RequiredDate = DateTime.Today.AddDays(7);
            Specifications.Clear();
            Specifications.Add(new JobOrderSpecification { Id = 1, SpecificationName = "Specification 1" });

            OnPropertyChanged(nameof(TotalQty));
            OnPropertyChanged(nameof(TotalSQM));
            OnPropertyChanged(nameof(TotalAmount));
        }

        #endregion
    }
}