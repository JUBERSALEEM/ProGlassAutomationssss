using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ProGlassAutomation.Models;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation.Views.JobOrder
{
    public partial class JobOrderView : UserControl
    {
        public JobOrderViewModel ViewModel { get; private set; }

        public JobOrderView()
        {
            InitializeComponent();
            Loaded += JobOrderView_Loaded;
        }

        private void JobOrderView_Loaded(object sender, RoutedEventArgs e)
        {
            // Set ViewModel reference
            ViewModel = DataContext as JobOrderViewModel;
        }

        private void AddRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is JobOrderSpecification spec && ViewModel != null)
            {
                ViewModel.AddItemToSpecification(spec);
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is JobOrderItem item && ViewModel != null)
            {
                // Use dispatcher to batch UI updates
                Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    ViewModel.RemoveItemFromSpecification(item);
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }
    }
}