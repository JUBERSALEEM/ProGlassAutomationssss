// Services/PdfService.cs - CORRECTED
using ProGlassAutomation.Data.Database;
using System;
using System.Drawing;
using System.Drawing.Printing;

namespace ProGlassAutomation.Services
{
    public static class PdfService
    {
        public static void PrintProformaInvoice(ProformaInvoiceModel invoice)
        {
            var printDoc = new PrintDocument();
            printDoc.PrintPage += (object sender, PrintPageEventArgs e) =>
            {
                if (e == null || e.Graphics == null) return;
                var graphics = e.Graphics;

                var font = new Font("Arial", 12);
                var boldFont = new Font("Arial", 14, FontStyle.Bold);
                var smallFont = new Font("Arial", 9);

                float y = 20;
                // Guard against null event args or Graphics
                if (e?.Graphics == null) return;
                float pageWidth = e.PageBounds.Width;

                graphics.DrawString("PROFORMA INVOICE", boldFont, Brushes.Black, pageWidth / 2 - 80, y);
                y += 30;

                graphics.DrawString($"Invoice No: {invoice.InvoiceNo}", font, Brushes.Black, 50, y);
                graphics.DrawString($"Date: {invoice.InvoiceDate:yyyy-MM-dd}", font, Brushes.Black, pageWidth - 200, y);
                y += 25;
                graphics.DrawString($"Valid Until: {invoice.ValidUntil:yyyy-MM-dd}", font, Brushes.Black, 50, y);
                y += 30;

                graphics.DrawString("From:", boldFont, Brushes.Black, 50, y);
                y += 20;
                graphics.DrawString(invoice.CompanyName, font, Brushes.Black, 50, y); y += 15;
                graphics.DrawString($"TRN: {invoice.CompanyTRN}", smallFont, Brushes.Black, 50, y); y += 15;
                graphics.DrawString(invoice.CompanyLocation, smallFont, Brushes.Black, 50, y); y += 15;
                graphics.DrawString($"Phone: {invoice.CompanyPhone}", smallFont, Brushes.Black, 50, y); y += 30;

                graphics.DrawString("Bill To:", boldFont, Brushes.Black, 50, y);
                y += 20;
                graphics.DrawString(invoice.CustomerName, font, Brushes.Black, 50, y); y += 15;
                graphics.DrawString($"TRN: {invoice.CustomerTRN}", smallFont, Brushes.Black, 50, y); y += 15;
                graphics.DrawString(invoice.CustomerAddress, smallFont, Brushes.Black, 50, y); y += 30;

                graphics.DrawString($"Project: {invoice.ProjectName}", font, Brushes.Black, 50, y); y += 15;
                graphics.DrawString($"Location: {invoice.ProjectLocation}", smallFont, Brushes.Black, 50, y); y += 15;
                graphics.DrawString($"LPO No: {invoice.LPONo}", smallFont, Brushes.Black, 50, y); y += 15;
                graphics.DrawString($"Attention: {invoice.AttentionName} | Contact: {invoice.ContactNo}", smallFont, Brushes.Black, 50, y); y += 30;

                graphics.FillRectangle(Brushes.LightGray, 50, y, pageWidth - 100, 20);
                graphics.DrawString("Sr#", font, Brushes.Black, 55, y + 3);
                graphics.DrawString("Description", font, Brushes.Black, 90, y + 3);
                graphics.DrawString("Width", font, Brushes.Black, 300, y + 3);
                graphics.DrawString("Height", font, Brushes.Black, 370, y + 3);
                graphics.DrawString("Qty", font, Brushes.Black, 440, y + 3);
                graphics.DrawString("SQM", font, Brushes.Black, 480, y + 3);
                graphics.DrawString("Price", font, Brushes.Black, 540, y + 3);
                graphics.DrawString("Total", font, Brushes.Black, 620, y + 3);
                y += 25;

                foreach (var piItem in invoice.Items)
                {
                    graphics.DrawString(piItem.SrNo.ToString(), smallFont, Brushes.Black, 55, y);
                    graphics.DrawString(piItem.GlassRef, smallFont, Brushes.Black, 90, y);
                    graphics.DrawString($"{piItem.Width1:F0}x{piItem.Height1:F0}", smallFont, Brushes.Black, 300, y);
                    graphics.DrawString($"{piItem.Width2:F0}x{piItem.Height2:F0}", smallFont, Brushes.Black, 370, y);
                    graphics.DrawString(piItem.Qty.ToString(), smallFont, Brushes.Black, 440, y);
                    graphics.DrawString($"{piItem.TotalSQM:F2}", smallFont, Brushes.Black, 480, y);
                    graphics.DrawString($"{piItem.Price:F2}", smallFont, Brushes.Black, 540, y);
                    graphics.DrawString($"{piItem.TotalPrice:F2}", smallFont, Brushes.Black, 620, y);
                    y += 18;
                }

                y += 20;
                graphics.DrawString($"Sub Total: AED {invoice.TotalAmount:F2}", font, Brushes.Black, 500, y); y += 20;
                graphics.DrawString($"VAT (5%): AED {invoice.VATAmount:F2}", font, Brushes.Black, 500, y); y += 20;
                graphics.DrawString($"NET AMOUNT: AED {invoice.NetAmount:F2}", boldFont, Brushes.Black, 500, y); y += 30;

                if (!string.IsNullOrWhiteSpace(invoice.Notes))
                {
                    graphics.DrawString("Notes:", boldFont, Brushes.Black, 50, y); y += 20;
                    graphics.DrawString(invoice.Notes, smallFont, Brushes.Black, 50, y);
                }

                y = e.PageBounds.Height - 60;
                graphics.DrawString("Thank you for your business!", smallFont, Brushes.Gray, 50, y);
                graphics.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", smallFont, Brushes.Gray, pageWidth - 200, y);
            };

            printDoc.Print();
        }

