using System;
using System.Windows;
using System.Windows.Controls;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.Views.SheetStore
{
    public partial class SheetDialog : Window
    {
        public Sheet NewSheet { get; private set; }
        public bool IsEditMode { get; private set; }
        public int EditId { get; private set; }

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
            // Categories
            var categories = Sheet.Categories;
            if (categories != null && categories.Any())
            {
                CategoryComboBox.Items.Clear();
                foreach (string c in categories)
                    CategoryComboBox.Items.Add(c);
                CategoryComboBox.SelectedIndex = 0;
            }

            // Thicknesses
            var thicknesses = Sheet.Thicknesses;
            if (thicknesses != null && thicknesses.Any())
            {
                ThicknessComboBox.Items.Clear();
                foreach (string t in thicknesses)
                    ThicknessComboBox.Items.Add(t);
                ThicknessComboBox.SelectedIndex = 2;
            }

            // Colors
            var colorItems = Sheet.ColorItems;
            if (colorItems != null && colorItems.Any())
            {
                ColorComboBox.ItemsSource = null;
                ColorComboBox.ItemsSource = colorItems;
                ColorComboBox.SelectedIndex = 0;
            }

            // Sizes
            var sizes = Sheet.StandardSizes;
            if (sizes != null && sizes.Any())
            {
                WidthComboBox.Items.Clear();
                HeightComboBox.Items.Clear();

                foreach (var size in sizes)
                {
                    string widthStr = size.Width.ToString();
                    string heightStr = size.Height.ToString();

                    if (!WidthComboBox.Items.Contains(widthStr))
                        WidthComboBox.Items.Add(widthStr);

                    if (!HeightComboBox.Items.Contains(heightStr))
                        HeightComboBox.Items.Add(heightStr);
                }

                WidthComboBox.SelectedIndex = 0;
                HeightComboBox.SelectedIndex = 0;
            }
        }

        private void SetupDefaults()
        {
            DatePicker.SelectedDate = DateTime.Now;
            TimeText.Text = DateTime.Now.ToString("HH:mm:ss");

            TotalStockText.TextChanged += OnStockChanged;
            UsedSheetsText.TextChanged += OnStockChanged;
            WidthComboBox.SelectionChanged += OnDimensionChanged;
            HeightComboBox.SelectionChanged += OnDimensionChanged;

            UpdateBalance();
            UpdateSqm();
        }

        private void OnStockChanged(object sender, TextChangedEventArgs e)
        {
            UpdateBalance();
        }

        private void OnDimensionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
                UpdateSqm();
        }

        private void LoadSheet(Sheet sheet)
        {
            if (sheet == null)
            {
                HeaderText.Text = "ADD NEW SHEET";
                IsEditMode = false;
                EditId = 0;
                return;
            }

            IsEditMode = true;
            EditId = sheet.Id;
            HeaderText.Text = "EDIT SHEET";

            // Category
            for (int i = 0; i < CategoryComboBox.Items.Count; i++)
            {
                if (CategoryComboBox.Items[i]?.ToString() == sheet.Category)
                {
                    CategoryComboBox.SelectedIndex = i;
                    break;
                }
            }

            // Thickness
            for (int i = 0; i < ThicknessComboBox.Items.Count; i++)
            {
                if (ThicknessComboBox.Items[i]?.ToString() == sheet.Thickness)
                {
                    ThicknessComboBox.SelectedIndex = i;
                    break;
                }
            }

            // Color
            var colorItems = Sheet.ColorItems;
            if (colorItems != null)
            {
                for (int i = 0; i < colorItems.Count; i++)
                {
                    if (colorItems[i].Name == sheet.Color)
                    {
                        ColorComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Width
            for (int i = 0; i < WidthComboBox.Items.Count; i++)
            {
                if (WidthComboBox.Items[i]?.ToString() == sheet.Width.ToString())
                {
                    WidthComboBox.SelectedIndex = i;
                    break;
                }
            }

            // Height
            for (int i = 0; i < HeightComboBox.Items.Count; i++)
            {
                if (HeightComboBox.Items[i]?.ToString() == sheet.Height.ToString())
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

            if (sheet.CreatedDate != DateTime.MinValue)
            {
                DatePicker.SelectedDate = sheet.CreatedDate;
                TimeText.Text = sheet.CreatedDate.ToString("HH:mm:ss");
            }

            UpdateBalance();
            UpdateSqm();
        }

        private void UpdateSqm()
        {
            if (WidthComboBox.SelectedItem == null || HeightComboBox.SelectedItem == null)
            {
                SqmText.Text = "0";
                return;
            }

            int w = 0, h = 0;
            int.TryParse(WidthComboBox.SelectedItem.ToString(), out w);
            int.TryParse(HeightComboBox.SelectedItem.ToString(), out h);

            if (w > 0 && h > 0)
                SqmText.Text = Math.Round(w * h / 1000000.0, 2).ToString("N2");
            else
                SqmText.Text = "0";
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
                // Validation
                if (string.IsNullOrWhiteSpace(ThicknessComboBox.Text))
                {
                    MessageBox.Show("Thickness is required!", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                if (WidthComboBox.SelectedItem != null)
                    int.TryParse(WidthComboBox.SelectedItem.ToString(), out width);
                if (HeightComboBox.SelectedItem != null)
                    int.TryParse(HeightComboBox.SelectedItem.ToString(), out height);

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

                // Get selected values
                string category = CategoryComboBox.SelectedItem?.ToString() ?? "HD Clear";
                string thickness = ThicknessComboBox.SelectedItem?.ToString() ?? "4mm";

                // Create new Sheet
                NewSheet = new Sheet
                {
                    Id = IsEditMode ? EditId : 0,
                    SrNo = 0,
                    Category = category,
                    Thickness = thickness,
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
                    Supplier = SupplierText.Text ?? "",
                    SupplierName = SupplierText.Text ?? "",
                    Description = DescriptionText.Text ?? "",
                    CreatedDate = date,
                    LatestPurchaseDate = date
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving sheet: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}