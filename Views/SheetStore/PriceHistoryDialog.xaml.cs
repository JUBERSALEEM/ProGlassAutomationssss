using System;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;
using ProGlassAutomation.Services;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class PriceHistoryDialog : Window
    {
        public PriceHistoryDialog()
        {
            InitializeComponent();
            LoadSheets();
        }

        private void LoadSheets()
        {
            var sheets = SheetStoreService.Instance.GetAllSheets();
            SheetComboBox.ItemsSource = sheets;
            if (sheets.Count > 0)
            {
                SheetComboBox.SelectedIndex = 0;
            }
        }

        private void SheetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SheetComboBox.SelectedItem is Sheet sheet)
            {
                var history = SheetStoreService.Instance.GetPurchaseHistory(sheet.Id);
                PriceGrid.ItemsSource = history;
                TotalRecordsText.Text = $"{history.Count} records";
            }
        }
    }
}