using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class SheetStoreView : UserControl
    {
        public SheetStoreView()
        {
            InitializeComponent();

            // Cleanup ViewModel service event subscription when view is unloaded
            Unloaded += SheetStoreView_Unloaded;
        }

        private void SheetStoreView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SheetStoreViewModel viewModel)
            {
                viewModel.Cleanup();
            }

            Unloaded -= SheetStoreView_Unloaded;
        }
    }
}