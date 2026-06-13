using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace ProGlassAutomation.Views.Optimization
{
    // =====================================================
    // ENUMS
    // =====================================================

    public enum RotationPolicy
    {
        None = 0,
        Rotate90 = 1,
        BestFit = 2,
        FirstFit = 3,
        StripFill = 4,
        ColumnFill = 5,
        RowFill = 6
    }

    public enum NestingStrategy
    {
        BestArea,
        ShortSideFit,
        LongSideFit,
        Guillotine,
        Skyline,
        BottomLeft,
        BestPerimeter
    }

    // =====================================================
    // ENGINE CLASS
    // =====================================================

    public class OptimizationEngine
    {
        // =====================================================
        // TRIM & CUT SETTINGS
        // =====================================================

        private double _lr = 15, _br = 15, _tr = 15, _rm = 15, _kerf = 4.0, _breakout = 4.0;
        private double _bridgeWidth = 15;
        private RotationPolicy _rotationPolicy = RotationPolicy.BestFit;
        private NestingStrategy _nestingStrategy = NestingStrategy.BestArea;

        // =====================================================
        // ENGINE STATE
        // =====================================================

        private List<MaxRect> _freeRects = new List<MaxRect>();
        private List<RemnantPiece> _remnants = new List<RemnantPiece>();
        private CostModel _costModel = new CostModel();
        private PlacementConstraint _constraints = new PlacementConstraint();
        private bool _twoPassEnabled = true;
        private bool _remnantReuseEnabled = true;
        private List<CutSequence> _cutSequences = new List<CutSequence>();

        // =====================================================
        // RESULTS
        // =====================================================

        private double _overallUtilization = 0;
        private double _overallWastage = 0;
        private int _totalPartsCut = 0;
        private int _totalPartsUnplaced = 0;
        private double _usedSQM = 0;
        private int _currentIndex = 0;

        // =====================================================
        // PUBLIC API
        // =====================================================

        public void Configure(double lm, double rm, double tm, double bm, double kerf, double breakout)
        {
            _lr = lm; _rm = rm; _tr = tm; _br = bm;
            _kerf = kerf; _breakout = breakout;
        }

        public void SetRotationPolicy(RotationPolicy policy)
        {
            _rotationPolicy = policy;
        }

        public void SetStrategy(NestingStrategy strategy)
        {
            _nestingStrategy = strategy;
        }

        // =====================================================
        // MAIN OPTIMIZATION ENGINE
        // =====================================================

        public void ExecuteNesting(List<StockSheet> stockSheets, List<CutPart> cutParts,
                                ObservableCollection<OptimizationResult> results,
                                List<PlacedPart> allPlacedParts,
                                ObservableCollection<OptimizationJob> savedJobs)
        {
            // Clear all results
            results.Clear();
            allPlacedParts.Clear();
            _remnants.Clear();
            _cutSequences.Clear();
            _freeRects.Clear();

            // Expand parts by quantity
            var allParts = new List<CutPart>();
            int partId = 1;
            foreach (var p in cutParts)
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

            // CRITICAL: Sort by height (vertical), then width - fills gaps better
            allParts = allParts.OrderByDescending(x => Math.Max(x.L, x.W))
                               .ThenBy(x => Math.Min(x.L, x.W))
                               .ThenByDescending(x => x.L * x.W)
                               .ToList();

            // CRITICAL: Enable rotation for EVERYTHING
            foreach (var p in allParts) p.Rot = true;

            var sortedStock = stockSheets.OrderByDescending(s => s.L * s.W).ToList();

            // Statistics
            double totalUsedAreaAll = 0, totalAreaUsedSheets = 0;
            int totalSheetsUsed = 0;
            _totalPartsCut = 0;
            _totalPartsUnplaced = 0;
            _usedSQM = 0;

            // FORCED: Use BestArea strategy for maximum utilization
            _nestingStrategy = NestingStrategy.BestArea;

            // Process each stock type
            foreach (var stock in sortedStock)
            {
                double oneSheetArea = stock.L * stock.W / 1000000.0;
                int availableQty = stock.Qty;
                if (availableQty <= 0) continue;

                // Calculate usable area after trim margins
                double usableW = stock.L - _lr - _rm;
                double usableH = stock.W - _tr - _br;

                if (usableW < _breakout || usableH < _breakout) continue;

                int sheetsUsedForThisStock = 0;

                // Process each sheet
                for (int sheetNum = 0; sheetNum < availableQty; sheetNum++)
                {
                    var remaining = allParts.Where(p => !p.IsPlaced).ToList();
                    if (remaining.Count == 0) break;

                    // Initialize MaxRects
                    _freeRects.Clear();
                    _freeRects.Add(new MaxRect(0, 0, usableW, usableH));

                    double usedAreaThisSheet = 0;
                    int placedOnThisSheet = 0;
                    var placedOnThisSheetList = new List<PlacedPart>();
                    var sheetCuts = new List<CutOperation>();
                    var placedCoordinates = new List<(double X, double Y, double W, double H)>();

                    // Create cut sequence for digital twin
                    var cutSequence = new CutSequence
                    {
                        SheetId = $"{stock.Ref}-{sheetNum + 1}"
                    };

                    // Place parts with lookahead scoring
                    foreach (var part in remaining)
                    {
                        if (part.IsPlaced) continue;

                        // Validate constraints before placement
                        if (!_constraints.ValidatePlacement(part.L, part.W))
                            continue;

                        // Evaluate both orientations
                        var placement = EvaluatePlacementWithLookahead(part, _kerf);

                        if (placement == null) continue;

                        // Verify no overlap BEFORE committing placement
                        double newX = placement.X + _lr;
                        double newY = placement.Y + _tr;
                        double newW = placement.IsRotated ? part.W : part.L;
                        double newH = placement.IsRotated ? part.L : part.W;

                        // Check overlap with already placed parts
                        if (HasOverlap(newX, newY, newW, newH, placedCoordinates))
                        {
                            System.Diagnostics.Debug.WriteLine($"OVERLAP PREVENTED: {part.Ref} @ ({newX},{newY},{newW},{newH})");
                            continue;
                        }

                        // Commit placement
                        part.IsPlaced = true;
                        part.PlacedX = placement.X;
                        part.PlacedY = placement.Y;
                        part.PlacedW = newW;
                        part.PlacedH = newH;

                        placedOnThisSheetList.Add(new PlacedPart
                        {
                            Ref = part.Ref,
                            X = newX,
                            Y = newY,
                            L = newW,
                            W = newH,
                            IsRotated = placement.IsRotated,
                            Sheet = stock.Ref,
                            SheetNum = sheetNum + 1
                        });

                        // Track coordinates for overlap detection
                        placedCoordinates.Add((newX, newY, newW, newH));

                        // Track area
                        double partArea = (newW * newH) / 1000000.0;
                        _usedSQM += partArea;
                        placedOnThisSheet++;
                        _totalPartsCut++;

                        // Update free rects (Guillotine split)
                        UpdateFreeRectsWithGuillotine(placement, newW + _kerf, newH + _kerf);

                        // Generate toolpath operations
                        GenerateToolpathForPart(part, cutSequence, placement.X + _lr, placement.Y + _tr, _kerf);
                    }

                    // Track usable remnants for reuse
                    if (_remnantReuseEnabled)
                    {
                        CollectRemnants(stock.Ref, sheetNum + 1);
                    }

                    // Record sheet result
                    if (placedOnThisSheet > 0)
                    {
                        usedAreaThisSheet = placedOnThisSheetList.Sum(p => (p.L * p.W) / 1000000.0);

                        sheetsUsedForThisStock++;
                        totalSheetsUsed++;
                        totalAreaUsedSheets += oneSheetArea;

                        double thisSheetUtil = oneSheetArea > 0 ? (usedAreaThisSheet / oneSheetArea) * 100.0 : 0.0;

                        if (thisSheetUtil > 100.0)
                        {
                            System.Diagnostics.Debug.WriteLine($"WARNING: Sheet utilization >100% on {stock.Ref}-{sheetsUsedForThisStock}. Clamping values.");
                            usedAreaThisSheet = Math.Min(usedAreaThisSheet, oneSheetArea);
                            thisSheetUtil = oneSheetArea > 0 ? (usedAreaThisSheet / oneSheetArea) * 100.0 : 0.0;
                        }

                        System.Diagnostics.Debug.WriteLine($"Sheet {sheetsUsedForThisStock}: Used={usedAreaThisSheet.ToString("F4")} m², Sheet={oneSheetArea.ToString("F4")} m², Util={thisSheetUtil.ToString("F2")}%");

                        results.Add(new OptimizationResult
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

                        // Add used area to totals
                        totalUsedAreaAll += usedAreaThisSheet;

                        allPlacedParts.AddRange(placedOnThisSheetList);
                        _cutSequences.Add(cutSequence);
                    }

                    // Continue to next sheet if no parts placed this sheet
                    if (placedOnThisSheet == 0) break;
                }
            }

            // Second pass - placing additional parts on remnants
            double areaFromRemnants = 0;
            if (_twoPassEnabled && _remnants.Count > 0)
            {
                var unplacedParts = allParts.Where(p => !p.IsPlaced).ToList();
                if (unplacedParts.Count > 0)
                {
                    areaFromRemnants = RunSecondPassNesting(unplacedParts);
                }
            }

            // Update statistics
            _totalPartsUnplaced = allParts.Count(p => !p.IsPlaced);

            // Calculate utilization
            var sheetResults = results.Where(r => r.Ref != "TOTAL").ToList();
            if (sheetResults.Count > 0)
            {
                double totalPartArea = sheetResults.Sum(r => r.Area);
                double totalFullArea = sheetResults.Sum(r => (r.L * r.W) / 1000000.0);

                System.Diagnostics.Debug.WriteLine($"DEBUG: {sheetResults.Count} sheets, PartArea={totalPartArea:F4} m², FullArea={totalFullArea:F4} m²");

                _overallUtilization = totalFullArea > 0 ? (totalPartArea / totalFullArea) * 100 : 0;
                _overallWastage = 100 - _overallUtilization;

                if (_overallUtilization > 100) _overallUtilization = 100;
                if (_overallUtilization < 0) _overallUtilization = 0;
                _overallWastage = 100 - _overallUtilization;
            }
            else
            {
                _overallUtilization = 0;
                _overallWastage = 100;
            }

            // Add TOTAL result
            results.Add(new OptimizationResult
            {
                Ref = "TOTAL",
                L = 0,
                W = 0,
                Used = totalSheetsUsed,
                Area = totalUsedAreaAll,
                Util = _overallUtilization,
                Waste = _overallWastage
            });
        }

        // =====================================================
        // OVERLAP DETECTION
        // =====================================================

        private bool HasOverlap(double x, double y, double w, double h, List<(double X, double Y, double W, double H)> placedRects)
        {
            const double eps = 0.001;
            double x2 = x + w;
            double y2 = y + h;

            foreach (var rect in placedRects)
            {
                double rx1 = rect.X;
                double ry1 = rect.Y;
                double rx2 = rect.X + rect.W;
                double ry2 = rect.Y + rect.H;

                bool notOverlap = (x2 <= rx1 + eps) || (x + eps >= rx2) ||
                                 (y2 <= ry1 + eps) || (y + eps >= ry2);

                if (!notOverlap)
                {
                    return true;
                }
            }
            return false;
        }

        // =====================================================
        // PATCH 2: STRATEGY SELECTION
        // =====================================================

        private NestingStrategy SelectBestStrategy(List<CutPart> parts, StockSheet stock, double kerf)
        {
            var strategies = new[]
            {
                NestingStrategy.BestArea,
                NestingStrategy.Guillotine,
                NestingStrategy.ShortSideFit,
                NestingStrategy.BottomLeft
            };

            NestingStrategy bestStrategy = NestingStrategy.BestArea;
            double bestUtil = 0;

            var testParts = parts.Take(15).ToList();

            foreach (var strat in strategies)
            {
                var testRects = new List<MaxRect>();
                testRects.Add(new MaxRect(0, 0, stock.L - _lr - _rm, stock.W - _tr - _br));

                double testUsed = 0;
                var testPartsCopy = testParts.Select(p => new CutPart
                {
                    Id = p.Id,
                    Ref = p.Ref,
                    L = p.L,
                    W = p.W,
                    Rot = p.Rot
                }).ToList();

                foreach (var p in testPartsCopy)
                {
                    var placement = FindBestMaxRect(p.L + kerf, p.W + kerf, p.Rot, testRects, strat);
                    if (placement != null)
                    {
                        testUsed += p.L * p.W;
                        UpdateFreeRectsTest(placement, p.L + kerf, p.W + kerf, testRects);
                    }
                }

                double util = (testUsed / (stock.L * stock.W)) * 100;
                if (util > bestUtil)
                {
                    bestUtil = util;
                    bestStrategy = strat;
                }
            }

            return bestStrategy;
        }

        private MaxRect FindBestMaxRect(double w, double h, bool canRotate, List<MaxRect> freeRects, NestingStrategy strategy)
        {
            var matches = FindMatchingMaxRects(w, h, freeRects);
            if (matches.Count > 0)
            {
                return matches.OrderBy(r => ScorePlacement(r, w, h, strategy)).First();
            }
            return null;
        }

        private List<MaxRect> FindMatchingMaxRects(double w, double h, List<MaxRect> freeRects)
        {
            return freeRects.Where(r => r.Fits(w, h)).OrderBy(r => r.Area).ToList();
        }

        private double ScorePlacement(MaxRect rect, double w, double h, NestingStrategy strategy)
        {
            double score = 0;

            switch (strategy)
            {
                case NestingStrategy.BestArea:
                case NestingStrategy.ShortSideFit:
                    score = rect.Area - (w * h);
                    double shortSide = Math.Min(rect.Width - w, rect.Height - h);
                    score += shortSide * 0.1;
                    break;

                case NestingStrategy.LongSideFit:
                    double leftover = Math.Max(rect.Width - w, rect.Height - h);
                    score = leftover * (rect.Width + rect.Height);
                    break;

                case NestingStrategy.BestPerimeter:
                    score = (rect.Width + rect.Height) - (w + h);
                    break;

                case NestingStrategy.BottomLeft:
                    score = (rect.Y + rect.X) * 1000;
                    break;

                case NestingStrategy.Skyline:
                    score = rect.Y * 1000 + rect.X;
                    break;

                case NestingStrategy.Guillotine:
                    double vertSplit = Math.Abs(rect.Width - w);
                    double horizSplit = Math.Abs(rect.Height - h);
                    score = Math.Min(vertSplit, horizSplit);
                    break;
            }

            return score;
        }

        // =====================================================
        // PATCH 3: LOOKAHEAD PLACEMENT EVALUATION
        // =====================================================

        private PlacementNode EvaluatePlacementWithLookahead(CutPart part, double kerf)
        {
            double w = part.L + kerf;
            double h = part.W + kerf;
            double wRot = part.W + kerf;
            double hRot = part.L + kerf;

            double bestScore = double.MaxValue;
            PlacementNode bestPlacement = null;

            foreach (var rect in _freeRects)
            {
                // Non-rotated
                if (rect.Fits(w, h))
                {
                    double score = ScorePlacement(rect, w, h, _nestingStrategy);

                    double leftoverArea = rect.Area - (w * h);
                    double shortSide = Math.Min(rect.Width - w, rect.Height - h);

                    score = leftoverArea * 1000 + shortSide * 10;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestPlacement = new PlacementNode
                        {
                            X = rect.X,
                            Y = rect.Y,
                            Width = part.L,
                            Height = part.W,
                            IsRotated = false,
                            Score = score
                        };
                    }
                }

                // Rotated - ENFORCE THIS MORE
                if (part.Rot && _constraints.AllowRotation && rect.Fits(wRot, hRot))
                {
                    double score = ScorePlacement(rect, wRot, hRot, _nestingStrategy);

                    double leftoverArea = rect.Area - (wRot * hRot);
                    double shortSide = Math.Min(rect.Width - wRot, rect.Height - hRot);

                    score = leftoverArea * 1000 + shortSide * 10;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestPlacement = new PlacementNode
                        {
                            X = rect.X,
                            Y = rect.Y,
                            Width = part.W,
                            Height = part.L,
                            IsRotated = true,
                            Score = score
                        };
                    }
                }
            }

            return bestPlacement;
        }

        private double ScoreLookahead(List<MaxRect> freeRects, CutPart currentPart)
        {
            if (freeRects == null || freeRects.Count == 0) return 0;

            double totalArea = freeRects.Sum(r => r.Area);

            double fragPenalty = 0;
            foreach (var r in freeRects)
            {
                double shortSide = Math.Min(r.Width, r.Height);
                double longSide = Math.Max(r.Width, r.Height);
                if (longSide <= 0) continue;
                double aspectFactor = 1.0 - (shortSide / longSide);
                fragPenalty += r.Area * aspectFactor;
            }

            double smallCount = freeRects.Count(r => Math.Min(r.Width, r.Height) < Math.Max(_constraints.MinRemnantSize, Math.Min(currentPart.L, currentPart.W) * 0.5));

            double score = totalArea - (fragPenalty * 0.35) - (smallCount * (currentPart.L * currentPart.W) * 0.5);
            return score;
        }

        // =====================================================
        // PATCH 1: MAXRECTS CORE OPERATIONS
        // =====================================================

        private void UpdateFreeRectsWithGuillotine(PlacementNode placement, double w, double h)
        {
            const double eps = 0.01;
            var usedRect = _freeRects.FirstOrDefault(r =>
                placement.X + eps >= r.X && placement.Y + eps >= r.Y &&
                placement.X + w <= r.X + r.Width + eps && placement.Y + h <= r.Y + r.Height + eps);

            if (usedRect == null) return;

            _freeRects.Remove(usedRect);

            double rightW = usedRect.Width - w;
            double bottomH = usedRect.Height - h;

            if (rightW > _constraints.MinRemnantSize)
            {
                _freeRects.Add(new MaxRect(placement.X + w, placement.Y, rightW, usedRect.Height));
            }

            if (bottomH > _constraints.MinRemnantSize)
            {
                _freeRects.Add(new MaxRect(placement.X, placement.Y + h, usedRect.Width, bottomH));
            }

            MergeFreeRects();
            PruneContainedFreeRects(_freeRects);
        }

        private void UpdateFreeRectsTest(MaxRect placement, double w, double h, List<MaxRect> freeRects)
        {
            const double eps = 0.01;
            var usedRect = freeRects.FirstOrDefault(r =>
                placement.X + eps >= r.X && placement.Y + eps >= r.Y &&
                placement.X + w <= r.X + r.Width + eps && placement.Y + h <= r.Y + r.Height + eps);

            if (usedRect == null) return;

            freeRects.Remove(usedRect);

            double rightW = usedRect.Width - w;
            double bottomH = usedRect.Height - h;

            if (rightW > _constraints.MinRemnantSize)
                freeRects.Add(new MaxRect(placement.X + w, placement.Y, rightW, usedRect.Height));

            if (bottomH > _constraints.MinRemnantSize)
                freeRects.Add(new MaxRect(placement.X, placement.Y + h, usedRect.Width, bottomH));

            PruneContainedFreeRects(freeRects);
        }

        private void SimulatePlacement(MaxRect rect, double w, double h, bool rotated, List<MaxRect> freeRects)
        {
            const double eps = 0.01;
            var used = freeRects.FirstOrDefault(r =>
                rect.X + eps >= r.X && rect.Y + eps >= r.Y &&
                rect.X + w <= r.X + r.Width + eps && rect.Y + h <= r.Y + r.Height + eps);

            if (used == null) return;

            freeRects.Remove(used);

            double rightW = used.Width - w;
            double bottomH = used.Height - h;

            if (rightW > _constraints.MinRemnantSize)
                freeRects.Add(new MaxRect(rect.X + w, rect.Y, rightW, used.Height));

            if (bottomH > _constraints.MinRemnantSize)
                freeRects.Add(new MaxRect(rect.X, rect.Y + h, used.Width, bottomH));

            PruneContainedFreeRects(freeRects);
        }

        private void MergeFreeRects()
        {
            bool merged;
            do
            {
                merged = false;
                for (int i = 0; i < _freeRects.Count && !merged; i++)
                {
                    for (int j = i + 1; j < _freeRects.Count && !merged; j++)
                    {
                        var a = _freeRects[i];
                        var b = _freeRects[j];

                        // Horizontal merge
                        if (Math.Abs(a.Y - b.Y) < 0.01 && Math.Abs(a.Height - b.Height) < 0.01)
                        {
                            if (Math.Abs(a.X + a.Width - b.X) < 0.01)
                            {
                                _freeRects[i] = new MaxRect(a.X, a.Y, a.Width + b.Width, a.Height);
                                _freeRects.RemoveAt(j);
                                merged = true;
                            }
                            else if (Math.Abs(b.X + b.Width - a.X) < 0.01)
                            {
                                _freeRects[i] = new MaxRect(b.X, b.Y, a.Width + b.Width, b.Height);
                                _freeRects.RemoveAt(j);
                                merged = true;
                            }
                        }
                        // Vertical merge
                        else if (Math.Abs(a.X - b.X) < 0.01 && Math.Abs(a.Width - b.Width) < 0.01)
                        {
                            if (Math.Abs(a.Y + a.Height - b.Y) < 0.01)
                            {
                                _freeRects[i] = new MaxRect(a.X, a.Y, a.Width, a.Height + b.Height);
                                _freeRects.RemoveAt(j);
                                merged = true;
                            }
                            else if (Math.Abs(b.Y + b.Height - a.Y) < 0.01)
                            {
                                _freeRects[i] = new MaxRect(b.X, b.Y, b.Width, a.Height + b.Height);
                                _freeRects.RemoveAt(j);
                                merged = true;
                            }
                        }
                    }
                }
            } while (merged);
            PruneContainedFreeRects(_freeRects);
        }

        private void PruneContainedFreeRects(List<MaxRect> list)
        {
            bool removed;
            do
            {
                removed = false;
                for (int i = 0; i < list.Count; i++)
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (i == j) continue;
                        var a = list[i];
                        var b = list[j];
                        if (a.X + 0.01 >= b.X && a.Y + 0.01 >= b.Y && a.X + a.Width <= b.X + b.Width + 0.01 && a.Y + a.Height <= b.Y + b.Height + 0.01)
                        {
                            list.RemoveAt(i);
                            removed = true;
                            break;
                        }
                    }
                    if (removed) break;
                }
            } while (removed);
        }

        // =====================================================
        // PATCH 5 & 9: REMNANT COLLECTION & SCORING
        // =====================================================

        private void CollectRemnants(string sheetRef, int sheetNum)
        {
            foreach (var rect in _freeRects)
            {
                if (rect.Area > 100000 && Math.Min(rect.Width, rect.Height) > _constraints.MinRemnantSize)
                {
                    var remnant = new RemnantPiece
                    {
                        Id = $"R{_remnants.Count + 1}",
                        X = rect.X,
                        Y = rect.Y,
                        Width = rect.Width,
                        Height = rect.Height,
                        SourceSheet = sheetRef,
                        SheetNumber = sheetNum,
                        CreatedAt = DateTime.Now
                    };

                    remnant.ValueScore = remnant.UsabilityScore;

                    if (remnant.ValueScore > 0.3)
                    {
                        _remnants.Add(remnant);
                    }
                }
            }
        }

        // =====================================================
        // PATCH 4: SECOND PASS NESTING
        // =====================================================

        private double RunSecondPassNesting(List<CutPart> unplacedParts)
        {
            if (!_twoPassEnabled || unplacedParts.Count == 0) return 0;
            if (_remnants.Count == 0) return 0;

            var usableRemnants = _remnants
                .Where(r => !r.IsReused && r.ValueScore > 0.2)
                .OrderByDescending(r => r.Area)
                .ToList();

            if (usableRemnants.Count == 0) return 0;

            double partsAreaOnRemnants = 0;

            foreach (var remnant in usableRemnants)
            {
                var partsOnThisRemnant = new List<CutPart>();
                var placedOnRemnant = new List<(double X, double Y, double W, double H)>();

                foreach (var part in unplacedParts)
                {
                    if (part.IsPlaced) continue;

                    double pW = part.L + _kerf;
                    double pH = part.W + _kerf;

                    bool canFitNormal = pW <= remnant.Width && pH <= remnant.Height;

                    bool canFitRotated = false;
                    if (part.Rot)
                    {
                        canFitRotated = pH <= remnant.Width && pW <= remnant.Height;
                    }

                    if (!canFitNormal && !canFitRotated) continue;

                    double newX = remnant.X + _kerf;
                    double newY = remnant.Y + _kerf;
                    double newW = canFitRotated && !canFitNormal ? part.W : part.L;
                    double newH = canFitRotated && !canFitNormal ? part.L : part.W;

                    if (HasOverlap(newX, newY, newW, newH, placedOnRemnant))
                        continue;

                    bool rotated = canFitRotated && !canFitNormal;

                    part.IsPlaced = true;
                    part.PlacedX = remnant.X + _kerf;
                    part.PlacedY = remnant.Y + _kerf;
                    part.PlacedW = rotated ? part.W : part.L;
                    part.PlacedH = rotated ? part.L : part.W;

                    double partArea = (part.L * part.W) / 1000000.0;
                    partsAreaOnRemnants += partArea;
                    _usedSQM += partArea;
                    _totalPartsCut++;

                    partsOnThisRemnant.Add(part);
                    placedOnRemnant.Add((newX, newY, newW, newH));
                }

                if (partsOnThisRemnant.Count > 0)
                {
                    remnant.IsReused = true;
                }
            }

            _totalPartsUnplaced = unplacedParts.Count(p => !p.IsPlaced);
            return partsAreaOnRemnants;
        }

        // =====================================================
        // PATCH 10: DIGITAL TWIN TOOLPATH
        // =====================================================

        private void GenerateToolpathForPart(CutPart part, CutSequence sequence, double offsetX, double offsetY, double kerf)
        {
            double x = part.PlacedX;
            double y = part.PlacedY;
            double w = part.PlacedW;
            double h = part.PlacedH;

            // 4 cuts for rectangular part
            sequence.Operations.Add(new ToolpathOperation
            {
                PartId = part.Ref,
                Sequence = sequence.Operations.Count + 1,
                StartX = x + offsetX,
                StartY = y + offsetY,
                EndX = x + w + offsetX,
                EndY = y + offsetY,
                ToolType = "Cut"
            });

            sequence.Operations.Add(new ToolpathOperation
            {
                PartId = part.Ref,
                Sequence = sequence.Operations.Count + 1,
                StartX = x + w + offsetX,
                StartY = y + offsetY,
                EndX = x + w + offsetX,
                EndY = y + h + offsetY,
                ToolType = "Cut"
            });

            sequence.Operations.Add(new ToolpathOperation
            {
                PartId = part.Ref,
                Sequence = sequence.Operations.Count + 1,
                StartX = x + w + offsetX,
                StartY = y + h + offsetY,
                EndX = x + offsetX,
                EndY = y + h + offsetY,
                ToolType = "Cut"
            });

            sequence.Operations.Add(new ToolpathOperation
            {
                PartId = part.Ref,
                Sequence = sequence.Operations.Count + 1,
                StartX = x + offsetX,
                StartY = y + h + offsetY,
                EndX = x + offsetX,
                EndY = y + offsetY,
                ToolType = "Cut"
            });

            // Add bridge cuts if enabled
            if (_bridgeWidth > 0)
            {
                AddBridgeCuts(part, sequence, offsetX, offsetY);
            }
        }

        private void AddBridgeCuts(CutPart part, CutSequence sequence, double offsetX, double offsetY)
        {
            double x = part.PlacedX;
            double y = part.PlacedY;
            double w = part.PlacedW;
            double h = part.PlacedH;

            int bridgeCount = (int)Math.Floor(h / (_bridgeWidth * 4));
            if (bridgeCount < 1) bridgeCount = 1;

            double bridgeSpacing = h / (bridgeCount + 1);

            for (int i = 1; i <= bridgeCount; i++)
            {
                double bridgeY = y + (bridgeSpacing * i);

                sequence.Operations.Add(new ToolpathOperation
                {
                    PartId = part.Ref,
                    Sequence = sequence.Operations.Count + 1,
                    StartX = x + offsetX,
                    StartY = bridgeY + offsetY,
                    EndX = x + w + offsetX,
                    EndY = bridgeY + offsetY,
                    ToolType = "BridgeCut",
                    IsBridgeCut = true
                });

                sequence.BridgeCount++;
            }
        }

        // =====================================================
        // COST CALCULATION
        // =====================================================

        public double CalculateTotalCost(double usedSQM, List<OptimizationResult> results, string currencySymbol = "AED ")
        {
            if (usedSQM <= 0) return 0;

            double stockCost = usedSQM * _costModel.StockPricePerSQM;

            // Add waste penalty
            double wasteArea = 0;
            foreach (var result in results.Where(r => r.Ref != "TOTAL"))
            {
                double sheetArea = result.L * result.W / 1000000.0;
                wasteArea += sheetArea - result.Area;
            }

            stockCost += wasteArea * _costModel.WastePenaltyPerSQM / 1000;

            // Add remnant credit
            double remnantCredit = 0;
            foreach (var rem in _remnants.Where(r => !r.IsReused))
            {
                remnantCredit += _costModel.CalculateRemnantCredit(rem.Area);
            }
            stockCost -= remnantCredit;

            // Add kerf cost
            double totalKerf = 0;
            foreach (var seq in _cutSequences)
            {
                totalKerf += seq.TotalKerfLength;
            }
            stockCost += totalKerf * _costModel.KerfCostPerMm;

            return stockCost;
        }

        // (Removed - unused WinForms compatibility methods) //

        // =====================================================
        // PROPERTIES
        // =====================================================

        public double OverallUtilization => _overallUtilization;
        public double OverallWastage => _overallWastage;
        public int TotalPartsCut => _totalPartsCut;
        public int TotalPartsUnplaced => _totalPartsUnplaced;
        public double UsedSQM => _usedSQM;
        public int CurrentIndex => _currentIndex;

        // =====================================================
        // PUBLIC API - Returns placed parts
        // =====================================================

        public List<RemnantPiece> GetRemnants()
        {
            return _remnants;
        }

        public List<CutSequence> GetCutSequences()
        {
            return _cutSequences;
        }
    }

    // =====================================================
    // DATA CLASSES
    // =====================================================

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

    public class OptimizationJob
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public string StockJson { get; set; }
        public string PartsJson { get; set; }
        public int SheetsUsed { get; set; }
        public double Utilization { get; set; }
    }

    public class CutOperation
    {
        public string PartRef { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsRotated { get; set; }
    }

    // =====================================================
    // PATCH 1: MAXRECTS CLASSES
    // =====================================================

    public class MaxRect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public double Area => Width * Height;
        public double ShortSide => Math.Min(Width, Height);
        public double LongSide => Math.Max(Width, Height);
        public double AspectRatio => Width > 0 ? LongSide / ShortSide : double.MaxValue;

        public MaxRect() { }

        public MaxRect(double x, double y, double w, double h)
        {
            X = x; Y = y; Width = w; Height = h;
        }

        public bool Fits(double w, double h) => Width >= w && Height >= h;

        public MaxRect Clone() => new MaxRect(X, Y, Width, Height);
    }

    // =====================================================
    // PATCH 3: PLACEMENT NODE
    // =====================================================

    public class PlacementNode
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsRotated { get; set; }
        public double Score { get; set; }
    }

    // =====================================================
    // PATCH 5 & 9: REMNANT CLASSES
    // =====================================================

    public class RemnantPiece
    {
        public string Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string SourceSheet { get; set; }
        public int SheetNumber { get; set; }
        public bool IsReused { get; set; }
        public double ValueScore { get; set; }
        public DateTime CreatedAt { get; set; }

        public double Area => Width * Height;

        public double UsabilityScore
        {
            get
            {
                double aspectScore = Math.Min(Width, Height) / Math.Max(Width, Height);
                double sizeScore = Math.Min(Width, Height) > 200 ? 1.0 :
                                  Math.Min(Width, Height) > 100 ? 0.7 : 0.3;
                double areaScore = Area > 500000 ? 1.0 : Area > 200000 ? 0.6 : 0.2;
                return (aspectScore * 0.4 + sizeScore * 0.3 + areaScore * 0.3);
            }
        }
    }

    // =====================================================
    // PATCH 8: CONSTRAINT CLASSES
    // =====================================================

    public class PlacementConstraint
    {
        public double MinPartSize { get; set; } = 50;
        public double SafetyMarginX { get; set; } = 0;
        public double SafetyMarginY { get; set; } = 0;
        public double KerfCompensation { get; set; } = 4.0;
        public double MinRemnantSize { get; set; } = 20;
        public bool AllowRotation { get; set; } = true;
        public double MaxAspectRatio { get; set; } = 10.0;

        public bool ValidatePlacement(double w, double h)
        {
            if (w < 20 || h < 20) return false;
            return true;
        }
    }

    // =====================================================
    // PATCH 6: COST MODEL
    // =====================================================

    public class CostModel
    {
        public double StockPricePerSQM { get; set; } = 250;
        public double WastePenaltyPerSQM { get; set; } = 50;
        public double RemnantCreditPerSQM { get; set; } = 25;
        public double KerfCostPerMm { get; set; } = 0.01;
        public double OperatingCostPerHour { get; set; } = 150;
        public double SetupCostPerJob { get; set; } = 50;

        public double CalculateSheetCost(double usedArea, double sheetArea, double kerfLength)
        {
            double stockCost = (sheetArea / 1000000) * StockPricePerSQM;
            double wasteCost = ((sheetArea - usedArea) / 1000000) * WastePenaltyPerSQM;
            double kerfCost = kerfLength * KerfCostPerMm;
            return stockCost + wasteCost + kerfCost;
        }

        public double CalculateRemnantCredit(double area)
        {
            return (area / 1000000) * RemnantCreditPerSQM;
        }

        public double CalculateTotalCost(double usedArea, double totalSheetArea, double kerfLength, double remnantArea)
        {
            double cost = CalculateSheetCost(usedArea, totalSheetArea, kerfLength);
            cost -= CalculateRemnantCredit(remnantArea);
            return cost;
        }
    }

    // =====================================================
    // PATCH 10: DIGITAL TWIN CLASSES
    // =====================================================

    public class ToolpathOperation
    {
        public string PartId { get; set; }
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
        public double Depth { get; set; }
        public double Speed { get; set; }
        public int Sequence { get; set; }
        public string ToolType { get; set; } = "StraightCut";
        public bool IsBridgeCut { get; set; }

        public double Length => Math.Sqrt(Math.Pow(EndX - StartX, 2) + Math.Pow(EndY - StartY, 2));
    }

    public class CutSequence
    {
        public string SheetId { get; set; }
        public List<ToolpathOperation> Operations { get; set; } = new List<ToolpathOperation>();
        public double TotalKerfLength { get; set; }
        public double EstimatedTime { get; set; }
        public double BridgeCount { get; set; }

        public CutSequence()
        {
            Operations = new List<ToolpathOperation>();
        }

        public List<System.Windows.Point> GetToolpathPoints()
        {
            var points = new List<System.Windows.Point>();
            foreach (var op in Operations.OrderBy(o => o.Sequence))
            {
                points.Add(new System.Windows.Point(op.StartX, op.StartY));
                points.Add(new System.Windows.Point(op.EndX, op.EndY));
            }
            return points;
        }

        public double CalculateTotalCutLength()
        {
            return Operations.Sum(o => o.Length);
        }

        public double EstimateProcessingTime(double speedMmPerSec = 5000)
        {
            return CalculateTotalCutLength() / speedMmPerSec;
        }
    }

    // =====================================================
    // MULTI-SPECIFICATION CLASSES
    // =====================================================

    public class SpecificationModel
    {
        public string SpecificationName { get; set; }
        public bool IsSelected { get; set; }
        public List<InvoiceItemModel> Items { get; set; } = new List<InvoiceItemModel>();
    }

    public class InvoiceItemModel
    {
        public string GlassRef { get; set; }
        public double Width1 { get; set; }
        public double Height1 { get; set; }
        public double Width2 { get; set; }
        public double Height2 { get; set; }
        public int Qty { get; set; }
    }

    public class CombinedSpecItem
    {
        public string GlassRef { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public int Qty { get; set; }
        public string SourceSpec { get; set; }
    }
}