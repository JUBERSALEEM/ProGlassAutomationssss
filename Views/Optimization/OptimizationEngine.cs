using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using static ProGlassAutomation.Views.ProformaInvoice.ProformaInvoiceView;

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
        RowFill = 6,
        DynamicBest = 7,
        ComplexRotation = 8 // Evaluates snap safety, aspect matching, and boundary flush cuts
    }

    public enum NestingStrategy
    {
        BestArea, ShortSideFit, LongSideFit, Guillotine, Skyline, BottomLeft, BestPerimeter,
        ComplexIQ200V7 = 10, ComplexSkyline = 11, ComplexBestFit = 12
    }

    public enum EngineMode
    {
        IQ200V7 = 0,      // Default engine
        BeamSearch = 1,  // Beam search
        Genetic = 2,     // Genetic algorithm  
        DeepBeam = 3,     // Deep beam 250
        Simulation = 4,   // With penalties
        AutoSelect = 5,   // Test all, pick best
        BestOf3 = 6       // Run 3 times, pick best
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
        private RotationPolicy _rotationPolicy = RotationPolicy.ComplexRotation;
        private NestingStrategy _nestingStrategy = NestingStrategy.ComplexIQ200V7;

        private Random _rng = new Random(42);
        private EngineMode _engineMode = EngineMode.IQ200V7;

        private const double MAX_PART_SIZE = 99999;
        private const double MIN_SHEET_SIZE = 100;
        private const double EPSILON = 0.0001;

        // =====================================================
        // ENGINE STATE
        // =====================================================
        private List<MaxRect> _freeRects = new List<MaxRect>();
        private List<RemnantPiece> _remnants = new List<RemnantPiece>();
        private CostModel _costModel = new CostModel();
        private PlacementConstraint _constraints = new PlacementConstraint();
        private bool _twoPassEnabled = false;
        private bool _remnantReuseEnabled = false;
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

        public void SetRotationPolicy(RotationPolicy policy) { _rotationPolicy = policy; }
        public void SetStrategy(NestingStrategy strategy) { _nestingStrategy = strategy; }

        public void SetDeterministic() { _rng = new Random(42); }

        public void SetEngineMode(EngineMode mode) { _engineMode = mode; }
        public void SetCostModel(CostModel model) { _costModel = model; }

        private double ComputeCost(double usedArea, double sheetArea, double fragmentCount, double kerfLength)
        {
            double stockCost = (usedArea / 1000000.0) * _costModel.StockPricePerSQM;
            double wastePenalty = ((sheetArea - usedArea) / 1000000.0) * _costModel.WastePenaltyPerSQM;
            double fragmentPenalty = fragmentCount * 0.0;
            double kerfPenalty = (kerfLength / 1000.0) * _costModel.KerfCostPerMm;
            return stockCost + wastePenalty + fragmentPenalty + kerfPenalty;
        }

        // =====================================================
        // ROTATION HELPER
        // =====================================================

        private struct Orientation
        {
            public double Width { get; set; }
            public double Height { get; set; }
            public bool IsRotated { get; set; }
        }

        private List<Orientation> GetAllowedOrientations(CutPart part)
        {
            var list = new List<Orientation>();

            if (_rotationPolicy == RotationPolicy.None)
            {
                list.Add(new Orientation { Width = part.L, Height = part.W, IsRotated = false });
                return list;
            }

            if (_rotationPolicy == RotationPolicy.Rotate90)
            {
                list.Add(new Orientation { Width = part.W, Height = part.L, IsRotated = true });
                return list;
            }

            list.Add(new Orientation { Width = part.L, Height = part.W, IsRotated = false });

            if (part.Rot ||
                _rotationPolicy == RotationPolicy.BestFit ||
                _rotationPolicy == RotationPolicy.FirstFit ||
                _rotationPolicy == RotationPolicy.StripFill ||
                _rotationPolicy == RotationPolicy.ColumnFill ||
                _rotationPolicy == RotationPolicy.RowFill ||
                _rotationPolicy == RotationPolicy.DynamicBest ||
                _rotationPolicy == RotationPolicy.ComplexRotation)
            {
                list.Add(new Orientation { Width = part.W, Height = part.L, IsRotated = true });
            }

            return list;
        }

        // =====================================================
        // RUNTIME STRATEGY DISPATCHER
        // =====================================================

        public void ExecuteIQ200V7(List<StockSheet> stockSheets, List<CutPart> cutParts,
                            ObservableCollection<OptimizationResult> results,
                            List<PlacedPart> allPlacedParts,
                            ObservableCollection<OptimizationJob> savedJobs)
        {
            switch (_engineMode)
            {
                case EngineMode.IQ200V7:
                    ExecuteIQ200V7Core(stockSheets, cutParts, results, allPlacedParts, savedJobs);
                    break;
                case EngineMode.BeamSearch:
                    ExecuteBeamSearch(stockSheets, cutParts, results, allPlacedParts, savedJobs);
                    break;
                case EngineMode.Genetic:
                    ExecuteGenetic(stockSheets, cutParts, results, allPlacedParts, savedJobs);
                    break;
                case EngineMode.DeepBeam:
                    ExecuteDeepBeam(stockSheets, cutParts, results, allPlacedParts, savedJobs);
                    break;
                case EngineMode.Simulation:
                    ExecuteSimulation(stockSheets, cutParts, results, allPlacedParts, savedJobs);
                    break;
                case EngineMode.AutoSelect:
                    ExecuteMultiAlgorithm(stockSheets, cutParts, results, allPlacedParts, savedJobs);
                    break;
                case EngineMode.BestOf3:
                    ExecuteAutoSelect(stockSheets, cutParts, results, allPlacedParts, savedJobs);
                    break;
                default:
                    ExecuteIQ200V7Core(stockSheets, cutParts, results, allPlacedParts, savedJobs);
                    break;
            }
        }

        public void ExecuteNesting(List<StockSheet> stockSheets, List<CutPart> cutParts,
                                ObservableCollection<OptimizationResult> results,
                                List<PlacedPart> allPlacedParts,
                                ObservableCollection<OptimizationJob> savedJobs)
        {
            ExecuteIQ200V7(stockSheets, cutParts, results, allPlacedParts, savedJobs);
        }

        // =====================================================
        // MAIN OPTIMIZATION ENGINE (IQ200V7 Core)
        // =====================================================

        private void ExecuteIQ200V7Core(List<StockSheet> stockSheets, List<CutPart> cutParts,
                                ObservableCollection<OptimizationResult> results,
                                List<PlacedPart> allPlacedParts,
                                ObservableCollection<OptimizationJob> savedJobs)
        {
            var allParts = ExpandAndConstrainParts(cutParts);
            if (allParts.Count == 0) return;

            var sequences = new List<List<CutPart>>
            {
                allParts.OrderByDescending(x => x.L * x.W).ThenByDescending(x => Math.Max(x.L, x.W)).ToList(), // Area Desc
                allParts.OrderBy(x => x.L * x.W).ThenBy(x => Math.Min(x.L, x.W)).ToList(),                    // Area Asc (Small first)
                allParts.OrderByDescending(x => Math.Max(x.L, x.W)).ThenByDescending(x => x.L * x.W).ToList(), // Max Dim Desc
                allParts.OrderBy(x => Math.Min(x.L, x.W)).ThenBy(x => x.L * x.W).ToList(),                    // Min Dim Asc

                // Guillotine dimension grouping (aligns matching widths/heights to fit cleanly into strips)
                allParts.GroupBy(x => Math.Max(x.L, x.W))
                        .OrderByDescending(g => g.Key)
                        .SelectMany(g => g.OrderByDescending(x => Math.Min(x.L, x.W)))
                        .ToList(),

                // Aspect ratio grouping
                allParts.OrderByDescending(x => Math.Max(x.L, x.W) / Math.Min(x.L, x.W))
                        .ThenByDescending(x => x.L * x.W)
                        .ToList()
            };

            List<CutPart> bestSeq = sequences[0];
            double bestUtil = -1;

            foreach (var seq in sequences)
            {
                var tempResults = new ObservableCollection<OptimizationResult>();
                var tempPlaced = new List<PlacedPart>();

                RunPlacementWithSequence(seq, stockSheets, tempResults, tempPlaced);

                if (_overallUtilization > bestUtil)
                {
                    bestUtil = _overallUtilization;
                    bestSeq = seq;
                }
            }

            RunPlacementWithSequence(bestSeq, stockSheets, results, allPlacedParts);
        }

        // =====================================================
        // UNIFIED PLACEMENT ENGINE (THREAD-SAFE MEMORY PLACEMENT)
        // =====================================================

        private void RunPlacementWithSequence(List<CutPart> orderedParts, List<StockSheet> stockSheets,
                                              ICollection<OptimizationResult> results,
                                              List<PlacedPart> allPlacedParts)
        {
            Application.Current.Dispatcher.Invoke(() => {
                results.Clear();
                allPlacedParts.Clear();
            });

            _remnants.Clear();
            _cutSequences.Clear();
            _freeRects.Clear();

            foreach (var p in orderedParts) p.IsPlaced = false;

            var sortedStock = stockSheets.OrderByDescending(s => s.L * s.W).ToList();

            double totalUsedAreaAll = 0;
            double totalAreaUsedSheets = 0;
            int totalSheetsUsed = 0;
            _totalPartsCut = 0;
            _totalPartsUnplaced = 0;
            _usedSQM = 0;

            var localResults = new List<OptimizationResult>();
            var localPlacedList = new List<PlacedPart>();

            foreach (var stock in sortedStock)
            {
                double oneSheetArea = stock.L * stock.W / 1000000.0;
                int availableQty = stock.Qty;
                if (availableQty <= 0) continue;

                double usableW = stock.L - _lr - _rm;
                double usableH = stock.W - _tr - _br;

                if (usableW < _breakout || usableH < _breakout) continue;

                int sheetsUsedForThisStock = 0;

                for (int sheetNum = 0; sheetNum < availableQty; sheetNum++)
                {
                    var remaining = orderedParts.Where(p => !p.IsPlaced).ToList();
                    if (remaining.Count == 0) break;

                    _freeRects.Clear();
                    _freeRects.Add(new MaxRect(0, 0, usableW, usableH));

                    double usedAreaThisSheet = 0;
                    int placedOnThisSheet = 0;
                    var placedOnThisSheetList = new List<PlacedPart>();
                    var placedCoordinates = new List<(double X, double Y, double W, double H)>();

                    var cutSequence = new CutSequence { SheetId = $"{stock.Ref}-{sheetNum + 1}" };

                    foreach (var part in remaining)
                    {
                        if (part.IsPlaced) continue;

                        if (!_constraints.ValidatePlacement(part.L, part.W))
                            continue;

                        var placement = EvaluatePlacementWithLookahead(part, _kerf);
                        if (placement == null) continue;

                        double placeW = placement.Width;
                        double placeH = placement.Height;

                        double storeX = placement.X;
                        double storeY = placement.Y;

                        if (storeX < 0 || storeY < 0 ||
                            storeX + placeW > usableW + EPSILON || storeY + placeH > usableH + EPSILON)
                        {
                            continue;
                        }

                        if (HasOverlap(storeX, storeY, placeW, placeH, placedCoordinates))
                        {
                            continue;
                        }

                        part.IsPlaced = true;
                        part.PlacedX = storeX;
                        part.PlacedY = storeY;
                        part.PlacedW = placeW;
                        part.PlacedH = placeH;

                        placedOnThisSheetList.Add(new PlacedPart
                        {
                            Ref = part.Ref,
                            X = storeX,
                            Y = storeY,
                            L = placeW,
                            W = placeH,
                            IsRotated = placement.IsRotated,
                            Sheet = stock.Ref,
                            SheetNum = sheetNum + 1
                        });

                        placedCoordinates.Add((storeX, storeY, placeW, placeH));

                        double partArea = (placeW * placeH) / 1000000.0;
                        _usedSQM += partArea;
                        placedOnThisSheet++;
                        _totalPartsCut++;

                        UpdateFreeRectsWithGuillotine(placement, placeW, placeH);
                        GenerateToolpathForPart(part, cutSequence, placement.X + _lr, placement.Y + _tr, _kerf);
                    }

                    if (_remnantReuseEnabled)
                        CollectRemnants(stock.Ref, sheetNum + 1);

                    if (placedOnThisSheet > 0)
                    {
                        usedAreaThisSheet = placedOnThisSheetList.Sum(p => (p.L * p.W) / 1000000.0);
                        sheetsUsedForThisStock++;
                        totalSheetsUsed++;
                        totalAreaUsedSheets += oneSheetArea;

                        double thisSheetUtil = oneSheetArea > 0 ? (usedAreaThisSheet / oneSheetArea) * 100.0 : 0.0;

                        double fragmentCount = CountFragments(placedOnThisSheetList);
                        double kerfLength = cutSequence.CalculateTotalCutLength();
                        cutSequence.TotalKerfLength = kerfLength;

                        double cost = ComputeCost(usedAreaThisSheet * 1000000, oneSheetArea * 1000000, fragmentCount, kerfLength);

                        localResults.Add(new OptimizationResult
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

                        totalUsedAreaAll += usedAreaThisSheet;
                        localPlacedList.AddRange(placedOnThisSheetList);
                        _cutSequences.Add(cutSequence);
                    }

                    if (placedOnThisSheet == 0) break;
                }
            }

            if (_twoPassEnabled && _remnants.Count > 0)
            {
                var remainingParts = orderedParts.Where(p => !p.IsPlaced).ToList();
                if (remainingParts.Count > 0)
                    RunSecondPassNesting(remainingParts);
            }

            _totalPartsUnplaced = orderedParts.Count(p => !p.IsPlaced);

            var emptyResults = localResults.Where(r => r.Ref != "TOTAL" && r.Area <= 0).ToList();
            foreach (var empty in emptyResults)
            {
                localResults.Remove(empty);
                totalSheetsUsed--;
            }

            if (localResults.Count > 0)
            {
                double totalSheetArea = localResults.Sum(r => (r.L * r.W) / 1000000.0);
                _overallUtilization = totalSheetArea > 0 ? (totalUsedAreaAll / totalSheetArea) * 100.0 : 0.0;
                _overallWastage = 100.0 - _overallUtilization;
            }
            else
            {
                _overallUtilization = 0;
                _overallWastage = 100;
            }

            var totalRecord = new OptimizationResult
            {
                Ref = "TOTAL",
                L = 0,
                W = 0,
                Used = totalSheetsUsed,
                Area = totalUsedAreaAll,
                Util = _overallUtilization,
                Waste = _overallWastage
            };
            localResults.Add(totalRecord);

            // Thread-safe dispatch back to UI collection lists
            Application.Current.Dispatcher.Invoke(() => {
                foreach (var r in localResults) results.Add(r);
                foreach (var p in localPlacedList) allPlacedParts.Add(p);
            });
        }

        private List<CutPart> ApplyConstraints(List<CutPart> parts)
        {
            var valid = new List<CutPart>();
            foreach (var p in parts)
            {
                if (p.L >= MAX_PART_SIZE || p.W >= MAX_PART_SIZE)
                {
                    _totalPartsUnplaced++;
                    continue;
                }
                if (p.L <= 0 || p.W <= 0) continue;
                if (p.L < _breakout || p.W < _breakout) continue;
                valid.Add(p);
            }
            return valid;
        }

        private List<CutPart> ExpandAndConstrainParts(List<CutPart> cutParts)
        {
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
            return ApplyConstraints(allParts);
        }

        // =====================================================
        // BEAM SEARCH ENGINE
        // =====================================================

        private void ExecuteBeamSearch(List<StockSheet> stockSheets, List<CutPart> cutParts,
                                    ObservableCollection<OptimizationResult> results,
                                    List<PlacedPart> allPlacedParts,
                                    ObservableCollection<OptimizationJob> savedJobs)
        {
            var allParts = ExpandAndConstrainParts(cutParts);
            if (allParts.Count == 0) return;

            var candidates = new List<List<CutPart>>
            {
                allParts.OrderByDescending(x => x.L * x.W).ToList(),
                allParts.OrderByDescending(x => Math.Max(x.L, x.W)).ToList(),
                allParts.OrderByDescending(x => Math.Min(x.L, x.W)).ToList(),
                allParts.OrderBy(x => x.L * x.W).ToList()
            };

            List<CutPart> bestSequence = candidates[0];
            double bestCost = double.MaxValue;

            foreach (var sequence in candidates)
            {
                double cost = EvaluateSequenceCost(sequence, stockSheets);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestSequence = sequence;
                }
            }

            RunPlacementWithSequence(bestSequence, stockSheets, results, allPlacedParts);
        }

        // =====================================================
        // UPGRADED GENETIC ENGINE (HEURISTIC INITIALIZATION)
        // =====================================================

        private void ExecuteGenetic(List<StockSheet> stockSheets, List<CutPart> cutParts,
                                ObservableCollection<OptimizationResult> results,
                                List<PlacedPart> allPlacedParts,
                                ObservableCollection<OptimizationJob> savedJobs)
        {
            var allParts = ExpandAndConstrainParts(cutParts);
            if (allParts.Count == 0) return;

            int populationSize = 100;
            int generations = 100;
            var population = new List<Genome>();

            var seededSequences = new List<List<CutPart>>
            {
                allParts.OrderByDescending(x => x.L * x.W).ToList(),
                allParts.OrderBy(x => x.L * x.W).ToList(),
                allParts.OrderByDescending(x => Math.Max(x.L, x.W)).ToList(),
                allParts.OrderByDescending(x => Math.Min(x.L, x.W)).ToList()
            };

            foreach (var seq in seededSequences)
            {
                population.Add(new Genome { PartOrder = seq, Fitness = EvaluateSequenceUtilization(seq, stockSheets) });
            }

            while (population.Count < populationSize)
            {
                var genome = new Genome
                {
                    PartOrder = allParts.OrderBy(x => _rng.Next()).ToList()
                };
                genome.Fitness = EvaluateSequenceUtilization(genome.PartOrder, stockSheets);
                population.Add(genome);
            }

            for (int g = 0; g < generations; g++)
            {
                population = population.OrderByDescending(p => p.Fitness).Take(populationSize / 2).ToList();
                var nextGen = new List<Genome>(population);

                while (nextGen.Count < populationSize)
                {
                    var parent1 = population[_rng.Next(population.Count)];
                    var parent2 = population[_rng.Next(population.Count)];

                    var child = Crossover(parent1, parent2);
                    if (_rng.NextDouble() < 0.20)
                    {
                        Mutate(child);
                    }

                    child.Fitness = EvaluateSequenceUtilization(child.PartOrder, stockSheets);
                    nextGen.Add(child);
                }
                population = nextGen;
            }

            var best = population.OrderByDescending(p => p.Fitness).First();
            RunPlacementWithSequence(best.PartOrder, stockSheets, results, allPlacedParts);
        }

        // =====================================================
        // DEEP BEAM ENGINE
        // =====================================================

        private void ExecuteDeepBeam(List<StockSheet> stockSheets, List<CutPart> cutParts,
                                ObservableCollection<OptimizationResult> results,
                                List<PlacedPart> allPlacedParts,
                                ObservableCollection<OptimizationJob> savedJobs)
        {
            var allParts = ExpandAndConstrainParts(cutParts);
            if (allParts.Count == 0) return;

            List<CutPart> bestSequence = null;
            double bestCost = double.MaxValue;

            for (int iteration = 0; iteration < 250; iteration++)
            {
                var randomizedSequence = allParts.OrderBy(x => _rng.Next()).ToList();
                double cost = EvaluateSequenceCost(randomizedSequence, stockSheets);

                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestSequence = randomizedSequence;
                }
            }

            if (bestSequence != null)
            {
                RunPlacementWithSequence(bestSequence, stockSheets, results, allPlacedParts);
            }
        }

        // =====================================================
        // SIMULATION ENGINE
        // =====================================================

        private void ExecuteSimulation(List<StockSheet> stockSheets, List<CutPart> cutParts,
                            ObservableCollection<OptimizationResult> results,
                            List<PlacedPart> allPlacedParts,
                            ObservableCollection<OptimizationJob> savedJobs)
        {
            ExecuteIQ200V7Core(stockSheets, cutParts, results, allPlacedParts, savedJobs);

            foreach (var placed in allPlacedParts)
            {
                double thermalPenalty = (placed.L * placed.W) * 0.000001;
                double toolpathPenalty = (placed.L + placed.W) * 0.01;
            }
        }

        // =====================================================
        // MULTI-ALGORITHM
        // =====================================================

        private void ExecuteMultiAlgorithm(List<StockSheet> stockSheets, List<CutPart> cutParts,
                                    ObservableCollection<OptimizationResult> results,
                                    List<PlacedPart> allPlacedParts,
                                    ObservableCollection<OptimizationJob> savedJobs)
        {
            results.Clear();
            allPlacedParts.Clear();

            var strategies = new List<NestingStrategy>
            {
                NestingStrategy.BestArea,
                NestingStrategy.ShortSideFit,
                NestingStrategy.LongSideFit,
                NestingStrategy.Guillotine,
                NestingStrategy.Skyline,
                NestingStrategy.BottomLeft,
                NestingStrategy.ComplexSkyline,
                NestingStrategy.ComplexBestFit
            };

            double bestUtil = 0;
            NestingStrategy bestStrategy = NestingStrategy.Skyline;
            List<PlacedPart> bestPlacement = null;
            List<OptimizationResult> bestResults = null;

            foreach (var strategy in strategies)
            {
                _nestingStrategy = strategy;

                var testResults = new ObservableCollection<OptimizationResult>();
                var testPlaced = new List<PlacedPart>();

                ExecuteIQ200V7Core(stockSheets, cutParts, testResults, testPlaced, savedJobs);

                double util = _overallUtilization;

                if (util > bestUtil)
                {
                    bestUtil = util;
                    bestStrategy = strategy;
                    bestPlacement = new List<PlacedPart>(testPlaced);
                    bestResults = new List<OptimizationResult>(testResults);
                }
            }

            _nestingStrategy = bestStrategy;

            results.Clear();
            allPlacedParts.Clear();

            if (bestResults != null)
                foreach (var r in bestResults) results.Add(r);
            if (bestPlacement != null)
                foreach (var p in bestPlacement) allPlacedParts.Add(p);
        }

        // =====================================================
        // AUTO-SELECT
        // =====================================================

        private void ExecuteAutoSelect(List<StockSheet> stockSheets, List<CutPart> cutParts,
                                    ObservableCollection<OptimizationResult> results,
                                    List<PlacedPart> allPlacedParts,
                                    ObservableCollection<OptimizationJob> savedJobs)
        {
            double bestUtil = 0;
            List<PlacedPart> bestPlacement = null;
            List<OptimizationResult> bestResults = null;
            var allParts = ExpandAndConstrainParts(cutParts);

            for (int seed = 42; seed < 100; seed += 15)
            {
                _rng = new Random(seed);

                var testResults = new ObservableCollection<OptimizationResult>();
                var testPlaced = new List<PlacedPart>();

                var shuffledSequence = allParts.OrderBy(x => _rng.Next()).ToList();
                RunPlacementWithSequence(shuffledSequence, stockSheets, testResults, testPlaced);

                if (_overallUtilization > bestUtil)
                {
                    bestUtil = _overallUtilization;
                    bestPlacement = new List<PlacedPart>(testPlaced);
                    bestResults = new List<OptimizationResult>(testResults);
                }
            }

            results.Clear();
            allPlacedParts.Clear();

            if (bestResults != null)
                foreach (var r in bestResults) results.Add(r);
            if (bestPlacement != null)
                foreach (var p in bestPlacement) allPlacedParts.Add(p);
        }

        // =====================================================
        // OVERLAP DETECTION
        // =====================================================

        private bool HasOverlap(double x, double y, double w, double h, List<(double X, double Y, double W, double H)> placedRects)
        {
            double x2 = x + w;
            double y2 = y + h;

            foreach (var rect in placedRects)
            {
                bool notOverlap = (x2 <= rect.X + EPSILON) || (x >= rect.X + rect.W - EPSILON) ||
                                 (y2 <= rect.Y + EPSILON) || (y >= rect.Y + rect.H - EPSILON);

                if (!notOverlap) return true;
            }
            return false;
        }

        // =====================================================
        // HELPER METHODS
        // =====================================================

        private int CountFragments(List<PlacedPart> placed)
        {
            return placed.Count(p => (p.L * p.W) < 100000);
        }

        private double EvaluateSequenceCost(List<CutPart> sequence, List<StockSheet> stockSheets)
        {
            foreach (var p in sequence) p.IsPlaced = false;
            double totalArea = 0;
            int placedCount = 0;
            double totalStockArea = 0;

            foreach (var stock in stockSheets)
            {
                if (stock.Qty <= 0) continue;
                for (int i = 1; i <= stock.Qty; i++)
                {
                    var tempPlaced = new List<PlacedPart>();
                    PlacePartsGuillotine(sequence, stock.L - _lr - _rm, stock.W - _tr - _br, stock.Ref, i, tempPlaced);
                    totalArea += tempPlaced.Sum(p => p.L * p.W);
                    placedCount += tempPlaced.Count;
                    totalStockArea += stock.L * stock.W;
                }
            }

            double kerfLength = sequence.Where(p => p.IsPlaced).Sum(p => 2 * (p.L + p.W));
            return ComputeCost(totalArea, totalStockArea, sequence.Count - placedCount, kerfLength);
        }

        private double EvaluateSequenceUtilization(List<CutPart> sequence, List<StockSheet> stockSheets)
        {
            foreach (var p in sequence) p.IsPlaced = false;
            double totalUsed = 0;
            double totalStockArea = 0;

            foreach (var stock in stockSheets)
            {
                if (stock.Qty <= 0) continue;
                for (int i = 1; i <= stock.Qty; i++)
                {
                    var tempPlaced = new List<PlacedPart>();
                    PlacePartsGuillotine(sequence, stock.L - _lr - _rm, stock.W - _tr - _br, stock.Ref, i, tempPlaced);
                    totalUsed += tempPlaced.Sum(p => p.L * p.W);
                    totalStockArea += (stock.L * stock.W);
                }
            }

            return totalStockArea > 0 ? (totalUsed / totalStockArea) : 0;
        }

        private double ComputeSolutionCost(List<PlacedPart> placed)
        {
            if (placed.Count == 0) return double.MaxValue;
            double totalArea = placed.Sum(p => p.L * p.W);
            double fragments = CountFragments(placed);
            double kerfLength = placed.Sum(p => 2 * (p.L + p.W));
            return ComputeCost(totalArea, totalArea, fragments, kerfLength);
        }

        private double ComputeUtilization(List<PlacedPart> placed, StockSheet stock)
        {
            double used = placed.Sum(p => p.L * p.W);
            double total = stock.L * stock.W;
            return total > 0 ? (used / total) * 100 : 0;
        }

        // =====================================================
        // PLACEMENT METHODS
        // =====================================================

        private List<PlacedPart> PlacePartsGuillotine(List<CutPart> parts, double sheetW, double sheetH,
                                                string sheetRef, int sheetNum, List<PlacedPart> allPlaced)
        {
            var placed = new List<PlacedPart>();
            var freeRects = new List<MaxRect> { new MaxRect(0, 0, sheetW, sheetH) };

            foreach (var part in parts)
            {
                if (part.IsPlaced) continue;
                if (part.L < _breakout || part.W < _breakout) continue;

                var orientations = GetAllowedOrientations(part);

                MaxRect bestRect = null;
                bool rotated = false;
                double bestScore = double.MaxValue;
                Orientation bestO = new Orientation();

                foreach (var rect in freeRects)
                {
                    foreach (var o in orientations)
                    {
                        double pw = o.Width + _kerf;
                        double ph = o.Height + _kerf;

                        if (rect.Fits(pw, ph))
                        {
                            double score;
                            if (_nestingStrategy == NestingStrategy.ComplexSkyline)
                                score = ScorePlacementSkyline(rect, pw, ph);
                            else if (_nestingStrategy == NestingStrategy.Guillotine)
                                score = ScorePlacementGuillotine(rect, pw, ph);
                            else
                                score = ScorePlacement(rect, pw, ph, _nestingStrategy);

                            if (_rotationPolicy == RotationPolicy.FirstFit && o.IsRotated)
                            {
                                score += 1000;
                            }

                            if (score < bestScore)
                            {
                                bestScore = score;
                                bestRect = rect;
                                rotated = o.IsRotated;
                                bestO = o;
                            }
                        }
                    }
                }

                if (bestRect != null)
                {
                    double placeW = bestO.Width;
                    double placeH = bestO.Height;

                    part.IsPlaced = true;
                    part.PlacedX = bestRect.X + _kerf;
                    part.PlacedY = bestRect.Y + _kerf;
                    part.PlacedW = placeW;
                    part.PlacedH = placeH;

                    placed.Add(new PlacedPart
                    {
                        Ref = part.Ref,
                        X = part.PlacedX,
                        Y = part.PlacedY,
                        L = placeW,
                        W = placeH,
                        IsRotated = rotated,
                        Sheet = sheetRef,
                        SheetNum = sheetNum
                    });

                    UpdateFreeRectsTest(bestRect, placeW + _kerf, placeH + _kerf, freeRects);
                }
            }

            return placed;
        }

        private double ScorePlacement(MaxRect rect, double w, double h, NestingStrategy strategy)
        {
            double remW = rect.Width - w;
            double remH = rect.Height - h;

            double wasteW = 0;
            if (remW > 0.1)
            {
                if (remW < _breakout)
                {
                    if (Math.Abs(remW - _kerf) < 1.0 || remW < _kerf + 0.1)
                        wasteW = 0;
                    else
                        wasteW = remW * rect.Height;
                }
            }

            double wasteH = 0;
            if (remH > 0.1)
            {
                if (remH < _breakout)
                {
                    if (Math.Abs(remH - _kerf) < 1.0 || remH < _kerf + 0.1)
                        wasteH = 0;
                    else
                        wasteH = remH * rect.Width;
                }
            }

            double localWaste = wasteW + wasteH;

            double rectAspect = rect.Width / rect.Height;
            double partAspect = w / h;
            double aspectDiff = Math.Abs(rectAspect - partAspect);

            double matchW = Math.Abs(rect.Width - w);
            double matchH = Math.Abs(rect.Height - h);
            double edgeMatchBonus = 0;

            if (matchW < _kerf + 0.1 || matchW < _breakout)
                edgeMatchBonus -= 50000;

            if (matchH < _kerf + 0.1 || matchH < _breakout)
                edgeMatchBonus -= 50000;

            double score = edgeMatchBonus;

            if (_rotationPolicy == RotationPolicy.ColumnFill)
            {
                score += (rect.X * 5000) + rect.Y + (rect.Width - w) * 10;
                return score;
            }
            if (_rotationPolicy == RotationPolicy.RowFill)
            {
                score += (rect.Y * 5000) + rect.X + (rect.Height - h) * 10;
                return score;
            }
            if (_rotationPolicy == RotationPolicy.StripFill)
            {
                score += Math.Min(rect.Width - w, rect.Height - h) * 5 + (rect.Y * 100) + rect.X;
                return score;
            }

            // Apply complex rotation-specific scoring adjustments
            if (_rotationPolicy == RotationPolicy.ComplexRotation)
            {
                // 1. Aspect Ratio Alignment: Lay the long side of the part along the long side of the free rect.
                // This keeps leftover spaces wide and usable rather than skinny and restricted.
                bool rectIsWide = rect.Width >= rect.Height;
                bool partIsWide = w >= h;
                if (rectIsWide == partIsWide)
                {
                    score -= 15000;
                }
                else
                {
                    score += 8000;
                }

                // 2. Snap/Breakout sliver protection:
                // If the split leaves a leftover gap smaller than the breakout margin, it represents unrecoverable scrap.
                if (remW > 0.1 && remW < _breakout)
                {
                    score += 200000; // Severe penalty for creating unusable sliver on width
                }
                if (remH > 0.1 && remH < _breakout)
                {
                    score += 200000; // Severe penalty for creating unusable sliver on height
                }

                // 3. Guillotine-flush edge alignment:
                // If a dimension matches the container exactly, it prevents nested cuts.
                if (Math.Abs(remW) < EPSILON || Math.Abs(remW - _kerf) < 1.0)
                {
                    score -= 50000;
                }

                if (Math.Abs(remH) < EPSILON || Math.Abs(remH - _kerf) < 1.0)
                {
                    score -= 50000;
                }
            }

            double shortGap = 0.0;
            double longGap = 0.0;

            switch (strategy)
            {
                case NestingStrategy.BestArea:
                    score += rect.Area - (w * h) + localWaste * 10.0;
                    break;

                case NestingStrategy.ShortSideFit:
                    shortGap = Math.Min(remW, remH);
                    score += shortGap > 0 ? shortGap * 10 : -shortGap * 50;
                    score += localWaste * 5.0 + rect.Y * 50 + rect.X;
                    break;

                case NestingStrategy.LongSideFit:
                    longGap = Math.Max(remW, remH);
                    score += longGap > 0 ? longGap * 10 : -longGap * 50;
                    score += localWaste * 5.0 + rect.Y * 50 + rect.X;
                    break;

                case NestingStrategy.BottomLeft:
                    score += rect.Y * 1000 + rect.X + localWaste * 2.0;
                    break;

                case NestingStrategy.Guillotine:
                    double rightWaste = rect.Width - w;
                    double bottomWaste = rect.Height - h;

                    if (rightWaste == 0) score -= 1000;
                    if (bottomWaste == 0) score -= 1000;

                    score += (rightWaste + bottomWaste) * 5;
                    score += rect.Y * 100 + rect.X + localWaste * 5.0;
                    break;

                case NestingStrategy.Skyline:
                case NestingStrategy.ComplexSkyline:
                    double skylineScore = rect.Y * 10000;

                    if (rect.Height >= h && rect.Width >= w)
                    {
                        skylineScore -= (w * h) / 100;
                    }

                    if (rect.Width - w < 10) skylineScore -= 500;

                    score += skylineScore + localWaste * 5.0;
                    break;

                case NestingStrategy.ComplexBestFit:
                case NestingStrategy.BestPerimeter:
                    double perimeter = 2 * (w + h);
                    double fitRatio = Math.Min(rect.Width / w, rect.Height / h);
                    score += -perimeter * fitRatio;
                    score += rect.Y * 50 + rect.X + localWaste * 5.0;
                    break;

                case NestingStrategy.ComplexIQ200V7:
                default:

                    score += (localWaste * 500.0)
                          + (shortGap * 100.0)
                          + (longGap * 10.0)
                          + (aspectDiff * 50.0)
                          + (rect.Y * 50.0)
                          + (rect.X * 10.0);
                    break;
            }

            return score;
        }

        private double ScorePlacementSkyline(MaxRect rect, double w, double h)
        {
            double posScore = rect.Y * 500;
            double fillScore = 0;
            if (rect.Height >= h && rect.Width >= w)
            {
                fillScore = -(w * h) / 100;

                if (rect.Width - w < 5) fillScore -= 200;
                if (rect.Height - h < 5) fillScore -= 200;
            }

            posScore += rect.X;
            return posScore + fillScore;
        }

        private double ScorePlacementGuillotine(MaxRect rect, double w, double h)
        {
            double score = 0;
            double areaFit = rect.Area - (w * h);
            score = areaFit;

            double shortSide = Math.Min(rect.Width - w, rect.Height - h);
            if (shortSide >= 0) score += shortSide * 2;
            else score -= shortSide * 5;

            score += rect.Y * 2 + rect.X;
            return score;
        }

        private PlacementNode EvaluatePlacementWithLookahead(CutPart part, double kerf)
        {
            double bestScore = double.MaxValue;
            PlacementNode bestPlacement = null;

            var orientations = GetAllowedOrientations(part);

            foreach (var rect in _freeRects)
            {
                foreach (var o in orientations)
                {
                    double neededW = o.Width + kerf;
                    double neededH = o.Height + kerf;

                    if (rect.Fits(neededW, neededH))
                    {
                        double score = ScorePlacement(rect, neededW, neededH, _nestingStrategy);

                        if (_rotationPolicy == RotationPolicy.FirstFit && o.IsRotated)
                        {
                            score += 1000;
                        }

                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestPlacement = new PlacementNode
                            {
                                X = rect.X,
                                Y = rect.Y,
                                Width = o.Width,
                                Height = o.Height,
                                IsRotated = o.IsRotated,
                                Score = score
                            };
                        }
                    }
                }
            }

            return bestPlacement;
        }

        private void UpdateFreeRectsWithGuillotine(PlacementNode placement, double w, double h)
        {
            const double eps = 0.01;
            var usedRect = _freeRects.FirstOrDefault(r =>
                placement.X + eps >= r.X && placement.Y + eps >= r.Y &&
                placement.X + w <= r.X + r.Width + eps && placement.Y + h <= r.Y + r.Height + eps);

            if (usedRect == null) return;

            _freeRects.Remove(usedRect);

            double splitX = placement.X + w;
            double splitY = placement.Y + h;

            double rightW = (usedRect.X + usedRect.Width) - splitX;
            double bottomH = (usedRect.Y + usedRect.Height) - splitY;

            if (rightW * usedRect.Height > bottomH * usedRect.Width)
            {
                if (rightW > _breakout && usedRect.Height > _breakout)
                    _freeRects.Add(new MaxRect(splitX, usedRect.Y, rightW, usedRect.Height));

                double leftW = usedRect.Width - rightW;
                if (bottomH > _breakout && leftW > _breakout)
                    _freeRects.Add(new MaxRect(usedRect.X, splitY, leftW, bottomH));
            }
            else
            {
                if (rightW > _breakout && (usedRect.Height - bottomH) > _breakout)
                    _freeRects.Add(new MaxRect(splitX, usedRect.Y, rightW, usedRect.Height - bottomH));

                if (bottomH > _breakout && usedRect.Width > _breakout)
                    _freeRects.Add(new MaxRect(usedRect.X, splitY, usedRect.Width, bottomH));
            }

            MergeFreeRectsImproved(_freeRects);

            _freeRects.RemoveAll(r =>
    r.Width < _breakout ||
    r.Height < _breakout);
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

            double splitX = placement.X + w;
            double splitY = placement.Y + h;

            double rightW = (usedRect.X + usedRect.Width) - splitX;
            double bottomH = (usedRect.Y + usedRect.Height) - splitY;

            if (rightW * usedRect.Height > bottomH * usedRect.Width)
            {
                if (rightW > _breakout && usedRect.Height > _breakout)
                    freeRects.Add(new MaxRect(splitX, usedRect.Y, rightW, usedRect.Height));

                double leftW = usedRect.Width - rightW;
                if (bottomH > _breakout && leftW > _breakout)
                    freeRects.Add(new MaxRect(usedRect.X, splitY, leftW, bottomH));
            }
            else
            {
                if (rightW > _breakout && (usedRect.Height - bottomH) > _breakout)
                    freeRects.Add(new MaxRect(splitX, usedRect.Y, rightW, freeRects.Count > 0 ? usedRect.Height - bottomH : usedRect.Height - bottomH));

                if (bottomH > _breakout && usedRect.Width > _breakout)
                    freeRects.Add(new MaxRect(usedRect.X, splitY, usedRect.Width, bottomH));
            }

            MergeFreeRectsImproved(freeRects);
            freeRects.RemoveAll(r =>
    r.Width < _breakout ||
    r.Height < _breakout);
        }

        private void MergeFreeRectsImproved(List<MaxRect> rects)
        {
            bool merged;
            do
            {
                merged = false;
                for (int i = 0; i < rects.Count && !merged; i++)
                {
                    for (int j = i + 1; j < rects.Count && !merged; j++)
                    {
                        var a = rects[i];
                        var b = rects[j];

                        if (Math.Abs(a.Y - b.Y) < 0.1 && Math.Abs(a.Height - b.Height) < 0.1)
                        {
                            if (Math.Abs(a.X + a.Width - b.X) < 0.1 || Math.Abs(b.X + b.Width - a.X) < 0.1)
                            {
                                double newX = Math.Min(a.X, b.X);
                                rects[i] = new MaxRect(newX, a.Y, a.Width + b.Width, a.Height);
                                rects.RemoveAt(j);
                                merged = true;
                            }
                        }
                        else if (Math.Abs(a.X - b.X) < 0.1 && Math.Abs(a.Width - b.Width) < 0.1)
                        {
                            if (Math.Abs(a.Y + a.Height - b.Y) < 0.1 || Math.Abs(b.Y + b.Height - a.Y) < 0.1)
                            {
                                double newY = Math.Min(a.Y, b.Y);
                                rects[i] = new MaxRect(a.X, newY, a.Width, a.Height + b.Height);
                                rects.RemoveAt(j);
                                merged = true;
                            }
                        }
                    }
                }
            } while (merged);
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
                        _remnants.Add(remnant);
                }
            }
        }

        private double RunSecondPassNesting(List<CutPart> unplacedParts)
        {
            if (!_twoPassEnabled || !unplacedParts.Any()) return 0;
            if (_remnants.Count == 0) return 0;

            var usableRemnants = _remnants.Where(r => !r.IsReused && r.ValueScore > 0.2).OrderByDescending(r => r.Area);
            if (!usableRemnants.Any()) return 0;

            double partsAreaOnRemnants = 0;

            int partIndex = 0;

            foreach (var remnant in usableRemnants)
            {
                var remnantFreeRects = new List<MaxRect> { new MaxRect(remnant.X, remnant.Y, remnant.Width, remnant.Height) };
                var placedOnRemnant = new List<(double X, double Y, double W, double H)>();

                foreach (var part in unplacedParts)
                {
                    if (part.IsPlaced) continue;

                    if (partIndex < 6)
                    {
                        _rotationPolicy = RotationPolicy.Rotate90;
                    }
                    else
                    {
                        _rotationPolicy = RotationPolicy.None;
                    }

                    var orientations = GetAllowedOrientations(part);

                    MaxRect bestRect = null;
                    bool rotated = false;
                    Orientation bestO = new Orientation();

                    foreach (var rect in remnantFreeRects)
                    {
                        foreach (var o in orientations)
                        {
                            double pW = o.Width + _kerf;
                            double pH = o.Height + _kerf;

                            if (rect.Fits(pW, pH))
                            {
                                bestRect = rect;
                                rotated = o.IsRotated;
                                bestO = o;
                                break;
                            }
                        }
                        if (bestRect != null) break;
                    }

                    if (bestRect == null) continue;

                    double placeW = bestO.Width;
                    double placeH = bestO.Height;

                    double newX = bestRect.X + _kerf;
                    double newY = bestRect.Y + _kerf;

                    part.IsPlaced = true;
                    part.PlacedX = newX;
                    part.PlacedY = newY;
                    part.PlacedW = placeW;
                    part.PlacedH = placeH;

                    double partArea = (part.L * part.W) / 1000000.0;
                    partsAreaOnRemnants += partArea;
                    _usedSQM += partArea;
                    _totalPartsCut++;

                    placedOnRemnant.Add((newX, newY, placeW, placeH));

                    remnantFreeRects.Remove(bestRect);

                    double splitX = newX + placeW + _kerf;
                    double splitY = newY + placeH + _kerf;

                    double rightW = (bestRect.X + bestRect.Width) - splitX;
                    double bottomH = (bestRect.Y + bestRect.Height) - splitY;

                    if (rightW > _breakout && bestRect.Height > _breakout)
                        remnantFreeRects.Add(new MaxRect(splitX, bestRect.Y, rightW, bestRect.Height));
                    if (bottomH > _breakout && bestRect.Width > _breakout)
                        remnantFreeRects.Add(new MaxRect(bestRect.X, splitY, bestRect.Width, bottomH));

                    if (placedOnRemnant.Count > 0)
                        remnant.IsReused = true;

                    partIndex++;
                }
            }

            _totalPartsUnplaced = unplacedParts.Count(p => !p.IsPlaced);
            return partsAreaOnRemnants;
        }

        // =====================================================
        // TOOLPATH GENERATION
        // =====================================================

        private void GenerateToolpathForPart(CutPart part, CutSequence sequence, double offsetX, double offsetY, double kerf)
        {
            double x = part.PlacedX;
            double y = part.PlacedY;
            double w = part.PlacedW;
            double h = part.PlacedH;

            sequence.Operations.Add(new ToolpathOperation { PartId = part.Ref, Sequence = sequence.Operations.Count + 1, StartX = x + offsetX, StartY = y + offsetY, EndX = x + w + offsetX, EndY = y + offsetY, ToolType = "Cut" });
            sequence.Operations.Add(new ToolpathOperation { PartId = part.Ref, Sequence = sequence.Operations.Count + 1, StartX = x + w + offsetX, StartY = y + offsetY, EndX = x + w + offsetX, EndY = y + h + offsetY, ToolType = "Cut" });
            sequence.Operations.Add(new ToolpathOperation { PartId = part.Ref, Sequence = sequence.Operations.Count + 1, StartX = x + w + offsetX, StartY = y + h + offsetY, EndX = x + offsetX, EndY = y + h + offsetY, ToolType = "Cut" });
            sequence.Operations.Add(new ToolpathOperation { PartId = part.Ref, Sequence = sequence.Operations.Count + 1, StartX = x + offsetX, StartY = y + h + offsetY, EndX = x + offsetX, EndY = y + offsetY, ToolType = "Cut" });

            if (_bridgeWidth > 0)
            {
                int bridgeCount = (int)Math.Floor(h / (_bridgeWidth * 4));
                if (bridgeCount < 1) bridgeCount = 1;
                double bridgeSpacing = h / (bridgeCount + 1);

                for (int i = 1; i <= bridgeCount; i++)
                {
                    double bridgeY = y + (bridgeSpacing * i);
                    sequence.Operations.Add(new ToolpathOperation { PartId = part.Ref, Sequence = sequence.Operations.Count + 1, StartX = x + offsetX, StartY = bridgeY + offsetY, EndX = x + w + offsetX, EndY = bridgeY + offsetY, ToolType = "BridgeCut", IsBridgeCut = true });
                    sequence.BridgeCount++;
                }
            }
        }

        // =====================================================
        // GENETIC HELPERS (Permutation Safe)
        // =====================================================

        private Genome Crossover(Genome p1, Genome p2)
        {
            var child = new Genome();
            child.PartOrder = new List<CutPart>();

            int count = p1.PartOrder.Count;
            if (count == 0) return child;

            int start = _rng.Next(count);
            int end = _rng.Next(count);
            if (start > end) { int t = start; start = end; end = t; }

            var childParts = new CutPart[count];
            var usedIds = new HashSet<int>();

            for (int i = start; i <= end; i++)
            {
                childParts[i] = p1.PartOrder[i];
                usedIds.Add(p1.PartOrder[i].Id);
            }

            int currentChildIdx = (end + 1) % count;
            for (int i = 0; i < count; i++)
            {
                int p2Idx = (end + 1 + i) % count;
                var candidate = p2.PartOrder[p2Idx];
                if (!usedIds.Contains(candidate.Id))
                {
                    childParts[currentChildIdx] = candidate;
                    usedIds.Add(candidate.Id);
                    currentChildIdx = (currentChildIdx + 1) % count;
                }
            }

            child.PartOrder.AddRange(childParts);
            return child;
        }

        private void Mutate(Genome g)
        {
            if (g.PartOrder.Count < 2) return;
            int i = _rng.Next(g.PartOrder.Count);
            int j = _rng.Next(g.PartOrder.Count);
            var temp = g.PartOrder[i];
            g.PartOrder[i] = g.PartOrder[j];
            g.PartOrder[j] = temp;
        }

        // =====================================================
        // COST CALCULATION
        // =====================================================

        public double CalculateTotalCost(double usedSQM, List<OptimizationResult> results, string currencySymbol = "AED ")
        {
            if (usedSQM <= 0) return 0;

            double stockCost = usedSQM * _costModel.StockPricePerSQM;

            double wasteArea = 0;
            foreach (var result in results.Where(r => r.Ref != "TOTAL"))
            {
                double sheetArea = result.L * result.W / 1000000.0;
                wasteArea += sheetArea - result.Area;
            }

            stockCost += wasteArea * _costModel.WastePenaltyPerSQM;

            double remnantCredit = 0;
            foreach (var rem in _remnants.Where(r => !r.IsReused))
                remnantCredit += _costModel.CalculateRemnantCredit(rem.Area);
            stockCost -= remnantCredit;

            double totalKerf = 0;
            foreach (var seq in _cutSequences)
                totalKerf += seq.CalculateTotalCutLength();
            stockCost += totalKerf * _costModel.KerfCostPerMm;

            return stockCost;
        }

        // =====================================================
        // PROPERTIES
        // =====================================================

        public double OverallUtilization => _overallUtilization;
        public double OverallWastage => _overallWastage;
        public int TotalPartsCut => _totalPartsCut;
        public int TotalPartsUnplaced => _totalPartsUnplaced;
        public double UsedSQM => _usedSQM;
        public int TotalSheetsGenerated => _cutSequences.Count;

        public void ResetState()
        {
            _currentIndex = 0;
            _freeRects.Clear();
        }

        public List<RemnantPiece> GetRemnants() => _remnants;
        public List<CutSequence> GetCutSequences() => _cutSequences;
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

    // =====================================================
    // HELPER CLASSES
    // =====================================================

    public class MaxRect
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Area => Width * Height;

        public MaxRect() { }
        public MaxRect(double x, double y, double w, double h) { X = x; Y = y; Width = w; Height = h; }
        public bool Fits(double w, double h) => Width >= w && Height >= h;
    }

    public class PlacementNode
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsRotated { get; set; }
        public double Score { get; set; }
    }

    public class BeamNode
    {
        public List<PlacedPart> Placed = new List<PlacedPart>();
        public double Cost { get; set; }
        public double Utilization { get; set; }
    }

    public class Genome
    {
        public List<CutPart> PartOrder = new List<CutPart>();
        public List<PlacedPart> Placed = new List<PlacedPart>();
        public double Fitness { get; set; }
        public double Cost { get; set; }
        public double Utilization { get; set; }
    }

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
                double sizeScore = Math.Min(Width, Height) > 200 ? 1.0 : Math.Min(Width, Height) > 100 ? 0.7 : 0.3;
                double areaScore = Area > 500000 ? 1.0 : Area > 200000 ? 0.6 : 0.2;
                return (aspectScore * 0.4 + sizeScore * 0.3 + areaScore * 0.3);
            }
        }
    }

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

    public class CostModel
    {
        public double StockPricePerSQM { get; set; } = 250;
        public double WastePenaltyPerSQM { get; set; } = 0;
        public double RemnantCreditPerSQM { get; set; } = 0;
        public double KerfCostPerMm { get; set; } = 0.01;
        public double OperatingCostPerHour { get; set; } = 0;
        public double SetupCostPerJob { get; set; } = 50;

        public double CalculateSheetCost(double usedArea, double sheetArea, double kerfLength)
        {
            double stockCost = (sheetArea / 1000000) * StockPricePerSQM;
            double wasteCost = ((sheetArea - usedArea) / 1000000) * WastePenaltyPerSQM;
            double kerfCost = kerfLength * KerfCostPerMm;
            return stockCost + wasteCost + kerfCost;
        }

        public double CalculateRemnantCredit(double area) => (area / 1000000) * RemnantCreditPerSQM;
    }

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

        public CutSequence() { Operations = new List<ToolpathOperation>(); }

        public List<Point> GetToolpathPoints()
        {
            var points = new List<Point>();
            foreach (var op in Operations.OrderBy(o => o.Sequence))
            {
                points.Add(new Point(op.StartX, op.StartY));
                points.Add(new Point(op.EndX, op.EndY));
            }
            return points;
        }

        public double CalculateTotalCutLength() => Operations.Sum(o => o.Length);
        public double EstimateProcessingTime(double speedMmPerSec = 5000) => CalculateTotalCutLength() / speedMmPerSec;
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