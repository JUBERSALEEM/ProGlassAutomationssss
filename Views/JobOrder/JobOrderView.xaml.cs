using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.JobOrder
{
    public partial class JobOrderView : UserControl
    {
        public JobOrderView()
        {
            InitializeComponent();
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is JobOrderSpecification spec)
            {
                if (DataContext is ViewModels.JobOrderViewModel vm)
                {
                    vm.AddItemToSpecification(spec);
                }
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is JobOrderItem item)
            {
                if (DataContext is ViewModels.JobOrderViewModel vm)
                {
                    vm.RemoveItemFromSpecification(item);
                }
            }
        }
    }
}