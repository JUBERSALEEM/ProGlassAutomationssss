using System.Windows.Controls;
using System.Windows;

namespace ProGlassAutomation.Views.GlassOptimization
{
    public partial class GlassOptimizationView : UserControl
    {
        public GlassOptimizationView() => InitializeComponent();
        private void SelectAll(object s, RoutedEventArgs e) { if (s is TextBox t) t.SelectAll(); }
    }
}