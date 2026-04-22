using System.Windows;
using System.Windows.Controls;

namespace ProGlassAutomation.Views.SGU
{
    public partial class SguView : UserControl
    {
        public SguView()
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