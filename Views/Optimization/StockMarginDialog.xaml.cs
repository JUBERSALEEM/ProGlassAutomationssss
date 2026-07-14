using System;
using System.Windows;
using System.Windows.Controls;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class StockMarginDialog : Window
    {
        public double LM { get; private set; }
        public double RM { get; private set; }
        public double TM { get; private set; }
        public double BM { get; private set; }
        public int SelectedThickness { get; private set; }

        public StockMarginDialog()
        {
            InitializeComponent();
        }

        private void Thickness_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag == null) return;
            if (!int.TryParse(btn.Tag.ToString(), out int thickness)) return;

            if (!OptimizationViewModel.TrimPresets.TryGetValue(thickness, out var preset))
                return;

            txtLM.Text = preset.LM.ToString("0");
            txtRM.Text = preset.RM.ToString("0");
            txtTM.Text = preset.TM.ToString("0");
            txtBM.Text = preset.BM.ToString("0");

            LM = preset.LM;
            RM = preset.RM;
            TM = preset.TM;
            BM = preset.BM;
            SelectedThickness = thickness;

            btnApply.IsEnabled = true;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedThickness <= 0) return;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}