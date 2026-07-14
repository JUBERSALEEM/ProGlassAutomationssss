using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ProGlassAutomation.Views.Optimization
{
    public enum RotationMode
    {
        None,
        Rotate90,
        BestFit
    }

    public class OptimizationEngine
    {
        private const double Eps = 1e-6;

        public OptimizationResult Execute(
            List<StockSheet> stockSheets,
            List<CutPart> parts,
            double trimLeft,
            double trimRight,
            double trimTop,
            double trimBottom,
            double kerf,
            RotationMode rotationMode)
        {
            var sw = Stopwatch.StartNew();
            var result = new OptimizationResult();

            if (stockSheets == null || parts == null || !stockSheets.Any() || !parts.Any())
            {
                sw.Stop();
                result.RuntimeMs = sw.ElapsedMilliseconds;
                return result;
            }

            // Expand stock inventory: each sheet instance separate
            var stockInstances = new Queue<StockSheet>();
            foreach (var s in stockSheets)
            {
                int qty = Math.Max(1, s.Qty);
                for (int i = 0; i < qty; i++)
                    stockInstances.Enqueue(new StockSheet { L = s.L, W = s.W, Qty = 1 });
            }

            // Expand parts: each instance separate
            var partInstances = new List<CutPart>();
            foreach (var p in parts)
            {
                int qty = Math.Max(1, p.Qty);
                for (int i = 0; i < qty; i++)
                    partInstances.Add(new CutPart
                    {
                        Label = p.Label ?? "",
                        L = p.L, W = p.W, Qty = 1
                    });
            }

            // ✅ Best-Fit Decreasing: sort parts largest first
            partInstances = partInstances.OrderByDescending(p => p.L * p.W).ToList();

            var sheetResults = new List<SheetBuildState>();
            int totalPartsPlaced = 0;
            double totalUsedArea = 0;
            double totalUsableArea = 0;
            double stockL = stockSheets[0].L;
            double stockW = stockSheets[0].W;
            double usableL = stockL - trimLeft - trimRight;
            double usableW = stockW - trimTop - trimBottom;

            if (usableL <= 0 || usableW <= 0)
            {
                sw.Stop();
                result.RuntimeMs = sw.ElapsedMilliseconds;
                return result;
            }

            // ✅ Open an initial pool of empty sheets
            // We'll open new ones as needed during the main loop.
            // Strategy: try to place each part in the BEST existing
            // sheet (smallest leftover). Only open a new sheet if
            // no existing sheet can fit it.
            int currentSheetCapacity = stockInstances.Count;

            foreach (var part in partInstances)
            {
                if (part.L <= 0 || part.W <= 0) continue;

                // Find the best (smallest leftover waste) fit across all open sheets
                PlacementResult? bestOverall = null;
                int bestSheetIdx = -1;
                double bestScore = double.MaxValue;

                for (int sIdx = 0; sIdx < sheetResults.Count; sIdx++)
                {
                    var state = sheetResults[sIdx];
                    var fit = FindBestPlacement(part, state.FreeRects, rotationMode, kerf);
                    if (fit == null) continue;
                    // Score: smaller leftover is better
                    double score = (fit.LeftoverW * fit.LeftoverH) + (fit.RotationPenalty);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestOverall = fit;
                        bestSheetIdx = sIdx;
                    }
                }

                if (bestOverall != null)
                {
                    // Place in the best existing sheet
                    PlacePart(bestOverall, part, sheetResults[bestSheetIdx]);
                    totalPartsPlaced++;
                    totalUsedArea += bestOverall.PhysicalArea;
                }
                else if (stockInstances.Count > 0)
                {
                    // Open a new sheet
                    var stock = stockInstances.Dequeue();
                    var newState = new SheetBuildState
                    {
                        SheetIndex = sheetResults.Count,
                        StockWidth = stock.L,
                        StockHeight = stock.W,
                        UsableWidth = usableL,
                        UsableHeight = usableW,
                        FreeRects = new List<RectF>
                        {
                            new RectF(trimLeft, trimTop, usableL, usableW)
                        }
                    };
                    sheetResults.Add(newState);

                    // Try to place this part on the new sheet
                    var fit = FindBestPlacement(part, newState.FreeRects, rotationMode, kerf);
                    if (fit != null)
                    {
                        PlacePart(fit, part, newState);
                        totalPartsPlaced++;
                        totalUsedArea += fit.PhysicalArea;
                    }
                    // If the new sheet can't even fit the largest remaining part,
                    // something is very wrong - skip.
                }
                // If no existing sheet can fit it AND no new sheets available, skip.
            }

            // Build the result
            var finalSheets = new List<OptimizationResult>();
            int sheetNum = 1;
            foreach (var state in sheetResults)
            {
                if (state.PlacedParts.Count == 0) continue;

                double usedArea = state.PlacedParts.Sum(p => p.L * p.W);
                double utilArea = state.UsableWidth * state.UsableHeight;
                finalSheets.Add(new OptimizationResult
                {
                    Ref = $"S{sheetNum++}",
                    L = state.StockWidth,
                    W = state.StockHeight,
                    Area = usedArea,
                    Util = utilArea > 0 ? Math.Round((usedArea / utilArea) * 100, 2) : 0,
                    Waste = utilArea > 0 ? Math.Round(100 - (usedArea / utilArea) * 100, 2) : 0,
                    PlacedParts = state.PlacedParts
                });
                totalUsableArea += utilArea;
            }

            if (finalSheets.Count > 0)
            {
                result.Ref = finalSheets[0].Ref;
                result.L = finalSheets[0].L;
                result.W = finalSheets[0].W;
                result.Area = result.PlacedParts.Sum(p => p.L * p.W);
                result.Util = totalUsableArea > 0
                    ? Math.Round((totalUsedArea / totalUsableArea) * 100, 2)
                    : 0;
                result.Waste = totalUsableArea > 0
                    ? Math.Round(100 - (totalUsedArea / totalUsableArea) * 100, 2)
                    : 0;
                result.PlacedParts = finalSheets.SelectMany(s => s.PlacedParts).ToList();
            }

            sw.Stop();
            result.RuntimeMs = sw.ElapsedMilliseconds;
            return result;
        }

        // ✅ Place a part and update the sheet state
        private void PlacePart(PlacementResult fit, CutPart sourcePart, SheetBuildState state)
        {
            var placed = new PlacedPart
            {
                Label = sourcePart.Label ?? "",
                X = fit.X,
                Y = fit.Y,
                L = fit.PhysicalW,
                W = fit.PhysicalH,
                Rotated = fit.IsRotated,
                SheetIndex = state.SheetIndex
            };
            state.PlacedParts.Add(placed);

            // ✅ Update the free rects, padding by kerf to enforce the gap
            state.FreeRects = UpdateFreeRects(
                state.FreeRects, fit.X, fit.Y, fit.TotalW, fit.TotalH, 0);  // kerf already in TotalW
        }

        // ✅ Find the best placement for a part in a sheet
        private PlacementResult? FindBestPlacement(
            CutPart part,
            List<RectF> freeRects,
            RotationMode mode,
            double kerf)
        {
            if (part.L <= 0 || part.W <= 0) return null;

            PlacementResult? best = null;
            double bestScore = double.MaxValue;

            var orientations = GetOrientations(part.L, part.W, mode);

            foreach (var rect in freeRects)
            {
                foreach (var o in orientations)
                {
                    // Total width includes the kerf gap on each side
                    double totalW = o.W + kerf * 2;
                    double totalH = o.H + kerf * 2;

                    if (totalW > rect.Width + Eps || totalH > rect.Height + Eps)
                        continue;

                    // Physical size is the part itself, without kerf
                    double physW = o.W;
                    double physH = o.H;

                    // Leftover after placing the part + kerf
                    double leftoverW = rect.Width - totalW;
                    double leftoverH = rect.Height - totalH;
                    double leftoverArea = leftoverW * leftoverH;

                    // Score: prefer the placement with the smallest
                    // leftover area (best-fit). Also prefer bottom-left
                    // (smaller Y, then smaller X) for stable layout.
                    double score = leftoverArea * 10
                                 + (rect.Y * 1000)
                                 + (rect.X * 0.1)
                                 + (o.Rotated ? 1 : 0);

                    if (score < bestScore - Eps)
                    {
                        bestScore = score;
                        best = new PlacementResult
                        {
                            X = rect.X,
                            Y = rect.Y,
                            PhysicalW = physW,
                            PhysicalH = physH,
                            TotalW = totalW,
                            TotalH = totalH,
                            PhysicalArea = physW * physH,
                            LeftoverW = leftoverW,
                            LeftoverH = leftoverH,
                            IsRotated = o.Rotated,
                            RotationPenalty = o.Rotated ? 0.1 : 0
                        };
                    }
                }
            }

            return best;
        }

        private List<(double W, double H, bool Rotated)> GetOrientations(double w, double h, RotationMode mode)
        {
            var list = new List<(double, double, bool)>();
            if (mode == RotationMode.None)
            {
                list.Add((w, h, false));
            }
            else if (mode == RotationMode.Rotate90)
            {
                list.Add((h, w, true));
            }
            else
            {
                list.Add((w, h, false));
                if (Math.Abs(w - h) > Eps)
                    list.Add((h, w, true));
            }
            return list;
        }

        // ✅ Free rectangle in the sheet coordinate system
        private class RectF
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public RectF() { }
            public RectF(double x, double y, double w, double h)
            {
                X = x; Y = y; Width = w; Height = h;
            }
        }

        // ✅ Placement result with all the dimensions we need
        private class PlacementResult
        {
            public double X { get; set; }
            public double Y { get; set; }
            public double PhysicalW { get; set; }   // part width without kerf
            public double PhysicalH { get; set; }   // part height without kerf
            public double TotalW { get; set; }       // part + kerf (used to update free rects)
            public double TotalH { get; set; }
            public double PhysicalArea { get; set; }
            public double LeftoverW { get; set; }
            public double LeftoverH { get; set; }
            public bool IsRotated { get; set; }
            public double RotationPenalty { get; set; }
        }

        // ✅ Sheet being built up (with its free rects and placed parts)
        private class SheetBuildState
        {
            public int SheetIndex { get; set; }
            public double StockWidth { get; set; }
            public double StockHeight { get; set; }
            public double UsableWidth { get; set; }
            public double UsableHeight { get; set; }
            public List<RectF> FreeRects { get; set; } = new List<RectF>();
            public List<PlacedPart> PlacedParts { get; set; } = new List<PlacedPart>();
        }

        // ✅ Update the list of free rectangles after placing a part.
        // The placed rect (x, y, w, h) ALREADY includes the kerf gap.
        private List<RectF> UpdateFreeRects(List<RectF> freeRects, double x, double y, double w, double h, double kerf)
        {
            var result = new List<RectF>();
            double placedRight = x + w;
            double placedBottom = y + h;

            foreach (var rect in freeRects)
            {
                // If the free rect doesn't intersect with the placed rect at all, keep it
                if (placedRight <= rect.X + Eps || x >= rect.X + rect.Width - Eps ||
                    placedBottom <= rect.Y + Eps || y >= rect.Y + rect.Height - Eps)
                {
                    result.Add(rect);
                    continue;
                }

                // Otherwise, split the free rect into up to 4 sub-rects around the placed rect
                if (x > rect.X + Eps)
                    result.Add(new RectF(rect.X, rect.Y, x - rect.X, rect.Height));

                if (placedRight < rect.X + rect.Width - Eps)
                    result.Add(new RectF(placedRight, rect.Y, rect.X + rect.Width - placedRight, rect.Height));

                if (y > rect.Y + Eps)
                    result.Add(new RectF(rect.X, rect.Y, rect.Width, y - rect.Y));

                if (placedBottom < rect.Y + rect.Height - Eps)
                    result.Add(new RectF(rect.X, placedBottom, rect.Width, rect.Y + rect.Height - placedBottom));
            }

            return PruneFreeRects(result);
        }

        private List<RectF> PruneFreeRects(List<RectF> rects)
        {
            if (rects == null || rects.Count == 0) return rects ?? new List<RectF>();

            var kept = new List<RectF>();
            foreach (var r in rects)
            {
                if (r.Width <= Eps || r.Height <= Eps) continue;

                bool contained = false;
                for (int j = 0; j < rects.Count; j++)
                {
                    var b = rects[j];
                    if (ReferenceEquals(r, b)) continue;
                    if (b.Width <= Eps || b.Height <= Eps) continue;

                    if (r.X >= b.X - Eps &&
                        r.Y >= b.Y - Eps &&
                        r.X + r.Width <= b.X + b.Width + Eps &&
                        r.Y + r.Height <= b.Y + b.Height + Eps)
                    {
                        contained = true;
                        break;
                    }
                }

                if (!contained) kept.Add(r);
            }

            return kept;
        }
    }

    // ══════════════════════════════════════════════════════
    // MODEL CLASSES
    // ══════════════════════════════════════════════════════

    public class StockSheet
    {
        public double L { get; set; }
        public double W { get; set; }
        public int Qty { get; set; } = 1;
    }

    public class CutPart
    {
        public string Label { get; set; } = "";
        public double L { get; set; }
        public double W { get; set; }
        public int Qty { get; set; } = 1;
    }

    public class PlacedPart
    {
        public string Label { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double L { get; set; }
        public double W { get; set; }
        public bool Rotated { get; set; }
        public int SheetIndex { get; set; }
    }

    public class OptimizationResult
    {
        public string Ref { get; set; } = "";
        public double L { get; set; }
        public double W { get; set; }
        public double Area { get; set; }
        public double Util { get; set; }
        public double Waste { get; set; }
        public List<PlacedPart> PlacedParts { get; set; } = new List<PlacedPart>();
        public long RuntimeMs { get; set; }
    }
}