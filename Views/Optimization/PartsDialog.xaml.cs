using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class PartsDialog : Window
    {
        private readonly OptimizationViewModel _vm;
        private CutPart? _editing;

        public PartsDialog(OptimizationViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            dgParts.ItemsSource = _vm.Parts;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string label = txtLabel.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(label)) label = "Untitled";

            if (!double.TryParse(txtWidth.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double w) || w <= 0)
            {
                MessageBox.Show("Enter valid width.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!double.TryParse(txtHeight.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double h) || h <= 0)
            {
                MessageBox.Show("Enter valid height.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_editing == null)
            {
                _vm.Parts.Add(new CutPart { L = w, W = h, Qty = 1 });
            }
            else
            {
                _editing.L = w;
                _editing.W = h;
                _editing = null;
                btnAdd.Content = "+ Add Part";
            }

            ClearForm();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CutPart part)
            {
                _editing = part;
                txtLabel.Text = "Part";  // No Label on CutPart; we just update dimensions
                txtWidth.Text = part.L.ToString();
                txtHeight.Text = part.W.ToString();
                btnAdd.Content = "✓ Update";
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CutPart part)
            {
                var r = MessageBox.Show($"Delete part ({part.L}×{part.W})?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes) _vm.Parts.Remove(part);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void ClearForm()
        {
            txtLabel.Text = "Glass Panel";
            txtWidth.Text = "1000";
            txtHeight.Text = "800";
        }
    }
}