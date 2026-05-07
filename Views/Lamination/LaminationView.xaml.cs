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

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }
    }
}