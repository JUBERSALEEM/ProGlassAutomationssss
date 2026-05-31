using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ProGlassAutomation.Models;
using Newtonsoft.Json;

namespace ProGlassAutomation.ViewModels
{
	public class ProformaInvoiceListViewModel : INotifyPropertyChanged
	{
		private readonly string _defaultFolderPath;

		public ProformaInvoiceListViewModel()
		{
			// Use same Data folder as ProformaInvoiceViewModel
			_defaultFolderPath = Path.Combine(
				AppDomain.CurrentDomain.BaseDirectory, "Data");

			AllInvoices = new ObservableCollection<ProformaInvoiceModel>();
			FilteredInvoices = new ObservableCollection<ProformaInvoiceModel>();
			StatusOptions = new ObservableCollection<StatusOption>
	{
		new StatusOption { Label = "All", Value = "" },
		new StatusOption { Label = "Draft", Value = "Draft" },
		new StatusOption { Label = "Sent", Value = "Sent" },
		new StatusOption { Label = "Confirmed", Value = "Confirmed" },
		new StatusOption { Label = "Hold", Value = "Hold" },
		new StatusOption { Label = "Revised", Value = "Revised" },
		new StatusOption { Label = "Completed", Value = "Completed" },
		new StatusOption { Label = "Cancelled", Value = "Cancelled" }
	};
			SalesmanOptions = new ObservableCollection<SalesmanOption>();

			// Subscribe to event when new invoice is saved from editor
			SharedViewModels.ProformaInvoiceVM.InvoiceToBeAdded += OnInvoiceToBeAdded;

			InitializeCommands();
			LoadInvoicesFromFolder(_defaultFolderPath);
			ApplyFilters();  // FIX: Apply filters after loading
			CalculateStatistics();
		}

		// Event handler for new invoices from editor
		private void OnInvoiceToBeAdded(ProformaInvoiceModel invoice)
		{
			if (invoice == null) return;

			// Check if already exists
			var existing = AllInvoices.FirstOrDefault(i => i.InvoiceNo == invoice.InvoiceNo);
			if (existing == null)
			{
				AllInvoices.Add(invoice);
				ApplyFilters();
				System.Diagnostics.Debug.WriteLine($"[ListVM] Added invoice: {invoice.InvoiceNo}");
			}
		}

		public ObservableCollection<ProformaInvoiceModel> AllInvoices { get; set; }
		public ObservableCollection<ProformaInvoiceModel> FilteredInvoices { get; set; }
		public ObservableCollection<StatusOption> StatusOptions { get; set; }
		public ObservableCollection<SalesmanOption> SalesmanOptions { get; set; }

		private ProformaInvoiceModel? _selectedInvoice;
		public ProformaInvoiceModel? SelectedInvoice
		{
			get => _selectedInvoice;
			set { _selectedInvoice = value; OnPropertyChanged(); }
		}

		private string _searchText = "";
		public string SearchText
		{
			get => _searchText;
			set { _searchText = value; OnPropertyChanged(); ApplyFilters(); }
		}

		private string? _selectedStatus;
		public string? SelectedStatus
		{
			get => _selectedStatus;
			set { _selectedStatus = value; OnPropertyChanged(); ApplyFilters(); }
		}

		private string? _selectedSalesman;
		public string? SelectedSalesman
		{
			get => _selectedSalesman;
			set { _selectedSalesman = value; OnPropertyChanged(); ApplyFilters(); }
		}

		private DateTime? _dateFrom;
		public DateTime? DateFrom
		{
			get => _dateFrom;
			set { _dateFrom = value; OnPropertyChanged(); ApplyFilters(); }
		}

		private DateTime? _dateTo;
		public DateTime? DateTo
		{
			get => _dateTo;
			set { _dateTo = value; OnPropertyChanged(); ApplyFilters(); }
		}

		private bool _filterDraft = true;
		public bool FilterDraft
		{
			get => _filterDraft;
			set { _filterDraft = value; OnPropertyChanged(); ApplyFilters(); }
		}

		private bool _filterSent = true;
		public bool FilterSent
		{
			get => _filterSent;
			set { _filterSent = value; OnPropertyChanged(); ApplyFilters(); }
		}

		private bool _filterConfirmed = true;
		public bool FilterConfirmed
		{
			get => _filterConfirmed;
			set { _filterConfirmed = value; OnPropertyChanged(); ApplyFilters(); }
		}

