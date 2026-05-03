using System;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class SheetDialog : Window
    {
        public Sheet NewSheet { get; private set; }
        public bool IsEditMode { get; set; }
        public int EditId { get; set; }

        public SheetDialog()
        {
            InitializeComponent();
            LoadPredefinedData();
            SetupDefaults();
        }

        public SheetDialog(Sheet sheet) : this()
        {
            LoadSheet(sheet);
        }

        private void LoadPredefinedData()
        {
            // Categories (150+)
            foreach (string c in Sheet.Categories)
                CategoryComboBox.Items.Add(c);
            CategoryComboBox.SelectedIndex = 0;

            // Thicknesses (10)
            foreach (string t in Sheet.Thicknesses)
                ThicknessComboBox.Items.Add(t);
            ThicknessComboBox.SelectedIndex = 2; // 4mm

            // Colors (18)
            ColorComboBox.ItemsSource = Sheet.ColorItems;

            // Sizes (100+)
            foreach (var size in Sheet.StandardSizes)
            {
                if (!WidthComboBox.Items.Contains(size.Width.ToString()))
                    WidthComboBox.Items.Add(size.Width.ToString());
                if (!HeightComboBox.Items.Contains(size.Height.ToString()))
                    HeightComboBox.Items.Add(size.Height.ToString());
            }
            WidthComboBox.SelectedIndex = 0;
            HeightComboBox.SelectedIndex = 0;
        }

        private void SetupDefaults()
        {
            DatePicker.SelectedDate = DateTime.Now;
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss");
            ColorComboBox.SelectedIndex = 0;

            TotalStockText.TextChanged += delegate { UpdateBalance(); };
            UsedSheetsText.TextChanged += delegate { UpdateBalance(); };
            WidthComboBox.SelectionChanged += delegate { UpdateSqm(); };
            HeightComboBox.SelectionChanged += delegate { UpdateSqm(); };
        }

        private void LoadSheet(Sheet sheet)
        {
            if (sheet == null) return;

            IsEditMode = true;
            EditId = sheet.Id;
            HeaderText.Text = "EDIT SHEET";

            // Category
            for (int i = 0; i < CategoryComboBox.Items.Count; i++)
            {
                if (CategoryComboBox.Items[i].ToString() == sheet.Category)
                {
                    CategoryComboBox.SelectedIndex = i;
                    break;
                }
            }

            // Thickness
            for (int i = 0; i < ThicknessComboBox.Items.Count; i++)
            {
                if (ThicknessComboBox.Items[i].ToString() == sheet.Thickness)
                {
                    ThicknessComboBox.SelectedIndex = i;
                    break;
                }
            }

            // Color
            for (int i = 0; i < Sheet.ColorItems.Count; i++)
            {
                if (Sheet.ColorItems[i].Name == sheet.Color)
                {
                    ColorComboBox.SelectedIndex = i;
                    break;
                }
            }

            // Width
            for (int i = 0; i < WidthComboBox.Items.Count; i++)
            {
                if (WidthComboBox.Items[i].ToString() == sheet.Width.ToString())
                {
                    WidthComboBox.SelectedIndex = i;
                    break;
                }
            }

            // Height
            for (int i = 0; i < HeightComboBox.Items.Count; i++)
            {
                if (HeightComboBox.Items[i].ToString() == sheet.Height.ToString())
                {
                    HeightComboBox.SelectedIndex = i;
                    break;
                }
            }

            // Other fields
            PurchasePriceText.Text = sheet.PurchasePrice.ToString();
            SellPriceText.Text = sheet.SellPrice.ToString();
            TotalStockText.Text = sheet.TotalStock.ToString();
            UsedSheetsText.Text = sheet.UsedSheets.ToString();
            SupplierText.Text = sheet.Supplier ?? "";
            DescriptionText.Text = sheet.Description ?? "";
            DatePicker.SelectedDate = sheet.CreatedDate;
            TimeText.Text = sheet.CreatedDate.ToString("HH:mm:ss");

            UpdateBalance();
            UpdateSqm();
        }

        private void UpdateSqm()
        {
            int w = 0, h = 0;
            int.TryParse(WidthComboBox.SelectedItem?.ToString(), out w);
            int.TryParse(HeightComboBox.SelectedItem?.ToString(), out h);
            if (w > 0 && h > 0)
                SqmText.Text = Math.Round(w * h / 1000000.0, 2).ToString("N2");
        }

        private void UpdateBalance()
        {
            int total = 0, used = 0;
            int.TryParse(TotalStockText.Text, out total);
            int.TryParse(UsedSheetsText.Text, out used);
            BalanceText.Text = (total - used).ToString();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ThicknessComboBox.Text))
                {
                    MessageBox.Show("Thickness is required!");
                    return;
                }

                // Get color
                string colorName = "Clear";
                string colorHex = "#E8F4F8";
                if (ColorComboBox.SelectedItem is GlassColorItem selectedColor)
                {
                    colorName = selectedColor.Name;
                    colorHex = selectedColor.Hex;
                }

                // Get dimensions
                int width = 0, height = 0;
                int.TryParse(WidthComboBox.SelectedItem?.ToString(), out width);
                int.TryParse(HeightComboBox.SelectedItem?.ToString(), out height);

                // Parse values
                int stock = 0, used = 0;
                decimal purchase = 0, sell = 0;
                int.TryParse(TotalStockText.Text, out stock);
                int.TryParse(UsedSheetsText.Text, out used);
                decimal.TryParse(PurchasePriceText.Text, out purchase);
                decimal.TryParse(SellPriceText.Text, out sell);

                // Calculate SQM
                double sqm = (width > 0 && height > 0) ? Math.Round(width * height / 1000000.0, 2) : 0;

                // Date
                DateTime date = DatePicker.SelectedDate ?? DateTime.Now;
                if (TimeSpan.TryParse(TimeText.Text, out TimeSpan time))
                    date = date.Date + time;

                NewSheet = new Sheet
                {
                    Id = IsEditMode ? EditId : 0,
                    SrNo = 0,
                    Category = CategoryComboBox.SelectedItem?.ToString() ?? "HD Clear",
                    Thickness = ThicknessComboBox.SelectedItem?.ToString() ?? "4mm",
                    Color = colorName,
                    ColorHex = colorHex,
                    Width = width,
                    Height = height,
                    SquareMeter = sqm,
                    PurchasePrice = purchase,
                    SellPrice = sell,
                    TotalStock = stock,
                    UsedSheets = used,
                    BalanceSheets = stock - used,
                    IsActive = true,
                    Supplier = SupplierText.Text,
                    SupplierName = SupplierText.Text,
                    Description = DescriptionText.Text,
                    CreatedDate = date,
                    LatestPurchaseDate = date
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}