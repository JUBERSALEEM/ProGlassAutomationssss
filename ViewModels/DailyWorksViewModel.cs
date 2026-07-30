using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProGlassAutomation.Data;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using ProGlassAutomation.ViewModels.DailyWorks.Models;
using ProGlassAutomation.ViewModels.DailyWorks.Repositories;

// Add aliases to disambiguate
using DbHelper = ProGlassAutomation.Data.Database.DbHelper;
using DbDailyWork = ProGlassAutomation.Data.DbDailyWork;  // ✅ CORRECT

namespace ProGlassAutomation.ViewModels
{
    /// <summary>
    /// ViewModel - Manual properties, uses RelayCommand from toolkit
    /// </summary>
    // PATCH 54: Implement IDisposable for proper cleanup
    public partial class DailyWorksViewModel : ObservableObject, IDisposable
    {
        private readonly ProGlassAutomation.ViewModels.DailyWorks.Repositories.IDailyWorkRepository _repository;

        // Main collection
        private ObservableCollection<DailyWorkModel> _dailyWorks = new();
        public ObservableCollection<DailyWorkModel> DailyWorks
        {
            get => _dailyWorks;
            set => SetProperty(ref _dailyWorks, value);
        }

        // Filtered view
        private ICollectionView? _filteredView;
        public ICollectionView FilteredDataView
        {
            get
            {
                if (_filteredView == null && DailyWorks.Count > 0)
                {
                    _filteredView = CollectionViewSource.GetDefaultView(_dailyWorks);
                    _filteredView.Filter = FilterPredicate;
                }
                return _filteredView!;
            }
        }

        private bool FilterPredicate(object obj)
        {
            if (obj is not DailyWorkModel work) return false;

            // PATCH 78: Split filter into composable checks
            if (!MatchesSearchFilter(work)) return false;
            if (!MatchesStatusFilter(work)) return false;
            if (!MatchesProductionStatusFilter(work)) return false;
            if (!MatchesTypeOfWorkFilter(work)) return false;
            if (!MatchesSalesmanFilter(work)) return false;
            if (!MatchesCompanyFilter(work)) return false;
            if (!MatchesColorFilter(work)) return false;
            if (!MatchesPINumberFilter(work)) return false;
            if (!MatchesCustomerReferenceFilter(work)) return false;
            if (!MatchesDateFilter(work)) return false;
            if (!MatchesFavoritesFilter(work)) return false;

            return true;
        }

        // PATCH 78: Individual filter methods - each handles one field
        private bool MatchesSearchFilter(DailyWorkModel work)
        {
            if (work == null) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            return MatchesSearch(work, SearchText.ToLowerInvariant());
        }

        private bool MatchesStatusFilter(DailyWorkModel work)
        {
            if (string.IsNullOrWhiteSpace(FilterStatus)) return true;
            return work.Status == FilterStatus;
        }

        private bool MatchesProductionStatusFilter(DailyWorkModel work)
        {
            if (string.IsNullOrWhiteSpace(FilterProductionStatus)) return true;
            return work.ProductionStatus == FilterProductionStatus;
        }

        private bool MatchesTypeOfWorkFilter(DailyWorkModel work)
        {
            if (string.IsNullOrWhiteSpace(FilterTypeOfWork)) return true;
            return work.TypeOfWork == FilterTypeOfWork;
        }

        private bool MatchesSalesmanFilter(DailyWorkModel work)
        {
            if (string.IsNullOrWhiteSpace(FilterSalesman)) return true;
            return work.Salesman == FilterSalesman;
        }

        private bool MatchesCompanyFilter(DailyWorkModel work)
        {
            if (string.IsNullOrWhiteSpace(FilterCompany)) return true;
            return work.Company == FilterCompany;
        }

        private bool MatchesColorFilter(DailyWorkModel work)
        {
            if (string.IsNullOrWhiteSpace(FilterColor)) return true;
            return work.Color == FilterColor;
        }

        private bool MatchesPINumberFilter(DailyWorkModel work)
        {
            if (string.IsNullOrWhiteSpace(FilterPINumber)) return true;
            return work.PiNumber == FilterPINumber;
        }

        private bool MatchesCustomerReferenceFilter(DailyWorkModel work)
        {
            if (string.IsNullOrWhiteSpace(FilterCustomerReference)) return true;
            return work.CustomerReference == FilterCustomerReference;
        }

        private bool MatchesDateFilter(DailyWorkModel work)
        {
            if (FilterStartDate.HasValue && work.Date < FilterStartDate) return false;
            if (FilterEndDate.HasValue && work.Date > FilterEndDate) return false;
            return true;
        }

        private bool MatchesFavoritesFilter(DailyWorkModel work)
        {
            if (!_filterFavoritesOnly) return true;
            return work.IsFavorite;
        }

        // PATCH 143: Wildcard search support
        private bool MatchesSearch(DailyWorkModel work, string search)
        {
            if (string.IsNullOrEmpty(search)) return true;

            // Support wildcard (* and ?)
            if (search.Contains('*') || search.Contains('?'))
            {
                return ContainsWildcard(work.Company ?? "", search) ||
                       ContainsWildcard(work.PiNumber ?? "", search) ||
                       ContainsWildcard(work.CustomerReference ?? "", search) ||
                       ContainsWildcard(work.TypeOfWork ?? "", search) ||
                       ContainsWildcard(work.Salesman ?? "", search) ||
                       ContainsWildcard(work.Notes ?? "", search);
            }

            // Exact search (original behavior)
            return work.Company.ToLowerInvariant().Contains(search) ||
                   work.PiNumber.ToLowerInvariant().Contains(search) ||
                   work.CustomerReference.ToLowerInvariant().Contains(search) ||
                   work.Salesman.ToLowerInvariant().Contains(search) ||
                   work.Notes.ToLowerInvariant().Contains(search);
        }

        private bool ContainsWildcard(string source, string search)
        {
            if (string.IsNullOrEmpty(search)) return true;

            var pattern = "^" + System.Text.RegularExpressions.Regex.Escape(search)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") + "$";

            return System.Text.RegularExpressions.Regex.IsMatch(source ?? "", pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Selection - PATCH 63: Manual property (source generator had issues)
        private DailyWorkModel? _selectedItem;
        public DailyWorkModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                // PATCH 148: Push to undo stack on selection change
                if (value != null)
                {
                    PushToUndo(value);
                }
            }
        }

