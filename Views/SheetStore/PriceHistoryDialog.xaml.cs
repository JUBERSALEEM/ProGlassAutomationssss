using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;
using ProGlassAutomation.Services;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class PriceHistoryDialog : Window
    {
        public ObservableCollection<PriceHistory> PriceHistory { get; set; }
        public ObservableCollection<SheetSelectItem> Sheets { get; set; }

        public PriceHistoryDialog()
        {
            InitializeComponent();

            PriceHistory = new ObservableCollection<PriceHistory>();
            Sheets = new ObservableCollection<SheetSelectItem>();

            DataContext = this;
            LoadSheets();
        }

        private void LoadSheets()
        {
            var sheets = SheetStoreService.Instance.GetAllSheets();
            Sheets.Clear();
            foreach (var sheet in sheets)
            {
                Sheets.Add(new SheetSelectItem
                {
                    Id = sheet.Id,
                    DisplayName = $"{sheet.Category} - {sheet.Thickness} {sheet.Color}"
                });
            }
            SheetComboBox.ItemsSource = Sheets;

            if (Sheets.Count > 0)
            {
                SheetComboBox.SelectedIndex = 0;
            }
        }

        private void SheetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SheetComboBox.SelectedItem is SheetSelectItem selectedSheet)
            {
                LoadPriceHistory(selectedSheet.Id);
            }
        }

        private void LoadPriceHistory(string sheetId)
        {
            PriceHistory.Clear();
            var history = SheetStoreService.Instance.GetPriceHistory(sheetId);
            foreach (var item in history)
            {
                PriceHistory.Add(item);
            }
            TotalRecordsText.Text = $"{PriceHistory.Count} records";
        }
    }

    // Renamed to avoid conflict
    public class SheetSelectItem
    {
        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
}