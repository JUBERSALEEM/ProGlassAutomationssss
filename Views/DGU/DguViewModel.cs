using Microsoft.Win32;
using ProGlassAutomation.Data.Database;
using ProGlassAutomation.Helpers;
using ProGlassAutomation.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ProGlassAutomation.Views.DGU
{
    public class DguViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public List<string> ThicknessList { get; } = new() { "6mm", "8mm", "10mm", "12mm", "15mm", "19mm" };
        public List<string> ColorList { get; } = new() { "Clear", "Green", "Blue", "Grey" };
        public List<string> AspList { get; } = new() { "6mm", "8mm", "10mm", "12mm", "14mm", "16mm", "18mm", "20mm", "22mm", "24mm" };
        public List<string> ProfitList { get; } = new() { "15%", "20%", "25%", "30%", "35%" };

        public ObservableCollection<DguRecord> Records { get; set; } = new();
        public ObservableCollection<DguRecord> FilteredRecords => Records;
        public ICommand SaveCommand { get; }
        public ICommand ExportPdfCommand { get; }
        public ICommand DeleteCommand { get; }

        public DguViewModel()
        {
            DbHelper.Init();
            foreach (var item in DbHelper.GetAllDgu()) Records.Add(item);
            SaveCommand = new RelayCommand(Save);
            ExportPdfCommand = new RelayCommand(ExportToPdf);
            DeleteCommand = new RelayCommand<DguRecord>(DeleteRecord);
            Thickness1 = "6mm"; Thickness2 = "6mm"; Color1 = "Clear"; Color2 = "Clear";
            AspType = "12mm"; Profit = "15%"; Sheet1 = "0"; Sheet2 = "0";
            Width = "1000"; Height = "1000"; Qty = "1";
            Calculate();
        }

        private string _thickness1 = "6mm", _thickness2 = "6mm", _color1 = "Clear", _color2 = "Clear";
        private string _sheet1 = "0", _sheet2 = "0", _aspType = "12mm", _profit = "15%";
        private string _result = "0.00", _width = "1000", _height = "1000", _qty = "1";
        private string _totalSqm = "0.00", _totalPrice = "0.00", _vatAmount = "0.00", _grossTotal = "0.00";
        private bool _isHistoryVisible;

        public string Thickness1 { get => _thickness1; set { _thickness1 = value; OnChange(); Calculate(); } }
        public string Thickness2 { get => _thickness2; set { _thickness2 = value; OnChange(); Calculate(); } }
        public string Color1 { get => _color1; set { _color1 = value; OnChange(); } }
        public string Color2 { get => _color2; set { _color2 = value; OnChange(); } }
        public string Sheet1 { get => _sheet1; set { _sheet1 = value; OnChange(); Calculate(); } }
        public string Sheet2 { get => _sheet2; set { _sheet2 = value; OnChange(); Calculate(); } }
        public string AspType { get => _aspType; set { _aspType = value; OnChange(); Calculate(); } }
        public string Profit { get => _profit; set { _profit = value; OnChange(); Calculate(); } }
        public string Result { get => _result; set { _result = value; OnChange(); } }
        public string Width { get => _width; set { _width = value; OnChange(); Calculate(); } }
        public string Height { get => _height; set { _height = value; OnChange(); Calculate(); } }
        public string Qty { get => _qty; set { _qty = value; OnChange(); Calculate(); } }
        public string TotalSqm { get => _totalSqm; set { _totalSqm = value; OnChange(); } }
        public string TotalPrice { get => _totalPrice; set { _totalPrice = value; OnChange(); } }
        public string VatAmount { get => _vatAmount; set { _vatAmount = value; OnChange(); } }
        public string GrossTotal { get => _grossTotal; set { _grossTotal = value; OnChange(); } }
        public bool IsHistoryVisible { get => _isHistoryVisible; set { _isHistoryVisible = value; OnChange(); } }
        public string SpecificationSummary => $"{Thickness1} {Color1} FT Glass + {AspType} ASP + {Thickness2} {Color2} FT Glass";

        public void Save()
        {
            Calculate();
            double resultValue = double.TryParse(Result, out var r) ? r : 0;
            var record = new DguRecord
            {
                Thickness1 = Thickness1,
                Color1 = Color1,
                Thickness2 = Thickness2,
                Color2 = Color2,
                Spacer = AspType,
                Result = resultValue,
                CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            Records.Insert(0, record);
            DbHelper.SaveDgu(Thickness1, Color1, Thickness2, Color2, AspType, resultValue);
            OnChange(nameof(FilteredRecords));
            MessageBox.Show("Record saved to history!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void DeleteRecord(DguRecord record)
        {
            if (record != null)
            {
                Records.Remove(record);
                DbHelper.DeleteDgu(record.Id);
                OnChange(nameof(FilteredRecords));
            }
        }

        public void LoadRecord(DguRecord record)
        {
            if (record != null)
            {
                Thickness1 = record.Thickness1;
                Color1 = record.Color1;
                Thickness2 = record.Thickness2;
                Color2 = record.Color2;
                AspType = record.Spacer;
                Calculate();
            }
        }

        public void ExportToPdf()
        {
            try
            {
                Calculate();
                var dialog = new SaveFileDialog { Title = "Save DGU Quotation", Filter = "PDF Files (*.pdf)|*.pdf", DefaultExt = "pdf", FileName = $"DGU_Quote_{DateTime.Now:yyyyMMdd_HHmmss}", InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) };
                if (dialog.ShowDialog() != true) return;
                string filename = dialog.FileName;

                Document.Create(container => container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    page.Header().Column(c => c.Item().Background("#0A6ED1").Padding(12).Row(r => { r.RelativeItem().Text("PRO GLASS AUTOMATION").FontSize(16).Bold().FontColor("#FFFFFF"); r.ConstantItem(140).AlignRight().Text($"{DateTime.Now:dd MMM yyyy}").FontSize(9).FontColor("#FFFFFF"); }));
                    page.Content().PaddingVertical(20).Column(col =>
                    {
                        col.Item().AlignCenter().Text("DGU QUOTATION").FontSize(18).Bold().FontColor("#0A6ED1");
                        col.Item().AlignCenter().Text($"#{DateTime.Now:yyyyMMdd_HHmmss}").FontSize(8).FontColor("#64748B");
                        col.Item().PaddingTop(15).Background("#FEF3C7").Padding(12).Text(SpecificationSummary).FontSize(12).Bold().FontColor("#78350F");
                        col.Item().PaddingTop(15).Table(t => { t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); }); t.Cell().Text("Size (WxH):").Bold(); t.Cell().Text($"{Width}x{Height} mm"); t.Cell().Text("Quantity:").Bold(); t.Cell().Text(Qty); t.Cell().Text("Total SQM:").Bold(); t.Cell().Text(TotalSqm); });
                        col.Item().PaddingTop(15).Table(pt => { pt.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(1); }); pt.Cell().Padding(6).Text("SUB TOTAL").Bold(); pt.Cell().AlignRight().Padding(6).Text($"AED {TotalPrice}").Bold(); pt.Cell().Padding(6).Text("5% VAT"); pt.Cell().AlignRight().Padding(6).Text($"AED {VatAmount}"); });
                        col.Item().PaddingTop(15).Background("#0A6ED1").Padding(15).AlignCenter().Text($"GROSS TOTAL: AED {GrossTotal}").FontSize(20).Bold().FontColor("#FFFFFF");
                        col.Item().PaddingTop(20).Text("Terms: Prices in AED | Valid for 2 days from quotation date | Price may increase any time | Payment: As per discussion").FontSize(7).FontColor("#64748B");
                    });
                    page.Footer().Background("#1E3A5F").Padding(8).AlignCenter().Text("PRO GLASS AUTOMATION | Dubai, UAE | jubersaleem01@gmail.com").FontSize(8).FontColor("#FFFFFF");
                })).GeneratePdf(filename);
                MessageBox.Show($"PDF Exported Successfully!\n\nFile: {filename}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show($"PDF Export Failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private double GetAspPrice() => AspType switch { "6mm" or "8mm" or "10mm" or "12mm" => 45, "14mm" => 48, "16mm" => 50, "18mm" => 52, "20mm" => 55, "22mm" => 58, "24mm" => 60, _ => 45 };

        public void Calculate()
        {
            double u = P(Sheet1) + P(Sheet2);
            double factor = Profit switch { "15%" => 0.85, "20%" => 0.80, "25%" => 0.75, "30%" => 0.70, "35%" => 0.65, _ => 0.80 };
            double margin = Profit switch { "15%" => 0.15, "20%" => 0.20, "25%" => 0.25, "30%" => 0.30, "35%" => 0.35, _ => 0.20 };
            double unitPrice = (u / factor + GetAspPrice()) * (1 + margin);
            Result = unitPrice.ToString("0.00");
            double sqm = (P(Width) / 1000 * P(Height) / 1000) * P(Qty);
            TotalSqm = sqm.ToString("0.00");
            double tot = unitPrice * sqm;
            TotalPrice = tot.ToString("0.00");
            VatAmount = (tot * 0.05).ToString("0.00");
            GrossTotal = (tot * 1.05).ToString("0.00");
        }

        private double P(string v) => double.TryParse(v, out var x) ? x : 0;
        private void OnChange([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}