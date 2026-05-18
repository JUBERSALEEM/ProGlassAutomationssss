using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation.Views.ProformaInvoice
{
    public partial class ProformaInvoiceView : UserControl
    {
        private ProformaInvoiceViewModel _viewModel;

        public ProformaInvoiceView()
        {
            InitializeComponent();
            _viewModel = new ProformaInvoiceViewModel();
            DataContext = _viewModel;
            Loaded += ProformaInvoiceView_Loaded;
        }

        private void ProformaInvoiceView_Loaded(object sender, RoutedEventArgs e)
        {
            // Set print area reference after visual tree is loaded
            _viewModel.InvoicePrintArea = this.MainContentBorder;
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SpecificationModel spec)
            {
                _viewModel.AddItem(spec);
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is InvoiceItemModel item)
            {
                _viewModel.RemoveItem(item);
            }
        }
    }
}