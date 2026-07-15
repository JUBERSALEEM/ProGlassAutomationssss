using System;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation
{
    public static class SharedViewModels
    {
        // ==================== Proforma Invoice (Single Editor) ====================
        private static ProformaInvoiceViewModel? _proformaInvoiceVM;
        public static ProformaInvoiceViewModel ProformaInvoiceVM
        {
            get
            {
                if (_proformaInvoiceVM == null)
                    _proformaInvoiceVM = new ProformaInvoiceViewModel();
                return _proformaInvoiceVM;
            }
        }

        // ==================== Proforma Invoice List (Browse/Filter) ====================
        private static ProformaInvoiceListViewModel? _proformaInvoiceListVM;
        public static ProformaInvoiceListViewModel ProformaInvoiceListVM
        {
            get
            {
                if (_proformaInvoiceListVM == null)
                    _proformaInvoiceListVM = new ProformaInvoiceListViewModel();
                return _proformaInvoiceListVM;
            }
        }

        // ==================== Job Order (Single Editor) ====================
        private static JobOrderViewModel? _jobOrderVM;
        public static JobOrderViewModel JobOrderVM
        {
            get
            {
                if (_jobOrderVM == null)
                    _jobOrderVM = new JobOrderViewModel();
                return _jobOrderVM;
            }
        }

        // ✅ NEW: Job Order List (Browse/Filter) - needed for dashboard
        private static JobOrderListViewModel? _jobOrderListVM;
        public static JobOrderListViewModel JobOrderListVM
        {
            get
            {
                if (_jobOrderListVM == null)
                    _jobOrderListVM = new JobOrderListViewModel();
                return _jobOrderListVM;
            }
        }

        // ✅ NEW: Delivery ViewModel - needed for dashboard
        private static DeliveryViewModel? _deliveryVM;
        public static DeliveryViewModel DeliveryVM
        {
            get
            {
                if (_deliveryVM == null)
                    _deliveryVM = new DeliveryViewModel();
                return _deliveryVM;
            }
        }

        // ✅ NEW: Daily Works ViewModel - needed for dashboard
        private static DailyWorksViewModel? _dailyWorksVM;
        public static DailyWorksViewModel DailyWorksVM
        {
            get
            {
                if (_dailyWorksVM == null)
                    _dailyWorksVM = new DailyWorksViewModel();
                return _dailyWorksVM;
            }
        }

        // ==================== Events ====================
        public static event Action? InvoiceListRefreshRequested;
        public static event Action? DataRefreshRequested;

        public static void RequestInvoiceListRefresh()
        {
            InvoiceListRefreshRequested?.Invoke();
            DataRefreshRequested?.Invoke();
        }

        public static void RequestDataRefresh()
        {
            DataRefreshRequested?.Invoke();
        }
    }
}