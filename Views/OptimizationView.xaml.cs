using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace ProGlassAutomation.Views
{
    public partial class OptimizationView : UserControl
    {
        private ObservableCollection<StockSheet> _stockSheets = new ObservableCollection<StockSheet>();
        private ObservableCollection<CutPart> _cutParts = new ObservableCollection<CutPart>();
        private ObservableCollection<OptimizationResult> _results = new ObservableCollection<OptimizationResult>();
        private List<PlacedPart> _allPlacedParts = new List<PlacedPart>();

        private double _zoomLevel = 1.0;
        private string _selectedSheetName = "";

        // Navigation indices
        private int _currentTypeIndex = 0;
        private int _currentSheetNumWithinType = 0;

        // Trim values
        private double _lr = 15, _br = 15, _tr = 15, _rm = 15, _kerf = 15, _breakout = 15;

        public OptimizationView()
        {
            InitializeComponent();
            DataContext = this;

            dgStock.ItemsSource = _stockSheets;
            dgParts.ItemsSource = _cutParts;
            icResults.ItemsSource = _results;
            cmbSheetSelector.ItemsSource = _results;

            LoadDefaultData();
        }

        private void LoadDefaultData()
        {
            // Stock: 2875×2100 qty1, 2500×1675 qty9, 2800×1234 qty25
            _stockSheets.Add(new StockSheet { Ref = "S1", L = 2875, W = 2100, Qty = 1 });
            _stockSheets.Add(new StockSheet { Ref = "S2", L = 2500, W = 1675, Qty = 9 });
            _stockSheets.Add(new StockSheet { Ref = "S3", L = 2800, W = 1234, Qty = 25 });

            // Default parts
            _cutParts.Add(new CutPart { Ref = "P1", L = 1200, W = 900, Rot = true, Qty = 5 });
            _cutParts.Add(new CutPart { Ref = "P2", L = 800, W = 600, Rot = true, Qty = 8 });
            _cutParts.Add(new CutPart { Ref = "P3", L = 1500, W = 800, Rot = true, Qty = 3 });
        }

        // ================= TAB HANDLERS =================

        private void OptTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                string tab = btn.Tag.ToString();

                btnStock.Style = (Style)FindResource("TabInactive");
                btnParts.Style = (Style)FindResource("TabInactive");
                btnSettings.Style = (Style)FindResource("TabInactive");
                btnSummary.Style = (Style)FindResource("TabInactive");

                btn.Style = (Style)FindResource("TabActive");

                pnlStock.Visibility = tab == "Stock" ? Visibility.Visible : Visibility.Collapsed;
                pnlParts.Visibility = tab == "Parts" ? Visibility.Visible : Visibility.Collapsed;
                pnlSettings.Visibility = tab == "Settings" ? Visibility.Visible : Visibility.Collapsed;
                pnlSummary.Visibility = tab == "Summary" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // ================= STOCK HANDLERS =================

        private void AddStockSheet_Click(object sender, RoutedEventArgs e)
        {
            int num = _stockSheets.Count + 1;
            _stockSheets.Add(new StockSheet { Ref = $"S{num}", L = 3210, W = 2250, Qty = 99999 });
        }

        private void DeleteStockRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StockSheet sheet)
                _stockSheets.Remove(sheet);
        }

        // ================= PARTS HANDLERS =================

        private void AddPart_Click(object sender, RoutedEventArgs e)
        {
            _cutParts.Add(new CutPart { Ref = $"P{_cutParts.Count + 1}", L = 1000, W = 1000, Rot = true, Qty = 1 });
        }

        private void DeletePartRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CutPart part)
                _cutParts.Remove(part);
        }

        // ================= PASTE FROM EXCEL =================

        private void DG_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Enable paste
        }

        private void DG_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                string text = e.Data.GetData(DataFormats.Text) as string;
                if (sender == dgStock)
                    ParseStockData(text);
                else if (sender == dgParts)
                    ParsePartsData(text);
            }
        }

        private void ParseStockData(string text)
        {
            var lines = text.Split('\n', '\r');
            foreach (var line in lines)
            {
                var parts = line.Split('\t', ' ');
                if (parts.Length >= 3)
                {
                    if (double.TryParse(parts[0].Trim(), out double w) &&
                        double.TryParse(parts[1].Trim(), out double h) &&
                        int.TryParse(parts[2].Trim(), out int qty))
                    {
                        _stockSheets.Add(new StockSheet
                        {
                            Ref = $"S{_stockSheets.Count + 1}",
                            L = w,
                            W = h,
                            Qty = qty
                        });
                    }
                }
            }
        }

        private void ParsePartsData(string text)
        {
            var lines = text.Split('\n', '\r');
            foreach (var line in lines)
            {
                var parts = line.Split('\t', ' ');
                if (parts.Length >= 3)
                {
                    if (double.TryParse(parts[0].Trim(), out double w) &&
                        double.TryParse(parts[1].Trim(), out double h) &&
                        int.TryParse(parts[2].Trim(), out int qty))
                    {
                        _cutParts.Add(new CutPart
                        {
                            Ref = $"P{_cutParts.Count + 1}",
                            L = w,
                            W = h,
                            Rot = true,
                            Qty = qty
                        });
                    }
                }
            }
        }

        // ================= OPTIMIZATION =================

        private void RunOptimization_Click(object sender, RoutedEventArgs e)
        {
            double.TryParse(txtLR.Text, out _lr);
            double.TryParse(txtBR.Text, out _br);
            double.TryParse(txtTR.Text, out _tr);
            double.TryParse(txtRM.Text, out _rm);
            double.TryParse(txtKerf.Text, out _kerf);
            double.TryParse(txtBreakout.Text, out _breakout);

            if (_stockSheets.Count == 0 || _cutParts.Count == 0)
            {
                MessageBox.Show("Please add stock sheets and parts first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                bool autoRotate = chkAutoRotate.IsChecked == true;
                RunNestingAlgorithm(_kerf, autoRotate);
                ShowTab("Summary");
                MessageBox.Show("Optimization completed!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowTab(string tab)
        {
            btnStock.Style = (Style)FindResource("TabInactive");
            btnParts.Style = (Style)FindResource("TabInactive");
            btnSettings.Style = (Style)FindResource("TabInactive");
            btnSummary.Style = (Style)FindResource("TabInactive");

            switch (tab)
            {
                case "Stock": btnStock.Style = (Style)FindResource("TabActive"); break;
                case "Parts": btnParts.Style = (Style)FindResource("TabActive"); break;
                case "Settings": btnSettings.Style = (Style)FindResource("TabActive"); break;
                case "Summary": btnSummary.Style = (Style)FindResource("TabActive"); break;
            }

            pnlStock.Visibility = tab == "Stock" ? Visibility.Visible : Visibility.Collapsed;
            pnlParts.Visibility = tab == "Parts" ? Visibility.Visible : Visibility.Collapsed;
            pnlSettings.Visibility = tab == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            pnlSummary.Visibility = tab == "Summary" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RunNestingAlgorithm(double kerf, bool autoRotate)
        {
            _results.Clear();
            _allPlacedParts.Clear();

            var allParts = new List<CutPart>();
            int partId = 1;
            foreach (var p in _cutParts)
            {
                for (int i = 0; i < p.Qty; i++)
                {
                    allParts.Add(new CutPart
                    {
                        Id = partId++,
                        Ref = p.Ref,
                        L = p.L,
                        W = p.W,
                        Rot = p.Rot,
                        Qty = 1,
                        IsPlaced = false
                    });
                }
            }

            var sortedStock = _stockSheets.OrderByDescending(s => s.L * s.W).ToList();

            double totalUsedAreaAll = 0;
            double totalAreaUsedSheets = 0;

            foreach (var stock in sortedStock)
            {
                double oneSheetArea = stock.L * stock.W / 1000000.0;
                int availableQty = stock.Qty;

                if (availableQty <= 0) continue;

                double trimLR = _lr;
                double trimRM = _rm;
                double trimTR = _tr;
                double trimBR = _br;

                double usableW = stock.L - trimLR - trimRM;
                double usableH = stock.W - trimTR - trimBR;

                int sheetsUsed = 0;
                double usedArea = 0;

                for (int sheetNum = 0; sheetNum < availableQty; sheetNum++)
                {
                    var remaining = allParts.Where(p => !p.IsPlaced).ToList();
                    if (remaining.Count == 0) break;

                    double currentX = 0;
                    double currentY = 0;
                    double rowHeight = 0;
                    bool sheetFull = false;
                    int placedOnThisSheet = 0;

                    while (!sheetFull)
                    {
                        CutPart placed = null;
                        double pWidth = 0, pHeight = 0;

                        foreach (var part in remaining)
                        {
                            if (part.IsPlaced) continue;

                            double w = part.L;
                            double h = part.W;

                            if (autoRotate && part.Rot && h <= usableW && w <= usableH)
                            {
                                if (w > h) { double t = w; w = h; h = t; }
                            }

                            double gapW = w + kerf;
                            double gapH = h + kerf;

                            if (currentX + gapW <= usableW && currentY + gapH <= usableH)
                            {
                                placed = part;
                                pWidth = w;
                                pHeight = h;
                                break;
                            }
                        }

                        if (placed == null)
                        {
                            if (rowHeight > 0)
                            {
                                currentX = 0;
                                currentY += rowHeight + kerf;
                                rowHeight = 0;
                                if (currentY >= usableH) sheetFull = true;
                            }
                            else { sheetFull = true; }
                            continue;
                        }

                        placed.IsPlaced = true;
                        placed.PlacedX = currentX;
                        placed.PlacedY = currentY;
                        placed.PlacedW = pWidth;
                        placed.PlacedH = pHeight;

                        _allPlacedParts.Add(new PlacedPart
                        {
                            Ref = placed.Ref,
                            X = currentX + trimLR,
                            Y = currentY + trimTR,
                            L = pWidth,
                            W = pHeight,
                            Sheet = stock.Ref,
                            SheetNum = sheetNum + 1
                        });

                        double partArea = (pWidth * pHeight) / 1000000.0;
                        usedArea += partArea;
                        totalUsedAreaAll += partArea;

                        double stepW = pWidth + kerf;
                        double stepH = pHeight + kerf;
                        currentX += stepW;
                        rowHeight = Math.Max(rowHeight, stepH);
                        placedOnThisSheet++;
                    }

                    if (placedOnThisSheet > 0) sheetsUsed++;
                }

                if (sheetsUsed > 0)
                {
                    double thisSheetTotalArea = oneSheetArea * sheetsUsed;
                    double thisUtil = (usedArea / thisSheetTotalArea) * 100;
                    double thisWaste = 100 - thisUtil;
                    totalAreaUsedSheets += thisSheetTotalArea;

                    _results.Add(new OptimizationResult
                    {
                        Ref = stock.Ref,
                        L = stock.L,
                        W = stock.W,
                        Used = sheetsUsed,
                        Area = usedArea,
                        Util = thisUtil,
                        Waste = thisWaste
                    });
                }
            }

            int placedCount = allParts.Count(p => p.IsPlaced);
            int unplacedCount = allParts.Count - placedCount;

            double totalUtil = totalAreaUsedSheets > 0 ? (totalUsedAreaAll / totalAreaUsedSheets) * 100 : 0;
            double totalWaste = 100 - totalUtil;

            int totalUsed = _results.Sum(r => r.Used);
            int totalAvailable = _stockSheets.Sum(s => s.Qty);

            txtSheetsUsed.Text = totalUsed.ToString();
            txtSheetsRemaining.Text = (totalAvailable - totalUsed).ToString();
            txtUtilization.Text = $"{totalUtil:N2}%";
            txtWaste.Text = $"{totalWaste:N2}%";

            txtTotalSheetsUsed.Text = totalUsed.ToString();
            txtTotalPartsCut.Text = placedCount.ToString();
            txtAvgUtilization.Text = $"{totalUtil:N2}%";
            txtTotalStats.Text = $"{totalUsed} sheets, {placedCount} parts";

            if (unplacedCount > 0)
            {
                pnlUnplaced.Visibility = Visibility.Visible;
                txtUnplaced.Text = $"{unplacedCount} parts could not be placed";
            }
            else
            {
                pnlUnplaced.Visibility = Visibility.Collapsed;
            }

            Draw2DLayouts();

            _currentTypeIndex = 0;
            _currentSheetNumWithinType = 0;
            if (_results.Count > 0)
            {
                _selectedSheetName = _results[0].Ref;
                cmbSheetSelector.SelectedIndex = 0;
            }

            UpdateLayoutCount();
        }

        // ================= 2D LAYOUT DRAWING =================

        private void Draw2DLayouts()
        {
            PreviewCanvas.Children.Clear();
            LayoutCanvas.Children.Clear();

            if (_allPlacedParts.Count == 0) return;

            DrawCurrentLayout();
        }

        private void DrawCurrentLayout()
        {
            PreviewCanvas.Children.Clear();
            LayoutCanvas.Children.Clear();

            if (_results.Count == 0) return;

            var currentResult = _results[_currentTypeIndex];
            var partsOnSheet = _allPlacedParts
                .Where(p => p.Sheet == currentResult.Ref && p.SheetNum == _currentSheetNumWithinType + 1)
                .ToList();

            if (partsOnSheet.Count == 0)
            {
                partsOnSheet = _allPlacedParts
                    .Where(p => p.Sheet == currentResult.Ref)
                    .ToList();

                if (partsOnSheet.Count == 0) return;
            }

            double trimLR = _lr;
            double trimRM = _rm;
            double trimTR = _tr;
            double trimBR = _br;

            double sheetW = currentResult.L;
            double sheetH = currentResult.W;

            double canvasW = 700;
            double canvasH = 500;
            double margin = 30;

            double availableW = canvasW - margin * 2;
            double availableH = canvasH - margin * 2;

            double scaleX = availableW / sheetW;
            double scaleY = availableH / sheetH;
            double scale = Math.Min(scaleX, scaleY) * _zoomLevel;

            double drawW = sheetW * scale;
            double drawH = sheetH * scale;

            double startX = (canvasW - drawW) / 2;
            double startY = margin;

            // Left trim
            Rectangle trimLeft = new Rectangle
            {
                Width = trimLR * scale,
                Height = drawH,
                Fill = new SolidColorBrush(Color.FromArgb(180, 239, 68, 68)),
                Stroke = new SolidColorBrush(Color.FromRgb(185, 28, 28)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            Canvas.SetLeft(trimLeft, startX);
            Canvas.SetTop(trimLeft, startY);
            PreviewCanvas.Children.Add(trimLeft);

            // Right trim
            Rectangle trimRight = new Rectangle
            {
                Width = trimRM * scale,
                Height = drawH,
                Fill = new SolidColorBrush(Color.FromArgb(180, 239, 68, 68)),
                Stroke = new SolidColorBrush(Color.FromRgb(185, 28, 28)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            Canvas.SetLeft(trimRight, startX + drawW - (trimRM * scale));
            Canvas.SetTop(trimRight, startY);
            PreviewCanvas.Children.Add(trimRight);

            // Top trim
            Rectangle trimTop = new Rectangle
            {
                Width = drawW,
                Height = trimTR * scale,
                Fill = new SolidColorBrush(Color.FromArgb(180, 239, 68, 68)),
                Stroke = new SolidColorBrush(Color.FromRgb(185, 28, 28)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            Canvas.SetLeft(trimTop, startX);
            Canvas.SetTop(trimTop, startY);
            PreviewCanvas.Children.Add(trimTop);

            // Bottom trim
            Rectangle trimBottom = new Rectangle
            {
                Width = drawW,
                Height = trimBR * scale,
                Fill = new SolidColorBrush(Color.FromArgb(180, 239, 68, 68)),
                Stroke = new SolidColorBrush(Color.FromRgb(185, 28, 28)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 4, 2 }
            };
            Canvas.SetLeft(trimBottom, startX);
            Canvas.SetTop(trimBottom, startY + drawH - (trimBR * scale));
            PreviewCanvas.Children.Add(trimBottom);

            // Sheet background
            Rectangle sheet = new Rectangle
            {
                Width = drawW,
                Height = drawH,
                Fill = new SolidColorBrush(Color.FromRgb(13, 17, 23)),
                Stroke = new SolidColorBrush(Color.FromRgb(100, 120, 140)),
                StrokeThickness = 2
            };
            Canvas.SetLeft(sheet, startX);
            Canvas.SetTop(sheet, startY);
            PreviewCanvas.Children.Add(sheet);

            // Grid lines
            for (int i = 1; i < 4; i++)
            {
                double vLineX = startX + drawW * i / 4;
                Line vLine = new Line
                {
                    X1 = vLineX,
                    Y1 = startY,
                    X2 = vLineX,
                    Y2 = startY + drawH,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                };
                PreviewCanvas.Children.Add(vLine);

                double hLineY = startY + drawH * i / 4;
                Line hLine = new Line
                {
                    X1 = startX,
                    Y1 = hLineY,
                    X2 = startX + drawW,
                    Y2 = hLineY,
                    Stroke = new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                };
                PreviewCanvas.Children.Add(hLine);
            }

            // Part colors
            Color[] partColors = new Color[]
            {
                Color.FromRgb(59, 130, 246),
                Color.FromRgb(16, 185, 129),
                Color.FromRgb(139, 92, 246),
                Color.FromRgb(245, 158, 11),
                Color.FromRgb(239, 68, 68),
                Color.FromRgb(6, 182, 212)
            };

            var partGroups = partsOnSheet.GroupBy(p => p.Ref).ToList();
            var colorMap = new Dictionary<string, Color>();
            for (int i = 0; i < partGroups.Count(); i++)
                colorMap[partGroups.ElementAt(i).Key] = partColors[i % partColors.Length];

            // Draw parts
            foreach (var part in partsOnSheet)
            {
                double px = startX + part.X * scale;
                double py = startY + part.Y * scale;
                double pw = part.L * scale;
                double ph = part.W * scale;

                var color = colorMap[part.Ref];

                Rectangle partRect = new Rectangle
                {
                    Width = pw,
                    Height = ph,
                    Fill = new SolidColorBrush(color),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 1,
                    Opacity = 0.85
                };
                Canvas.SetLeft(partRect, px);
                Canvas.SetTop(partRect, py);
                PreviewCanvas.Children.Add(partRect);

                if (pw > 25 && ph > 15)
                {
                    TextBlock label = new TextBlock
                    {
                        Text = part.Ref,
                        FontSize = Math.Max(7, pw / 18),
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Colors.White)
                    };
                    Canvas.SetLeft(label, px + 2);
                    Canvas.SetTop(label, py + 2);
                    PreviewCanvas.Children.Add(label);
                }
            }

            PreviewCanvas.Width = canvasW;
            PreviewCanvas.Height = canvasH;

            // Header
            var result = _results[_currentTypeIndex];
            txtCurrentSheet.Text = $"{result.Ref}: {result.L:N0} × {result.W:N0}mm - Sheet {_currentSheetNumWithinType + 1}/{result.Used} | U: {result.Util:N2}% W: {result.Waste:N2}%";
            LayoutCanvas.Width = drawW;
            LayoutCanvas.Height = drawH;
        }

        // ================= CONTROLS =================

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _zoomLevel = Math.Min(_zoomLevel + 0.1, 2.5);
            txtZoom.Text = $"{(_zoomLevel * 100):N0}%";
            DrawCurrentLayout();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            _zoomLevel = Math.Max(_zoomLevel - 0.1, 0.3);
            txtZoom.Text = $"{(_zoomLevel * 100):N0}%";
            DrawCurrentLayout();
        }

        private void SheetSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (cmbSheetSelector.SelectedIndex >= 0 && cmbSheetSelector.SelectedIndex < _results.Count)
            {
                _currentTypeIndex = cmbSheetSelector.SelectedIndex;
                _currentSheetNumWithinType = 0;
                _selectedSheetName = _results[_currentTypeIndex].Ref;
                DrawCurrentLayout();
                UpdateLayoutCount();
            }
        }

        private void PrevLayout_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSheetNumWithinType > 0)
            {
                _currentSheetNumWithinType--;
                DrawCurrentLayout();
                UpdateLayoutCount();
            }
            else if (_currentTypeIndex > 0)
            {
                _currentTypeIndex--;
                var prevType = _results[_currentTypeIndex];
                _currentSheetNumWithinType = prevType.Used - 1;
                cmbSheetSelector.SelectedIndex = _currentTypeIndex;
                DrawCurrentLayout();
                UpdateLayoutCount();
            }
        }

        private void NextLayout_Click(object sender, RoutedEventArgs e)
        {
            var currentResult = _results[_currentTypeIndex];

            if (_currentSheetNumWithinType < currentResult.Used - 1)
            {
                _currentSheetNumWithinType++;
                DrawCurrentLayout();
                UpdateLayoutCount();
            }
            else if (_currentTypeIndex < _results.Count - 1)
            {
                _currentTypeIndex++;
                _currentSheetNumWithinType = 0;
                cmbSheetSelector.SelectedIndex = _currentTypeIndex;
                DrawCurrentLayout();
                UpdateLayoutCount();
            }
        }

        private void UpdateLayoutCount()
        {
            int totalSheets = _results.Sum(r => r.Used);

            int actualSheetNum = 0;
            for (int i = 0; i < _currentTypeIndex; i++)
            {
                actualSheetNum += _results[i].Used;
            }
            actualSheetNum += _currentSheetNumWithinType + 1;

            txtLayoutNum.Text = $" {actualSheetNum}/{totalSheets} ";
        }

        private void ClearOptimization_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Clear all data?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _stockSheets.Clear();
                _cutParts.Clear();
                _results.Clear();
                _allPlacedParts.Clear();

                txtSheetsUsed.Text = "0";
                txtSheetsRemaining.Text = "0";
                txtUtilization.Text = "0%";
                txtWaste.Text = "0%";

                txtTotalSheetsUsed.Text = "0";
                txtTotalPartsCut.Text = "0";
                txtAvgUtilization.Text = "0%";
                txtTotalStats.Text = "0 sheets, 0 parts";

                PreviewCanvas.Children.Clear();
                LayoutCanvas.Children.Clear();
                pnlUnplaced.Visibility = Visibility.Collapsed;
            }
        }

        private void ExportResults_Click(object sender, RoutedEventArgs e)
        {
            if (_results.Count == 0)
            {
                MessageBox.Show("No results to export.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files|*.csv",
                    FileName = $"Optimization_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    using (var writer = new System.IO.StreamWriter(dialog.FileName))
                    {
                        writer.WriteLine("Sheet Ref,Length mm,Width mm,Used Qty,Util %,Waste %,Area sqm");
                        foreach (var r in _results)
                        {
                            writer.WriteLine($"{r.Ref},{r.L},{r.W},{r.Used},{r.Util:N2},{r.Waste:N2},{r.Area:N4}");
                        }
                        int totalUsed = _results.Sum(r => r.Used);
                        int totalAvailable = _stockSheets.Sum(s => s.Qty);
                        double totalUtil = double.Parse(txtAvgUtilization.Text.Replace("%", ""));
                        writer.WriteLine($"TOTAL,,,{totalUsed},,{totalUtil:N2},{100 - totalUtil:N2}");
                    }
                    MessageBox.Show($"Exported:\n{dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PrintLayouts_Click(object sender, RoutedEventArgs e)
        {
            if (_results.Count == 0)
            {
                MessageBox.Show("No layouts to print.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var printWindow = new Window
            {
                Title = "Cutting Layouts - ProGlass",
                Width = 900,
                Height = 700,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = new SolidColorBrush(Colors.White)
            };

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            var stack = new StackPanel { Margin = new Thickness(20) };

            stack.Children.Add(new TextBlock
            {
                Text = "Glass Cutting Layouts",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20)
            });

            foreach (var r in _results)
            {
                var border = new Border
                {
                    BorderBrush = new SolidColorBrush(Colors.Black),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(15),
                    Margin = new Thickness(0, 0, 0, 15),
                    Background = new SolidColorBrush(Color.FromRgb(248, 250, 252))
                };

                var text = new TextBlock
                {
                    Text = $"{r.Ref}: {r.L:N0} × {r.W:N0} mm - Used: {r.Used} sheets\nUtilization: {r.Util:N2}%  |  Wastage: {r.Waste:N2}%  |  Area: {r.Area:N4} sqm",
                    FontSize = 13
                };
                border.Child = text;
                stack.Children.Add(border);
            }

            var totalBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 0)
            };
            var totalText = new TextBlock
            {
                Text = $"TOTAL: {_results.Sum(r => r.Used)} sheets used",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White)
            };
            totalBorder.Child = totalText;
            stack.Children.Add(totalBorder);

            scroll.Content = stack;
            printWindow.Content = scroll;
            printWindow.ShowDialog();
        }
    }

    // ================= DATA CLASSES =================

    public class StockSheet
    {
        public string Ref { get; set; } = "";
        public double L { get; set; }
        public double W { get; set; }
        public int Qty { get; set; }
        public double Area => (L * W) / 1000000.0;
    }

    public class CutPart
    {
        public int Id { get; set; }
        public string Ref { get; set; } = "";
        public double L { get; set; }
        public double W { get; set; }
        public bool Rot { get; set; }
        public int Qty { get; set; }
        public double Area => (L * W * Qty) / 1000000.0;
        public bool IsPlaced { get; set; }
        public double PlacedX { get; set; }
        public double PlacedY { get; set; }
        public double PlacedW { get; set; }
        public double PlacedH { get; set; }
    }

    public class OptimizationResult
    {
        public string Ref { get; set; } = "";
        public double L { get; set; }
        public double W { get; set; }
        public int Used { get; set; }
        public double Area { get; set; }
        public double Util { get; set; }
        public double Waste { get; set; }
    }

    public class PlacedPart
    {
        public string Ref { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double L { get; set; }
        public double W { get; set; }
        public string Sheet { get; set; } = "";
        public int SheetNum { get; set; }
    }
}