        public static PrintDocument CreatePrintDocumentForPI(ProformaInvoiceModel invoice)
        {
            var printDoc = new PrintDocument();
            printDoc.PrintPage += (sender, e) =>
            {
                if (e == null) throw new ArgumentNullException(nameof(e));
                var graphics = e.Graphics ?? throw new ArgumentNullException(nameof(e.Graphics));

                var font = new Font("Arial", 12);
                var boldFont = new Font("Arial", 14, FontStyle.Bold);
                var smallFont = new Font("Arial", 9);

                float y = 20;
                float pageWidth = e.PageBounds.Width;

                graphics.DrawString("PROFORMA INVOICE", boldFont, Brushes.Black, pageWidth / 2 - 80, y);
                y += 30;

                graphics.DrawString($"Invoice No: {invoice.InvoiceNo}", font, Brushes.Black, 50, y);
                graphics.DrawString($"Date: {invoice.InvoiceDate:yyyy-MM-dd}", font, Brushes.Black, pageWidth - 200, y);
                y += 25;
                graphics.DrawString($"Valid Until: {invoice.ValidUntil:yyyy-MM-dd}", font, Brushes.Black, 50, y);
                y += 30;

                graphics.DrawString("From:", boldFont, Brushes.Black, 50, y);
                y += 20;
                graphics.DrawString(invoice.CompanyName, font, Brushes.Black, 50, y); y += 15;
                graphics.DrawString($"TRN: {invoice.CompanyTRN}", smallFont, Brushes.Black, 50, y); y += 15;
                graphics.DrawString(invoice.CompanyLocation, smallFont, Brushes.Black, 50, y); y += 15;
                graphics.DrawString($"Phone: {invoice.CompanyPhone}", smallFont, Brushes.Black, 50, y); y += 30;

                graphics.DrawString("Bill To:", boldFont, Brushes.Black, 50, y);
                y += 20;
                graphics.DrawString(invoice.CustomerName, font, Brushes.Black, 50, y); y += 15;
                graphics.DrawString($"TRN: {invoice.CustomerTRN}", smallFont, Brushes.Black, 50, y); y += 15;
                graphics.DrawString(invoice.CustomerAddress, smallFont, Brushes.Black, 50, y); y += 30;

                graphics.DrawString($"Project: {invoice.ProjectName}", font, Brushes.Black, 50, y); y += 15;
                graphics.DrawString($"Location: {invoice.ProjectLocation}", smallFont, Brushes.Black, 50, y); y += 15;
                graphics.DrawString($"LPO No: {invoice.LPONo}", smallFont, Brushes.Black, 50, y); y += 15;
                graphics.DrawString($"Attention: {invoice.AttentionName} | Contact: {invoice.ContactNo}", smallFont, Brushes.Black, 50, y); y += 30;

                graphics.FillRectangle(Brushes.LightGray, 50, y, pageWidth - 100, 20);
                graphics.DrawString("Sr#", font, Brushes.Black, 55, y + 3);
                graphics.DrawString("Description", font, Brushes.Black, 90, y + 3);
                graphics.DrawString("Width", font, Brushes.Black, 300, y + 3);
                graphics.DrawString("Height", font, Brushes.Black, 370, y + 3);
                graphics.DrawString("Qty", font, Brushes.Black, 440, y + 3);
                graphics.DrawString("SQM", font, Brushes.Black, 480, y + 3);
                graphics.DrawString("Price", font, Brushes.Black, 540, y + 3);
                graphics.DrawString("Total", font, Brushes.Black, 620, y + 3);
                y += 25;

                foreach (var piItem in invoice.Items)
                {
                    graphics.DrawString(piItem.SrNo.ToString(), smallFont, Brushes.Black, 55, y);
                    graphics.DrawString(piItem.GlassRef, smallFont, Brushes.Black, 90, y);
                    graphics.DrawString($"{piItem.Width1:F0}x{piItem.Height1:F0}", smallFont, Brushes.Black, 300, y);
                    graphics.DrawString($"{piItem.Width2:F0}x{piItem.Height2:F0}", smallFont, Brushes.Black, 370, y);
                    graphics.DrawString(piItem.Qty.ToString(), smallFont, Brushes.Black, 440, y);
                    graphics.DrawString($"{piItem.TotalSQM:F2}", smallFont, Brushes.Black, 480, y);
                    graphics.DrawString($"{piItem.Price:F2}", smallFont, Brushes.Black, 540, y);
                    graphics.DrawString($"{piItem.TotalPrice:F2}", smallFont, Brushes.Black, 620, y);
                    y += 18;
                }

                y += 20;
                graphics.DrawString($"Sub Total: AED {invoice.TotalAmount:F2}", font, Brushes.Black, 500, y); y += 20;
                graphics.DrawString($"VAT (5%): AED {invoice.VATAmount:F2}", font, Brushes.Black, 500, y); y += 20;
                graphics.DrawString($"NET AMOUNT: AED {invoice.NetAmount:F2}", boldFont, Brushes.Black, 500, y); y += 30;

                if (!string.IsNullOrWhiteSpace(invoice.Notes))
                {
                    graphics.DrawString("Notes:", boldFont, Brushes.Black, 50, y); y += 20;
                    graphics.DrawString(invoice.Notes, smallFont, Brushes.Black, 50, y);
                }

                y = e.PageBounds.Height - 60;
                graphics.DrawString("Thank you for your business!", smallFont, Brushes.Gray, 50, y);
                graphics.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", smallFont, Brushes.Gray, pageWidth - 200, y);
            };

            return printDoc;
        }

