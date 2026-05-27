using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProGlassAutomation.ViewModels;

// Use alias to avoid conflict with JobOrder folder namespace
using JO = ProGlassAutomation.Models.JobOrder;

namespace ProGlassAutomation.Views
{
    public partial class JobOrderListView : UserControl
    {
        public JobOrderListView()
        {
            InitializeComponent();

            // Ensure DataContext is set
            if (DataContext == null)
            {
                DataContext = new JobOrderListViewModel();
            }
        }

        private JobOrderListViewModel VM => DataContext as JobOrderListViewModel;

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (VM?.SelectedJobOrder != null)
            {
                VM.OpenJobOrderCommand.Execute(VM.SelectedJobOrder);
            }
        }
    }
}