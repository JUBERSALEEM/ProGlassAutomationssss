using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.ViewModels.TaxInvoice
{
    public class TaxInvoiceViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<TaxInvoiceModel> _invoices = new();
        private TaxInvoiceModel _selectedInvoice;
        private TaxInvoiceModel _editingInvoice;
        private bool _isEditing;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<TaxInvoiceModel> Invoices
        {
            get => _invoices;
            set { _invoices = value; OnPropertyChanged(); }
        }

        public TaxInvoiceModel SelectedInvoice
        {
            get => _selectedInvoice;
            set { _selectedInvoice = value; OnPropertyChanged(); }
        }

        public TaxInvoiceModel EditingInvoice
        {
            get => _editingInvoice;
            set { _editingInvoice = value; OnPropertyChanged(); }
        }

        public bool IsEditing
        {
            get => _isEditing;
            set { _isEditing = value; OnPropertyChanged(); }
        }

        public ICommand RefreshCommand { get; }
        public ICommand CreateFromDeliveryCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public TaxInvoiceViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadInvoices());
            CreateFromDeliveryCommand = new RelayCommand(param => CreateFromDelivery(param as ProGlassAutomation.Data.Database.Delivery));
            SaveCommand = new RelayCommand(_ => SaveInvoice());
            CancelCommand = new RelayCommand(_ => { IsEditing = false; EditingInvoice = null; });

            LoadInvoices();
        }

        public void LoadInvoices()
        {
            try
            {
                var list = DbHelper.GetAllTaxInvoices();
                Invoices = new ObservableCollection<TaxInvoiceModel>(list);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading tax invoices: {ex.Message}", "Error");
            }
        }

        public void CreateFromDelivery(ProGlassAutomation.Data.Database.Delivery delivery)
        {
            if (delivery == null) return;

            try
            {
                var items = DbHelper.GetDeliveryItems(delivery.Id);
                var totalDelivered = items.Sum(x => x.DeliveredQty);

                if (totalDelivered <= 0)
                {
                    MessageBox.Show("No items have been delivered yet for this delivery note.", "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var nextInvoiceNumber = DbHelper.GenerateNextTaxInvoiceNumber();

                var newInvoice = new TaxInvoiceModel
                {
                    InvoiceNumber = nextInvoiceNumber,
                    DeliveryOrderId = delivery.Id,
                    ClientName = delivery.Company ?? "",
                    ClientTRN = "",
                    ClientAddress = "",
                    InvoiceDate = DateTime.Today,
                    DueDate = DateTime.Today.AddDays(30),
                    Status = "Pending",
                    PaymentStatus = "Unpaid",
                    VATPercent = 5.0,
                    Notes = $"Tax invoice generated for Delivery Note: {delivery.PINumber}"
                };

                int srNo = 1;
                foreach (var item in items)
                {
                    if (item.DeliveredQty > 0)
                    {
                        var invItem = new TaxInvoiceItemModel
                        {
                            SrNo = srNo++,
                            Description = $"Glass Item Delivery (Delivered Qty: {item.DeliveredQty})",
                            Qty = item.DeliveredQty,
                            UnitPrice = 250.0, // Default base price
                            TotalPrice = item.DeliveredQty * 250.0
                        };
                        newInvoice.Items.Add(invItem);
                    }
                }

                newInvoice.SubTotal = newInvoice.Items.Sum(x => x.TotalPrice);
                newInvoice.VATAmount = Math.Round(newInvoice.SubTotal * (newInvoice.VATPercent / 100.0), 2);
                newInvoice.TotalAmount = newInvoice.SubTotal + newInvoice.VATAmount;
                newInvoice.BalanceAmount = newInvoice.TotalAmount;

                EditingInvoice = newInvoice;
                IsEditing = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating invoice from delivery: {ex.Message}", "Error");
            }
        }

        private void SaveInvoice()
        {
            if (EditingInvoice == null) return;

            try
            {
                EditingInvoice.SubTotal = EditingInvoice.Items.Sum(x => x.TotalPrice);
                EditingInvoice.VATAmount = Math.Round(EditingInvoice.SubTotal * (EditingInvoice.VATPercent / 100.0), 2);
                EditingInvoice.TotalAmount = EditingInvoice.SubTotal + EditingInvoice.VATAmount;
                EditingInvoice.BalanceAmount = EditingInvoice.TotalAmount - EditingInvoice.PaidAmount;

                DbHelper.SaveTaxInvoice(EditingInvoice);
                IsEditing = false;
                EditingInvoice = null;
                LoadInvoices();

                MessageBox.Show("Tax Invoice saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving tax invoice: {ex.Message}", "Error");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
