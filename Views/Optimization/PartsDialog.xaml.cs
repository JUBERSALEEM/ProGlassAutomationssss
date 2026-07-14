using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class PartsDialog : Window
    {
        private readonly OptimizationViewModel _vm;
        private DemandPart? _editing;

        public PartsDialog(OptimizationViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;  // ✅ FIX: so {Binding DemandParts} resolves
            dgParts.ItemsSource = _vm.DemandParts;

            _vm.DemandParts.CollectionChanged += (s, e) => UpdateSummary();
            UpdateSummary();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLabel.Text))
            {
                MessageBox.Show("Please enter a part label.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtLabel.Focus(); return;
            }
            if (!double.TryParse(txtLength.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double l) || l <= 0)
            { MessageBox.Show("Enter a valid length.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); txtLength.Focus(); return; }
            if (!double.TryParse(txtWidth.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double w) || w <= 0)
            { MessageBox.Show("Enter a valid width.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); txtWidth.Focus(); return; }
            if (!int.TryParse(txtQty.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty) || qty <= 0)
            { MessageBox.Show("Enter a valid quantity.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning); txtQty.Focus(); return; }

            if (_editing == null)
            {
                _vm.DemandParts.Add(new DemandPart
                {
                    SrNo = _vm.DemandParts.Count + 1,
                    Label = txtLabel.Text.Trim(),
                    L = l,
                    W = w,
                    Qty = qty
                });
            }
            else
            {
                _editing.Label = txtLabel.Text.Trim();
                _editing.L = l;
                _editing.W = w;
                _editing.Qty = qty;
                _editing = null;
                btnAdd.Content = "+ Add Part";
                btnCancel.Visibility = Visibility.Collapsed;
            }

            ClearForm();
            Reindex();
            UpdateSummary();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DemandPart part)
            {
                _editing = part;
                txtSrNo.Text = part.SrNo.ToString();
                txtLabel.Text = part.Label;
                txtLength.Text = part.L.ToString(CultureInfo.InvariantCulture);
                txtWidth.Text = part.W.ToString(CultureInfo.InvariantCulture);
                txtQty.Text = part.Qty.ToString(CultureInfo.InvariantCulture);
                btnAdd.Content = "✓ Update";
                btnCancel.Visibility = Visibility.Visible;
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DemandPart part)
            {
                var r = MessageBox.Show($"Delete '{part.Label}' (Sr #{part.SrNo})?", "Confirm Delete",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    if (_editing == part)
                    {
                        _editing = null;
                        btnAdd.Content = "+ Add Part";
                        btnCancel.Visibility = Visibility.Collapsed;
                    }
                    _vm.DemandParts.Remove(part);
                    Reindex();
                    UpdateSummary();
                }
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _editing = null;
            btnAdd.Content = "+ Add Part";
            btnCancel.Visibility = Visibility.Collapsed;
            ClearForm();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void ClearForm()
        {
            txtSrNo.Text = "1";
            txtLabel.Text = "Glass-001";
            txtLength.Text = "1000";
            txtWidth.Text = "800";
            txtQty.Text = "1";
        }

        private void Reindex()
        {
            for (int i = 0; i < _vm.DemandParts.Count; i++)
            {
                if (_vm.DemandParts[i].SrNo != i + 1)
                    _vm.DemandParts[i].SrNo = i + 1;
            }
        }

        private void UpdateSummary()
        {
            if (!IsLoaded) return;
            txtPartTypes.Text = _vm.PartTypesCount.ToString("N0");
            txtTotalPieces.Text = _vm.TotalPartsToCut.ToString("N0");
            txtTotalArea.Text = _vm.TotalPartsArea.ToString("F3");
            txtUniqueDims.Text = _vm.UniqueDimensionsCount.ToString("N0");
        }
    }
}