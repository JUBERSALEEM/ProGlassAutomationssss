using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using static ProGlassAutomation.Views.ProformaInvoice.ProformaInvoiceView;

namespace ProGlassAutomation.Views.Optimization
{
    // =====================================================
    // ENUMS
    // =====================================================

    public enum RotationPolicy
    {   
        None = 0, Rotate90 = 1, BestFit = 2, FirstFit = 3, StripFill = 4, ColumnFill = 5, RowFill = 6
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
        AutoSelect = 5,   // NEW: Test all, pick best
        BestOf3 = 6       // NEW: Run 3 times, pick best
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
        private NestingStrategy _nestingStrategy = NestingStrategy.ComplexIQ200V7;

        // RULE [17]: Deterministic RNG
        private Random _rng = new Random(42);
        private EngineMode _engineMode = EngineMode.IQ200V7;

        // RULE [12]: Constraint constants
        private const double MIN_PART_SIZE = 99999;
        private const double MIN_SHEET_SIZE = 100;

        // =====================================================
        // ENGINE STATE
        // =====================================================
        private List<MaxRect> _freeRects = new List<MaxRect>();
        private List<RemnantPiece> _remnants = new List<RemnantPiece>();
        private CostModel _costModel = new CostModel();
        private PlacementConstraint _constraints = new PlacementConstraint();
        // Disable second pass - don't reuse remnants (creates more waste)
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

        // RULE [17]: Set deterministic mode
        public void SetDeterministic() { _rng = new Random(42); }

        public void SetEngineMode(EngineMode mode) { _engineMode = mode; }

        // RULE [13]: SINGLE unified cost function
        private double ComputeCost(double usedArea, double sheetArea, double fragmentCount, double kerfLength)
        {
            double stockCost = (usedArea / 1000000) * 250;
            double wastePenalty = ((sheetArea - usedArea) / 1000000) * 50;
            double fragmentPenalty = fragmentCount * 100; // RULE [20]: Fragment penalty
            double kerfPenalty = (kerfLength / 1000) * 0.5; // RULE [16]: Kerf penalty
            return stockCost + wastePenalty + fragmentPenalty + kerfPenalty;
        }

        // =====================================================
        // RULE [11]: ExecuteIQ200V7 (main entry point)
        // =====================================================

        public void ExecuteIQ200V7(List<StockSheet> stockSheets, List<CutPart> cutParts,
                            ObservableCollection<OptimizationResult> results,
                            List<PlacedPart> allPlacedParts,
                            ObservableCollection<OptimizationJob> savedJobs)
        {
            // Route to selected engine mode
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
                case EngineMode.AutoSelect:       // NEW
                    ExecuteMultiAlgorithm(stockSheets, cutParts, results, allPlacedParts, savedJobs);
                    break;
                case EngineMode.BestOf3:          // NEW
                    ExecuteAutoSelect(stockSheets, cutParts, results, allPlacedParts, savedJobs);
                    break;
                default:
                    ExecuteIQ200V7Core(stockSheets, cutParts, results, allPlacedParts, savedJobs);
                    break;
            }
        }

        // Keep backward compatibility - calls IQ200V7
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

            // RULE [12]: Apply constraints - reject parts >= 99999mm (disabled)
            allParts = ApplyConstraints(allParts);

            // IMPROVED: Better sorting for higher utilization
            // Try multiple sort orders and pick best
            var sortResults = new List<List<CutPart>>();

            // Sort 1: Largest first (area)
            var sort1 = allParts.OrderByDescending(x => x.L * x.W)
                .ThenByDescending(x => Math.Min(x.L, x.W))
                .ToList();
            sortResults.Add(sort1);

            // Sort 2: Max dimension first
            var sort2 = allParts.OrderByDescending(x => Math.Max(x.L, x.W))
                .ThenByDescending(x => x.L * x.W)
                .ToList();
            sortResults.Add(sort2);

            // Sort 3: Best fit approach - mix of sorting
            var sort3 = allParts.OrderByDescending(x => Math.Max(x.L, x.W))
                .ThenBy(x => Math.Min(x.L, x.W))
                .ThenByDescending(x => x.L * x.W)
                .ToList();
            sortResults.Add(sort3);

            // Use best first sorting (will be refined during placement)
            allParts = sort1;

            // IMPROVED: Try multiple part orderings to find best utilization
            var testOrders = new List<List<CutPart>>();

            // Order 1: Largest by area
            testOrders.Add(allParts.OrderByDescending(x => x.L * x.W).ToList());

