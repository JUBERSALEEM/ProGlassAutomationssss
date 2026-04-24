using Microsoft.Win32;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Helpers;
using ProGlassAutomation.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ProGlassAutomation.Views.SGU
{
    public class SguViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<string> ThicknessOptions { get; } = new() { "6mm", "8mm", "10mm", "12mm", "15mm", "19mm" };
        public ObservableCollection<string> ColorOptions { get; } = new() { "Clear", "HD Grey", "HD Blue", "HD Green", "HD Bronze" };
        public ObservableCollection<string> ProfitOptions { get; } = new() { "15%", "20%", "25%", "30%", "35%" };
        public ObservableCollection<SguRecordUI> Records { get; set; } = new();
        public static QuotationService Quotation { get; set; } = new();

        public ICommand SaveCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand AddToQuoteCommand { get; }
        public ICommand ClearCommand { get; }

        public SguViewModel()
        {
            DbHelper.Init();
            LoadHistory();
            SaveCommand = new RelayCommand(Save);
            ExportPdfCommand = new RelayCommand(ExportToPdf);
            DeleteCommand = new RelayCommand<SguRecordUI>(DeleteRecord);
            AddToQuoteCommand = new RelayCommand(AddToQuote);
            ClearCommand = new RelayCommand(Clear);
            Thickness = "6mm"; Color = "Clear"; Profit = "15%";
            Sheet = ""; Cutting = ""; Tempering = "";
            Width = "1000"; Height = "1000"; Qty = "1";
            IsHistoryVisible = true;
            Calculate();
        }

        private double _sheet, _cutting, _tempering, _result, _width = 1000, _height = 1000, _qty = 1;
        private double _totalSqm, _totalPrice, _vatAmount, _grossTotal;

        public string Sheet { get => _sheet == 0 ? "" : _sheet.ToString(); set { double.TryParse(value, out _sheet); OnChange(); Calculate(); } }
        public string Cutting { get => _cutting == 0 ? "" : _cutting.ToString(); set { double.TryParse(value, out _cutting); OnChange(); Calculate(); } }
        public string Tempering { get => _tempering == 0 ? "" : _tempering.ToString(); set { double.TryParse(value, out _tempering); OnChange(); Calculate(); } }
        public string Thickness { get; set; }
        public string Color { get; set; }
        public string Profit { get; set; }
        public string Result { get => _result.ToString("0.00"); set { double.TryParse(value, out _result); OnChange(); } }
        public string Width { get => _width == 0 ? "" : _width.ToString(); set { double.TryParse(value, out _width); OnChange(); Calculate(); } }
        public string Height { get => _height == 0 ? "" : _height.ToString(); set { double.TryParse(value, out _height); OnChange(); Calculate(); } }
        public string Qty { get => _qty == 0 ? "" : _qty.ToString(); set { double.TryParse(value, out _qty); OnChange(); Calculate(); } }
        public string TotalSqm { get => _totalSqm.ToString("0.00"); set { double.TryParse(value, out _totalSqm); OnChange(); } }
        public string TotalPrice { get => _totalPrice.ToString("0.00"); set { double.TryParse(value, out _totalPrice); OnChange(); } }
        public string VatAmount { get => _vatAmount.ToString("0.00"); set { double.TryParse(value, out _vatAmount); OnChange(); } }
        public string GrossTotal { get => _grossTotal.ToString("0.00"); set { double.TryParse(value, out _grossTotal); OnChange(); } }
        public bool IsHistoryVisible { get; set; }
        public string Spec => $"{Thickness ?? "6mm"} {Color ?? "Clear"} FT Glass";

        private void LoadHistory()
        {
            try
            {
                Records.Clear();
                foreach (var r in DbHelper.GetAllFormatted())
                {
                    var dateStr = r.CreatedAt.ToString();
                    var date = DateTime.TryParse(dateStr, out var d) ? d : DateTime.Now;
                    Records.Add(new SguRecordUI
                    {
                        Id = r.Id,
                        DisplayText = $"{r.Thickness} {r.Color} FT Glass - {r.Result:0.00} AED - {date:dd MMM yyyy HH:mm}",
                        Thickness = r.Thickness,
                        Color = r.Color,
                        Result = r.Result,
                        CreatedAtStr = dateStr
                    });
                }
            }
            catch { }
        }

        private void Calculate() { try { double baseSheet = _sheet / 0.85; double total = baseSheet + _cutting + _tempering; double profitValue = ParseProfit(Profit); _result = total * (1 + profitValue / 100); OnChange(nameof(Result)); double sqm = (_width / 1000 * _height / 1000) * (_qty == 0 ? 1 : _qty); _totalSqm = sqm; OnChange(nameof(TotalSqm)); double tot = _result * sqm; _totalPrice = tot; _vatAmount = tot * 0.05; _grossTotal = tot * 1.05; OnChange(nameof(TotalPrice)); OnChange(nameof(VatAmount)); OnChange(nameof(GrossTotal)); } catch { } }

        private void Save() { try { Calculate(); DbHelper.Save(Thickness ?? "6mm", Color ?? "Clear", _result); LoadHistory(); MessageBox.Show("Saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information); } catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); } }

        private void DeleteRecord(SguRecordUI r) { if (r != null) { Records.Remove(r); DbHelper.DeleteSgu(r.Id); } }

        private void AddToQuote() { try { Calculate(); Quotation.AddItem(Spec, Width ?? "1000", Height ?? "1000", "0", "0", Qty ?? "1", Result); MessageBox.Show("Added to Quotation!", "Success", MessageBoxButton.OK, MessageBoxImage.Information); } catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); } }

        private void Clear() { Sheet = ""; Cutting = ""; Tempering = ""; Width = "1000"; Height = "1000"; Qty = "1"; Calculate(); }

        private void ExportToPdf() { try { Calculate(); var dialog = new SaveFileDialog { Title = "Save SGU Quotation", Filter = "PDF Files (*.pdf)|*.pdf", DefaultExt = "pdf", FileName = $"SGU_Quote_{DateTime.Now:yyyyMMdd_HHmmss}", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) }; if (dialog.ShowDialog() != true) return; Document.Create(c => c.Page(p => { p.Size(PageSizes.A4); p.Margin(30); p.DefaultTextStyle(x => x.FontSize(10)); p.Header().Column(h => h.Item().Background("#0A6ED1").Padding(12).Row(r => { r.RelativeItem().Text("PRO GLASS AUTOMATION").FontSize(16).Bold().FontColor("#FFFFFF"); r.ConstantItem(140).AlignRight().Text($"{DateTime.Now:dd MMM yyyy}").FontSize(9).FontColor("#FFFFFF"); })); p.Content().PaddingVertical(20).Column(col => { col.Item().AlignCenter().Text("SGU QUOTATION").FontSize(18).Bold().FontColor("#0A6ED1"); col.Item().AlignCenter().Text($"#{DateTime.Now:yyyyMMdd_HHmmss}").FontSize(8).FontColor("#64748B"); col.Item().PaddingTop(15).Background("#FEF3C7").Padding(12).Text(Spec).FontSize(12).Bold().FontColor("#78350F"); col.Item().PaddingTop(15).Table(t => { t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); }); t.Cell().Text("Size (WxH):").Bold(); t.Cell().Text($"{Width ?? "1000"}x{Height ?? "1000"} mm"); t.Cell().Text("Quantity:").Bold(); t.Cell().Text(Qty ?? "1"); t.Cell().Text("Total SQM:").Bold(); t.Cell().Text(TotalSqm); }); col.Item().PaddingTop(15).Table(pt => { pt.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(1); }); pt.Cell().Padding(6).Text("SUB TOTAL").Bold(); pt.Cell().AlignRight().Padding(6).Text($"AED {TotalPrice}").Bold(); pt.Cell().Padding(6).Text("5% VAT"); pt.Cell().AlignRight().Padding(6).Text($"AED {VatAmount}"); }); col.Item().PaddingTop(15).Background("#0A6ED1").Padding(15).AlignCenter().Text($"GROSS TOTAL: AED {GrossTotal}").FontSize(20).Bold().FontColor("#FFFFFF"); col.Item().PaddingTop(20).Text("Terms: Prices in AED | Valid for 2 days from quotation date | Price may increase any time | Payment: As per discussion").FontSize(7).FontColor("#64748B"); }); p.Footer().Background("#1E3A5F").Padding(8).AlignCenter().Text("PRO GLASS AUTOMATION | Dubai, UAE | jubersaleem01@gmail.com").FontSize(8).FontColor("#FFFFFF"); })).GeneratePdf(dialog.FileName); MessageBox.Show($"PDF Exported!\n{dialog.FileName}", "Success", MessageBoxButton.OK, MessageBoxImage.Information); } catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); } }

        private double ParseProfit(string p) => string.IsNullOrWhiteSpace(p) ? 15 : double.TryParse(p.Replace("%", ""), out var r) ? r : 15;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnChange([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class SguRecordUI
    {
        public int Id { get; set; }
        public string Thickness { get; set; }
        public string Color { get; set; }
        public double Result { get; set; }
        public string CreatedAtStr { get; set; }
        public string DisplayText { get; set; }
    }
}