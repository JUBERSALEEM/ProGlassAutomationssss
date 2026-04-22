using System.Windows.Controls;

namespace ProGlassAutomation.Views.DGULamination
{
    public partial class DGULaminationView : UserControl
    {
        public DGULaminationView()
        {
            InitializeComponent();

            // 🔥 CRITICAL FIX (THIS WAS MISSING = BLANK PAGE ISSUE)
            DataContext = new DGULaminationViewModel();
        }
    }
}