		private bool _filterHold = true;
		public bool FilterHold
		{
			get => _filterHold;
			set { _filterHold = value; OnPropertyChanged(); ApplyFilters(); }
		}

		private bool _filterRevised = true;
		public bool FilterRevised
		{
			get => _filterRevised;
			set { _filterRevised = value; OnPropertyChanged(); ApplyFilters(); }
		}

		private bool _filterCompleted = true;
		public bool FilterCompleted
		{
			get => _filterCompleted;
			set { _filterCompleted = value; OnPropertyChanged(); ApplyFilters(); }
		}

		private bool _filterCancelled = false;
		public bool FilterCancelled
		{
			get => _filterCancelled;
			set { _filterCancelled = value; OnPropertyChanged(); ApplyFilters(); }
		}

		private bool _filterConvertedToJO = false;
		public bool FilterConvertedToJO
		{
			get => _filterConvertedToJO;
			set { _filterConvertedToJO = value; OnPropertyChanged(); ApplyFilters(); }
		}

		private int _totalInvoiceCount;
		public int TotalInvoiceCount
		{
			get => _totalInvoiceCount;
			set { _totalInvoiceCount = value; OnPropertyChanged(); }
		}

		private double _totalAmountAll;
		public double TotalAmountAll
		{
			get => _totalAmountAll;
			set { _totalAmountAll = value; OnPropertyChanged(); }
		}

		private double _totalSQMAll;
		public double TotalSQMAll
		{
			get => _totalSQMAll;
			set { _totalSQMAll = value; OnPropertyChanged(); }
		}

		private int _convertedToJOCount;
		public int ConvertedToJOCount
		{
			get => _convertedToJOCount;
			set { _convertedToJOCount = value; OnPropertyChanged(); }
		}

		// ==================== Commands ====================

		public ICommand NewInvoiceCommand { get; private set; }
		public ICommand EditInvoiceCommand { get; private set; }
		public ICommand ViewInvoiceCommand { get; private set; }
		public ICommand DeleteInvoiceCommand { get; private set; }
		public ICommand ConvertToJobOrderCommand { get; private set; }
		public ICommand CreateJobOrderCommand { get; private set; }
		public ICommand OpenFolderCommand { get; private set; }
		public ICommand ExportAllCommand { get; private set; }
		public ICommand RefreshCommand { get; private set; }
		public ICommand ClearFiltersCommand { get; private set; }
		public ICommand FilterTodayCommand { get; private set; }
		public ICommand FilterThisWeekCommand { get; private set; }
		public ICommand FilterThisMonthCommand { get; private set; }
		public ICommand FilterThisYearCommand { get; private set; }

