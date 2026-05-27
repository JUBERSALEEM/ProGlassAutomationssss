using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;
using ProGlassAutomation.ViewModels;

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
            // Add item to last specification
            if (DataContext is JobOrderViewModel vm && vm.Specifications.Count > 0)
            {
                var spec = vm.Specifications[vm.Specifications.Count - 1];
                spec.Items.Add(new JobOrderItem
                {
                    Id = spec.Items.Count + 1,
                    SrNo = spec.Items.Count + 1,
                    Qty = 1
                });
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is JobOrderItem item)
            {
                if (DataContext is JobOrderViewModel vm)
                {
                    foreach (var spec in vm.Specifications)
                    {
                        if (spec.Items.Contains(item))
                        {
                            spec.Items.Remove(item);
                            // Renumber
                            for (int i = 0; i < spec.Items.Count; i++)
                            {
                                spec.Items[i].SrNo = i + 1;
                            }
                            break;
                        }
                    }
                }
            }
        }
    }
}