        public static void PrintJobOrder(JobOrderModel jo)
        {
            var printDoc = new PrintDocument();
            printDoc.PrintPage += (sender, e) =>
            {
                if (e?.Graphics == null)
                    return;

                var graphics = e.Graphics;
                var font = new Font("Arial", 12);
                var boldFont = new Font("Arial", 14, FontStyle.Bold);
                var smallFont = new Font("Arial", 9);

                float y = 20;
                float pageWidth = e.PageBounds.Width;

            e.Graphics.DrawString("JOB ORDER", boldFont, Brushes.Black, pageWidth / 2 - 60, y);
            y += 30;

            e.Graphics.DrawString($"JO Number: {jo.JONumber}", font, Brushes.Black, 50, y);
            e.Graphics.DrawString($"Date: {jo.JODate:yyyy-MM-dd}", font, Brushes.Black, pageWidth - 200, y);
            y += 25;
            e.Graphics.DrawString($"Required Date: {jo.RequiredDate:yyyy-MM-dd}", font, Brushes.Black, 50, y);
            y += 30;

            e.Graphics.DrawString("Customer:", boldFont, Brushes.Black, 50, y);
            y += 20;
            e.Graphics.DrawString(jo.ClientName, font, Brushes.Black, 50, y); y += 15;
            e.Graphics.DrawString($"Project: {jo.ProjectName}", smallFont, Brushes.Black, 50, y); y += 15;
            e.Graphics.DrawString($"Location: {jo.ProjectLocation}", smallFont, Brushes.Black, 50, y); y += 30;

            e.Graphics.FillRectangle(Brushes.LightGray, 50, y, pageWidth - 100, 20);
            e.Graphics.DrawString("Sr#", font, Brushes.Black, 55, y + 3);
            e.Graphics.DrawString("Glass Ref", font, Brushes.Black, 90, y + 3);
            e.Graphics.DrawString("Width", font, Brushes.Black, 250, y + 3);
            e.Graphics.DrawString("Height", font, Brushes.Black, 320, y + 3);
            e.Graphics.DrawString("Ordered", font, Brushes.Black, 400, y + 3);
            e.Graphics.DrawString("Released", font, Brushes.Black, 470, y + 3);
            e.Graphics.DrawString("Balance", font, Brushes.Black, 540, y + 3);
            e.Graphics.DrawString("Price", font, Brushes.Black, 620, y + 3);
            y += 25;

            foreach (var joItem in jo.Items)
            {
                e.Graphics.DrawString(joItem.SrNo.ToString(), smallFont, Brushes.Black, 55, y);
                e.Graphics.DrawString(joItem.GlassRef, smallFont, Brushes.Black, 90, y);
                e.Graphics.DrawString($"{joItem.Width:F0}", smallFont, Brushes.Black, 250, y);
                e.Graphics.DrawString($"{joItem.Height:F0}", smallFont, Brushes.Black, 320, y);
                e.Graphics.DrawString(joItem.OrderedQty.ToString(), smallFont, Brushes.Black, 400, y);
                e.Graphics.DrawString(joItem.ReleasedQty.ToString(), smallFont, Brushes.Black, 470, y);
                e.Graphics.DrawString(joItem.BalanceQty.ToString(), smallFont, Brushes.Black, 540, y);
                e.Graphics.DrawString($"{joItem.Price:F2}", smallFont, Brushes.Black, 620, y);
                y += 18;
            }

            y += 20;
            e.Graphics.DrawString($"Total Qty: {jo.TotalQty}", font, Brushes.Black, 50, y); y += 20;
            e.Graphics.DrawString($"Released: {jo.ReleasedQty}", font, Brushes.Black, 50, y); y += 20;
            e.Graphics.DrawString($"Balance: {jo.BalanceQty}", boldFont, Brushes.Black, 50, y); y += 20;
            e.Graphics.DrawString($"Total Amount: AED {jo.TotalAmount:F2}", boldFont, Brushes.Black, 50, y); y += 30;
            e.Graphics.DrawString($"Status: {jo.Status}", boldFont, Brushes.Black, 50, y);

                if (!string.IsNullOrWhiteSpace(jo.Notes))
                {
                    y += 25;
                    e.Graphics.DrawString("Notes:", boldFont, Brushes.Black, 50, y); y += 20;
                    e.Graphics.DrawString(jo.Notes, smallFont, Brushes.Black, 50, y);
                }

                y = e.PageBounds.Height - 60;
                e.Graphics.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", smallFont, Brushes.Gray, pageWidth - 200, y);
            };

            printDoc.Print();
        }