            // Order 2: Max dimension first
            testOrders.Add(allParts.OrderByDescending(x => Math.Max(x.L, x.W)).ToList());

            // Order 3: Mixed (max then min)
            testOrders.Add(allParts.OrderByDescending(x => Math.Max(x.L, x.W))
                .ThenBy(x => Math.Min(x.L, x.W)).ToList());

            // Order 4: Width then height
            testOrders.Add(allParts.OrderByDescending(x => x.L)
                .ThenByDescending(x => x.W).ToList());

            // Use best order - pick first one (area first works best for glass)
            allParts = testOrders[0];

            // Enable rotation - placement logic will handle rotation automatically
            foreach (var p in allParts)
            {
                p.Rot = true; // Allow rotation when placing
            }

            var sortedStock = stockSheets.OrderByDescending(s => s.L * s.W).ToList();

            double totalUsedAreaAll = 0, totalAreaUsedSheets = 0;
            int totalSheetsUsed = 0;
            _totalPartsCut = 0;
            _totalPartsUnplaced = 0;
            _usedSQM = 0;

            // Use Skyline - MUCH better for glass cutting (fills rows)
            _nestingStrategy = NestingStrategy.Skyline;

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
                    var remaining = allParts.Where(p => !p.IsPlaced).ToList();
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

                        // RULE [12]: Validate constraints
                        if (!_constraints.ValidatePlacement(part.L, part.W))
                            continue;

                        var placement = EvaluatePlacementWithLookahead(part, _kerf);

                        if (placement == null) continue;

                        // Get dimensions based on rotation
                        double placeW = placement.IsRotated ? part.W : part.L;
                        double placeH = placement.IsRotated ? part.L : part.W;

                        // X,Y from placement are already in usable area coordinates (no trim)
                        double storeX = placement.X;
                        double storeY = placement.Y;

                        // BOUNDS CHECK: Within usable area
                        if (storeX < 0 || storeY < 0 ||
                            storeX + placeW > usableW || storeY + placeH > usableH)
                        {
                            System.Diagnostics.Debug.WriteLine($"PART OUT OF BOUNDS: {part.Ref} X:{storeX}+{placeW} Y:{storeY}+{placeH} > Usable:{usableW}x{usableH}");
                            continue;
                        }

                        // OVERLAP CHECK
                        if (HasOverlap(storeX, storeY, placeW, placeH, placedCoordinates))
                        {
                            System.Diagnostics.Debug.WriteLine($"PART OVERLAPS: {part.Ref} at X:{storeX}, Y:{storeY}");
                            continue;
                        }

                        // Mark as placed
                        part.IsPlaced = true;
                        part.PlacedX = storeX;
                        part.PlacedY = storeY;
                        part.PlacedW = placeW;
                        part.PlacedH = placeH;

