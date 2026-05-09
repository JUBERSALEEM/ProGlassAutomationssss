using System.Windows.Controls;
using System.Windows.Input;
using ProGlassAutomation.ViewModels;

namespace ProGlassAutomation.Views
{
    public partial class DailyWorksView : UserControl
    {
        public DailyWorksView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is DailyWorksViewModel vm && vm.SelectedDataRowView != null)
            {
                vm.EditCommand.Execute(vm.SelectedDataRowView);
            }
        }
    }
}