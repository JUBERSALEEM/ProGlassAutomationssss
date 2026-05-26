using System;

namespace ProGlassAutomation.ViewModels
{
    public static class SharedViewModels
    {
        private static ProformaInvoiceViewModel? _proformaInvoiceVM;

        public static ProformaInvoiceViewModel ProformaInvoiceVM
        {
            get
            {
                if (_proformaInvoiceVM == null)
                {
                    _proformaInvoiceVM = new ProformaInvoiceViewModel();
                    System.Diagnostics.Debug.WriteLine("[Shared] Created new ProformaInvoiceViewModel");
                }
                return _proformaInvoiceVM;
            }
        }

        public static void Reset()
        {
            _proformaInvoiceVM = null;
        }
    }
}