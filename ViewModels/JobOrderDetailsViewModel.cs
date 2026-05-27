using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.ViewModels
{
    public class JobOrderDetailsViewModel : INotifyPropertyChanged
    {
        private JobOrder _jobOrder;
        private ProformaInvoiceModel _relatedProformaInvoice;
        private bool _isEditing;

        public event PropertyChangedEventHandler PropertyChanged;

        // XAML binds to "CurrentJobOrder"
        public JobOrder CurrentJobOrder
        {
            get => _jobOrder;
            set
            {
                _jobOrder = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanEdit));
                OnPropertyChanged(nameof(CanDelete));
                OnPropertyChanged(nameof(HasJobOrder));
                OnPropertyChanged(nameof(IsPending));
                OnPropertyChanged(nameof(IsInProgress));
                OnPropertyChanged(nameof(IsCompleted));
            }
        }

        public ProformaInvoiceModel RelatedProformaInvoice
        {
            get => _relatedProformaInvoice;
            set
            {
                _relatedProformaInvoice = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasProformaInvoice));
            }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                _isEditing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanEdit));
            }
        }

        public bool CanEdit => !IsEditing && HasJobOrder;
        public bool CanDelete => !IsEditing && HasJobOrder;
        public bool HasJobOrder => _jobOrder != null;
        public bool HasProformaInvoice => _relatedProformaInvoice != null;

        public bool IsPending => CurrentJobOrder?.Status == "Pending";
        public bool IsInProgress => CurrentJobOrder?.Status == "In Progress";
        public bool IsCompleted => CurrentJobOrder?.Status == "Completed";

        public ICommand BackCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ConvertToDeliveryCommand { get; }
        public ICommand ViewProformaInvoiceCommand { get; }

        public JobOrderDetailsViewModel()
        {
            BackCommand = new RelayCommand(ExecuteBack);
            EditCommand = new RelayCommand(_ => IsEditing = true, _ => CanEdit);
            SaveCommand = new RelayCommand(ExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
            DeleteCommand = new RelayCommand(ExecuteDelete, _ => CanDelete);
            PrintCommand = new RelayCommand(ExecutePrint);
            ExportCommand = new RelayCommand(ExecuteExport);
            ConvertToDeliveryCommand = new RelayCommand(ExecuteConvertToDelivery, _ => HasJobOrder);
            ViewProformaInvoiceCommand = new RelayCommand(ExecuteViewProforma, _ => HasProformaInvoice);
        }

        public void LoadJobOrder(JobOrder job, ProformaInvoiceModel pi = null)
        {
            CurrentJobOrder = job;
            RelatedProformaInvoice = pi;
        }

        public void LoadJobOrder(int jobOrderId)
        {
            // Load from shared JobOrderVM collection
            if (Application.Current.MainWindow?.DataContext is MainViewModel mainVM)
            {
                var job = mainVM.JobOrderVM?.GetJobOrderById(jobOrderId);
                if (job != null)
                {
                    CurrentJobOrder = job;
                    return;
                }
            }

            // Fallback: try loading from database
            try
            {
                var dbJobs = Data.Database.DbHelper.GetAllJobOrders();
                var dbJob = dbJobs.FirstOrDefault(j => j.Id == jobOrderId);
                if (dbJob != null)
                {
                    var job = new JobOrder
                    {
                        Id = dbJob.Id,
                        JobNumber = dbJob.JONumber,
                        CustomerName = dbJob.ClientName,
                        ProjectName = dbJob.ProjectName,
                        ProjectLocation = dbJob.ProjectLocation,
                        Date = dbJob.JODate,
                        RequiredDate = dbJob.RequiredDate,
                        Status = dbJob.Status,
                        Notes = dbJob.Notes,
                        TotalQty = dbJob.TotalQty
                    };
                    CurrentJobOrder = job;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[JobOrderDetails] LoadJobOrder Error: {ex.Message}");
            }
        }

        private void ExecuteBack(object _)
        {
            if (Application.Current.MainWindow?.DataContext is MainViewModel mainVM)
            {
                mainVM.ShowJobOrders();
            }
        }

        private void ExecuteEdit(object _)
        {
            IsEditing = true;
        }

        private void ExecuteSave(object _)
        {
            if (CurrentJobOrder == null) return;

            try
            {
                // Save to database
                var dbJob = new Data.Database.JobOrderModel
                {
                    Id = CurrentJobOrder.Id,
                    JONumber = CurrentJobOrder.JobNumber,
                    ClientName = CurrentJobOrder.CustomerName,
                    ProjectName = CurrentJobOrder.ProjectName,
                    ProjectLocation = CurrentJobOrder.ProjectLocation,
                    JODate = CurrentJobOrder.Date,
                    RequiredDate = CurrentJobOrder.RequiredDate,
                    Status = CurrentJobOrder.Status,
                    TotalQty = CurrentJobOrder.TotalQty,
                    Notes = CurrentJobOrder.Notes
                };

                Data.Database.DbHelper.SaveJobOrder(dbJob);

                IsEditing = false;
                MessageBox.Show($"Job Order {CurrentJobOrder.JobNumber} saved!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteCancel(object _)
        {
            IsEditing = false;
        }

        private void ExecuteDelete(object _)
        {
            if (CurrentJobOrder == null) return;

            var result = MessageBox.Show(
                $"Delete Job Order {CurrentJobOrder.JobNumber}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Data.Database.DbHelper.DeleteJobOrder(CurrentJobOrder.Id);
                    MessageBox.Show("Job Order deleted!", "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ExecuteBack(null);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecutePrint(object _)
        {
            if (CurrentJobOrder == null) return;

            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    // Print logic here
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteExport(object _)
        {
            // Export logic
        }

        private void ExecuteConvertToDelivery(object _)
        {
            if (CurrentJobOrder == null)
            {
                MessageBox.Show("Please load a Job Order first.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"Convert Job Order {CurrentJobOrder.JobNumber} to Delivery?",
                "Convert to Delivery",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (Application.Current.MainWindow?.DataContext is MainViewModel mainVM)
                {
                    mainVM.CreateDeliveryFromJobOrder(CurrentJobOrder);
                }
            }
        }

        private void ExecuteViewProforma(object _)
        {
            // Navigate to PI details
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}