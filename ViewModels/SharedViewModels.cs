using System;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation
{
    public static class SharedViewModels
    {
        // ==================== Proforma Invoice (Single) ====================

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

        // ==================== Proforma Invoice List ====================

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

        // ==================== Job Order ====================

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

        // ==================== Events ====================

        public static event Action? InvoiceListRefreshRequested;

        public static void RequestInvoiceListRefresh()
        {
            InvoiceListRefreshRequested?.Invoke();
        }
    }
}