using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation.Views.DGU
{
    public partial class DguView : UserControl
    {
        private readonly DguViewModel _vm;

        public DguView()
        {
            InitializeComponent();

            _vm = new DguViewModel();
            this.DataContext = _vm;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _vm.Save();

            MessageBox.Show(
                $"Saved Successfully\nResult: {_vm.Result}",
                "DGU System",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            _vm.Calc();
        }
    }
}