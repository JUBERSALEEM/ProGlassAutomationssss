using System.Windows;
using System.Windows.Controls;

namespace ProGlassAutomation.Views.Lamination
{
    public partial class LaminationView : UserControl
    {
        public LaminationView()
        {
            InitializeComponent();
        }

        private void SelectAllText_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }
    }
}