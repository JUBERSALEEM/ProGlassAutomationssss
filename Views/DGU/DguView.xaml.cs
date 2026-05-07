// DguView.xaml.cs
using System.Windows;
using System.Windows.Controls;

namespace ProGlassAutomation.Views.DGU
{
    public partial class DguView : UserControl
    {
        public DguView()
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