        public static void PrintDeliveryOrder(DeliveryOrderModel d)
        {
            var printDoc = new PrintDocument();
            printDoc.PrintPage += (sender, e) =>
            {
                if (d == null || e == null || e.Graphics == null) return;
                var graphics = e.Graphics;

                var font = new Font("Arial", 12);
                var boldFont = new Font("Arial", 14, FontStyle.Bold);
                var smallFont = new Font("Arial", 9);

                float y = 20;
                float pageWidth = e.PageBounds.Width;

                graphics.DrawString("DELIVERY ORDER", boldFont, Brushes.Black, pageWidth / 2 - 70, y);
                y += 30;

                e.Graphics.DrawString($"DO Number: {d.DONumber}", font, Brushes.Black, 50, y);
                e.Graphics.DrawString($"Date: {d.DODate:yyyy-MM-dd}", font, Brushes.Black, pageWidth - 200, y);
                y += 30;

                e.Graphics.DrawString("Deliver To:", boldFont, Brushes.Black, 50, y);
                y += 20;
                e.Graphics.DrawString(d.ClientName ?? string.Empty, font, Brushes.Black, 50, y); y += 15;
                e.Graphics.DrawString($"Project: {d.ProjectName ?? string.Empty}", smallFont, Brushes.Black, 50, y); y += 30;

                e.Graphics.DrawString($"Vehicle: {d.VehicleNumber ?? string.Empty}", font, Brushes.Black, 50, y); y += 20;
                e.Graphics.DrawString($"Driver: {d.DriverName ?? string.Empty}", font, Brushes.Black, 50, y); y += 30;

                e.Graphics.FillRectangle(Brushes.LightGray, 50, y, pageWidth - 100, 20);
                e.Graphics.DrawString("Sr#", font, Brushes.Black, 55, y + 3);
                e.Graphics.DrawString("Glass Ref", font, Brushes.Black, 90, y + 3);
                e.Graphics.DrawString("Width", font, Brushes.Black, 250, y + 3);
                e.Graphics.DrawString("Height", font, Brushes.Black, 320, y + 3);
                e.Graphics.DrawString("Delivered Qty", font, Brushes.Black, 400, y + 3);
                y += 25;

                if (d.Items != null)
                {
                    foreach (var doItem in d.Items)
                    {
                        e.Graphics.DrawString(doItem.SrNo.ToString(), smallFont, Brushes.Black, 55, y);
                        e.Graphics.DrawString(doItem.GlassRef ?? string.Empty, smallFont, Brushes.Black, 90, y);
                        e.Graphics.DrawString($"{doItem.Width:F0}", smallFont, Brushes.Black, 250, y);
                        e.Graphics.DrawString($"{doItem.Height:F0}", smallFont, Brushes.Black, 320, y);
                        e.Graphics.DrawString(doItem.DeliveredQty.ToString(), smallFont, Brushes.Black, 400, y);
                        y += 18;
                    }
                }

                y += 20;
                e.Graphics.DrawString($"Total Qty: {d.TotalQty}", font, Brushes.Black, 50, y); y += 20;
                e.Graphics.DrawString($"Delivered: {d.DeliveredQty}", boldFont, Brushes.Black, 50, y); y += 30;
                e.Graphics.DrawString($"Status: {d.Status ?? string.Empty}", boldFont, Brushes.Black, 50, y);

                if (!string.IsNullOrWhiteSpace(d.Notes))
                {
                    y += 25;
                    e.Graphics.DrawString("Notes:", boldFont, Brushes.Black, 50, y); y += 20;
                    e.Graphics.DrawString(d.Notes ?? string.Empty, smallFont, Brushes.Black, 50, y);
                }

                y += 40;
                e.Graphics.DrawString("Received By: _______________________", font, Brushes.Black, 50, y);
                e.Graphics.DrawString("Date: _______________________", font, Brushes.Black, 350, y);

                y = e.PageBounds.Height - 60;
                e.Graphics.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", smallFont, Brushes.Gray, pageWidth - 200, y);
            };

            printDoc.Print();
        }

