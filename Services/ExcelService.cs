// Services/ExcelService.cs
using ClosedXML.Excel;
using ProGlassAutomation.Data.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ProGlassAutomation.Services
{
    public static class ExcelService
    {
        public static List<ProformaInvoiceModel> ImportProformaInvoices(string filePath)
        {
            var invoices = new List<ProformaInvoiceModel>();

            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

            foreach (var row in rows)
            {
                try
                {
                    var invoice = new ProformaInvoiceModel
                    {
                        InvoiceNo = row.Cell(1).GetString(),
                        CustomerName = row.Cell(2).GetString(),
                        CustomerTRN = row.Cell(3).GetString(),
                        CustomerAddress = row.Cell(4).GetString(),
                        ProjectName = row.Cell(5).GetString(),
                        ProjectLocation = row.Cell(6).GetString(),
                        LPONo = row.Cell(7).GetString(),
                        AttentionName = row.Cell(8).GetString(),
                        ContactNo = row.Cell(9).GetString(),
                        InvoiceDate = row.Cell(10).GetDateTime(),
                        ValidUntil = row.Cell(11).GetDateTime(),
                        Status = row.Cell(12).GetString(),
                        Notes = row.Cell(13).GetString()
                    };
                    invoices.Add(invoice);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Import Row Error] {ex.Message}");
                }
            }

            return invoices;
        }

        public static void ExportProformaInvoices(List<ProformaInvoiceModel> invoices, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Proforma Invoices");

            worksheet.Cell(1, 1).Value = "PI Number";
            worksheet.Cell(1, 2).Value = "Customer Name";
            worksheet.Cell(1, 3).Value = "TRN";
            worksheet.Cell(1, 4).Value = "Address";
            worksheet.Cell(1, 5).Value = "Project Name";
            worksheet.Cell(1, 6).Value = "Project Location";
            worksheet.Cell(1, 7).Value = "LPO Number";
            worksheet.Cell(1, 8).Value = "Attention";
            worksheet.Cell(1, 9).Value = "Contact";
            worksheet.Cell(1, 10).Value = "PI Date";
            worksheet.Cell(1, 11).Value = "Valid Until";
            worksheet.Cell(1, 12).Value = "Status";
            worksheet.Cell(1, 13).Value = "Total Amount";
            worksheet.Cell(1, 14).Value = "VAT Amount";
            worksheet.Cell(1, 15).Value = "Net Amount";
            worksheet.Cell(1, 16).Value = "Notes";

            int row = 2;
            foreach (var inv in invoices)
            {
                worksheet.Cell(row, 1).Value = inv.InvoiceNo;
                worksheet.Cell(row, 2).Value = inv.CustomerName;
                worksheet.Cell(row, 3).Value = inv.CustomerTRN;
                worksheet.Cell(row, 4).Value = inv.CustomerAddress;
                worksheet.Cell(row, 5).Value = inv.ProjectName;
                worksheet.Cell(row, 6).Value = inv.ProjectLocation;
                worksheet.Cell(row, 7).Value = inv.LPONo;
                worksheet.Cell(row, 8).Value = inv.AttentionName;
                worksheet.Cell(row, 9).Value = inv.ContactNo;
                worksheet.Cell(row, 10).Value = inv.InvoiceDate;
                worksheet.Cell(row, 11).Value = inv.ValidUntil;
                worksheet.Cell(row, 12).Value = inv.Status;
                worksheet.Cell(row, 13).Value = inv.TotalAmount;
                worksheet.Cell(row, 14).Value = inv.VATAmount;
                worksheet.Cell(row, 15).Value = inv.NetAmount;
                worksheet.Cell(row, 16).Value = inv.Notes;
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public static void ExportJobOrders(List<JobOrderModel> orders, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Job Orders");

            worksheet.Cell(1, 1).Value = "JO Number";
            worksheet.Cell(1, 2).Value = "Client Name";
            worksheet.Cell(1, 3).Value = "Project";
            worksheet.Cell(1, 4).Value = "JO Date";
            worksheet.Cell(1, 5).Value = "Required Date";
            worksheet.Cell(1, 6).Value = "Status";
            worksheet.Cell(1, 7).Value = "Total Qty";
            worksheet.Cell(1, 8).Value = "Released Qty";
            worksheet.Cell(1, 9).Value = "Balance Qty";
            worksheet.Cell(1, 10).Value = "Total Amount";

            int row = 2;
            foreach (var jo in orders)
            {
                worksheet.Cell(row, 1).Value = jo.JONumber;
                worksheet.Cell(row, 2).Value = jo.ClientName;
                worksheet.Cell(row, 3).Value = jo.ProjectName;
                worksheet.Cell(row, 4).Value = jo.JODate;
                worksheet.Cell(row, 5).Value = jo.RequiredDate;
                worksheet.Cell(row, 6).Value = jo.Status;
                worksheet.Cell(row, 7).Value = jo.TotalQty;
                worksheet.Cell(row, 8).Value = jo.ReleasedQty;
                worksheet.Cell(row, 9).Value = jo.BalanceQty;
                worksheet.Cell(row, 10).Value = jo.TotalAmount;
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public static void ExportDeliveryOrders(List<DeliveryOrderModel> orders, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Delivery Orders");

            worksheet.Cell(1, 1).Value = "DO Number";
            worksheet.Cell(1, 2).Value = "Client Name";
            worksheet.Cell(1, 3).Value = "Project";
            worksheet.Cell(1, 4).Value = "DO Date";
            worksheet.Cell(1, 5).Value = "Status";
            worksheet.Cell(1, 6).Value = "Vehicle";
            worksheet.Cell(1, 7).Value = "Driver";
            worksheet.Cell(1, 8).Value = "Total Qty";
            worksheet.Cell(1, 9).Value = "Delivered Qty";
            worksheet.Cell(1, 10).Value = "Notes";

            int row = 2;
            foreach (var d in orders)
            {
                worksheet.Cell(row, 1).Value = d.DONumber;
                worksheet.Cell(row, 2).Value = d.ClientName;
                worksheet.Cell(row, 3).Value = d.ProjectName;
                worksheet.Cell(row, 4).Value = d.DODate;
                worksheet.Cell(row, 5).Value = d.Status;
                worksheet.Cell(row, 6).Value = d.VehicleNumber;
                worksheet.Cell(row, 7).Value = d.DriverName;
                worksheet.Cell(row, 8).Value = d.TotalQty;
                worksheet.Cell(row, 9).Value = d.DeliveredQty;
                worksheet.Cell(row, 10).Value = d.Notes;
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public static void ExportTaxInvoices(List<TaxInvoiceModel> invoices, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Tax Invoices");

            worksheet.Cell(1, 1).Value = "Invoice Number";
            worksheet.Cell(1, 2).Value = "Client Name";
            worksheet.Cell(1, 3).Value = "TRN";
            worksheet.Cell(1, 4).Value = "Invoice Date";
            worksheet.Cell(1, 5).Value = "Due Date";
            worksheet.Cell(1, 6).Value = "Status";
            worksheet.Cell(1, 7).Value = "Payment Status";
            worksheet.Cell(1, 8).Value = "SubTotal";
            worksheet.Cell(1, 9).Value = "VAT (5%)";
            worksheet.Cell(1, 10).Value = "Total Amount";
            worksheet.Cell(1, 11).Value = "Paid Amount";
            worksheet.Cell(1, 12).Value = "Balance";

            int row = 2;
            foreach (var ti in invoices)
            {
                worksheet.Cell(row, 1).Value = ti.InvoiceNumber;
                worksheet.Cell(row, 2).Value = ti.ClientName;
                worksheet.Cell(row, 3).Value = ti.ClientTRN;
                worksheet.Cell(row, 4).Value = ti.InvoiceDate;
                worksheet.Cell(row, 5).Value = ti.DueDate;
                worksheet.Cell(row, 6).Value = ti.Status;
                worksheet.Cell(row, 7).Value = ti.PaymentStatus;
                worksheet.Cell(row, 8).Value = ti.SubTotal;
                worksheet.Cell(row, 9).Value = ti.VATAmount;
                worksheet.Cell(row, 10).Value = ti.TotalAmount;
                worksheet.Cell(row, 11).Value = ti.PaidAmount;
                worksheet.Cell(row, 12).Value = ti.BalanceAmount;
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public static void ExportDailyWork(List<DailyWork> records, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Daily Work");

            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "Company";
            worksheet.Cell(1, 3).Value = "PI Number";
            worksheet.Cell(1, 4).Value = "Customer Ref";
            worksheet.Cell(1, 5).Value = "Type of Work";
            worksheet.Cell(1, 6).Value = "Production Status";
            worksheet.Cell(1, 7).Value = "Qty";
            worksheet.Cell(1, 8).Value = "SQM";
            worksheet.Cell(1, 9).Value = "Salesman";
            worksheet.Cell(1, 10).Value = "Color";
            worksheet.Cell(1, 11).Value = "Notes";

            int row = 2;
            foreach (var w in records)
            {
                worksheet.Cell(row, 1).Value = w.Date;
                worksheet.Cell(row, 2).Value = w.Company;
                worksheet.Cell(row, 3).Value = w.PINumber;
                worksheet.Cell(row, 4).Value = w.CustomerReference;
                worksheet.Cell(row, 5).Value = w.TypeOfWork;
                worksheet.Cell(row, 6).Value = w.ProductionStatus;
                worksheet.Cell(row, 7).Value = w.Qty;
                worksheet.Cell(row, 8).Value = w.SQM;
                worksheet.Cell(row, 9).Value = w.Salesman;
                worksheet.Cell(row, 10).Value = w.Color;
                worksheet.Cell(row, 11).Value = w.Notes;
                row++;
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }
    }
}