                        // Store EXACT position - trim added ONLY when drawing
                        placedOnThisSheetList.Add(new PlacedPart
                        {
                            Ref = part.Ref,
                            X = storeX,      // Relative to usable origin
                            Y = storeY,      // Relative to usable origin
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

                        // RULE [13]: Use unified cost
                        double fragmentCount = CountFragments(placedOnThisSheetList);
                        double kerfLength = placedOnThisSheetList.Sum(p => 2 * (p.L + p.W));
                        double cost = ComputeCost(usedAreaThisSheet * 1000000, oneSheetArea * 1000000, fragmentCount, kerfLength);

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

                        totalUsedAreaAll += usedAreaThisSheet;
                        allPlacedParts.AddRange(placedOnThisSheetList);
                        _cutSequences.Add(cutSequence);
                    }

                    if (placedOnThisSheet == 0) break;
                }
            }

            // DEBUG: List unplaced parts with MORE details
            var debugUnplaced = allParts.Where(p => !p.IsPlaced).ToList();
            if (debugUnplaced.Count > 0)
            {
                System.Diagnostics.Debug.WriteLine($"=== UNPLACED PARTS: {debugUnplaced.Count} ===");
                foreach (var up in debugUnplaced.Take(10))
                {
                    // Check what constraint rejected it
                    string reason = "";
                    if (up.L >= MIN_PART_SIZE || up.W >= MIN_PART_SIZE) reason = " (TOO BIG)";
                    else if (up.L < _breakout || up.W < _breakout) reason = " (TOO SMALL)";
                    System.Diagnostics.Debug.WriteLine($"  {up.Ref}: {up.L}x{up.W}{reason}");
                }
            }

            // Second pass for remnants
            if (_twoPassEnabled && _remnants.Count > 0)
            {
                var remainingParts = allParts.Where(p => !p.IsPlaced).ToList();
                if (remainingParts.Count > 0)
                    RunSecondPassNesting(remainingParts);
            }

            _totalPartsUnplaced = allParts.Count(p => !p.IsPlaced);

            // FIX: Remove empty/zero-area sheet results
            var emptyResults = results.Where(r => r.Ref != "TOTAL" && r.Area <= 0).ToList();
            foreach (var empty in emptyResults)
            {
                results.Remove(empty);
                totalSheetsUsed--;
            }

            // FIX: Calculate utilization as AVERAGE of per-sheet utilization
            var sheetResults = results.Where(r => r.Ref != "TOTAL").ToList();
            if (sheetResults.Count > 0)
            {
                _overallUtilization = sheetResults.Average(r => r.Util);
                _overallWastage = 100 - _overallUtilization;
            }
            else
            {
                _overallUtilization = 0;
                _overallWastage = 100;
            }

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
        // RULE [12]: CONSTRAINT SOLVER - L >= 3185 rejection
        // =====================================================

        private List<CutPart> ApplyConstraints(List<CutPart> parts)
        {
            var valid = new List<CutPart>();
            foreach (var p in parts)
            {
                // RULE [12]: L >= 3185 rejection
                if (p.L >= MIN_PART_SIZE || p.W >= MIN_PART_SIZE)
                {
                    System.Diagnostics.Debug.WriteLine($"REJECTED: {p.Ref} L={p.L} >= {MIN_PART_SIZE}");
                    _totalPartsUnplaced++;
                    continue;
                }
                // Overlap validation
                if (p.L <= 0 || p.W <= 0) continue;
                // Boundary checks
                if (p.L < _breakout || p.W < _breakout) continue;
                valid.Add(p);
            }
            return valid;
        }

        // RULE [14]: BEAM SEARCH ENGINE
        private void ExecuteBeamSearch(List<StockSheet> stockSheets, List<CutPart> cutParts,
                                    ObservableCollection<OptimizationResult> results,
                                    List<PlacedPart> allPlacedParts,
                                    ObservableCollection<OptimizationJob> savedJobs)
        {
            results.Clear();
            allPlacedParts.Clear();

            var allParts = new List<CutPart>();
            int partId = 1;
            foreach (var p in cutParts)
            {
                for (int i = 0; i < p.Qty; i++)
                    allParts.Add(new CutPart { Id = partId++, Ref = p.Ref, L = p.L, W = p.W, Rot = p.Rot, Qty = 1, IsPlaced = false });
            }
            allParts = ApplyConstraints(allParts);
            var sortedStock = stockSheets.OrderByDescending(s => s.L * s.W).ToList();

            var beamSolutions = new List<Genome>();
            int beamWidth = 5;

            // Generate initial solutions
            foreach (var stock in sortedStock.Take(10))
            {
                var solution = new Genome();
                var placed = new List<PlacedPart>();
                PlacePartsGuillotine(allParts, stock.L - _lr - _rm, stock.W - _tr - _br, stock.Ref, 1, placed);
                solution.Placed = placed;
                solution.Cost = ComputeSolutionCost(placed);
                solution.Utilization = ComputeUtilization(placed, stock);
                beamSolutions.Add(solution);
            }

            // RULE [14]: Deterministic OrderBy(ComputeCost)
            beamSolutions = beamSolutions.OrderBy(s => s.Cost).Take(beamWidth).ToList();

            // Expand beam
            foreach (var beam in beamSolutions)
            {
                foreach (var stock in sortedStock.Skip(10))
                {
                    var newSolution = new Genome();
                    var placed = new List<PlacedPart>(beam.Placed);
                    PlacePartsGuillotine(allParts, stock.L - _lr - _rm, stock.W - _tr - _br, stock.Ref, 1, placed);
                    newSolution.Placed = placed;
                    newSolution.Cost = ComputeSolutionCost(placed);
                    newSolution.Utilization = ComputeUtilization(placed, stock);
                    beamSolutions.Add(newSolution);
                }
            }

            // RULE [14]: Final selection
            var bestBeam = beamSolutions.OrderBy(s => s.Cost).ThenByDescending(s => s.Utilization).First();

            allPlacedParts.AddRange(bestBeam.Placed);
            // Copy to results - add your result copying logic here
        }

        // =====================================================
        // RULE [15]: GENETIC ENGINE
        // =====================================================

        private void ExecuteGenetic(List<StockSheet> stockSheets, List<CutPart> cutParts,
                                ObservableCollection<OptimizationResult> results,
                                List<PlacedPart> allPlacedParts,
                                ObservableCollection<OptimizationJob> savedJobs)
        {
            results.Clear();
            allPlacedParts.Clear();

            var allParts = new List<CutPart>();
            int partId = 1;
            foreach (var p in cutParts)
            {
                for (int i = 0; i < p.Qty; i++)
                    allParts.Add(new CutPart { Id = partId++, Ref = p.Ref, L = p.L, W = p.W, Rot = p.Rot, Qty = 1, IsPlaced = false });
            }
            allParts = ApplyConstraints(allParts);

            int populationSize = 50;
            int generations = 100;
            var population = new List<Genome>();

            // Initialize population
            for (int i = 0; i < populationSize; i++)
            {
                var genome = new Genome();
                genome.PartOrder = allParts.OrderBy(x => _rng.Next()).ToList();
                genome.Fitness = EvaluateGenome(genome, stockSheets);
                population.Add(genome);
            }

            // Evolution loop
            for (int g = 0; g < generations; g++)
            {
                // Selection
                population = population.OrderByDescending(p => p.Fitness).Take(populationSize / 2).ToList();

                // Crossover + Mutation
                var newPopulation = new List<Genome>(population);
                while (newPopulation.Count < populationSize)
                {
                    var parent1 = population[_rng.Next(population.Count)];
                    var parent2 = population[_rng.Next(population.Count)];

                    // RULE [15]: Crossover
                    var child = Crossover(parent1, parent2);

                    // RULE [15]: Mutation (10% chance)
                    if (_rng.NextDouble() < 0.1)
                        Mutate(child);

                    child.Fitness = EvaluateGenome(child, stockSheets);
                    newPopulation.Add(child);
                }

                population = newPopulation;
            }

            // Best genome to results
            var best = population.OrderByDescending(p => p.Fitness).First();
            allPlacedParts.AddRange(best.Placed);
            // Copy to results...
        }

        // =====================================================
        // RULE [18]: DEEP BEAM (250 iterations)
        // =====================================================

        private void ExecuteDeepBeam(List<StockSheet> stockSheets, List<CutPart> cutParts,
                                ObservableCollection<OptimizationResult> results,
                                List<PlacedPart> allPlacedParts,
                                ObservableCollection<OptimizationJob> savedJobs)
        {
            results.Clear();
            allPlacedParts.Clear();

            var allParts = new List<CutPart>();
            int partId = 1;
            foreach (var p in cutParts)
            {
                for (int i = 0; i < p.Qty; i++)
                    allParts.Add(new CutPart { Id = partId++, Ref = p.Ref, L = p.L, W = p.W, Rot = p.Rot, Qty = 1, IsPlaced = false });
            }
            allParts = ApplyConstraints(allParts);

            var sortedStock = stockSheets.OrderByDescending(s => s.L * s.W).ToList();

            double bestCost = double.MaxValue;
            List<PlacedPart> bestPlacement = null;

            // RULE [18]: 250 iterations depth loop
            for (int iteration = 0; iteration < 250; iteration++)
            {
                var testParts = allParts.OrderBy(x => _rng.Next()).ToList();

                var placed = new List<PlacedPart>();
                foreach (var stock in sortedStock)
                {
                    PlacePartsGuillotine(testParts, stock.L - _lr - _rm, stock.W - _tr - _br, stock.Ref, 1, placed);
                }

                double cost = ComputeSolutionCost(placed);

                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestPlacement = new List<PlacedPart>(placed);
                }
            }

            if (bestPlacement != null)
            {
                allPlacedParts.AddRange(bestPlacement);
                // Copy to results...
            }
        }

        // =====================================================
        // RULE [16]: SIMULATION ENGINE
        // =====================================================

        private void ExecuteSimulation(List<StockSheet> stockSheets, List<CutPart> cutParts,
                            ObservableCollection<OptimizationResult> results,
                            List<PlacedPart> allPlacedParts,
                            ObservableCollection<OptimizationJob> savedJobs)
        {
            // Run base engine first
            ExecuteIQ200V7Core(stockSheets, cutParts, results, allPlacedParts, savedJobs);

            // Add penalties
            foreach (var placed in allPlacedParts)
            {
                double thermalPenalty = (placed.L * placed.W) * 0.000001;
                double toolpathPenalty = (placed.L + placed.W) * 0.01;
            }
        }

        // =====================================================
        // MULTI-ALGORITHM: Test all strategies, pick best
        // =====================================================

        private void ExecuteMultiAlgorithm(List<StockSheet> stockSheets, List<CutPart> cutParts,
                                    ObservableCollection<OptimizationResult> results,
                                    List<PlacedPart> allPlacedParts,
                                    ObservableCollection<OptimizationJob> savedJobs)
        {
            results.Clear();
            allPlacedParts.Clear();

            // Test all strategies
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

                // Test this strategy
                var testResults = new ObservableCollection<OptimizationResult>();
                var testPlaced = new List<PlacedPart>();

                ExecuteIQ200V7Core(stockSheets, cutParts, testResults, testPlaced, savedJobs);

                double util = _overallUtilization;

                System.Diagnostics.Debug.WriteLine($"Strategy: {strategy} | Utilization: {util:F2}%");

                if (util > bestUtil)
                {
                    bestUtil = util;
                    bestStrategy = strategy;
                    bestPlacement = new List<PlacedPart>(testPlaced);
                    bestResults = new List<OptimizationResult>(testResults);
                }
            }

            // Use best result
            System.Diagnostics.Debug.WriteLine($"BEST STRATEGY: {bestStrategy} | {bestUtil:F2}%");

            results.Clear();
            allPlacedParts.Clear();

            if (bestResults != null)
                foreach (var r in bestResults) results.Add(r);
            if (bestPlacement != null)
                foreach (var p in bestPlacement) allPlacedParts.Add(p);
        }

        // =====================================================
        // AUTO-SELECT: Run multiple seeds, pick best
        // =====================================================

        private void ExecuteAutoSelect(List<StockSheet> stockSheets, List<CutPart> cutParts,
                                    ObservableCollection<OptimizationResult> results,
                                    List<PlacedPart> allPlacedParts,
                                    ObservableCollection<OptimizationJob> savedJobs)
        {
            double bestUtil = 0;
            List<PlacedPart> bestPlacement = null;
            List<OptimizationResult> bestResults = null;

            // Try different seeds
            for (int seed = 42; seed < 100; seed += 15)
            {
                _rng = new Random(seed);

                var testResults = new ObservableCollection<OptimizationResult>();
                var testPlaced = new List<PlacedPart>();

                ExecuteIQ200V7Core(stockSheets, cutParts, testResults, testPlaced, savedJobs);

                System.Diagnostics.Debug.WriteLine($"Seed {seed}: {_overallUtilization:F2}%");

                if (_overallUtilization > bestUtil)
                {
                    bestUtil = _overallUtilization;
                    bestPlacement = new List<PlacedPart>(testPlaced);
                    bestResults = new List<OptimizationResult>(testResults);
                }
            }

            // Apply best
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
            const double eps = 0.001;
            double x2 = x + w;
            double y2 = y + h;

            foreach (var rect in placedRects)
            {
                double rx2 = rect.X + rect.W;
                double ry2 = rect.Y + rect.H;

                bool notOverlap = (x2 <= rect.X + eps) || (x + eps >= rx2) ||
                                 (y2 <= rect.Y + eps) || (y + eps >= ry2);

                if (!notOverlap) return true;
            }
            return false;
        }

        // =====================================================
        // HELPER METHODS
        // =====================================================

        // RULE [20]: Fragment count
        private int CountFragments(List<PlacedPart> placed)
        {
            return placed.Count(p => (p.L * p.W) < 100000);
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

                double pw = part.L + _kerf;
                double ph = part.W + _kerf;
                double pwRot = part.W + _kerf;
                double phRot = part.L + _kerf;

                bool rotated = false;
                MaxRect bestRect = null;
                double minScore = double.MaxValue;

                double bestScore = double.MaxValue;

                // Try ALL positions with complex scoring
                foreach (var rect in freeRects)
                {
                    // Normal orientation
                    if (rect.Fits(pw, ph))
                    {
                        double score;
                        if (_nestingStrategy == NestingStrategy.ComplexSkyline)
                            score = ScorePlacementSkyline(rect, pw, ph);
                        else if (_nestingStrategy == NestingStrategy.Guillotine)
                            score = ScorePlacementGuillotine(rect, pw, ph);
                        else
                            score = ScorePlacement(rect, pw, ph, _nestingStrategy);

                        if (score < bestScore) { bestScore = score; bestRect = rect; rotated = false; }
                    }

                    // Rotated orientation (ALWAYS check for complex)
                    if (part.Rot && rect.Fits(pwRot, phRot))
                    {
                        double score;
                        if (_nestingStrategy == NestingStrategy.ComplexSkyline)
                            score = ScorePlacementSkyline(rect, pwRot, phRot);
                        else if (_nestingStrategy == NestingStrategy.Guillotine)
                            score = ScorePlacementGuillotine(rect, pwRot, phRot);
                        else
                            score = ScorePlacement(rect, pwRot, phRot, _nestingStrategy);

                        if (score < bestScore) { bestScore = score; bestRect = rect; rotated = true; }
                    }
                }

                if (bestRect != null)
                {
                    double placeW = rotated ? part.W : part.L;
                    double placeH = rotated ? part.L : part.W;

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

        // ADVANCED Multi-Factor Scoring
        private double ScorePlacement(MaxRect rect, double w, double h, NestingStrategy strategy)
        {
            double score = 0;

            switch (strategy)
            {
                case NestingStrategy.BestArea:
                    // Minimize leftover area
                    score = rect.Area - (w * h);
                    break;

                case NestingStrategy.ShortSideFit:
                    // Prefer fits where short side aligns with short side
                    double shortGap = Math.Min(rect.Width - w, rect.Height - h);
                    score = shortGap > 0 ? shortGap * 10 : -shortGap * 50;
                    score += rect.Y * 50 + rect.X;
                    break;

                case NestingStrategy.LongSideFit:
                    // Prefer fits where long side aligns with long side
                    double longGap = Math.Max(rect.Width - w, rect.Height - h);
                    score = longGap > 0 ? longGap * 10 : -longGap * 50;
                    score += rect.Y * 50 + rect.X;
                    break;

                case NestingStrategy.BottomLeft:
                    // Bottom-left first
                    score = rect.Y * 1000 + rect.X;
                    break;

                case NestingStrategy.Guillotine:
                    // Best for guillotine cuts
                    double rightWaste = rect.Width - w;
                    double bottomWaste = rect.Height - h;

                    // Prefer cuts that create clean rectangles
                    if (rightWaste == 0) score -= 1000;
                    if (bottomWaste == 0) score -= 1000;

                    score += (rightWaste + bottomWaste) * 5;
                    score += rect.Y * 100 + rect.X;
                    break;

                case NestingStrategy.Skyline:
                case NestingStrategy.ComplexSkyline:
                    // Row-by-row filling
                    double skylineScore = rect.Y * 10000;

                    // Prefer positions that fill row completely
                    if (rect.Height >= h && rect.Width >= w)
                    {
                        skylineScore -= (w * h) / 100;
                    }

                    // Bonus for tight horizontal fit
                    if (rect.Width - w < 10) skylineScore -= 500;

                    score = skylineScore;
                    break;

                case NestingStrategy.ComplexBestFit:
                case NestingStrategy.BestPerimeter:
                    // Best perimeter fit
                    double perimeter = 2 * (w + h);
                    double fitRatio = Math.Min(rect.Width / w, rect.Height / h);
                    score = -perimeter * fitRatio;
                    score += rect.Y * 50 + rect.X;
                    break;

                default:
                    // Default: minimize leftover area
                    score = rect.Area - (w * h);
                    break;
            }

            return score;
        }

        // IMPROVED Skyline - Row by Row with better fill
        private double ScorePlacementSkyline(MaxRect rect, double w, double h)
        {
            // Factor 1: Y position (lower = better)
            double posScore = rect.Y * 10000;

            // Factor 2: Fill ratio (bigger fills = better)
            double fillScore = 0;
            if (rect.Height >= h && rect.Width >= w)
            {
                fillScore = -(w * h) / 100; // Bigger = better

                // Bonus for tight fits
                if (rect.Width - w < 5) fillScore -= 200;
                if (rect.Height - h < 5) fillScore -= 200;
            }

            // Factor 3: Prefer bottom-left
            posScore += rect.X;

            return posScore + fillScore;
        }

        // Guillotine Complex - Best Area Fit
        private double ScorePlacementGuillotine(MaxRect rect, double w, double h)
        {
            double score = 0;

            // Best Area: minimize leftover
            double areaFit = rect.Area - (w * h);
            score = areaFit;

            // Short side fit preference
            double shortSide = Math.Min(rect.Width - w, rect.Height - h);
            if (shortSide >= 0) score += shortSide * 2;
            else score -= shortSide * 5; // Penalty for too big

            // Position preference
            score += rect.Y * 10 + rect.X;

            return score;
        }

        // REMOVED - Already defined above (line 793)
        // This was duplicate

        private PlacementNode EvaluatePlacementWithLookahead(CutPart part, double kerf)
        {
            double w = part.L;
            double h = part.W;
            double wRot = part.W;
            double hRot = part.L;

            double bestScore = double.MaxValue;
            PlacementNode bestPlacement = null;

            // IMPROVED: Try more positions with better scoring
            const double step = 10; // Smaller step = better fits (10mm)

            // Try BOTH guillotine and sky_line positions
            bool tryGuillotine = true;
            bool trySkyline = true;

            foreach (var rect in _freeRects)
            {
                // Skip tiny rectangles
                if (rect.Area < 10000) continue;

                // Try multiple positions within this free rect
                for (double py = rect.Y; py <= rect.Y + rect.Height - h; py += step)
                {
                    for (double px = rect.X; px <= rect.X + rect.Width - w; px += step)
                    {
                        // Check NORMAL orientation at this position
                        if (px + w <= rect.X + rect.Width && py + h <= rect.Y + rect.Height)
                        {
                            double leftoverArea = rect.Area - (w * h);
                            double shortSide = Math.Min(rect.Width - w, rect.Height - h);
                            double fitScore = shortSide >= 0 ? shortSide : -shortSide * 10;

                            // Bonus for tight fits
                            if (rect.Width - w < 20) fitScore -= 100;
                            if (rect.Height - h < 20) fitScore -= 100;

                            double score = leftoverArea + fitScore;

                            if (score < bestScore)
                            {
                                bestScore = score;
                                bestPlacement = new PlacementNode { X = px, Y = py, Width = w, Height = h, IsRotated = false, Score = score };
                            }
                        }

                        // Check ROTATED orientation at this position
                        if (part.Rot && px + wRot <= rect.X + rect.Width && py + hRot <= rect.Y + rect.Height)
                        {
                            double leftoverArea = rect.Area - (wRot * hRot);
                            double shortSide = Math.Min(rect.Width - wRot, rect.Height - hRot);
                            double fitScore = shortSide >= 0 ? shortSide : -shortSide * 10;

                            if (rect.Width - wRot < 20) fitScore -= 100;
                            if (rect.Height - hRot < 20) fitScore -= 100;
                            double score = leftoverArea + fitScore;

                            if (score < bestScore)
                            {
                                bestScore = score;
                                bestPlacement = new PlacementNode { X = px, Y = py, Width = wRot, Height = hRot, IsRotated = true, Score = score };
                            }
                        }
                    }
                }

                // Also try at rectangle origins (original logic)
                if (rect.Fits(w, h))
                {
                    double leftoverArea = rect.Area - (w * h);
                    double shortSide = Math.Min(rect.Width - w, rect.Height - h);
                    double score = leftoverArea + shortSide;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestPlacement = new PlacementNode { X = rect.X, Y = rect.Y, Width = w, Height = h, IsRotated = false, Score = score };
                    }
                }

                if (part.Rot && rect.Fits(wRot, hRot))
                {
                    double leftoverArea = rect.Area - (wRot * hRot);
                    double shortSide = Math.Min(rect.Width - wRot, rect.Height - hRot);
                    double score = leftoverArea + shortSide;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestPlacement = new PlacementNode { X = rect.X, Y = rect.Y, Width = wRot, Height = hRot, IsRotated = true, Score = score };
                    }
                }
            }

            if (bestPlacement == null)
            {
                System.Diagnostics.Debug.WriteLine($"NO FIT: {part.Ref} {part.L}x{part.W}");
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

            double rightW = usedRect.Width - w;
            double bottomH = usedRect.Height - h;
            double splitX = placement.X + w;
            double splitY = placement.Y + h;

            // IMPROVED: Create RIGHT rectangle (full height remaining)
            if (rightW > _breakout && usedRect.Height > _breakout)
                _freeRects.Add(new MaxRect(splitX, placement.Y, rightW, usedRect.Height));

            // IMPROVED: Create BOTTOM rectangle (full width remaining)
            if (bottomH > _breakout && usedRect.Width > _breakout)
                _freeRects.Add(new MaxRect(placement.X, splitY, usedRect.Width, bottomH));

            // IMPROVED: Merge adjacent rectangles to reduce fragmentation
            MergeFreeRectsImproved(_freeRects);

            // Remove tiny fragments (prune small waste)
            _freeRects.RemoveAll(r => r.Area < _breakout * _breakout * 2);
            PruneContainedFreeRects(_freeRects);
        }

        // IMPROVED: Guillotine split - better rectangle management
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
            double rightX = placement.X + w;
            double bottomY = placement.Y + h;

            // IMPROVED: Create right and bottom rectangles
            if (rightW > _breakout && usedRect.Height > _breakout)
                freeRects.Add(new MaxRect(rightX, placement.Y, rightW, usedRect.Height));

            if (bottomH > _breakout && usedRect.Width > _breakout)
                freeRects.Add(new MaxRect(placement.X, bottomY, usedRect.Width, bottomH));

            // Merge adjacent rectangles to reduce fragmentation
            MergeFreeRectsImproved(freeRects);

            // Remove tiny fragments that waste memory
            freeRects.RemoveAll(r => r.Area < 50000);
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

                        // Horizontal merge
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
                        // Vertical merge
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

        // DISABLED - Don't merge rectangles (keeps fragmented waste areas)
        private void MergeFreeRects()
        {
            // Do nothing - keep rectangles separate (more waste!)
            // Skipping merge entirely

            // Optional: Just prune tiny ones > 50000
            _freeRects.RemoveAll(r => r.Area < 50000);
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
            if (!_twoPassEnabled || unplacedParts.Count == 0) return 0;
            if (_remnants.Count == 0) return 0;

            var usableRemnants = _remnants.Where(r => !r.IsReused && r.ValueScore > 0.2).OrderByDescending(r => r.Area).ToList();
            if (usableRemnants.Count == 0) return 0;

            double partsAreaOnRemnants = 0;

            foreach (var remnant in usableRemnants)
            {
                var placedOnRemnant = new List<(double X, double Y, double W, double H)>();

                foreach (var part in unplacedParts)
                {
                    if (part.IsPlaced) continue;

                    double pW = part.L + _kerf;
                    double pH = part.W + _kerf;

                    bool canFitNormal = pW <= remnant.Width && pH <= remnant.Height;
                    bool canFitRotated = false;
                    if (part.Rot)
                        canFitRotated = pH <= remnant.Width && pW <= remnant.Height;

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

                    placedOnRemnant.Add((newX, newY, newW, newH));
                }

                if (placedOnRemnant.Count > 0)
                    remnant.IsReused = true;
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
        // GENETIC HELPERS
        // =====================================================

        private Genome Crossover(Genome p1, Genome p2)
        {
            var child = new Genome();
            int split = _rng.Next(p1.PartOrder.Count);
            child.PartOrder = new List<CutPart>();
            child.PartOrder.AddRange(p1.PartOrder.Take(split));
            child.PartOrder.AddRange(p2.PartOrder.Skip(split));
            return child;
        }

        private void Mutate(Genome g)
        {
            int i = _rng.Next(g.PartOrder.Count);
            int j = _rng.Next(g.PartOrder.Count);
            var temp = g.PartOrder[i];
            g.PartOrder[i] = g.PartOrder[j];
            g.PartOrder[j] = temp;
        }

        private double EvaluateGenome(Genome g, List<StockSheet> stockSheets)
        {
            var testParts = new List<CutPart>(g.PartOrder);
            double totalUtil = 0;
            foreach (var stock in stockSheets.Take(5))
            {
                var placed = new List<PlacedPart>();
                PlacePartsGuillotine(testParts, stock.L - _lr - _rm, stock.W - _tr - _br, stock.Ref, 1, placed);
                totalUtil += ComputeUtilization(placed, stock);
            }
            return totalUtil;
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

            stockCost += wasteArea * _costModel.WastePenaltyPerSQM / 1000;

            double remnantCredit = 0;
            foreach (var rem in _remnants.Where(r => !r.IsReused))
                remnantCredit += _costModel.CalculateRemnantCredit(rem.Area);
            stockCost -= remnantCredit;

            double totalKerf = 0;
            foreach (var seq in _cutSequences)
                totalKerf += seq.TotalKerfLength;
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
        public List<PlacedPart> Placed = new List<PlacedPart>(); // ADD THIS
        public double Fitness { get; set; }
        public double Cost { get; set; } // ADD THIS
        public double Utilization { get; set; } // ADD THIS
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