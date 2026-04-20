using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Views.Lamination;

namespace ProGlassAutomation.Views.Lamination
{
    public partial class LaminationView : UserControl
    {
        public LaminationView()
        {
            InitializeComponent();

            // ONLY ONE CONSTRUCTOR — NO DUPLICATION
            this.DataContext = new LaminationViewModel();
        }
    }
}