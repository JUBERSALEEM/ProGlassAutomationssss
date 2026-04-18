using System.Windows.Controls;
using ProGlassAutomation.ViewModels.SGU;

namespace ProGlassAutomation.Views.SGU
{
    public partial class SguView : UserControl
    {
        private SguViewModel vm;

        public SguView()
        {
            InitializeComponent();
            vm = new SguViewModel();
            DataContext = vm;
        }

        private void Save_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            vm.Save();
        }
    }
}