using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using ProGlassAutomation.Models;

namespace ProGlassAutomation.ViewModels
{
    public class ProformaInvoiceViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void Notify(params string[] props)
        {
            foreach (var p in props)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }

        protected bool Set<T>(ref T field, T value, params string[] props)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            Notify(props);
            return true;
        }

        // ==================== INVOICE DATA ====================
        private ProformaInvoiceModel _invoice;
        public ProformaInvoiceModel Invoice
        {
            get => _invoice;
            set => Set(ref _invoice, value, nameof(Invoice));
        }

        // ==================== FILE MANAGEMENT ====================
        private ObservableCollection<FileListItem> _savedFiles = new();
        public ObservableCollection<FileListItem> SavedFiles
        {
            get => _savedFiles;
            set => Set(ref _savedFiles, value, nameof(SavedFiles));
        }

        private FileListItem _selectedFile;
        public FileListItem SelectedFile
        {
            get => _selectedFile;
            set
            {
                Set(ref _selectedFile, value, nameof(SelectedFile));
                if (value != null)
                    LoadInvoice(value.FilePath);
            }
        }

        private string _currentFileName = "Untitled";
        public string CurrentFileName
        {
            get => _currentFileName;
            set => Set(ref _currentFileName, value, nameof(CurrentFileName));
        }

        private bool _isLMVisible = true;
        public bool IsLMVisible
        {
            get => _isLMVisible;
            set => Set(ref _isLMVisible, value, nameof(IsLMVisible));
        }

        // ==================== COMMANDS ====================
        public ICommand NewInvoiceCommand { get; }
        public ICommand SaveInvoiceCommand { get; }
        public ICommand SaveAsCommand { get; }
        public ICommand OpenInvoiceCommand { get; }
        public ICommand DeleteInvoiceCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand AddSpecificationCommand { get; }
        public ICommand RemoveSpecificationCommand { get; }
        public ICommand ToggleLMCommand { get; }
        public ICommand LoadFileCommand { get; }

        // ==================== CONSTRUCTOR ====================
        public ProformaInvoiceViewModel()
        {
            Invoice = new ProformaInvoiceModel();
            Invoice.InvoiceNo = Invoice.GenerateInvoiceNo();

            // Initialize commands
            NewInvoiceCommand = new PI_RelayCommand(_ => NewInvoice());
            SaveInvoiceCommand = new PI_RelayCommand(_ => SaveInvoice());
            SaveAsCommand = new PI_RelayCommand(_ => SaveAsInvoice());
            OpenInvoiceCommand = new PI_RelayCommand(_ => OpenInvoice());
            DeleteInvoiceCommand = new PI_RelayCommand(_ => DeleteInvoice(), _ => SelectedFile != null);
            PrintCommand = new PI_RelayCommand(_ => PrintInvoice());
            AddSpecificationCommand = new PI_RelayCommand(_ => AddSpecification());
            RemoveSpecificationCommand = new PI_RelayCommand(_ => RemoveSpecification(), _ => Invoice.Specifications.Count > 0);
            ToggleLMCommand = new PI_RelayCommand(_ => ToggleLM());
            LoadFileCommand = new PI_RelayCommand(path => { if (path is string s) LoadInvoice(s); });

            LoadSavedFiles();
        }

        // ==================== FILE MANAGEMENT ====================
        private string GetInvoicesFolder()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Invoices", "Proforma");
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }

        public void LoadSavedFiles()
        {
            SavedFiles.Clear();
            string folder = GetInvoicesFolder();

            if (Directory.Exists(folder))
            {
                var files = Directory.GetFiles(folder, "*.json")
                    .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                    .ToList();

                foreach (var file in files)
                {
                    try
                    {
                        var item = SimpleJsonDeserialize<ProformaInvoiceModel>(File.ReadAllText(file));
                        if (item != null)
                        {
                            SavedFiles.Add(new FileListItem
                            {
                                FileName = Path.GetFileNameWithoutExtension(file),
                                FilePath = file,
                                InvoiceNo = item.InvoiceNo,
                                CustomerName = item.CustomerName,
                                InvoiceDate = item.InvoiceDate,
                                LastModified = new FileInfo(file).LastWriteTime
                            });
                        }
                    }
                    catch { }
                }
            }
        }

        private void NewInvoice()
        {
            if (Invoice.IsDirty)
            {
                var result = MessageBox.Show("Do you want to save changes before creating a new invoice?",
                    "Save Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                    SaveInvoice();
                else if (result == MessageBoxResult.Cancel)
                    return;
            }

            Invoice = new ProformaInvoiceModel();
            Invoice.InvoiceNo = Invoice.GenerateInvoiceNo();
            CurrentFileName = "Untitled";
            Invoice.FilePath = "";
            LoadSavedFiles();
        }

        private void SaveInvoice()
        {
            if (string.IsNullOrEmpty(Invoice.FilePath))
            {
                SaveAsInvoice();
                return;
            }

            try
            {
                Invoice.CalculateTotals();
                string json = SimpleJsonSerialize(Invoice);
                File.WriteAllText(Invoice.FilePath, json);
                Invoice.IsDirty = false;
                CurrentFileName = Path.GetFileNameWithoutExtension(Invoice.FilePath);
                LoadSavedFiles();
                MessageBox.Show("Invoice saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving invoice: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveAsInvoice()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Proforma Invoice|*.json",
                    InitialDirectory = GetInvoicesFolder(),
                    FileName = $"{Invoice.InvoiceNo}.json",
                    Title = "Save Proforma Invoice"
                };

                if (dialog.ShowDialog() == true)
                {
                    Invoice.CalculateTotals();
                    Invoice.FilePath = dialog.FileName;
                    string json = SimpleJsonSerialize(Invoice);
                    File.WriteAllText(dialog.FileName, json);
                    Invoice.IsDirty = false;
                    CurrentFileName = Path.GetFileNameWithoutExtension(dialog.FileName);
                    LoadSavedFiles();
                    MessageBox.Show("Invoice saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving invoice: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenInvoice()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Proforma Invoice|*.json",
                    InitialDirectory = GetInvoicesFolder(),
                    Title = "Open Proforma Invoice"
                };

                if (dialog.ShowDialog() == true)
                {
                    LoadInvoice(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening invoice: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadInvoice(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("File not found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var invoice = SimpleJsonDeserialize<ProformaInvoiceModel>(File.ReadAllText(filePath));
                if (invoice != null)
                {
                    Invoice = invoice;
                    Invoice.FilePath = filePath;
                    Invoice.IsDirty = false;
                    CurrentFileName = Path.GetFileNameWithoutExtension(filePath);

                    // Re-attach calculation callbacks
                    foreach (var spec in Invoice.Specifications)
                    {
                        foreach (var item in spec.Items)
                        {
                            item.OnCalculationChanged = () =>
                            {
                                spec.CalculateSpecTotals();
                                Invoice.CalculateTotals();
                            };
                        }
                    }

                    Invoice.CalculateTotals();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading invoice: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteInvoice()
        {
            if (SelectedFile == null) return;

            var result = MessageBox.Show($"Are you sure you want to delete '{SelectedFile.InvoiceNo}'?",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (File.Exists(SelectedFile.FilePath))
                        File.Delete(SelectedFile.FilePath);

                    LoadSavedFiles();
                    MessageBox.Show("Invoice deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting invoice: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void PrintInvoice()
        {
            try
            {
                var printDialog = new System.Windows.Controls.PrintDialog();
                if (printDialog.ShowDialog() == true && InvoicePrintArea != null)
                {
                    printDialog.PrintVisual(InvoicePrintArea, $"Proforma Invoice - {Invoice.InvoiceNo}");
                    MessageBox.Show("Invoice sent to printer!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing invoice: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public System.Windows.Media.Visual InvoicePrintArea { get; set; }

        // ==================== SPECIFICATION METHODS ====================
        private void AddSpecification()
        {
            var spec = new SpecificationModel
            {
                SpecificationName = $"Specification {Invoice.Specifications.Count + 1}"
            };

            // Add first item by default
            var firstItem = new InvoiceItemModel { SrNo = 1 };
            firstItem.OnCalculationChanged = () =>
            {
                spec.CalculateSpecTotals();
                Invoice.CalculateTotals();
            };
            spec.Items.Add(firstItem);

            Invoice.Specifications.Add(spec);
            Invoice.IsDirty = true;
        }

        private void RemoveSpecification()
        {
            if (Invoice.Specifications.Count > 0)
            {
                Invoice.Specifications.RemoveAt(Invoice.Specifications.Count - 1);
                Invoice.CalculateTotals();
                Invoice.IsDirty = true;
            }
        }

        public void AddItem(SpecificationModel spec)
        {
            var item = new InvoiceItemModel
            {
                SrNo = spec.Items.Count + 1
            };
            item.OnCalculationChanged = () =>
            {
                spec.CalculateSpecTotals();
                Invoice.CalculateTotals();
            };
            spec.Items.Add(item);
            Invoice.IsDirty = true;
        }

        public void RemoveItem(InvoiceItemModel item)
        {
            var spec = Invoice.Specifications.FirstOrDefault(s => s.Items.Contains(item));
            if (spec != null && spec.Items.Count > 1)
            {
                spec.Items.Remove(item);

                // Renumber
                for (int i = 0; i < spec.Items.Count; i++)
                    spec.Items[i].SrNo = i + 1;

                spec.CalculateSpecTotals();
                Invoice.CalculateTotals();
                Invoice.IsDirty = true;
            }
        }

        private void ToggleLM()
        {
            IsLMVisible = !IsLMVisible;
        }

        // ==================== SIMPLE JSON SERIALIZATION ====================
        private string SimpleJsonSerialize(object obj)
        {
            var type = obj.GetType();
            var props = type.GetProperties();
            var sb = new System.Text.StringBuilder();
            sb.Append("{");
            bool first = true;

            foreach (var prop in props)
            {
                try
                {
                    var val = prop.GetValue(obj);
                    if (val == null) continue;

                    if (!first) sb.Append(",");
                    first = false;

                    if (prop.PropertyType == typeof(string))
                    {
                        sb.Append($"\"{prop.Name}\":\"{val}\"");
                    }
                    else if (prop.PropertyType == typeof(DateTime))
                    {
                        sb.Append($"\"{prop.Name}\":\"{((DateTime)val):yyyy-MM-ddTHH:mm:ss}\"");
                    }
                    else if (prop.PropertyType == typeof(bool))
                    {
                        sb.Append($"\"{prop.Name}\":{val.ToString().ToLower()}");
                    }
                    else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(double))
                    {
                        sb.Append($"\"{prop.Name}\":{val}");
                    }
                    else if (val is System.Collections.IEnumerable enumerable && !(val is string))
                    {
                        sb.Append($"\"{prop.Name}\":[");
                        bool firstItem = true;
                        foreach (var item in enumerable)
                        {
                            if (!firstItem) sb.Append(",");
                            firstItem = false;
                            sb.Append(SimpleJsonSerialize(item));
                        }
                        sb.Append("]");
                    }
                    else
                    {
                        sb.Append($"\"{prop.Name}\":\"{val}\"");
                    }
                }
                catch { }
            }

            sb.Append("}");
            return sb.ToString();
        }

        private T SimpleJsonDeserialize<T>(string json) where T : new()
        {
            var result = new T();
            var type = typeof(T);

            // Parse simple properties
            json = json.Trim();
            if (json.StartsWith("{")) json = json.Substring(1);
            if (json.EndsWith("}")) json = json.Substring(0, json.Length - 1);

            var segments = SplitJson(json);
            foreach (var segment in segments)
            {
                var kv = segment.Split(new[] { ':' }, 2);
                if (kv.Length != 2) continue;

                var key = kv[0].Trim().Trim('"');
                var val = kv[1].Trim().Trim('"');

                try
                {
                    var prop = type.GetProperty(key);
                    if (prop == null) continue;

                    if (prop.PropertyType == typeof(string))
                        prop.SetValue(result, val);
                    else if (prop.PropertyType == typeof(int))
                        prop.SetValue(result, int.Parse(val));
                    else if (prop.PropertyType == typeof(double))
                        prop.SetValue(result, double.Parse(val));
                    else if (prop.PropertyType == typeof(DateTime))
                        prop.SetValue(result, DateTime.Parse(val));
                    else if (prop.PropertyType == typeof(bool))
                        prop.SetValue(result, bool.Parse(val));
                }
                catch { }
            }

            return result;
        }

        private string[] SplitJson(string json)
        {
            var result = new System.Collections.Generic.List<string>();
            int depth = 0;
            int start = 0;
            bool inString = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];

                if (c == '"' && (i == 0 || json[i - 1] != '\\'))
                    inString = !inString;

                if (!inString)
                {
                    if (c == '[') depth++;
                    else if (c == ']') depth--;
                    else if (c == '{') depth++;
                    else if (c == '}') depth--;
                    else if (c == ',' && depth == 0)
                    {
                        result.Add(json.Substring(start, i - start));
                        start = i + 1;
                    }
                }
            }

            if (start < json.Length)
                result.Add(json.Substring(start));

            return result.ToArray();
        }
    }

    // ==================== FILE LIST ITEM ====================
    public class FileListItem
    {
        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string InvoiceNo { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public DateTime InvoiceDate { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    // ==================== RELAY COMMAND ====================
    public class PI_RelayCommand : ICommand
    {
        private readonly Action<object> _exec;
        private readonly Func<object, bool> _canExec;

        public PI_RelayCommand(Action<object> exec, Func<object, bool> canExec = null)
        {
            _exec = exec ?? throw new ArgumentNullException(nameof(exec));
            _canExec = canExec;
        }

        public PI_RelayCommand(Action exec, Func<bool> canExec = null)
            : this(_ => exec(), canExec != null ? _ => canExec() : null) { }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object p) => _canExec?.Invoke(p) ?? true;
        public void Execute(object p) => _exec(p);
    }
}