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
        private int _currentIndex = 0;
        private double _lr = 15, _br = 15, _tr = 15, _rm = 15, _kerf = 15, _breakout = 15;

        // Overall tracking
        private double _overallUtilization = 0;
        private double _overallWastage = 0;
        private int _totalPartsCut = 0;
        private int _totalPartsUnplaced = 0;
        private double _totalSQM = 0;
        private double _usedSQM = 0;

        public OptimizationView()
        {
            InitializeComponent();
            DataContext = this;

            dgStock.ItemsSource = _stockSheets;
            dgParts.ItemsSource = _cutParts;
            icResults.ItemsSource = _results;
            cmbSheetSelector.ItemsSource = _results;
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
                btnReport.Style = (Style)FindResource("TabInactive");

                btn.Style = (Style)FindResource("TabActive");

                pnlStock.Visibility = tab == "Stock" ? Visibility.Visible : Visibility.Collapsed;
                pnlParts.Visibility = tab == "Parts" ? Visibility.Visible : Visibility.Collapsed;
                pnlSettings.Visibility = tab == "Settings" ? Visibility.Visible : Visibility.Collapsed;
                pnlSummary.Visibility = tab == "Summary" ? Visibility.Visible : Visibility.Collapsed;
                pnlReport.Visibility = tab == "Report" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // ================= DATA HANDLERS =================

        private void AddStockSheet_Click(object sender, RoutedEventArgs e)
        {
            _stockSheets.Add(new StockSheet { Ref = $"S{_stockSheets.Count + 1}", L = 3210, W = 2250, Qty = 100 });
            UpdateStockSummary();
        }

        private void DeleteStockRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is StockSheet sheet)
            {
                _stockSheets.Remove(sheet);
                UpdateStockSummary();
            }
        }

        private void AddPart_Click(object sender, RoutedEventArgs e)
        {
            _cutParts.Add(new CutPart { Ref = $"P{_cutParts.Count + 1}", L = 1000, W = 1000, Rot = true, Qty = 1 });
            UpdatePartsSummary();
        }

        private void DeletePartRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is CutPart part)
            {
                _cutParts.Remove(part);
                UpdatePartsSummary();
            }
        }

        private void DG_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) { }

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
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
            UpdateStockSummary();
        }

        private void ParsePartsData(string text)
        {
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
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
            UpdatePartsSummary();
        }

        // ================= SUMMARIES =================

        private void UpdateStockSummary()
        {
            if (_stockSheets.Count == 0)
            {
                txtStockSummary.Text = "No stock added";
                return;
            }

            var groups = _stockSheets.GroupBy(s => new { s.L, s.W })
                .Select(g => new {
                    g.Key.L,
                    g.Key.W,
                    TotalQty = g.Sum(s => s.Qty),
                    SQM = g.Sum(s => s.L * s.W * s.Qty) / 1000000.0
                })
                .OrderByDescending(x => x.SQM)
                .ToList();

            var lines = groups.Select(g => $"{g.L:N0}×{g.W:N0}mm = {g.TotalQty} ({g.SQM:N2}m²)").ToList();
            lines.Insert(0, $"Stock: {_stockSheets.Count} types, {_stockSheets.Sum(s => s.Qty)} total");

            txtStockSummary.Text = string.Join("\n", lines);
        }

        private void UpdatePartsSummary()
        {
            if (_cutParts.Count == 0)
            {
                txtPartsSummary.Text = "No parts added";
                return;
            }

            var totalQty = _cutParts.Sum(p => p.Qty);
            var totalSQM = _cutParts.Sum(p => p.L * p.W * p.Qty) / 1000000.0;

            var groups = _cutParts.GroupBy(p => new { p.L, p.W, p.Ref })
                .Select(g => new {
                    Ref = g.Key.Ref,
                    L = g.Key.L,
                    W = g.Key.W,
                    Qty = g.Sum(x => x.Qty),
                    SQM = g.Sum(x => x.L * x.W * x.Qty) / 1000000.0
                })
                .OrderByDescending(x => x.SQM)
                .ToList();

            var lines = groups.Select(g => $"{g.Ref}: {g.L}×{g.W}×{g.Qty} = {g.SQM:N2}m²").ToList();
            lines.Insert(0, $"Parts: {totalQty} total ({totalSQM:N2}m²)");

            txtPartsSummary.Text = string.Join("\n", lines);
        }

        // ================= RUN OPTIMIZATION =================

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
                UpdateReportSection();
                ShowTab("Layouts");
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
            btnReport.Style = (Style)FindResource("TabInactive");

            switch (tab)
            {
                case "Stock": btnStock.Style = (Style)FindResource("TabActive"); break;
                case "Parts": btnParts.Style = (Style)FindResource("TabActive"); break;
                case "Settings": btnSettings.Style = (Style)FindResource("TabActive"); break;
                case "Layouts": btnSummary.Style = (Style)FindResource("TabActive"); break;
                case "Report": btnReport.Style = (Style)FindResource("TabActive"); break;
            }

            pnlStock.Visibility = tab == "Stock" ? Visibility.Visible : Visibility.Collapsed;
            pnlParts.Visibility = tab == "Parts" ? Visibility.Visible : Visibility.Collapsed;
            pnlSettings.Visibility = tab == "Settings" ? Visibility.Visible : Visibility.Collapsed;
            pnlSummary.Visibility = tab == "Layouts" ? Visibility.Visible : Visibility.Collapsed;
            pnlReport.Visibility = tab == "Report" ? Visibility.Visible : Visibility.Collapsed;
        }

        // ================= NESTING ALGORITHM =================

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

            allParts = allParts.OrderByDescending(x => x.L * x.W)
                             .ThenByDescending(x => Math.Max(x.L, x.W))
                             .ToList();

            var sortedStock = _stockSheets.OrderByDescending(s => s.L * s.W).ToList();

            double totalUsedAreaAll = 0;
            double totalAreaUsedSheets = 0;
            int totalSheetsUsed = 0;
            _totalPartsCut = 0;
            _totalPartsUnplaced = 0;
            _usedSQM = 0;

            foreach (var stock in sortedStock)
            {
                double oneSheetArea = stock.L * stock.W / 1000000.0;
                int availableQty = stock.Qty;

                if (availableQty <= 0) continue;

                double usableW = stock.L - _lr - _rm;
                double usableH = stock.W - _tr - _br;
                int sheetsUsedForThisStock = 0;

                for (int sheetNum = 0; sheetNum < availableQty; sheetNum++)
                {
                    var remaining = allParts.Where(p => !p.IsPlaced).ToList();
                    if (remaining.Count == 0) break;

                    double currentX = 0;
                    double currentY = 0;
                    double rowHeight = 0;
                    int placedOnThisSheet = 0;
                    double usedAreaThisSheet = 0;

                    while (true)
                    {
                        double remainingW = usableW - currentX;
                        double remainingH = usableH - currentY;

                        if (remainingW < _breakout || remainingH < _breakout)
                        {
                            if (rowHeight > 0)
                            {
                                currentX = 0;
                                currentY += rowHeight + kerf;
                                rowHeight = 0;
                                if (currentY >= usableH) break;
                            }
                            else break;
                            continue;
                        }

                        CutPart placed = null;
                        double pWidth = 0, pHeight = 0;
                        bool rotated = false;

                        foreach (var part in remaining)
                        {
                            if (part.IsPlaced) continue;

                            bool allowRotation = autoRotate && part.Rot;
                            double w1 = part.L, h1 = part.W;
                            bool fit1 = (currentX + w1 + kerf <= usableW && currentY + h1 + kerf <= usableH);

                            bool fit2 = false;
                            double w2 = 0, h2 = 0;
                            if (allowRotation)
                            {
                                w2 = part.W;
                                h2 = part.L;
                                fit2 = (currentX + w2 + kerf <= usableW && currentY + h2 + kerf <= usableH);
                            }

                            if (fit1 || fit2)
                            {
                                if (fit2 && (!fit1 || w2 > w1))
                                {
                                    placed = part;
                                    pWidth = w2;
                                    pHeight = h2;
                                    rotated = true;
                                }
                                else
                                {
                                    placed = part;
                                    pWidth = w1;
                                    pHeight = h1;
                                    rotated = false;
                                }
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
                                if (currentY >= usableH) break;
                            }
                            else break;
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
                            X = currentX + _lr,
                            Y = currentY + _tr,
                            L = pWidth,
                            W = pHeight,
                            IsRotated = rotated,
                            Sheet = stock.Ref,
                            SheetNum = sheetNum + 1
                        });

                        double partArea = (pWidth * pHeight) / 1000000.0;
                        usedAreaThisSheet += partArea;
                        _usedSQM += partArea;

                        currentX += pWidth + kerf;
                        rowHeight = Math.Max(rowHeight, pHeight);
                        placedOnThisSheet++;
                        _totalPartsCut++;
                    }

                    if (placedOnThisSheet > 0)
                    {
                        sheetsUsedForThisStock++;
                        totalSheetsUsed++;
                        totalAreaUsedSheets += oneSheetArea;
                        totalUsedAreaAll += usedAreaThisSheet;

                        double thisSheetUtil = (usedAreaThisSheet / oneSheetArea) * 100;

                        _results.Add(new OptimizationResult
                        {
                            Ref = $"{stock.Ref}-{sheetsUsedForThisStock}",
                            SheetRef = stock.Ref,
                            SheetNum = sheetsUsedForThisStock,
                            L = stock.L,
                            W = stock.W,
                            Used = 1,
                            Area = usedAreaThisSheet,
                            Util = thisSheetUtil,
                            Waste = 100 - thisSheetUtil
                        });
                    }
                }
            }

            _totalPartsUnplaced = allParts.Count(p => !p.IsPlaced);
            _overallUtilization = totalAreaUsedSheets > 0 ? (totalUsedAreaAll / totalAreaUsedSheets) * 100 : 0;
            _overallWastage = 100 - _overallUtilization;

            int totalAvailable = _stockSheets.Sum(s => s.Qty);

            _results.Add(new OptimizationResult
            {
                Ref = "TOTAL",
                L = 0,
                W = 0,
                Used = totalSheetsUsed,
                Area = totalUsedAreaAll,
                Util = _overallUtilization,
                Waste = _overallWastage
            });

            txtSheetsUsed.Text = totalSheetsUsed.ToString();
            txtSheetsRemaining.Text = (totalAvailable - totalSheetsUsed).ToString();
            txtUtilization.Text = $"{_overallUtilization:N1}%";
            txtWaste.Text = $"{_overallWastage:N1}%";

            txtTotalSheetsUsed.Text = totalSheetsUsed.ToString();
            txtTotalPartsCut.Text = _totalPartsCut.ToString();
            txtAvgUtilization.Text = $"{_overallUtilization:N1}%";
            txtTotalStats.Text = $"{totalSheetsUsed} sheets, {_totalPartsCut} parts";

            if (_totalPartsUnplaced > 0)
            {
                pnlUnplaced.Visibility = Visibility.Visible;
                txtUnplaced.Text = $"{_totalPartsUnplaced} parts could not be placed";
            }
            else
            {
                pnlUnplaced.Visibility = Visibility.Collapsed;
            }

            _currentIndex = 0;
            if (_results.Count > 0)
                cmbSheetSelector.SelectedIndex = 0;

            DrawCurrentLayout();
            UpdateLayoutCount();
        }

        // ================= REPORT SECTION =================

        private void UpdateReportSection()
        {
            spReportDetails.Children.Clear();

            // Header
            var header = new TextBlock
            {
                Text = "CUTTING REPORT",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            spReportDetails.Children.Add(header);

            // Overall Summary
            var overallBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 59, 100)),
                Padding = new Thickness(15),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var overallStack = new StackPanel();
            overallStack.Children.Add(new TextBlock
            {
                Text = "OVERALL SUMMARY",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 0, 0, 10)
            });
            overallStack.Children.Add(new TextBlock
            {
                Text = $"Total Sheets Used: {_results.Count(r => r.Ref != "TOTAL")}",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(245, 158, 11))
            });
            overallStack.Children.Add(new TextBlock
            {
                Text = $"Total Parts Cut: {_totalPartsCut}",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246))
            });
            overallStack.Children.Add(new TextBlock
            {
                Text = $"Glass Area Used: {_usedSQM:N2} m²",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129))
            });
            overallStack.Children.Add(new TextBlock
            {
                Text = $"Average Utilization: {_overallUtilization:N1}%",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(139, 92, 246))
            });
            overallStack.Children.Add(new TextBlock
            {
                Text = $"Wastage: {_overallWastage:N1}%",
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68))
            });

            if (_totalPartsUnplaced > 0)
            {
                overallStack.Children.Add(new TextBlock
                {
                    Text = $"⚠ Unplaced Parts: {_totalPartsUnplaced}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Foreground = new SolidColorBrush(Color.FromRgb(252, 165, 165)),
                    Margin = new Thickness(0, 10, 0, 0)
                });
            }

            overallBorder.Child = overallStack;
            spReportDetails.Children.Add(overallBorder);

            // Per Sheet Report
            var sheetsBySize = _results
                .Where(r => r.Ref != "TOTAL")
                .GroupBy(r => new { r.L, r.W, r.SheetRef })
                .OrderByDescending(g => g.Key.L * g.Key.W)
                .ToList();

            foreach (var sheetGroup in sheetsBySize)
            {
                var first = sheetGroup.First();
                var sheetCount = sheetGroup.Count();
                var totalUtil = sheetGroup.Average(s => s.Util);
                var totalArea = sheetGroup.Sum(s => s.Area);

                var sheetBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(26, 29, 35)),
                    Padding = new Thickness(12),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 0, 0, 8)
                };

                var sheetStack = new StackPanel();
                sheetStack.Children.Add(new TextBlock
                {
                    Text = $"{first.SheetRef}: {first.L:N0} × {first.W:N0}mm",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246))
                });
                sheetStack.Children.Add(new TextBlock
                {
                    Text = $"Sheets: {sheetCount} | Area: {totalArea:N3} m² | Util: {totalUtil:N1}%",
                    Foreground = new SolidColorBrush(Color.FromRgb(156, 163, 175)),
                    FontSize = 11
                });

                sheetBorder.Child = sheetStack;
                spReportDetails.Children.Add(sheetBorder);
            }

            // Auto-Sum Total
            var totalBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Padding = new Thickness(15),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 10, 0, 0)
            };

            var totalStack = new StackPanel();
            totalStack.Children.Add(new TextBlock
            {
                Text = "AUTO-SUM TOTAL",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.White)
            });
            totalStack.Children.Add(new TextBlock
            {
                Text = $"Total Qty: {_totalPartsCut} parts",
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.White)
            });
            totalStack.Children.Add(new TextBlock
            {
                Text = $"Total SQM: {_usedSQM:N2}",
                FontSize = 14,
                Foreground = new SolidColorBrush(Colors.White)
            });

            totalBorder.Child = totalStack;
            spReportDetails.Children.Add(totalBorder);
        }

        // ================= 2D LAYOUT DRAWING =================

        private void DrawCurrentLayout()
        {
            PreviewCanvas.Children.Clear();

            if (_results.Count == 0) return;
            if (_currentIndex < 0 || _currentIndex >= _results.Count) return;

            var currentResult = _results[_currentIndex];
            if (currentResult.Ref == "TOTAL") return;

            var partsOnSheet = _allPlacedParts
                .Where(p => p.Sheet == currentResult.SheetRef && p.SheetNum == currentResult.SheetNum)
                .ToList();

            if (partsOnSheet.Count == 0) return;

            double sheetW = currentResult.L;
            double sheetH = currentResult.W;

            double canvasW = 900;
            double canvasH = 650;
            double margin = 40;

            double availableW = canvasW - margin * 2;
            double availableH = canvasH - margin * 2;

            double scaleX = availableW / sheetW;
            double scaleY = availableH / sheetH;
            double scale = Math.Min(scaleX, scaleY) * _zoomLevel;

            double drawW = sheetW * scale;
            double drawH = sheetH * scale;

            double startX = (canvasW - drawW) / 2;
            double startY = margin;

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

            // Usable area
            double usableX = startX + _lr * scale;
            double usableY = startY + _tr * scale;
            double usableW_draw = drawW - (_lr + _rm) * scale;
            double usableH_draw = drawH - (_tr + _br) * scale;

            if (usableW_draw > 0 && usableH_draw > 0)
            {
                Rectangle usableArea = new Rectangle
                {
                    Width = usableW_draw,
                    Height = usableH_draw,
                    Fill = new SolidColorBrush(Color.FromArgb(20, 30, 130, 30)),
                    Stroke = new SolidColorBrush(Color.FromArgb(60, 100, 200, 100)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 2, 4 }
                };
                Canvas.SetLeft(usableArea, usableX);
                Canvas.SetTop(usableArea, usableY);
                PreviewCanvas.Children.Add(usableArea);
            }

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
                    Stroke = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
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
                    Stroke = new SolidColorBrush(Color.FromArgb(25, 255, 255, 255)),
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                };
                PreviewCanvas.Children.Add(hLine);
            }

            // Part colors
            Color[] partColors = new Color[]
            {
                Color.FromRgb(59, 130, 246), Color.FromRgb(16, 185, 129),
                Color.FromRgb(139, 92, 246), Color.FromRgb(245, 158, 11),
                Color.FromRgb(239, 68, 68), Color.FromRgb(6, 182, 212),
                Color.FromRgb(236, 72, 153), Color.FromRgb(34, 197, 94),
                Color.FromRgb(168, 85, 247), Color.FromRgb(251, 146, 60)
            };

            var partGroups = partsOnSheet.GroupBy(p => p.Ref).ToList();
            var colorMap = new Dictionary<string, Color>();
            for (int i = 0; i < partGroups.Count(); i++)
                colorMap[partGroups.ElementAt(i).Key] = partColors[i % partColors.Length];

            // Parts
            foreach (var part in partsOnSheet)
            {
                double px = startX + part.X * scale;
                double py = startY + part.Y * scale;
                double pw = part.L * scale;
                double ph = part.W * scale;
                var color = colorMap[part.Ref];

                // Part rectangle
                Rectangle partRect = new Rectangle
                {
                    Width = pw,
                    Height = ph,
                    Fill = new SolidColorBrush(color),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 1.5,
                    Opacity = 0.9,
                    RadiusX = 2,
                    RadiusY = 2
                };
                Canvas.SetLeft(partRect, px);
                Canvas.SetTop(partRect, py);
                PreviewCanvas.Children.Add(partRect);

                // Part label
                if (pw > 30 && ph > 18)
                {
                    TextBlock label = new TextBlock
                    {
                        Text = part.Ref,
                        FontSize = Math.Max(8, Math.Min(pw / 12, 14)),
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(Colors.White)
                    };
                    Canvas.SetLeft(label, px + 3);
                    Canvas.SetTop(label, py + 3);
                    PreviewCanvas.Children.Add(label);
                }

                // Rotation indicator
                if (part.IsRotated)
                {
                    TextBlock rotLabel = new TextBlock
                    {
                        Text = "↻",
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Color.FromRgb(255, 200, 50)),
                        FontWeight = FontWeights.Bold
                    };
                    Canvas.SetLeft(rotLabel, px + pw - 14);
                    Canvas.SetTop(rotLabel, py + 2);
                    PreviewCanvas.Children.Add(rotLabel);
                }

                // Kerf lines
                double kerfScale = _kerf * scale;
                double kerfMin = Math.Max(1.5, kerfScale);

                Rectangle kerfRight = new Rectangle
                {
                    Width = kerfMin,
                    Height = ph,
                    Fill = new SolidColorBrush(Color.FromRgb(200, 50, 50)),
                    Opacity = 0.7
                };
                Canvas.SetLeft(kerfRight, px + pw);
                Canvas.SetTop(kerfRight, py);
                PreviewCanvas.Children.Add(kerfRight);

                Rectangle kerfBottom = new Rectangle
                {
                    Width = pw,
                    Height = kerfMin,
                    Fill = new SolidColorBrush(Color.FromRgb(200, 50, 50)),
                    Opacity = 0.7
                };
                Canvas.SetLeft(kerfBottom, px);
                Canvas.SetTop(kerfBottom, py + ph);
                PreviewCanvas.Children.Add(kerfBottom);
            }

            // Trim margins
            Brush trimBrush = new SolidColorBrush(Color.FromArgb(180, 239, 68, 68));
            var dashArray = new DoubleCollection { 4, 2 };

            if (_lr > 0)
            {
                Rectangle trimLeft = new Rectangle
                {
                    Width = _lr * scale,
                    Height = drawH,
                    Fill = trimBrush,
                    StrokeDashArray = dashArray
                };
                Canvas.SetLeft(trimLeft, startX);
                Canvas.SetTop(trimLeft, startY);
                PreviewCanvas.Children.Add(trimLeft);
            }

            if (_tr > 0)
            {
                Rectangle trimTop = new Rectangle
                {
                    Width = drawW,
                    Height = _tr * scale,
                    Fill = trimBrush,
                    StrokeDashArray = dashArray
                };
                Canvas.SetLeft(trimTop, startX);
                Canvas.SetTop(trimTop, startY);
                PreviewCanvas.Children.Add(trimTop);
            }

            if (_br > 0)
            {
                Rectangle trimBottom = new Rectangle
                {
                    Width = drawW,
                    Height = _br * scale,
                    Fill = trimBrush,
                    StrokeDashArray = dashArray
                };
                Canvas.SetLeft(trimBottom, startX);
                Canvas.SetTop(trimBottom, startY + drawH - (_br * scale));
                PreviewCanvas.Children.Add(trimBottom);
            }

            if (_rm > 0)
            {
                Rectangle trimRight = new Rectangle
                {
                    Width = _rm * scale,
                    Height = drawH,
                    Fill = trimBrush,
                    StrokeDashArray = dashArray
                };
                Canvas.SetLeft(trimRight, startX + drawW - (_rm * scale));
                Canvas.SetTop(trimRight, startY);
                PreviewCanvas.Children.Add(trimRight);
            }

            // Sheet info
            TextBlock sheetInfo = new TextBlock
            {
                Text = $"{currentResult.Ref}: {currentResult.L:N0}×{currentResult.W:N0}mm | U: {currentResult.Util:N1}% | W: {currentResult.Waste:N1}%",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(59, 130, 246))
            };
            Canvas.SetLeft(sheetInfo, startX + drawW / 2 - 80);
            Canvas.SetTop(sheetInfo, startY + drawH + 10);
            PreviewCanvas.Children.Add(sheetInfo);

            PreviewCanvas.Width = canvasW;
            PreviewCanvas.Height = canvasH;

            // Update header
            txtCurrentSheet.Text = $"Layout {_currentIndex + 1}: {currentResult.Ref} | {currentResult.L:N0}×{currentResult.W:N0}mm | Utilization: {currentResult.Util:N1}% | Wastage: {currentResult.Waste:N1}%";

            // Update current layout stats
            txtCurrentLayoutStats.Text = $"Sheet: {currentResult.Ref} | Size: {currentResult.L:N0}×{currentResult.W:N0}mm | Parts: {partsOnSheet.Count} | Utilization: {currentResult.Util:N1}% | Wastage: {currentResult.Waste:N1}%";
        }

        // ================= UI CONTROLS =================

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
            if (cmbSheetSelector.SelectedIndex >= 0 &&
                cmbSheetSelector.SelectedIndex < _results.Count &&
                _results.Count > 0)
            {
                _currentIndex = cmbSheetSelector.SelectedIndex;
                if (_results[_currentIndex].Ref != "TOTAL")
                {
                    DrawCurrentLayout();
                }
                UpdateLayoutCount();
            }
        }

        private void PrevLayout_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0 && _results.Count > 0)
            {
                _currentIndex--;
                cmbSheetSelector.SelectedIndex = _currentIndex;
                DrawCurrentLayout();
                UpdateLayoutCount();
            }
        }

        private void NextLayout_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _results.Count - 1 && _results.Count > 0)
            {
                _currentIndex++;
                cmbSheetSelector.SelectedIndex = _currentIndex;
                DrawCurrentLayout();
                UpdateLayoutCount();
            }
        }

        private void UpdateLayoutCount()
        {
            int totalSheets = _results.Count(r => r.Ref != "TOTAL");
            txtLayoutNum.Text = $" {_currentIndex + 1}/{totalSheets} ";
        }

        private void ClearOptimization_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Clear all data?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
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

                txtStockSummary.Text = "No stock added";
                txtPartsSummary.Text = "No parts added";
                txtCurrentLayoutStats.Text = "Select a layout to view";

                _currentIndex = 0;
                PreviewCanvas.Children.Clear();
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
                        foreach (var r in _results.Where(r => r.Ref != "TOTAL"))
                        {
                            writer.WriteLine($"{r.Ref},{r.L},{r.W},{r.Used},{r.Util:N2},{r.Waste:N2},{r.Area:N4}");
                        }
                        int totalUsed = _results.Count(r => r.Ref != "TOTAL");
                        writer.WriteLine($"TOTAL,,,{totalUsed},,{_overallUtilization:N2},{_overallWastage:N2}");
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

            foreach (var r in _results.Where(r => r.Ref != "TOTAL"))
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
                    Text = $"{r.Ref}: {r.L:N0} × {r.W:N0} mm\nUtilization: {r.Util:N2}%  |  Wastage: {r.Waste:N2}%",
                    FontSize = 13
                };
                border.Child = text;
                stack.Children.Add(border);
            }

            int totalUsed = _results.Count(r => r.Ref != "TOTAL");
            var totalBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 10, 0, 0)
            };
            var totalText = new TextBlock
            {
                Text = $"TOTAL: {totalUsed} sheets used",
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
        public string SheetRef { get; set; } = "";
        public int SheetNum { get; set; }
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
        public bool IsRotated { get; set; }
        public string Sheet { get; set; } = "";
        public int SheetNum { get; set; }
    }
}