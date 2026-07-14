using ProGlassAutomation.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ProGlassAutomation.Views.Optimization
{
    public partial class OptimizationView : UserControl
    {
        private readonly OptimizationViewModel _vm;
        private int _currentSheetIndex = 0;

        // Edit-state tracking for inline add/edit forms
        private StockSheetViewModel? _editingStock;
        private DemandPart? _editingPart;

        // Cached button styles
        private Style? _ghostButtonStyle;
        private Style? _primaryButtonStyle;

        public OptimizationView()
        {
            InitializeComponent();
            _vm = new OptimizationViewModel();
            DataContext = _vm;

            // Cache styles after visual tree is built
            Loaded += (s, e) =>
            {
                _ghostButtonStyle = (Style)FindResource("GhostButton");
                _primaryButtonStyle = (Style)FindResource("PrimaryButton");
                DrawLayoutCanvas();
            };

            Loaded += OptimizationView_Loaded;
        }

        private void OptimizationView_Loaded(object sender, RoutedEventArgs e)
        {
            SwitchTab("Overview");
        }

        // ══════════════════════════════════════════════════════
        // TAB SWITCH
        // ══════════════════════════════════════════════════════
        private void SwitchTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tabName)
            {
                SwitchTab(tabName);
            }
        }

        private void SwitchTab(string tabName)
        {
            // Hide all tabs
            tabOverview.Visibility = Visibility.Collapsed;
            tabStock.Visibility = Visibility.Collapsed;
            tabParts.Visibility = Visibility.Collapsed;
            tabSettings.Visibility = Visibility.Collapsed;
            tabLayout.Visibility = Visibility.Collapsed;
            tabReport.Visibility = Visibility.Collapsed;

            // Reset all tab button styles
            ResetTabButton(btnTabOverview);
            ResetTabButton(btnTabStock);
            ResetTabButton(btnTabParts);
            ResetTabButton(btnTabSettings);
            ResetTabButton(btnTabLayout);
            ResetTabButton(btnTabReport);

            // Show selected tab + highlight its button
            switch (tabName)
            {
                case "Overview":
                    tabOverview.Visibility = Visibility.Visible;
                    SetActiveTabButton(btnTabOverview);
                    break;
                case "Stock":
                    tabStock.Visibility = Visibility.Visible;
                    SetActiveTabButton(btnTabStock);
                    break;
                case "Parts":
                    tabParts.Visibility = Visibility.Visible;
                    SetActiveTabButton(btnTabParts);
                    break;
                case "Settings":
                    tabSettings.Visibility = Visibility.Visible;
                    SetActiveTabButton(btnTabSettings);
                    break;
                case "Layout":
                    tabLayout.Visibility = Visibility.Visible;
                    SetActiveTabButton(btnTabLayout);
                    DrawLayoutCanvas();
                    SyncLayoutGridSelection();
                    break;
                case "Report":
                    tabReport.Visibility = Visibility.Visible;
                    SetActiveTabButton(btnTabReport);
                    PopulateFullTextReport();
                    SwitchSubTab("Breakdown");
                    break;
            }
        }

        private void ResetTabButton(Button btn)
        {
            if (btn == null || _ghostButtonStyle == null) return;
            btn.Style = _ghostButtonStyle;
        }

        private void SetActiveTabButton(Button btn)
        {
            if (btn == null || _primaryButtonStyle == null) return;
            btn.Style = _primaryButtonStyle;
        }

        // ══════════════════════════════════════════════════════
        // STOCK TAB — INLINE ADD/EDIT/DELETE
        // ══════════════════════════════════════════════════════
        private StockSheetViewModel? _editingStockSheet;

        private void StockAdd_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseStockForm(out string? error)) { ShowError(error!); return; }
            var stock = BuildStockFromForm();
            if (_editingStockSheet == null)
            {
                _vm.StockSheets.Add(stock);
            }
            else
            {
                var s = _editingStockSheet;
                s.Name = stock.Name;
                s.L = stock.L;
                s.W = stock.W;
                s.Qty = stock.Qty;
                s.PricePerM2 = stock.PricePerM2;
                s.LM = stock.LM;
                s.RM = stock.RM;
                s.TM = stock.TM;
                s.BM = stock.BM;
                _editingStockSheet = null;
                btnStockAdd.Content = "+ Add Stock";
                btnStockCancel.Visibility = Visibility.Collapsed;
            }
            ClearStockForm();
            ReindexStock();
        }

        private void StockEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is StockSheetViewModel s)
            {
                _editingStockSheet = s;
                txtStockName.Text = s.Name;
                txtStockW.Text = s.L.ToString(CultureInfo.InvariantCulture);
                txtStockH.Text = s.W.ToString(CultureInfo.InvariantCulture);
                txtStockQty.Text = s.Qty.ToString(CultureInfo.InvariantCulture);
                txtStockPrice.Text = s.PricePerM2 > 0 ? s.PricePerM2.ToString(CultureInfo.InvariantCulture) : "";
                txtStockLM.Text = s.LM.ToString(CultureInfo.InvariantCulture);
                txtStockRM.Text = s.RM.ToString(CultureInfo.InvariantCulture);
                txtStockTM.Text = s.TM.ToString(CultureInfo.InvariantCulture);
                txtStockBM.Text = s.BM.ToString(CultureInfo.InvariantCulture);
                btnStockAdd.Content = "✓ Update";
                btnStockCancel.Visibility = Visibility.Visible;
            }
        }

        private void StockDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is StockSheetViewModel s)
            {
                var r = MessageBox.Show($"Delete '{s.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    if (_editingStockSheet == s) { _editingStockSheet = null; btnStockAdd.Content = "+ Add Stock"; btnStockCancel.Visibility = Visibility.Collapsed; }
                    _vm.StockSheets.Remove(s);
                    ReindexStock();
                }
            }
        }

        private void StockCancel_Click(object sender, RoutedEventArgs e)
        {
            _editingStockSheet = null;
            btnStockAdd.Content = "+ Add Stock";
            btnStockCancel.Visibility = Visibility.Collapsed;
            ClearStockForm();
        }

        private bool TryParseStockForm(out string? error)
        {
            if (string.IsNullOrWhiteSpace(txtStockName.Text)) { error = "Enter a sheet name."; return false; }
            if (!double.TryParse(txtStockW.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double w) || w <= 0) { error = "Enter valid width."; return false; }
            if (!double.TryParse(txtStockH.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double h) || h <= 0) { error = "Enter valid height."; return false; }
            if (!int.TryParse(txtStockQty.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty) || qty < 0) { error = "Enter valid quantity."; return false; }
            error = null;
            return true;
        }

        private StockSheetViewModel BuildStockFromForm()
        {
            double.TryParse(txtStockPrice.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double price);
            double.TryParse(txtStockLM.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double lm);
            double.TryParse(txtStockRM.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double rm);
            double.TryParse(txtStockTM.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double tm);
            double.TryParse(txtStockBM.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double bm);
            return new StockSheetViewModel
            {
                Index = _vm.StockSheets.Count + 1,
                Name = txtStockName.Text.Trim(),
                L = double.Parse(txtStockW.Text, CultureInfo.InvariantCulture),
                W = double.Parse(txtStockH.Text, CultureInfo.InvariantCulture),
                Qty = int.Parse(txtStockQty.Text, CultureInfo.InvariantCulture),
                PricePerM2 = price,
                LM = lm,
                RM = rm,
                TM = tm,
                BM = bm
            };
        }

        private void ClearStockForm()
        {
            txtStockName.Text = "Standard Sheet";
            txtStockW.Text = "3210";
            txtStockH.Text = "2250";
            txtStockQty.Text = "100";
            txtStockPrice.Text = "25";
            txtStockLM.Text = "15";
            txtStockRM.Text = "15";
            txtStockTM.Text = "15";
            txtStockBM.Text = "15";
        }

        private void ReindexStock()
        {
            for (int i = 0; i < _vm.StockSheets.Count; i++)
                _vm.StockSheets[i].Index = i + 1;
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        // ══════════════════════════════════════════════════════
        // PARTS TAB — INLINE ADD/EDIT/DELETE
        // ══════════════════════════════════════════════════════
        private void PartAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPartLabel.Text)) { ShowError("Enter a part label."); return; }
            if (!double.TryParse(txtPartL.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double l) || l <= 0) { ShowError("Enter valid length."); return; }
            if (!double.TryParse(txtPartW.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double w) || w <= 0) { ShowError("Enter valid width."); return; }
            if (!int.TryParse(txtPartQty.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int qty) || qty <= 0) { ShowError("Enter valid quantity."); return; }

            if (_editingPart == null)
            {
                _vm.DemandParts.Add(new DemandPart
                {
                    SrNo = _vm.DemandParts.Count + 1,
                    Label = txtPartLabel.Text.Trim(),
                    L = l,
                    W = w,
                    Qty = qty
                });
            }
            else
            {
                var p = _editingPart;
                p.Label = txtPartLabel.Text.Trim();
                p.L = l; p.W = w; p.Qty = qty;
                _editingPart = null;
                btnPartAdd.Content = "+ Add Part";
                btnPartCancel.Visibility = Visibility.Collapsed;
            }
            ClearPartForm();
        }

        private void PartEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DemandPart p)
            {
                _editingPart = p;
                txtPartSr.Text = p.SrNo.ToString();
                txtPartLabel.Text = p.Label;
                txtPartL.Text = p.L.ToString(CultureInfo.InvariantCulture);
                txtPartW.Text = p.W.ToString(CultureInfo.InvariantCulture);
                txtPartQty.Text = p.Qty.ToString(CultureInfo.InvariantCulture);
                btnPartAdd.Content = "✓ Update";
                btnPartCancel.Visibility = Visibility.Visible;
            }
        }

        private void PartDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DemandPart p)
            {
                var r = MessageBox.Show($"Delete '{p.Label}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    if (_editingPart == p) { _editingPart = null; btnPartAdd.Content = "+ Add Part"; btnPartCancel.Visibility = Visibility.Collapsed; }
                    _vm.DemandParts.Remove(p);
                    ReindexParts();
                }
            }
        }

        private void PartCancel_Click(object sender, RoutedEventArgs e)
        {
            _editingPart = null;
            btnPartAdd.Content = "+ Add Part";
            btnPartCancel.Visibility = Visibility.Collapsed;
            ClearPartForm();
        }

        private void ClearPartForm()
        {
            txtPartSr.Text = "1";
            txtPartLabel.Text = "Glass-001";
            txtPartL.Text = "1000";
            txtPartW.Text = "800";
            txtPartQty.Text = "1";
        }

        private void ReindexParts()
        {
            for (int i = 0; i < _vm.DemandParts.Count; i++)
                _vm.DemandParts[i].SrNo = i + 1;
        }

        // ══════════════════════════════════════════════════════
        // SETTINGS TAB — STOCK MARGINS PICKER (small dialog OK)
        // ══════════════════════════════════════════════════════
        private void OpenStockMargins_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new StockMarginDialog { Owner = Window.GetWindow(this) };
                if (dlg.ShowDialog() == true)
                {
                    txtBreakLeft.Text = dlg.LM.ToString("0", CultureInfo.InvariantCulture);
                    txtBreakRight.Text = dlg.RM.ToString("0", CultureInfo.InvariantCulture);
                    txtBreakTop.Text = dlg.TM.ToString("0", CultureInfo.InvariantCulture);
                    txtBreakBottom.Text = dlg.BM.ToString("0", CultureInfo.InvariantCulture);
                    txtBreakMin.Text = Math.Min(dlg.LM, Math.Min(dlg.RM, Math.Min(dlg.TM, dlg.BM)))
                                          .ToString("0", CultureInfo.InvariantCulture);
                    MessageBox.Show($"Applied {dlg.SelectedThickness} mm preset:\nLM={dlg.LM} RM={dlg.RM} TM={dlg.TM} BM={dlg.BM} mm",
                        "Stock Margins Applied", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OptimizationView] OpenStockMargins: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════
        // 2D LAYOUT TAB — INLINE CANVAS (NO POPUP)
        // ══════════════════════════════════════════════════════
        private void LayoutCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawLayoutCanvas();

        private void LayoutPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.LastSheets == null || _vm.LastSheets.Count == 0) return;
            _currentSheetIndex = Math.Max(0, _currentSheetIndex - 1);
            DrawLayoutCanvas();
            SyncLayoutGridSelection();
        }

        private void LayoutNext_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.LastSheets == null || _vm.LastSheets.Count == 0) return;
            _currentSheetIndex = Math.Min(_vm.LastSheets.Count - 1, _currentSheetIndex + 1);
            DrawLayoutCanvas();
            SyncLayoutGridSelection();
        }

        private void SyncLayoutGridSelection()
        {
            if (dgReportBreakdownInline == null || _vm.Layouts == null) return;
            if (_currentSheetIndex < 0 || _currentSheetIndex >= _vm.Layouts.Count) return;
            int targetSheetNum = _currentSheetIndex + 1;
            for (int i = 0; i < _vm.Layouts.Count; i++)
            {
                if (_vm.Layouts[i].SheetNum == targetSheetNum)
                {
                    dgReportBreakdownInline.SelectedIndex = i;
                    dgReportBreakdownInline.ScrollIntoView(_vm.Layouts[i]);
                    break;
                }
            }
        }

        private void DrawLayoutCanvas()
        {
            if (LayoutCanvas == null) return;
            LayoutCanvas.Children.Clear();

            if (_vm.LastSheets == null || _vm.LastSheets.Count == 0)
            {
                txtLayoutSheetIndicator.Text = "Sheet 0 / 0";
                return;
            }

            if (_currentSheetIndex >= _vm.LastSheets.Count) _currentSheetIndex = 0;
            var sheet = _vm.LastSheets[_currentSheetIndex];

            txtLayoutSheetIndicator.Text = $"Sheet {_currentSheetIndex + 1} / {_vm.LastSheets.Count}";

            double cw = LayoutCanvas.ActualWidth > 0 ? LayoutCanvas.ActualWidth - 40 : 1200;
            double ch = LayoutCanvas.ActualHeight > 0 ? LayoutCanvas.ActualHeight - 40 : 600;
            if (sheet.StockWidth <= 0 || sheet.StockHeight <= 0) return;

            double scale = Math.Min(cw / sheet.StockWidth, ch / sheet.StockHeight);
            if (scale <= 0) scale = 0.1;
            double sw = sheet.StockWidth * scale;
            double sh = sheet.StockHeight * scale;
            double ox = (LayoutCanvas.ActualWidth - sw) / 2;
            double oy = 20;

            // Stock border
            var border = new Rectangle
            {
                Width = sw,
                Height = sh,
                Stroke = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)),
                StrokeThickness = 2,
                Fill = Brushes.White
            };
            Canvas.SetLeft(border, ox);
            Canvas.SetTop(border, oy);
            LayoutCanvas.Children.Add(border);

            // Width label
            var wt = new TextBlock
            {
                Text = $"{sheet.StockWidth:N0} mm (Width)",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80))
            };
            Canvas.SetLeft(wt, ox + sw / 2 - 60);
            Canvas.SetTop(wt, oy - 22);
            LayoutCanvas.Children.Add(wt);

            // Height label
            var ht = new TextBlock
            {
                Text = $"{sheet.StockHeight:N0} mm (Height)",
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)),
                RenderTransform = new RotateTransform(-90)
            };
            Canvas.SetLeft(ht, ox - 30);
            Canvas.SetTop(ht, oy + sh / 2 + 50);
            LayoutCanvas.Children.Add(ht);

            // Placed parts
            var partFill = new SolidColorBrush(Color.FromArgb(220, 0xA8, 0x55, 0xF7));
            var partStroke = new SolidColorBrush(Color.FromRgb(0x93, 0x4B, 0xE0));

            if (sheet.PlacedParts != null)
            {
                foreach (var p in sheet.PlacedParts)
                {
                    if (p.L <= 0 || p.W <= 0) continue;
                    var r = new Rectangle
                    {
                        Width = p.L * scale,
                        Height = p.W * scale,
                        Fill = partFill,
                        Stroke = partStroke,
                        StrokeThickness = 1.5
                    };
                    Canvas.SetLeft(r, ox + p.X * scale);
                    Canvas.SetTop(r, oy + p.Y * scale);
                    LayoutCanvas.Children.Add(r);

                    var lbl = new TextBlock
                    {
                        Text = $"{p.L:N0}×{p.W:N0}{(p.Rotated ? "\n↻" : "")}",
                        FontSize = 11,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White,
                        TextAlignment = TextAlignment.Center
                    };
                    Canvas.SetLeft(lbl, ox + p.X * scale);
                    Canvas.SetTop(lbl, oy + p.Y * scale);
                    lbl.Width = p.L * scale;
                    lbl.Height = p.W * scale;
                    lbl.Padding = new Thickness(4);
                    LayoutCanvas.Children.Add(lbl);
                }
            }
        }

        // ══════════════════════════════════════════════════════
        // REPORT TAB — INLINE (NO POPUP)
        // ══════════════════════════════════════════════════════
        private void SwitchSubTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                SwitchSubTab(tag);
            }
        }

        private void SwitchSubTab(string subTab)
        {
            reportBreakdownPanel.Visibility = subTab == "Breakdown" ? Visibility.Visible : Visibility.Collapsed;
            reportFullTextPanel.Visibility = subTab == "FullText" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void PopulateFullTextReport()
        {
            if (txtFullReport == null) return;
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("  MaxNest Optimization Report");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"  Generated:    {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"  Runtime:      {_vm.LastResult?.RuntimeMs ?? 0} ms");
            sb.AppendLine();
            sb.AppendLine("─── SUMMARY ────────────────────────────────────────────────");
            sb.AppendLine($"  Total sheets:   {_vm.LastSheets?.Count ?? 0}");
            sb.AppendLine($"  Total pieces:   {_vm.LastResult?.PlacedParts?.Count ?? 0}");
            sb.AppendLine($"  Utilization:    {_vm.Utilization:0.00}%");
            sb.AppendLine($"  Waste:          {_vm.Waste:0.00}%");
            sb.AppendLine();
            if (_vm.LastSheets != null && _vm.LastSheets.Count > 0)
            {
                sb.AppendLine("─── PER-SHEET BREAKDOWN ────────────────────────────────────");
                foreach (var s in _vm.LastSheets)
                {
                    sb.AppendLine($"  Sheet #{s.SheetNum}  •  {s.PlacedParts?.Count ?? 0} parts  •  {s.Utilization:0.0}% yield");
                }
            }
            sb.AppendLine();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            txtFullReport.Text = sb.ToString();
        }

        // ══════════════════════════════════════════════════════
        // RUN OPTIMIZATION
        // ══════════════════════════════════════════════════════
        private void RunOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (_vm.StockSheets.Count == 0 || _vm.Parts.Count == 0)
            {
                MessageBox.Show("Please add stock sheets and parts first.",
                    "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                int rotation = 2;
                bool success = _vm.RunOptimizationSync(
                    GetDouble(txtKerf), GetDouble(txtTrim),
                    GetDouble(txtBreakLeft), GetDouble(txtBreakRight),
                    GetDouble(txtBreakTop), GetDouble(txtBreakBottom),
                    GetDouble(txtBreakMin), rotation, 95);

                if (success)
                {
                    _currentSheetIndex = 0;
                    MessageBox.Show(
                        $"Optimization complete!\n\n{_vm.LastResult?.PlacedParts?.Count ?? 0} parts placed across {_vm.SheetsUsed} sheets.\nUtilization: {_vm.Utilization:0.0}%\n\nClick the 2D Layout tab to visualize.",
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Optimization error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ══════════════════════════════════════════════════════
        // COMPATIBILITY (kept for external callers)
        // ══════════════════════════════════════════════════════
        public int SheetsUsed => _vm.SheetsUsed;
        public double AverageUtilization => _vm.AverageUtilization;
        public List<OptimizationResult> GetResultsList() => _vm.GetResultsList();

        public void ImportInvoiceItems(List<InvoiceItemModel> items)
        {
            try { _vm.ImportInvoiceItems(items); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OptimizationView] ImportInvoiceItems: {ex.Message}"); }
        }

        public void RunOptimizationFromInvoice()
        {
            try { _vm.RunOptimizationFromInvoice(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OptimizationView] RunOptimizationFromInvoice: {ex.Message}"); }
        }

        public void SetStockSheet(double width, double height)
        {
            try { _vm.SetStockSheet(width, height); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OptimizationView] SetStockSheet: {ex.Message}"); }
        }

        public void SetTrimSettings(double lr, double br, double tr, double rm, double kerf, double breakout)
        {
            try { _vm.SetTrimSettings(lr, br, tr, rm, kerf, breakout); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[OptimizationView] SetTrimSettings: {ex.Message}"); }
        }

        private double GetDouble(TextBox tb)
        {
            if (tb == null) return 0;
            return double.TryParse(tb.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0;
        }
    }
}