        // PATCH 113: Check for duplicates before save
        private DailyWorkModel? CheckForDuplicate()
        {
            if (EditingWork == null) return null;

            return DailyWorks.FirstOrDefault(w =>
                w.Id != EditingWork.Id &&
                string.Equals(w.PiNumber?.Trim(), EditingWork.PiNumber?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                string.Equals(w.CustomerReference?.Trim(), EditingWork.CustomerReference?.Trim(), StringComparison.OrdinalIgnoreCase));
        }


        // PATCH 20: Selection tracking with HashSet for O(1) lookups
        private HashSet<int> _selectedIds = new();

        private int _selectedCount;
        public int SelectedCount => _selectedCount;

        public void UpdateSelectedIds(IEnumerable<int> ids)
        {
            _selectedIds = new HashSet<int>(ids);
            _selectedCount = _selectedIds.Count;
            OnPropertyChanged(nameof(SelectedCount));
        }

        // PATCH 116: Soft delete - store deleted items for undo
        private readonly Stack<DailyWorkModel> _deletedItems = new();
        public int DeletedCount => _deletedItems.Count;

        // PATCH 148: Undo/Redo stacks
        private readonly Stack<DailyWorkModel> _undoStack = new();
        private readonly Stack<DailyWorkModel> _redoStack = new();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        // PATCH 149: Activity log
        public ObservableCollection<ActivityLog> ActivityLogs { get; } = new();

        public void LogActivity(string action, string details)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                ActivityLogs.Insert(0, new ActivityLog
                {
                    Timestamp = DateTime.Now,
                    Action = action,
                    Details = details,
                    User = Environment.UserName
                });

                while (ActivityLogs.Count > 100)
                    ActivityLogs.RemoveAt(ActivityLogs.Count - 1);
            });
        }

        // PATCH 112: Unified debounce - single CancellationToken approach
        private CancellationTokenSource? _searchDebounceToken;
        private readonly TimeSpan _debounceDelay = TimeSpan.FromMilliseconds(300);

        // PATCH 153: Auto-save draft timer
        private System.Timers.Timer? _autoSaveTimer;

        // PATCH 153: Start auto-save on construction
        private void StartAutoSave()
        {
            _autoSaveTimer = new System.Timers.Timer(60000); // 60 seconds
            _autoSaveTimer.Elapsed += async (s, e) => await SaveDraftAsync();
            _autoSaveTimer.Start();
        }

        // PATCH 153: Save draft to local file
        private async Task SaveDraftAsync()
        {
            try
            {
                if (EditingWork != null)
                {
                    var draft = System.Text.Json.JsonSerializer.Serialize(EditingWork);
                    var draftPath = "draft.json";
                    await System.IO.File.WriteAllTextAsync(draftPath, draft);
                    System.Diagnostics.Debug.WriteLine($"[DailyWorksVM] Draft auto-saved at {DateTime.Now}");
                }
            }
            catch { /* Ignore errors */ }
        }

        // PATCH 45-46: Explicit command properties
        public RelayCommand ExportSelectedToCsvCommand { get; private set; }
        public RelayCommand ImportFromCsvCommand { get; private set; }
        // PATCH 144: Export to Excel command
        public RelayCommand ExportToExcelCommand { get; private set; }
        // DELETE BUTTON FIX: Add DeleteCommand property
        public RelayCommand DeleteCommand { get; private set; }
        // PATCH 167: Export with preset command
        public RelayCommand ExportWithPresetCommand { get; private set; }
        // PATCH 168: Backup/Restore commands
        public RelayCommand BackupDatabaseCommand { get; private set; }
        public RelayCommand RestoreDatabaseCommand { get; private set; }
        // ToggleDarkModeCommand is auto-generated by [RelayCommand]

        // PATCH 16: Filter debounce
        private CancellationTokenSource? _filterDebounceToken;

        // PATCH 112: Quick instant search toggle
        private bool _instantSearch = true;
        public bool InstantSearch
        {
            get => _instantSearch;
            set => SetProperty(ref _instantSearch, value);
        }

        // PATCH 154: Export format selection
        public string ExportFormat { get; set; } = "CSV";

        [RelayCommand]
        private async Task ExportAsync(string format)
        {
            ExportFormat = format;

            if (format == "CSV")
                await ExportToCsvAsync();
            else if (format == "Excel")
                await ExecuteExportToExcelAsync();
            else
                await ExportToCsvAsync();
        }

        // PATCH 123: Dark mode toggle
        private bool _isDarkMode = false;
        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (SetProperty(ref _isDarkMode, value))
                {
                    ApplyDarkMode();
                }
            }
        }

        private void ApplyDarkMode()
        {
            // Dark mode colors will be handled via resource dictionary or direct binding
            OnPropertyChanged(nameof(IsDarkMode));
        }

        // PATCH 126: Helper properties for dark mode colors
        public string CurrentBackground => IsDarkMode ? "#1F2937" : "#F1F5F9";
        public string CurrentForeground => IsDarkMode ? "#F9FAFB" : "#1E293B";
        public string CurrentCardBackground => IsDarkMode ? "#374151" : "#FFFFFF";
        public string CurrentHeaderBackground => IsDarkMode ? "#111827" : "#1E40AF";
        public string CurrentHeaderForeground => IsDarkMode ? "#F9FAFB" : "#FFFFFF";
        public string CurrentBorderColor => IsDarkMode ? "#4B5563" : "#3B82F6";

        // PATCH 129: Recent searches
        private readonly List<string> _recentSearches = new();
        public IReadOnlyList<string> RecentSearches => _recentSearches.AsReadOnly();

        private void AddRecentSearch(string search)
        {
            if (string.IsNullOrWhiteSpace(search)) return;

            // Remove if already exists
            _recentSearches.Remove(search);

            // Add to front
            _recentSearches.Insert(0, search);

            // Keep only last 5
            while (_recentSearches.Count > 5)
                _recentSearches.RemoveAt(_recentSearches.Count - 1);

            OnPropertyChanged(nameof(RecentSearches));

            // PATCH 160: Save to file
            SaveSearchHistory();
        }

        // PATCH 160: Save search history to file
        private void SaveSearchHistory()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(_recentSearches);
                System.IO.File.WriteAllText("search_history.json", json);
            }
            catch { /* Ignore errors */ }
        }

        // PATCH 160: Load search history from file
        private void LoadSearchHistory()
        {
            try
            {
                if (System.IO.File.Exists("search_history.json"))
                {
                    var json = System.IO.File.ReadAllText("search_history.json");
                    var searches = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                    if (searches != null)
                    {
                        _recentSearches.Clear();
                        _recentSearches.AddRange(searches.Take(5));
                        OnPropertyChanged(nameof(RecentSearches));
                    }
                }
            }
            catch { /* Ignore errors */ }
        }

        // NEW: Import directly from DailyWorks database table
        [RelayCommand]
        private async Task ImportFromDatabaseAsync()
        {
            try
            {
                var result = MessageBox.Show(
                    "Import all records from DailyWorks database table?\nThis will add records that don't exist in JSON.",
                    "Confirm Database Import",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                IsBusy = true;
                StatusMessage = "Importing from database...";

                // Get all records from database - returns Database.DailyWork objects
                var dbRecords = ProGlassAutomation.Data.Database.DbHelper.GetAllDailyWork();
                var imported = 0;
                var skipped = 0;

                foreach (var dbRecord in dbRecords)
                {
                    // Check if already exists (by PI Number and Customer Reference)
                    var existing = DailyWorks.FirstOrDefault(w =>
                        string.Equals(w.PiNumber?.Trim(), dbRecord.PINumber?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(w.CustomerReference?.Trim(), dbRecord.CustomerReference?.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (existing == null)
                    {
                        // Convert DbHelper DailyWork to DailyWorkModel
                        var newWork = new DailyWorkModel
                        {
                            Id = dbRecord.Id,
                            Date = dbRecord.Date,
                            Company = dbRecord.Company,
                            PiNumber = dbRecord.PINumber,
                            Color = dbRecord.Color,
                            CustomerReference = dbRecord.CustomerReference,
                            TypeOfWork = dbRecord.TypeOfWork,
                            ProductionStatus = dbRecord.ProductionStatus,
                            Qty = dbRecord.Qty,
                            Sqm = dbRecord.SQM,
                            Status = dbRecord.Status,
                            Salesman = dbRecord.Salesman,
                            Notes = dbRecord.Notes,
                            CreatedDate = dbRecord.CreatedDate
                        };
                        DailyWorks.Add(newWork);
                        imported++;
                    }
                    else
                    {
                        skipped++;
                    }
                }

                RefreshFilteredView();
                UpdateStatistics();
                StatusMessage = $"Imported {imported} records, skipped {skipped} existing!";
                LogActivity("DB IMPORT", $"Imported {imported} records from database");
            }
            catch (Exception ex)
            {
                StatusMessage = "Import failed: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // NEW: Combined import - JSON + Database
        [RelayCommand]
        private async Task ImportAllAsync()
        {
            try
            {
                var result = MessageBox.Show(
                    "Import all records from both JSON files AND database?\nThis ensures complete data.",
                    "Confirm Full Import",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                IsBusy = true;
                StatusMessage = "Loading all data...";

                // Reload from database (master source)
                await LoadDataAsync();

                // Also export to JSON for Delivery
                foreach (var work in DailyWorks)
                {
                    await ExportToJsonFileAsync(work);
                }

                StatusMessage = "Full import complete!";
                LogActivity("FULL IMPORT", "Imported from database and exported to JSON");
            }
            catch (Exception ex)
            {
                StatusMessage = "Import failed: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Note: SearchText needs custom setter for debounce, so keep manual
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    AddRecentSearch(value); // PATCH 129: Track recent searches

                    // PATCH 112: Unified debounce - CancellationToken approach
                    if (_instantSearch)
                    {
                        // Instant search - no debounce
                        RefreshFilteredView();
                    }
                    else
                    {
                        // Debounced search - cancel and restart
                        _searchDebounceToken?.Cancel();
                        _searchDebounceToken = new CancellationTokenSource();
                        _ = DebounceSearchAsync(_searchDebounceToken.Token);
                    }
                }
            }
        }

        // PATCH 112: Async debounce helper
        private async Task DebounceSearchAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(_debounceDelay, token);
                // Only refresh if token wasn't cancelled
                if (!token.IsCancellationRequested)
                {
                    RefreshFilteredView();
                }
            }
            catch (TaskCanceledException)
            {
                // Expected - debounce cancelled
            }
        }

        private string _filterStatus = string.Empty;
        public string FilterStatus
        {
            get => _filterStatus;
            set { if (SetProperty(ref _filterStatus, value)) DebounceFilterRefreshAsync(); }
        }

        private string _filterProductionStatus = string.Empty;
        public string FilterProductionStatus
        {
            get => _filterProductionStatus;
            set { if (SetProperty(ref _filterProductionStatus, value)) DebounceFilterRefreshAsync(); }
        }

        private string _filterTypeOfWork = string.Empty;
        public string FilterTypeOfWork
        {
            get => _filterTypeOfWork;
            set { if (SetProperty(ref _filterTypeOfWork, value)) DebounceFilterRefreshAsync(); }
        }

        private string _filterSalesman = string.Empty;
        public string FilterSalesman
        {
            get => _filterSalesman;
            set { if (SetProperty(ref _filterSalesman, value)) DebounceFilterRefreshAsync(); }
        }

        private string _filterCompany = string.Empty;
        public string FilterCompany
        {
            get => _filterCompany;
            set { if (SetProperty(ref _filterCompany, value)) DebounceFilterRefreshAsync(); }
        }

        private string _filterColor = string.Empty;
        public string FilterColor
        {
            get => _filterColor;
            set { if (SetProperty(ref _filterColor, value)) DebounceFilterRefreshAsync(); }
        }

        private string _filterPINumber = string.Empty;
        public string FilterPINumber
        {
            get => _filterPINumber;
            set { if (SetProperty(ref _filterPINumber, value)) DebounceFilterRefreshAsync(); }
        }

        private string _filterCustomerReference = string.Empty;
        public string FilterCustomerReference
        {
            get => _filterCustomerReference;
            set { if (SetProperty(ref _filterCustomerReference, value)) DebounceFilterRefreshAsync(); }
        }

        private DateTime? _filterStartDate;
        public DateTime? FilterStartDate
        {
            get => _filterStartDate;
            set { if (SetProperty(ref _filterStartDate, value)) DebounceFilterRefreshAsync(); }
        }

        private DateTime? _filterEndDate;
        public DateTime? FilterEndDate
        {
            get => _filterEndDate;
            set { if (SetProperty(ref _filterEndDate, value)) DebounceFilterRefreshAsync(); }
        }

        // Editing state - MANUAL
        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        private DailyWorkModel? _editingWork;
        public DailyWorkModel? EditingWork
        {
            get => _editingWork;
            set => SetProperty(ref _editingWork, value);
        }

        private string _statusMessage = "Ready";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        private bool _isDuplicateWarning;
        public bool IsDuplicateWarning
        {
            get => _isDuplicateWarning;
            set => SetProperty(ref _isDuplicateWarning, value);
        }

        private string _duplicateMessage = string.Empty;
        public string DuplicateMessage
        {
            get => _duplicateMessage;
            set => SetProperty(ref _duplicateMessage, value);
        }

        // PATCH 152: Validation error highlighted
        private string _validationError = string.Empty;
        public string ValidationError
        {
            get => _validationError;
            set => SetProperty(ref _validationError, value);
        }

        [RelayCommand]
        private async Task DeleteSelectedAsync(object? parameter)
        {
            // PATCH 55: Prevent command re-entry
            if (IsBusy) return;

            // Get selected IDs from the tracking list
            var selectedIds = _selectedIds?.ToList() ?? new List<int>();

            System.Diagnostics.Debug.WriteLine($"[DailyWorksVM] DeleteSelected called. Count: {selectedIds.Count}");

            // If empty, try to get from DataGrid parameter
            if (selectedIds.Count == 0 && parameter is DataGrid dg)
            {
                selectedIds.Clear();
                foreach (var item in dg.SelectedItems)
                {
                    if (item is DailyWorkModel work)
                    {
                        selectedIds.Add(work.Id);
                        System.Diagnostics.Debug.WriteLine($"[DailyWorksVM] Adding selected Id: {work.Id}");
                    }
                }
            }

            if (selectedIds.Count == 0)
            {
                StatusMessage = "No items selected! Please select items first.";
                MessageBox.Show("Please select items to delete.", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // PATCH 159: Extra confirmation for large batches
            if (selectedIds.Count > 10)
            {
                var confirmLarge = MessageBox.Show(
                    $"You are about to delete {selectedIds.Count} records.\nThis is a large number. Are you sure?",
                    "Confirm Large Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (confirmLarge != MessageBoxResult.Yes)
                    return;
            }

            var result = MessageBox.Show($"Delete {selectedIds.Count} selected records?",
                "Confirm Bulk Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                // PATCH 01, 04: Use repository instead of direct DbHelper call
                foreach (var id in selectedIds)
                {
                    await _repository.DeleteAsync(id);
                    System.Diagnostics.Debug.WriteLine($"[DailyWorksVM] Deleted Id: {id}");
                }

                await LoadDataAsync();
                _selectedIds.Clear(); // PATCH 20: Clear HashSet
                SelectedItem = null;
                StatusMessage = $"Deleted {selectedIds.Count} records successfully!";
                LogActivity("DELETE", $"Deleted {selectedIds.Count} records");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DailyWorksVM] DeleteSelected error: {ex.Message}");
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        // Busy state
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        // Options
        public ObservableCollection<string> CompanyOptions { get; } = new();
        public ObservableCollection<string> PINumberOptions { get; } = new();
        public ObservableCollection<string> CustomerReferenceOptions { get; } = new();
        public ObservableCollection<string> TypeOfWorkOptions { get; } = new();
        public ObservableCollection<string> StatusOptions { get; } = new();
        public ObservableCollection<string> ProductionStatusOptions { get; } = new();
        public ObservableCollection<string> SalesmanOptions { get; } = new();
        public ObservableCollection<string> ColorOptions { get; } = new();

        // PATCH 155: Import preview collection
        private ObservableCollection<DailyWorkModel> _importPreview = new();
        public ObservableCollection<DailyWorkModel> ImportPreview
        {
            get => _importPreview;
            set => SetProperty(ref _importPreview, value);
        }

        // PATCH 155: Preview import method
        [RelayCommand]
        private async Task PreviewImportAsync(string filePath)
        {
            try
            {
                var lines = await System.IO.File.ReadAllLinesAsync(filePath);
                ImportPreview.Clear();

                foreach (var line in lines.Skip(1).Take(10))
                {
                    try
                    {
                        var parts = ParseCsvLine(line);
                        if (parts.Length >= 10)
                        {
                            var work = new DailyWorkModel
                            {
                                Company = parts[1].Trim('"'),
                                PiNumber = parts[2].Trim('"'),
                                Color = parts[3].Trim('"'),
                                CustomerReference = parts[4].Trim('"'),
                                TypeOfWork = parts[5].Trim('"'),
                                ProductionStatus = parts[6].Trim('"'),
                                Qty = int.TryParse(parts[7], out var qty) ? qty : 0,
                                Sqm = double.TryParse(parts[8], out var sqm) ? sqm : 0,
                                Status = parts[9].Trim('"')
                            };
                            ImportPreview.Add(work);
                        }
                    }
                    catch { }
                }

                StatusMessage = $"Preview: {ImportPreview.Count} rows";
            }
            catch (Exception ex)
            {
                StatusMessage = "Preview failed: " + ex.Message;
            }
        }

        // PATCH 156: Statistics export
        [RelayCommand]
        private async Task ExportStatsAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    DefaultExt = ".csv",
                    FileName = $"DailyWorks_Stats_{DateTime.Now:yyyyMMdd}"
                };

                if (dialog.ShowDialog() == true)
                {
                    var lines = new List<string>
            {
                "Metric,Value",
                $"Total Records,{TotalRecords}",
                $"Total Qty,{TotalQty}",
                $"Total SQM,{TotalSQM:N2}",
                $"Filtered Records,{FilteredRecords}",
                $"Filtered Qty,{FilteredQty}",
                $"Filtered SQM,{FilteredSQM:N2}",
                $"Completed,{CompletedCount}",
                $"Pending,{PendingCount}",
                $"In Progress,{InProgressCount}",
                $"Companies,{TotalCompanies}",
                $"PI Numbers,{TotalPINumbers}"
            };

                    await System.IO.File.WriteAllLinesAsync(dialog.FileName, lines);
                    StatusMessage = "Statistics exported!";
                    LogActivity("EXPORT STATS", "Exported statistics");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Export failed: " + ex.Message;
            }
        }

        // Commands - Source generators auto-create commands from [RelayCommand]
        [RelayCommand]
        private async Task AddNewAsync()
        {
            EditingWork = DailyWorkModel.CreateNew();
            IsEditing = true;
            IsDuplicateWarning = false;
            DuplicateMessage = "";
            LogActivity("ADD", "Started adding new record");
        }

        [RelayCommand]
        private async Task EditAsync()
        {
            if (SelectedItem == null) return;
            EditingWork = SelectedItem.Clone();
            IsEditing = true;
            IsDuplicateWarning = false;
            DuplicateMessage = "";
            LogActivity("EDIT", $"Editing {SelectedItem.PiNumber}");
        }

        // FIX: Save record with JSON export for Delivery import
        [RelayCommand]
        private async Task SaveAsync()
        {
            if (EditingWork == null) return;

            // Validate
            var (isValid, message) = ValidateWork(EditingWork);
            if (!isValid)
            {
                StatusMessage = message;
                MessageBox.Show(message, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check duplicates
            var duplicate = CheckForDuplicate();
            if (duplicate != null)
            {
                IsDuplicateWarning = true;
                DuplicateMessage = $"Duplicate: {duplicate.Company} (PI: {duplicate.PiNumber}, Ref: {duplicate.CustomerReference})";
                return;
            }

            var dbEntity = DbDailyWork.FromUiModel(EditingWork);

            // FIX: Auto-set Status to "Confirmed" when ProductionStatus is "COMPLETED"
            if (!string.IsNullOrWhiteSpace(EditingWork.ProductionStatus) &&
                EditingWork.ProductionStatus.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
            {
                EditingWork.Status = "Confirmed";
                dbEntity.Status = "Confirmed";
            }
            // FIX: Set default Status for new records
            if (EditingWork.Id == 0 && string.IsNullOrWhiteSpace(EditingWork.Status))
            {
                EditingWork.Status = "Pending";
                dbEntity.Status = "Pending";
            }

            if (EditingWork.Id == 0)
            {
                EditingWork.Id = DailyWorks.Count > 0 ? DailyWorks.Max(w => w.Id) + 1 : 1;
                EditingWork.CreatedDate = DateTime.Now;
                await _repository.InsertAsync(dbEntity);
                DailyWorks.Add(EditingWork);
                LogActivity("SAVE", $"Created: {EditingWork.PiNumber}");
            }
            else
            {
                EditingWork.UpdateDate = DateTime.Today;
                await _repository.UpdateAsync(dbEntity);
                var existing = DailyWorks.FirstOrDefault(w => w.Id == EditingWork.Id);
                if (existing != null)
                {
                    var index = DailyWorks.IndexOf(existing);
                    DailyWorks[index] = EditingWork;
                }
                LogActivity("SAVE", $"Updated: {EditingWork.PiNumber}");
            }

            // FIX: Export to JSON file for Delivery import
            await ExportToJsonFileAsync(EditingWork);

            IsEditing = false;
            EditingWork = null;
            RefreshFilteredView();
            UpdateStatistics();
            StatusMessage = "Saved successfully!";
        }

        // IMPROVED: Export individual JSON file for Delivery import
        private async Task ExportToJsonFileAsync(DailyWorkModel work)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(work.PiNumber)) return;

                string dataFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!System.IO.Directory.Exists(dataFolder))
                {
                    System.IO.Directory.CreateDirectory(dataFolder);
                }

                // Use PI Number for filename (sanitize for file system)
                // FIX: More comprehensive sanitization for filenames
                string safePiNumber = work.PiNumber
                    .Replace("/", "_")
                    .Replace("\\", "_")
                    .Replace(":", "_")
                    .Replace("*", "_")
                    .Replace("?", "_")
                    .Replace("\"", "_")
                    .Replace("<", "_")
                    .Replace(">", "_")
                    .Replace("|", "_");

                // Also handle apostrophes and special chars
                safePiNumber = System.Text.RegularExpressions.Regex.Replace(safePiNumber, @"[^\w\-_]", "_");

                string jsonFileName = $"PI-{safePiNumber}.json";
                string jsonPath = System.IO.Path.Combine(dataFolder, jsonFileName);

                var jsonSettings = new Newtonsoft.Json.JsonSerializerSettings
                {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    DateFormatString = "yyyy-MM-ddTHH:mm:ss",
                    Formatting = Newtonsoft.Json.Formatting.Indented
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(work, jsonSettings);
                await System.IO.File.WriteAllTextAsync(jsonPath, json);

                System.Diagnostics.Debug.WriteLine($"[DailyWorksVM] Exported JSON: {jsonFileName}");

                // Log successful export for debugging
                LogActivity("JSON EXPORT", $"Exported {jsonFileName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DailyWorksVM] JSON export error: {ex.Message}");
                // Don't show error to user - non-critical
            }
        }

        // NEW: Bulk export all DailyWorks to JSON
        [RelayCommand]
        private async Task ExportAllToJsonAsync()
        {
            try
            {
                string dataFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!System.IO.Directory.Exists(dataFolder))
                {
                    System.IO.Directory.CreateDirectory(dataFolder);
                }

                var exported = 0;
                foreach (var work in DailyWorks)
                {
                    if (!string.IsNullOrWhiteSpace(work.PiNumber))
                    {
                        await ExportToJsonFileAsync(work);
                        exported++;
                    }
                }

                StatusMessage = $"Exported {exported} records to JSON!";
                LogActivity("BULK JSON EXPORT", $"Exported {exported} records");
            }
            catch (Exception ex)
            {
                StatusMessage = "Bulk export failed: " + ex.Message;
            }
        }

        // PATCH 147: Validate required fields
        public (bool IsValid, string Message) ValidateWork(DailyWorkModel work)
        {
            if (string.IsNullOrWhiteSpace(work.Company))
                return (false, "Company is required");

            if (string.IsNullOrWhiteSpace(work.PiNumber))
                return (false, "PI Number is required");

            if (work.Qty <= 0)
                return (false, "QTY must be greater than 0");

            if (work.Sqm <= 0)
                return (false, "SQM must be greater than 0");

            return (true, "Valid");
        }

        [RelayCommand]
        private void Cancel()
        {
            IsEditing = false;
            EditingWork = null;
            IsDuplicateWarning = false;
            DuplicateMessage = "";
            LogActivity("CANCEL", "Cancelled editing");
        }

        // PATCH 118: Undo delete command
        [RelayCommand]
        private async Task UndoDeleteAsync()
        {
            if (_deletedItems.Count == 0)
            {
                StatusMessage = "Nothing to undo!";
                return;
            }

            var restoredWork = _deletedItems.Pop();

            // Re-insert into database
            restoredWork.Id = 0; // Reset ID for new insert
            await _repository.InsertAsync(DbDailyWork.FromUiModel(restoredWork));

            DailyWorks.Add(restoredWork);
            RefreshFilteredView();
            UpdateStatistics();

            StatusMessage = $"Restored {restoredWork.Company}!";
            LogActivity("UNDO DELETE", $"Restored {restoredWork.PiNumber}");
        }

        // PATCH 148: Undo command
        [RelayCommand]
        private void Undo()
        {
            if (_undoStack.Count == 0)
            {
                StatusMessage = "Nothing to undo!";
                return;
            }

            if (SelectedItem != null)
                _redoStack.Push(CloneWork(SelectedItem));

            var previous = _undoStack.Pop();
            SelectedItem = previous;

            StatusMessage = "Undone!";
            LogActivity("UNDO", "Undo last action");
        }

        // PATCH 148: Redo command
        [RelayCommand]
        private void Redo()
        {
            if (_redoStack.Count == 0)
            {
                StatusMessage = "Nothing to redo!";
                return;
            }

            if (SelectedItem != null)
                _undoStack.Push(CloneWork(SelectedItem));

            var next = _redoStack.Pop();
            SelectedItem = next;

            StatusMessage = "Redone!";
            LogActivity("REDO", "Redo last action");
        }

        private DailyWorkModel CloneWork(DailyWorkModel work)
        {
            return new DailyWorkModel
            {
                Id = work.Id,
                Date = work.Date,
                Company = work.Company,
                PiNumber = work.PiNumber,
                Color = work.Color,
                CustomerReference = work.CustomerReference,
                TypeOfWork = work.TypeOfWork,
                ProductionStatus = work.ProductionStatus,
                Qty = work.Qty,
                Sqm = work.Sqm,
                Status = work.Status,
                Salesman = work.Salesman,
                Notes = work.Notes,
                CreatedDate = work.CreatedDate,
                UpdateDate = work.UpdateDate
            };
        }

        private void PushToUndo(DailyWorkModel work)
        {
            if (_undoStack.Count > 0 && _undoStack.Peek().Id == work.Id)
                return; // Don't push same item twice

            _undoStack.Push(CloneWork(work));
            _redoStack.Clear();

            // Keep stack limited
            while (_undoStack.Count > 50)
            {
                var temp = new Stack<DailyWorkModel>();
                while (_undoStack.Count > 0) temp.Push(_undoStack.Pop());
                temp.Pop(); // Remove oldest
                while (temp.Count > 0) _undoStack.Push(temp.Pop());
            }
        }

        // PATCH 124, 126: Toggle dark mode - FULL implementation
        [RelayCommand]
        private void ToggleDarkMode()
        {
            IsDarkMode = !IsDarkMode;

            // PATCH 126: Apply dark mode to app resources
            if (Application.Current?.MainWindow != null)
            {
                var bg = IsDarkMode ? "#1F2937" : "#F1F5F9";
                var fg = IsDarkMode ? "#F9FAFB" : "#1F293B";
                var card = IsDarkMode ? "#374151" : "#FFFFFF";
                var headerBg = IsDarkMode ? "#111827" : "#1E40AF";
                var headerFg = IsDarkMode ? "#F9FAFB" : "#FFFFFF";

                // Update window background
                Application.Current.MainWindow.Background = new System.Windows.Media.BrushConverter().ConvertFromString(bg) as System.Windows.Media.Brush;
            }

            // Notify all bindings to refresh
            OnPropertyChanged(nameof(IsDarkMode));

            StatusMessage = IsDarkMode ? "🌙 Dark mode ON" : "☀️ Light mode ON";
            LogActivity("THEME", IsDarkMode ? "Dark mode ON" : "Light mode ON");
        }

        [RelayCommand]
        private async Task RefreshAsync() => await LoadDataAsync();

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            FilterStatus = string.Empty;
            FilterProductionStatus = string.Empty;
            FilterTypeOfWork = string.Empty;
            FilterSalesman = string.Empty;
            FilterCompany = string.Empty;
            FilterColor = string.Empty;
            FilterPINumber = string.Empty;
            FilterCustomerReference = string.Empty;
            FilterStartDate = null;
            FilterEndDate = null;
            RefreshFilteredView();
            LogActivity("FILTER", "Cleared all filters");
        }

        // PATCH 41-44: Quick filter presets
        [RelayCommand]
        private void QuickFilterThisWeek()
        {
            FilterStartDate = DateTime.Today.AddDays(-7);
            FilterEndDate = DateTime.Today;
            RefreshFilteredView();
            LogActivity("FILTER", "This week");
        }

        [RelayCommand]
        private void QuickFilterThisMonth()
        {
            FilterStartDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            FilterEndDate = DateTime.Today;
            RefreshFilteredView();
            LogActivity("FILTER", "This month");
        }

        [RelayCommand]
        private void QuickFilterAllTime()
        {
            FilterStartDate = null;
            FilterEndDate = null;
            RefreshFilteredView();
            LogActivity("FILTER", "All time");
        }

        // PATCH 134: Export presets
        public string LastExportPreset { get; set; } = "Default";

        [RelayCommand]
        private async Task ExportPresetAsync(string preset)
        {
            LastExportPreset = preset;
            await ExportToCsvAsync();
        }

        // PATCH 91: Export to CSV - Export FILTERED records
        [RelayCommand]
        private async Task ExportToCsvAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = ".csv",
                    FileName = $"DailyWorks_Export_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    IsBusy = true;
                    StatusMessage = "Exporting...";

                    var lines = new List<string>
                    {
                        "Date,Company,PI Number,Color,Cust Ref,Type of Work,Production,QTY,SQM,Status,Salesman,Notes"
                    };

                    // PATCH 91 FIX: Export FILTERED records, not all records
                    var filteredList = FilteredDataView?.Cast<DailyWorkModel>().ToList() ?? DailyWorks.ToList();

                    foreach (var work in filteredList)
                    {
                        var line = $"\"{work.Date:dd-MM-yyyy}\",\"{work.Company}\",\"{work.PiNumber}\",\"{work.Color}\",\"{work.CustomerReference}\",\"{work.TypeOfWork}\",\"{work.ProductionStatus}\",{work.Qty},{work.Sqm:N2},\"{work.Status}\",\"{work.Salesman}\",\"{work.Notes}\"";
                        lines.Add(line);
                    }

                    await System.IO.File.WriteAllLinesAsync(dialog.FileName, lines);
                    // PATCH 111: Add export timestamp
                    StatusMessage = $"Exported {filteredList.Count} records at {DateTime.Now:HH:mm:ss}!";
                    LogActivity("EXPORT CSV", $"Exported {filteredList.Count} records");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Export failed: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // PATCH 45: Export SELECTED records only (command created in constructor)
        private async Task ExportSelectedToCsvAsync()
        {
            var selectedIds = _selectedIds?.ToList() ?? new List<int>();
            if (selectedIds.Count == 0)
            {
                StatusMessage = "No items selected!";
                return;
            }

            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = ".csv",
                    FileName = $"DailyWorks_Selected_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    IsBusy = true;
                    StatusMessage = "Exporting selected...";

                    var lines = new List<string>
                    {
                        "Date,Company,PI Number,Color,Cust Ref,Type of Work,Production,QTY,SQM,Status,Salesman,Notes"
                    };

                    var selectedList = DailyWorks.Where(w => selectedIds.Contains(w.Id)).ToList();

                    foreach (var work in selectedList)
                    {
                        var line = $"\"{work.Date:dd-MM-yyyy}\",\"{work.Company}\",\"{work.PiNumber}\",\"{work.Color}\",\"{work.CustomerReference}\",\"{work.TypeOfWork}\",\"{work.ProductionStatus}\",{work.Qty},{work.Sqm:N2},\"{work.Status}\",\"{work.Salesman}\",\"{work.Notes}\"";
                        lines.Add(line);
                    }

                    await System.IO.File.WriteAllLinesAsync(dialog.FileName, lines);
                    StatusMessage = $"Exported {selectedList.Count} selected records!";
                    LogActivity("EXPORT SELECTED", $"Exported {selectedList.Count} records");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Export failed: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // PATCH 144: Export to Excel
        private async Task ExecuteExportToExcelAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    DefaultExt = ".xlsx",
                    FileName = $"DailyWorks_{DateTime.Now:yyyyMMdd_HHmmss}"
                };

                if (dialog.ShowDialog() == true)
                {
                    IsBusy = true;
                    StatusMessage = "Exporting to Excel...";

                    // Note: Requires EPPlus NuGet package
                    var file = new System.IO.FileInfo(dialog.FileName);
                    using var package = new OfficeOpenXml.ExcelPackage(file);
                    var sheet = package.Workbook.Worksheets.Add("Daily Works");

                    var data = FilteredDataView?.Cast<DailyWorkModel>().ToList() ?? DailyWorks.ToList();
                    var cols = typeof(DailyWorkModel).GetProperties();

                    // Headers
                    for (int i = 0; i < cols.Length; i++)
                        sheet.Cells[1, i + 1].Value = cols[i].Name;

                    // Data
                    for (int row = 0; row < data.Count; row++)
                    {
                        for (int col = 0; col < cols.Length; col++)
                        {
                            var value = cols[col].GetValue(data[row]);
                            sheet.Cells[row + 2, col + 1].Value = value?.ToString();
                        }
                    }

                    package.Save();
                    StatusMessage = "Excel exported successfully!";
                    LogActivity("EXPORT EXCEL", $"Exported {data.Count} records");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }

        // PATCH 46: Import from CSV
        private async Task ImportFromCsvAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                    DefaultExt = ".csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var result = MessageBox.Show(
                        "This will add new records. Continue?",
                        "Confirm Import", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes) return;

                    IsBusy = true;
                    StatusMessage = "Importing...";

                    var lines = await System.IO.File.ReadAllLinesAsync(dialog.FileName);
                    var imported = 0;
                    var skipped = 0;

                    foreach (var line in lines.Skip(1))
                    {
                        try
                        {
                            var parts = ParseCsvLine(line);
                            if (parts.Length < 10) { skipped++; continue; }

                            var work = new DailyWorkModel
                            {
                                Company = parts[1].Trim('"'),
                                PiNumber = parts[2].Trim('"'),
                                Color = parts[3].Trim('"'),
                                CustomerReference = parts[4].Trim('"'),
                                TypeOfWork = parts[5].Trim('"'),
                                ProductionStatus = parts[6].Trim('"'),
                                Qty = int.TryParse(parts[7], out var qty) ? qty : 0,
                                Sqm = double.TryParse(parts[8], out var sqm) ? sqm : 0,
                                Status = parts[9].Trim('"'),
                                Salesman = parts.Length > 10 ? parts[10].Trim('"') : "",
                                Notes = parts.Length > 11 ? parts[11].Trim('"') : "",
                                Date = DateTime.Today,
                                CreatedDate = DateTime.Now
                            };

                            if (!string.IsNullOrWhiteSpace(work.Company))
                            {
                                await _repository.InsertAsync(DbDailyWork.FromUiModel(work));
                                imported++;
                            }
                            else
                            {
                                skipped++;
                            }
                        }
                        catch { skipped++; }
                    }

                    await LoadDataAsync();
                    StatusMessage = $"Imported {imported} records, skipped {skipped}!";
                    LogActivity("IMPORT", $"Imported {imported} records");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Import failed: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = "";
            var inQuotes = false;

            foreach (var c in line)
            {
                if (c == '"') inQuotes = !inQuotes;
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else current += c;
            }
            result.Add(current);
            return result.ToArray();
        }

        // PATCH 92: Print
        [RelayCommand]
        private void Print()
        {
            try
            {
                var printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    StatusMessage = "Printing...";
                    LogActivity("PRINT", "Printing report");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Print failed: " + ex.Message;
            }
        }

        // PATCH 95: Auto-refresh timer
        private System.Timers.Timer? _autoRefreshTimer;

        private bool _autoRefreshEnabled;
        public bool AutoRefreshEnabled
        {
            get => _autoRefreshEnabled;
            set
            {
                if (SetProperty(ref _autoRefreshEnabled, value))
                {
                    UpdateAutoRefresh();
                }
            }
        }

        private int _autoRefreshSeconds = 60;
        public int AutoRefreshSeconds
        {
            get => _autoRefreshSeconds;
            set
            {
                if (SetProperty(ref _autoRefreshSeconds, value))
                {
                    UpdateAutoRefresh();
                }
            }
        }

        // PATCH 161: Filter favorites only
        private bool _filterFavoritesOnly;
        public bool FilterFavoritesOnly
        {
            get => _filterFavoritesOnly;
            set
            {
                if (SetProperty(ref _filterFavoritesOnly, value))
                    RefreshFilteredView();
            }
        }

        // PATCH 161: Toggle favorite command
        [RelayCommand]
        private async Task ToggleFavoriteAsync()
        {
            if (SelectedItem == null) return;

            SelectedItem.IsFavorite = !SelectedItem.IsFavorite;

            var dbEntity = DbDailyWork.FromUiModel(SelectedItem);
            await _repository.UpdateAsync(dbEntity);

            StatusMessage = SelectedItem.IsFavorite ? "⭐ Added to favorites" : "☆ Removed from favorites";
            LogActivity("FAVORITE", $"Toggled favorite for {SelectedItem.PiNumber}");
        }

        private void UpdateAutoRefresh()
        {
            _autoRefreshTimer?.Stop();
            _autoRefreshTimer?.Dispose();
            _autoRefreshTimer = null;

            if (AutoRefreshEnabled && AutoRefreshSeconds > 0)
            {
                _autoRefreshTimer = new System.Timers.Timer(AutoRefreshSeconds * 1000);
                _autoRefreshTimer.Elapsed += async (s, e) =>
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await LoadDataAsync();
                    }, System.Windows.Threading.DispatcherPriority.Background);
                };
                _autoRefreshTimer.Start();
            }
        }

        // Invoice handling
        public event Action? RequestNavigateToInvoice;

        public void OnProformaInvoiceSaved(Models.ProformaInvoiceModel invoice)
        {
            _ = LoadDataAsync();
        }

        // PATCH 16: Debounce filter refresh
        private async void DebounceFilterRefreshAsync()
        {
            _filterDebounceToken?.Cancel();
            _filterDebounceToken = new CancellationTokenSource();

            try
            {
                await Task.Delay(150, _filterDebounceToken.Token);
                RefreshFilteredView();
            }
            catch (TaskCanceledException) { /* debounced */ }
        }

        // PATCH 17: Cached statistics - stored values updated incrementally
        private int _totalRecords;
        public int TotalRecords
        {
            get => _totalRecords;
            private set => SetProperty(ref _totalRecords, value);
        }

        private double _totalSQM;
        public double TotalSQM
        {
            get => _totalSQM;
            private set => SetProperty(ref _totalSQM, value);
        }

        private int _totalQty;
        public int TotalQty
        {
            get => _totalQty;
            private set => SetProperty(ref _totalQty, value);
        }

        private int _filteredRecords;
        public int FilteredRecords
        {
            get => _filteredRecords;
            private set => SetProperty(ref _filteredRecords, value);
        }

        private double _filteredSQM;
        public double FilteredSQM
        {
            get => _filteredSQM;
            private set => SetProperty(ref _filteredSQM, value);
        }

        private int _filteredQty;
        public int FilteredQty
        {
            get => _filteredQty;
            private set => SetProperty(ref _filteredQty, value);
        }

        public bool HasRecords => FilteredRecords > 0;

        // PATCH 17: Production status cached counts - updated incrementally
        private int _completedCount;
        public int CompletedCount
        {
            get => _completedCount;
            private set => SetProperty(ref _completedCount, value);
        }

        private int _pendingCount;
        public int PendingCount
        {
            get => _pendingCount;
            private set => SetProperty(ref _pendingCount, value);
        }

        private int _inProgressCount;
        public int InProgressCount
        {
            get => _inProgressCount;
            private set => SetProperty(ref _inProgressCount, value);
        }

        private void RefreshFilteredView()
        {
            _filteredView?.Refresh();
            UpdateFilteredStatisticsOnly();  // Only update filtered counts on filter change
        }

        // PATCH 17: Full statistics update - use on initial load
        private void UpdateStatistics()
        {
            // Update totals from entire collection
            TotalRecords = DailyWorks.Count;
            TotalSQM = DailyWorks.Sum(w => w.Sqm);
            TotalQty = DailyWorks.Sum(w => w.Qty);

            // Unique counts
            TotalCompanies = DailyWorks.Select(w => w.Company).Distinct().Count();
            TotalPINumbers = DailyWorks.Select(w => w.PiNumber).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().Count();

            // Subscribe to property changes for incremental updates
            foreach (var work in DailyWorks)
            {
                work.PropertyChanged += OnDailyWorkPropertyChanged;
            }

            // Update filtered counts
            UpdateFilteredStatisticsOnly();
        }

        // PATCH 17: Filtered statistics only - lightweight, no LINQ enumeration
        private void UpdateFilteredStatisticsOnly()
        {
            // Only count what's visible in filter
            if (_filteredView == null)
            {
                FilteredRecords = 0;
                FilteredSQM = 0;
                FilteredQty = 0;
                CompletedCount = 0;
                PendingCount = 0;
                InProgressCount = 0;
            }
            else
            {
                int filteredRecs = 0;
                double filteredSqm = 0;
                int filteredQty = 0;
                int completed = 0;
                int pending = 0;
                int inProgress = 0;

                foreach (DailyWorkModel? work in _filteredView)
                {
                    if (work == null) continue;
                    filteredRecs++;
                    filteredSqm += work.Sqm;
                    filteredQty += work.Qty;

                    // Status counts
                    switch (work.ProductionStatus)
                    {
                        case "COMPLETED":
                            completed++;
                            break;
                        case "PENDING":
                            pending++;
                            break;
                        case "IN PROGRESS":
                            inProgress++;
                            break;
                    }
                }

                FilteredRecords = filteredRecs;
                FilteredSQM = filteredSqm;
                FilteredQty = filteredQty;
                CompletedCount = completed;
                PendingCount = pending;
                InProgressCount = inProgress;
            }

            OnPropertyChanged(nameof(SelectedCount));
            OnPropertyChanged(nameof(HasRecords));
        }

        // PATCH 17: Incremental statistics update on property change
        private void OnDailyWorkPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not DailyWorkModel work) return;

            // Update total stats when an item's Qty/Sqm changes
            if (e.PropertyName is nameof(DailyWorkModel.Qty) or nameof(DailyWorkModel.Sqm))
            {
                // Recalculate totals from all items
                TotalSQM = DailyWorks.Sum(w => w.Sqm);
                TotalQty = DailyWorks.Sum(w => w.Qty);
            }

            // Update filtered stats (because item might have entered/left filter)
            UpdateFilteredStatisticsOnly();
        }

        private int _totalCompanies;
        public int TotalCompanies
        {
            get => _totalCompanies;
            private set => SetProperty(ref _totalCompanies, value);
        }

        private int _totalPINumbers;
        public int TotalPINumbers
        {
            get => _totalPINumbers;
            private set => SetProperty(ref _totalPINumbers, value);
        }

        // PATCH 107: Bulk production status
        private string _bulkProductionStatus = string.Empty;
        public string BulkProductionStatus
        {
            get => _bulkProductionStatus;
            set => SetProperty(ref _bulkProductionStatus, value);
        }

        // PATCH 119: Batch clone template
        private DailyWorkModel? _batchCloneTemplate;
        public DailyWorkModel? BatchCloneTemplate
        {
            get => _batchCloneTemplate;
            set => SetProperty(ref _batchCloneTemplate, value);
        }

        // PATCH 120: Clone selected records as new
        [RelayCommand]
        private async Task BatchCloneAsync()
        {
            var selectedIds = _selectedIds?.ToList() ?? new List<int>();
            if (selectedIds.Count == 0)
            {
                var grid = Application.Current.MainWindow?.FindName("MainDataGrid") as DataGrid;
                if (grid != null)
                {
                    foreach (var item in grid.SelectedItems)
                    {
                        if (item is DailyWorkModel work)
                            selectedIds.Add(work.Id);
                    }
                }
            }

            if (selectedIds.Count == 0)
            {
                StatusMessage = "No items selected!";
                return;
            }

            var result = MessageBox.Show(
                $"Clone {selectedIds.Count} selected records as new?",
                "Confirm Batch Clone", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var cloned = 0;
            try
            {
                foreach (var id in selectedIds)
                {
                    var original = DailyWorks.FirstOrDefault(w => w.Id == id);
                    if (original != null)
                    {
                        var clone = original.Clone();
                        clone.Id = 0;
                        clone.PiNumber = clone.PiNumber + "-CLONE";
                        clone.CreatedDate = DateTime.Now;
                        clone.Date = DateTime.Today;
                        await _repository.InsertAsync(DbDailyWork.FromUiModel(clone));
                        cloned++;
                    }
                }

                await LoadDataAsync();
                StatusMessage = $"Cloned {cloned} records!";
                LogActivity("BATCH CLONE", $"Cloned {cloned} records");
            }
            catch (Exception ex)
            {
                StatusMessage = "Clone failed: " + ex.Message;
            }
        }

        [RelayCommand]
        private async Task BulkUpdateProductionStatusAsync()
        {
            if (string.IsNullOrWhiteSpace(BulkProductionStatus)) return;

            var selectedIds = _selectedIds?.ToList() ?? new List<int>();
            if (selectedIds.Count == 0)
            {
                var grid = Application.Current.MainWindow?.FindName("MainDataGrid") as DataGrid;
                if (grid != null)
                {
                    foreach (var item in grid.SelectedItems)
                    {
                        if (item is DailyWorkModel work)
                            selectedIds.Add(work.Id);
                    }
                }
            }

            if (selectedIds.Count == 0)
            {
                StatusMessage = "No items selected!";
                return;
            }

            var result = MessageBox.Show(
                $"Update Production Status to '{BulkProductionStatus}' for {selectedIds.Count} records?",
                "Confirm Bulk Update", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                foreach (var id in selectedIds)
                {
                    var work = DailyWorks.FirstOrDefault(w => w.Id == id);
                    if (work != null)
                    {
                        work.ProductionStatus = BulkProductionStatus;
                        work.UpdateDate = DateTime.Today;
                        await _repository.UpdateAsync(DbDailyWork.FromUiModel(work));
                    }
                }

                RefreshFilteredView();
                UpdateStatistics();
                _selectedIds.Clear();
                OnPropertyChanged(nameof(SelectedCount));
                StatusMessage = $"Updated {selectedIds.Count} records to '{BulkProductionStatus}'!";
                LogActivity("BULK UPDATE", $"Updated {selectedIds.Count} records to {BulkProductionStatus}");
            }
            catch (Exception ex)
            {
                StatusMessage = "Update failed: " + ex.Message;
            }
        }

        // PATCH 121: Save column widths
        public void SaveColumnWidths(string widths)
        {
            var settings = AppSettings.Load();
            settings.ColumnWidths = widths;
            settings.Save();
        }

        public string GetColumnWidths()
        {
            var settings = AppSettings.Load();
            return settings.ColumnWidths ?? "";
        }

        // PATCH 12: Load data async
        public async Task LoadDataAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                var dbItems = await _repository.GetAllAsync();

                // Unsubscribe from old items before clearing
                foreach (var work in DailyWorks)
                {
                    work.PropertyChanged -= OnDailyWorkPropertyChanged;
                }

                DailyWorks.Clear();
                foreach (var item in dbItems)
                {
                    if (item != null)
                    {
                        DailyWorks.Add(item);
                    }
                }
                _filteredView = null;
                OnPropertyChanged(nameof(FilteredDataView));
                LoadFilterOptions();
                UpdateStatistics();

                StatusMessage = "Ready";
            }
            catch (Exception ex)
            {
                StatusMessage = "Load failed: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void LoadFilterOptions()
        {
            CompanyOptions.Clear();
            PINumberOptions.Clear();
            CustomerReferenceOptions.Clear();
            TypeOfWorkOptions.Clear();
            StatusOptions.Clear();
            ProductionStatusOptions.Clear();
            SalesmanOptions.Clear();
            ColorOptions.Clear();

            foreach (var company in DailyWorks.Select(w => w.Company).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c))
                CompanyOptions.Add(company);
            foreach (var pi in DailyWorks.Select(w => w.PiNumber).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().OrderBy(p => p))
                PINumberOptions.Add(pi);
            foreach (var custRef in DailyWorks.Select(w => w.CustomerReference).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c))
                CustomerReferenceOptions.Add(custRef);
            foreach (var type in DailyWorks.Select(w => w.TypeOfWork).Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().OrderBy(t => t))
                TypeOfWorkOptions.Add(type);
            foreach (var status in DailyWorks.Select(w => w.Status).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s))
                StatusOptions.Add(status);
            foreach (var prodStatus in DailyWorks.Select(w => w.ProductionStatus).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().OrderBy(p => p))
                ProductionStatusOptions.Add(prodStatus);
            foreach (var salesman in DailyWorks.Select(w => w.Salesman).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().OrderBy(s => s))
                SalesmanOptions.Add(salesman);
            foreach (var color in DailyWorks.Select(w => w.Color).Where(c => !string.IsNullOrWhiteSpace(c)).Distinct().OrderBy(c => c))
                ColorOptions.Add(color);
        }

        // Constructor
        public DailyWorksViewModel()
        {
            _isBusy = false;
            _repository = new ProGlassAutomation.ViewModels.DailyWorks.Repositories.DailyWorkRepository();

            // PATCH 45-46: Initialize commands
            ExportSelectedToCsvCommand = new RelayCommand(async () => await ExportSelectedToCsvAsync());
            ImportFromCsvCommand = new RelayCommand(async () => await ImportFromCsvAsync());
            ExportToExcelCommand = new RelayCommand(async () => await ExecuteExportToExcelAsync());
            DeleteCommand = new RelayCommand(async () => await DeleteSelectedAsync(null));

            // PATCH 167: Export with presets
            ExportWithPresetCommand = new RelayCommand((object? preset) => ExportPresetAsync(preset?.ToString() ?? "Default"));

            // PATCH 168: Backup and restore
            BackupDatabaseCommand = new RelayCommand(async () => await BackupDatabaseAsync());
            RestoreDatabaseCommand = new RelayCommand(async () => await RestoreDatabaseAsync());

            // PATCH 153: Start auto-save timer
            StartAutoSave();

            // PATCH 160: Load search history
            LoadSearchHistory();

            _ = LoadDataAsync();
        }

        private async Task BackupDatabaseAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "SQLite Database|*.db",
                    FileName = $"DailyWorks_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
                };

                if (dialog.ShowDialog() == true)
                {
                    System.IO.File.Copy("proglass.db", dialog.FileName, true);
                    StatusMessage = "Database backed up!";
                    LogActivity("BACKUP", "Database backed up");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Backup failed: " + ex.Message;
            }
        }

        // PATCH 168: Restore database
        private async Task RestoreDatabaseAsync()
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "SQLite Database|*.db"
                };

                if (dialog.ShowDialog() == true)
                {
                    var result = MessageBox.Show(
                        "This will replace current database. Continue?",
                        "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.IO.File.Copy(dialog.FileName, "proglass.db", true);
                        await LoadDataAsync();
                        StatusMessage = "Database restored!";
                        LogActivity("RESTORE", "Database restored");
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = "Restore failed: " + ex.Message;
            }
        }

        // PATCH 54: IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Timer cleanup
                _autoRefreshTimer?.Stop();
                _autoRefreshTimer?.Dispose();
                _autoRefreshTimer = null;

                // PATCH 153: Cleanup auto-save timer
                _autoSaveTimer?.Dispose();
                _autoSaveTimer = null;

                // PATCH 112: Cleanup debounce tokens
                _searchDebounceToken?.Cancel();
                _searchDebounceToken?.Dispose();
                _searchDebounceToken = null;

                _filterDebounceToken?.Cancel();
                _filterDebounceToken?.Dispose();
                _filterDebounceToken = null;

                // PATCH 56: Unsubscribe all PropertyChanged events to prevent memory leaks
                foreach (var work in DailyWorks)
                {
                    work.PropertyChanged -= OnDailyWorkPropertyChanged;
                }
                DailyWorks.Clear();

                // PATCH 56: Cancel any pending debounce tokens
                _filterDebounceToken?.Cancel();
                _filterDebounceToken?.Dispose();
                _filterDebounceToken = null;
            }
        }
    }
}