        public static void PrintTaxInvoice(TaxInvoiceModel invoice)
        {
            var printDoc = new PrintDocument();
            printDoc.PrintPage += (sender, e) =>
            {
                // Guard against null Graphics to satisfy nullable warnings
                if (e?.Graphics == null) return;

                var font = new Font("Arial", 12);
                var boldFont = new Font("Arial", 14, FontStyle.Bold);
                var smallFont = new Font("Arial", 9);

                float y = 20;
                float pageWidth = e.PageBounds.Width;

                e.Graphics.DrawString("TAX INVOICE", boldFont, Brushes.Black, pageWidth / 2 - 60, y);
                y += 30;

                e.Graphics.DrawString($"Invoice No: {invoice.InvoiceNumber}", font, Brushes.Black, 50, y);
                e.Graphics.DrawString($"Date: {invoice.InvoiceDate:yyyy-MM-dd}", font, Brushes.Black, pageWidth - 200, y);
                y += 25;
                e.Graphics.DrawString($"Due Date: {invoice.DueDate:yyyy-MM-dd}", font, Brushes.Black, 50, y);
                y += 30;

                e.Graphics.DrawString("Bill To:", boldFont, Brushes.Black, 50, y);
                y += 20;
                e.Graphics.DrawString(invoice.ClientName, font, Brushes.Black, 50, y); y += 15;
                e.Graphics.DrawString($"TRN: {invoice.ClientTRN}", smallFont, Brushes.Black, 50, y); y += 15;
                e.Graphics.DrawString(invoice.ClientAddress, smallFont, Brushes.Black, 50, y); y += 30;

                e.Graphics.FillRectangle(Brushes.LightGray, 50, y, pageWidth - 100, 20);
                e.Graphics.DrawString("Sr#", font, Brushes.Black, 55, y + 3);
                e.Graphics.DrawString("Description", font, Brushes.Black, 90, y + 3);
                e.Graphics.DrawString("Qty", font, Brushes.Black, 440, y + 3);
                e.Graphics.DrawString("Unit Price", font, Brushes.Black, 500, y + 3);
                e.Graphics.DrawString("Total", font, Brushes.Black, 620, y + 3);
                y += 25;

                foreach (var tiItem in invoice.Items)
                {
                    e.Graphics.DrawString(tiItem.SrNo.ToString(), smallFont, Brushes.Black, 55, y);
                    e.Graphics.DrawString(tiItem.Description, smallFont, Brushes.Black, 90, y);
                    e.Graphics.DrawString(tiItem.Qty.ToString(), smallFont, Brushes.Black, 440, y);
                    e.Graphics.DrawString($"{tiItem.UnitPrice:F2}", smallFont, Brushes.Black, 500, y);
                    e.Graphics.DrawString($"{tiItem.TotalPrice:F2}", smallFont, Brushes.Black, 620, y);
                    y += 18;
                }

                y += 20;
                e.Graphics.DrawString($"Sub Total: AED {invoice.SubTotal:F2}", font, Brushes.Black, 500, y); y += 20;
                e.Graphics.DrawString($"VAT ({invoice.VATPercent}%): AED {invoice.VATAmount:F2}", font, Brushes.Black, 500, y); y += 20;
                e.Graphics.DrawString($"TOTAL: AED {invoice.TotalAmount:F2}", boldFont, Brushes.Black, 500, y); y += 25;
                e.Graphics.DrawString($"Paid: AED {invoice.PaidAmount:F2}", font, Brushes.Black, 500, y); y += 20;
                e.Graphics.DrawString($"Balance: AED {invoice.BalanceAmount:F2}", boldFont, Brushes.Red, 500, y); y += 30;

                e.Graphics.DrawString($"Payment Status: {invoice.PaymentStatus}", boldFont, Brushes.Black, 50, y);

                if (!string.IsNullOrWhiteSpace(invoice.Notes))
                {
                    y += 25;
                    e.Graphics.DrawString("Notes:", boldFont, Brushes.Black, 50, y); y += 20;
                    e.Graphics.DrawString(invoice.Notes, smallFont, Brushes.Black, 50, y);
                }

                y = e.PageBounds.Height - 60;
                e.Graphics.DrawString("This is a tax invoice.", smallFont, Brushes.Gray, 50, y);
                e.Graphics.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", smallFont, Brushes.Gray, pageWidth - 200, y);
            };

            printDoc.Print();
        }