		private void InitializeCommands()
		{
			NewInvoiceCommand = new RelayCommand(ExecuteNewInvoice);
			EditInvoiceCommand = new RelayCommand<ProformaInvoiceModel>(ExecuteEditInvoice);
			ViewInvoiceCommand = new RelayCommand<ProformaInvoiceModel>(ExecuteViewInvoice);
			DeleteInvoiceCommand = new RelayCommand<ProformaInvoiceModel>(ExecuteDeleteInvoice);
			ConvertToJobOrderCommand = new RelayCommand<ProformaInvoiceModel>(ExecuteConvertToJobOrder);
			CreateJobOrderCommand = new RelayCommand<ProformaInvoiceModel>(ExecuteCreateJobOrder);
			OpenFolderCommand = new RelayCommand(ExecuteOpenFolder);
			ExportAllCommand = new RelayCommand(ExecuteExportAll);
			RefreshCommand = new RelayCommand(ExecuteRefresh);
			ClearFiltersCommand = new RelayCommand(ExecuteClearFilters);
			FilterTodayCommand = new RelayCommand(() => { DateFrom = DateTime.Today; DateTo = DateTime.Today; });
			FilterThisWeekCommand = new RelayCommand(() => { DateFrom = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek); DateTo = DateTime.Today; });
			FilterThisMonthCommand = new RelayCommand(() => { DateFrom = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); DateTo = DateTime.Today; });
			FilterThisYearCommand = new RelayCommand(() => { DateFrom = new DateTime(DateTime.Today.Year, 1, 1); DateTo = DateTime.Today; });
		}

		private void ExecuteNewInvoice(object? parameter)
		{
			try
			{
				SharedViewModels.ProformaInvoiceVM.CreateNewInvoice();

				var editorVM = SharedViewModels.ProformaInvoiceVM;
				var editorView = new ProGlassAutomation.Views.ProformaInvoice.ProformaInvoiceView
				{
					DataContext = editorVM
				};

				var mainWindow = Application.Current.MainWindow as MainWindow;
				mainWindow?.SetContent(editorView);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ExecuteEditInvoice(ProformaInvoiceModel? invoice)
		{
			if (invoice == null) return;
			try
			{
				SharedViewModels.ProformaInvoiceVM.LoadFromExistingInvoice(invoice);

				var editorVM = SharedViewModels.ProformaInvoiceVM;
				var editorView = new ProGlassAutomation.Views.ProformaInvoice.ProformaInvoiceView
				{
					DataContext = editorVM
				};

				var mainWindow = Application.Current.MainWindow as MainWindow;
				mainWindow?.SetContent(editorView);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ExecuteViewInvoice(ProformaInvoiceModel? invoice)
		{
			ExecuteEditInvoice(invoice);
		}

		private void ExecuteDeleteInvoice(ProformaInvoiceModel? invoice)
		{
			if (invoice == null) return;
			var result = MessageBox.Show($"Delete {invoice.InvoiceNo}?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if (result == MessageBoxResult.Yes)
			{
				try
				{
					var filePath = GetInvoiceFilePath(invoice.InvoiceNo);
					if (File.Exists(filePath))
					{
						var backupFolder = Path.Combine(_defaultFolderPath, "Deleted");
						if (!Directory.Exists(backupFolder)) Directory.CreateDirectory(backupFolder);
						File.Move(filePath, Path.Combine(backupFolder, Path.GetFileName(filePath)));
					}
					AllInvoices.Remove(invoice);
					FilteredInvoices.Remove(invoice);
					CalculateStatistics();
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}
		}

		private void ExecuteConvertToJobOrder(ProformaInvoiceModel? invoice)
		{
			if (invoice == null || invoice.IsConvertedToJobOrder) return;
			var result = MessageBox.Show($"Mark {invoice.InvoiceNo} as Confirmed?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
			if (result == MessageBoxResult.Yes)
			{
				invoice.IsConvertedToJobOrder = true;
				invoice.Status = "Confirmed";
				SaveInvoice(invoice);
				ApplyFilters();
				CalculateStatistics();
			}
		}

		private void ExecuteCreateJobOrder(ProformaInvoiceModel? invoice)
		{
			if (invoice == null) return;

			try
			{
				var jobOrderVM = SharedViewModels.JobOrderVM;

				jobOrderVM.ClearForNewJobOrder();
				jobOrderVM.LoadFromProformaInvoice(invoice);

				var jobOrderView = new ProGlassAutomation.Views.JobOrder.JobOrderView
				{
					DataContext = jobOrderVM
				};

				var mainWindow = Application.Current.MainWindow as MainWindow;
				if (mainWindow != null)
				{
					mainWindow.SetContent(jobOrderView);
				}
				else
				{
					var window = new Window
					{
						Title = $"Job Order - {invoice.InvoiceNo}",
						Content = jobOrderView,
						Width = 1200,
						Height = 800,
						WindowStartupLocation = WindowStartupLocation.CenterScreen
					};
					window.Show();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Error creating Job Order: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ExecuteOpenFolder(object? parameter)
		{
			var dialog = new Microsoft.Win32.OpenFolderDialog { InitialDirectory = _defaultFolderPath };
			if (dialog.ShowDialog() == true)
			{
				LoadInvoicesFromFolder(dialog.FolderName);
				ApplyFilters();
			}
		}

		private void ExecuteExportAll(object? parameter)
		{
			var dialog = new Microsoft.Win32.SaveFileDialog { Filter = "Excel|*.xlsx|CSV|*.csv", FileName = $"Invoices_{DateTime.Now:yyyyMMdd}" };
			if (dialog.ShowDialog() == true)
			{
				MessageBox.Show($"Exported {FilteredInvoices.Count} invoices");
			}
		}

		private void ExecuteRefresh(object? parameter)
		{
			LoadInvoicesFromFolder(_defaultFolderPath);
			ApplyFilters();
			System.Diagnostics.Debug.WriteLine("[ProformaInvoiceListVM] List refreshed");
		}

		private void ExecuteClearFilters(object? parameter)
		{
			SearchText = "";
			SelectedStatus = null;
			SelectedSalesman = null;
			DateFrom = null;
			DateTo = null;
			FilterDraft = true;
			FilterSent = true;
			FilterConfirmed = true;
			FilterHold = true;
			FilterRevised = true;
			FilterCompleted = true;
			FilterCancelled = false;
			FilterConvertedToJO = false;
		}

		private void ApplyFilters()
		{
			FilteredInvoices.Clear();
			var query = AllInvoices.AsEnumerable();

			if (!string.IsNullOrWhiteSpace(SearchText))
			{
				var s = SearchText.ToLower();
				query = query.Where(i => (i.InvoiceNo?.ToLower().Contains(s) ?? false)
					|| (i.CustomerName?.ToLower().Contains(s) ?? false)
					|| (i.ProjectName?.ToLower().Contains(s) ?? false)
					|| (i.CustomerReference?.ToLower().Contains(s) ?? false));
			}
			if (!string.IsNullOrWhiteSpace(SelectedStatus))
				query = query.Where(i => i.Status == SelectedStatus);
			if (!string.IsNullOrWhiteSpace(SelectedSalesman))
				query = query.Where(i => i.Salesman == SelectedSalesman);
			if (DateFrom.HasValue) query = query.Where(i => i.InvoiceDate >= DateFrom.Value);
			if (DateTo.HasValue) query = query.Where(i => i.InvoiceDate <= DateTo.Value.AddDays(1));
			query = query.Where(i =>
				(i.Status == "Draft" && FilterDraft)
				|| (i.Status == "Sent" && FilterSent)
				|| (i.Status == "Confirmed" && FilterConfirmed)
				|| (i.Status == "Hold" && FilterHold)
				|| (i.Status == "Revised" && FilterRevised)
				|| (i.Status == "Completed" && FilterCompleted)
				|| (i.Status == "Cancelled" && FilterCancelled));
			if (FilterConvertedToJO) query = query.Where(i => i.IsConvertedToJobOrder);

			foreach (var inv in query.OrderByDescending(i => i.InvoiceDate))
				FilteredInvoices.Add(inv);

			CalculateStatistics();
		}

		private void LoadInvoicesFromFolder(string folderPath)
		{
			AllInvoices.Clear();
			SalesmanOptions.Clear();
			if (!Directory.Exists(folderPath)) { Directory.CreateDirectory(folderPath); return; }

			var settings = new JsonSerializerSettings
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore
			};

			foreach (var file in Directory.GetFiles(folderPath, "*.json"))
			{
				try
				{
					var json = File.ReadAllText(file);
					var invoice = JsonConvert.DeserializeObject<ProformaInvoiceModel>(json, settings);
					if (invoice != null) AllInvoices.Add(invoice);
				}
				catch { }
			}

			foreach (var s in AllInvoices.Where(i => !string.IsNullOrWhiteSpace(i.Salesman)).Select(i => i.Salesman!).Distinct().OrderBy(x => x))
				SalesmanOptions.Add(new SalesmanOption { Name = s });
		}

		private void SaveInvoice(ProformaInvoiceModel invoice)
		{
			// Use same folder as ProformaInvoiceViewModel
			string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
			if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

			var settings = new JsonSerializerSettings
			{
				ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
				Formatting = Formatting.Indented
			};

			var json = JsonConvert.SerializeObject(invoice, settings);
			File.WriteAllText(GetInvoiceFilePath(invoice.InvoiceNo), json);
		}

		private string GetInvoiceFilePath(string invoiceNo)
		{
			// Use same folder as ProformaInvoiceViewModel
			string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
			return Path.Combine(folder, $"{invoiceNo}.json");
		}

		private void CalculateStatistics()
		{
			TotalInvoiceCount = FilteredInvoices.Count;
			TotalAmountAll = FilteredInvoices.Sum(i => i.GrandTotal);
			TotalSQMAll = FilteredInvoices.Sum(i => i.TotalSQM);
			ConvertedToJOCount = AllInvoices.Count(i => i.IsConvertedToJobOrder);
		}

		public void SaveOnExit()
		{
			try
			{
				foreach (var invoice in AllInvoices)
				{
					if (invoice.IsDirty)
					{
						SaveInvoice(invoice);
						invoice.IsDirty = false;
					}
				}
				System.Diagnostics.Debug.WriteLine("[ProformaInvoiceListVM] SaveOnExit complete");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[ProformaInvoiceListVM] SaveOnExit error: {ex.Message}");
			}
		}

		// ==================== INotifyPropertyChanged ====================

		public event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string? name = null)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	// ==================== Helper Classes ====================

	public class StatusOption
	{
		public string Label { get; set; } = "";
		public string Value { get; set; } = "";
	}

	public class SalesmanOption
	{
		public string Name { get; set; } = "";
	}
}