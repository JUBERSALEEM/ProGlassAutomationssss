using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using ProGlassAutomation.Models;
using ProGlassAutomation.Services;

namespace ProGlassAutomation.ViewModels
{
    public class SheetStoreViewModel : INotifyPropertyChanged
    {
        private string _searchTerm = "";
        private string _selectedCategory = "";
        private string _selectedThickness = "";
        private string _selectedColor = "";
        private bool _isLoading;
        private int _totalSheets;
        private int _filteredSheets;

        public ObservableCollection<Sheet> Sheets { get; } = new ObservableCollection<Sheet>();
        public ObservableCollection<string> Categories { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Thicknesses { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> Colors { get; } = new ObservableCollection<string>();

        public string SearchTerm
        {
            get => _searchTerm;
            set { _searchTerm = value; OnPropertyChanged(); SearchAsync(); }
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set { _selectedCategory = value; OnPropertyChanged(); SearchAsync(); }
        }

        public string SelectedThickness
        {
            get => _selectedThickness;
            set { _selectedThickness = value; OnPropertyChanged(); SearchAsync(); }
        }

        public string SelectedColor
        {
            get => _selectedColor;
            set { _selectedColor = value; OnPropertyChanged(); SearchAsync(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public int TotalSheets
        {
            get => _totalSheets;
            set { _totalSheets = value; OnPropertyChanged(); }
        }

        public int FilteredSheets
        {
            get => _filteredSheets;
            set { _filteredSheets = value; OnPropertyChanged(); }
        }

        public ICommand AddSheetCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ImportExcelCommand { get; }

        public SheetStoreViewModel()
        {
            AddSheetCommand = new RelayCommand(AddSheet);
            ExportExcelCommand = new AsyncRelayCommand(ExportExcel);
            ImportExcelCommand = new AsyncRelayCommand(ImportExcel);

            LoadFilters();
            SearchAsync();
        }

        private void LoadFilters()
        {
            Categories.Clear();
            Categories.Add("All");
            foreach (var cat in SheetStoreService.Instance.GetAllCategories())
                Categories.Add(cat);

            Thicknesses.Clear();
            Thicknesses.Add("All");
            foreach (var th in SheetStoreService.Instance.GetAllThicknesses())
                Thicknesses.Add(th);

            Colors.Clear();
            Colors.Add("All");
            foreach (var col in SheetStoreService.Instance.GetAllColors())
                Colors.Add(col);

            TotalSheets = SheetStoreService.Instance.GetAllSheets().Count;
        }

        private async void SearchAsync()
        {
            IsLoading = true;

            await Task.Run(() =>
            {
                var results = SheetStoreService.Instance.SearchSheets(
                    _searchTerm,
                    _selectedCategory == "All" ? "" : _selectedCategory,
                    _selectedThickness == "All" ? "" : _selectedThickness,
                    _selectedColor == "All" ? "" : _selectedColor);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Sheets.Clear();
                    foreach (var sheet in results)
                        Sheets.Add(sheet);

                    FilteredSheets = results.Count;
                });
            });

            IsLoading = false;
        }

        private void AddSheet(object? parameter)
        {
            // This would typically open a dialog and handle the result
        }

        private async Task ExportExcel()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"SheetInventory_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                var (success, message) = await SheetStoreService.Instance.ExportToExcelAsync(dialog.FileName);
                IsLoading = false;

                System.Windows.MessageBox.Show(message,
                    success ? "Success" : "Error",
                    System.Windows.MessageBoxButton.OK,
                    success ? System.Windows.MessageBoxImage.Information : System.Windows.MessageBoxImage.Error);
            }
        }

        private async Task ImportExcel()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                var (success, message) = await SheetStoreService.Instance.ImportFromExcelAsync(dialog.FileName);
                IsLoading = false;

                if (success)
                {
                    SearchAsync();
                    LoadFilters();
                }

                System.Windows.MessageBox.Show(message,
                    success ? "Success" : "Error",
                    System.Windows.MessageBoxButton.OK,
                    success ? System.Windows.MessageBoxImage.Information : System.Windows.MessageBoxImage.Error);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (_isExecuting) return;

            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}