        public static void PrintDailyWork(DailyWork w)
        {
            if (w == null) throw new ArgumentNullException(nameof(w));
            var printDoc = new PrintDocument();
            printDoc.PrintPage += (sender, e) =>
            {
                if (e?.Graphics == null) return;

                var graphics = e.Graphics;
                var font = new Font("Arial", 12);
                var boldFont = new Font("Arial", 14, FontStyle.Bold);
                var smallFont = new Font("Arial", 9);

                float y = 20;
                float pageWidth = e.PageBounds.Width;

                graphics.DrawString("DAILY WORK REPORT", boldFont, Brushes.Black, pageWidth / 2 - 80, y);
                y += 30;
                graphics.DrawString($"Date: {w.Date:yyyy-MM-dd}", font, Brushes.Black, 50, y);
                graphics.DrawString($"Company: {w.Company}", font, Brushes.Black, pageWidth - 250, y);
                y += 30;
                graphics.DrawString($"PI Number: {w.PINumber}", font, Brushes.Black, 50, y); y += 20;
                graphics.DrawString($"Customer Ref: {w.CustomerReference}", font, Brushes.Black, 50, y); y += 20;
                graphics.DrawString($"Type of Work: {w.TypeOfWork}", font, Brushes.Black, 50, y); y += 20;
                graphics.DrawString($"Production Status: {w.ProductionStatus}", font, Brushes.Black, 50, y); y += 20;
                graphics.DrawString($"Salesman: {w.Salesman}", font, Brushes.Black, 50, y); y += 20;
                graphics.DrawString($"Color: {w.Color}", font, Brushes.Black, 50, y); y += 30;

                graphics.FillRectangle(Brushes.LightGray, 50, y, pageWidth - 100, 20);
                graphics.DrawString("Qty", font, Brushes.Black, 55, y + 3);
                graphics.DrawString("SQM", font, Brushes.Black, 150, y + 3);
                graphics.DrawString("Status", font, Brushes.Black, 250, y + 3);
                graphics.DrawString("Daily Report", font, Brushes.Black, 400, y + 3);
                y += 25;

                graphics.DrawString(w.Qty.ToString(), smallFont, Brushes.Black, 55, y);
                graphics.DrawString($"{w.SQM:F2}", smallFont, Brushes.Black, 150, y);
                graphics.DrawString(w.Status, smallFont, Brushes.Black, 250, y);
                graphics.DrawString(w.DailyReportStatus, smallFont, Brushes.Black, 400, y);
                y += 30;

                if (!string.IsNullOrWhiteSpace(w.Notes))
                {
                    e.Graphics.DrawString("Notes:", boldFont, Brushes.Black, 50, y); y += 20;
                    e.Graphics.DrawString(w.Notes, smallFont, Brushes.Black, 50, y);
                }

                y = e.PageBounds.Height - 60;
                e.Graphics.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", smallFont, Brushes.Gray, pageWidth - 200, y);
            };

            printDoc.Print();
        }
    }
}