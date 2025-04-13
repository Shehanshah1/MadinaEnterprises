using MadinaEnterprises.Modules.Models;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Globalization;

namespace MadinaEnterprises.Modules.Util;

public static class ExportHelper
{
    public static string ExportContractToWord(Contracts contract, Ginners ginner, Mills mill)
    {
        string docPath = Path.Combine(FileSystem.AppDataDirectory, $"Contract_{contract.ContractID}.docx");

        using var stream = new MemoryStream();
        using var wordDoc = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true);

        var mainPart = wordDoc.AddMainDocumentPart();
        mainPart.Document = new Document(new Body());

        void AddParagraph(string text)
        {
            var para = new Paragraph(new Run(new Text(text)));
            mainPart.Document.Body.Append(para);
        }

        AddParagraph("Contract Summary");
        AddParagraph($"Contract ID: {contract.ContractID}");
        AddParagraph($"Date Created: {contract.DateCreated:yyyy-MM-dd}");
        AddParagraph($"Ginner: {ginner.GinnerName} ({ginner.GinnerID})");
        AddParagraph($"Mill: {mill.MillName} ({mill.MillID})");
        AddParagraph($"Total Bales: {contract.TotalBales}");
        AddParagraph($"Price Per Batch: {contract.PricePerBatch}");
        AddParagraph($"Total Amount: {contract.TotalAmount}");
        AddParagraph($"Commission (%): {contract.CommissionPercentage}");
        AddParagraph($"Delivery Notes: {contract.DeliveryNotes}");
        AddParagraph($"Payment Notes: {contract.PaymentNotes}");

        mainPart.Document.Save();
        File.WriteAllBytes(docPath, stream.ToArray());
        return docPath;
    }

    public static string ExportAllContractsToExcel(List<Contracts> contracts, List<Ginners> ginners, List<Mills> mills)
    {
        var allDeliveries = App.DatabaseService.GetAllDeliveries().Result;
        var allPayments = App.DatabaseService.GetAllPayments().Result;

        string filePath = Path.Combine(FileSystem.AppDataDirectory, $"1 Cotton Purchase {DateTime.Now.Year}.xlsx");
        using var workbook = new XLWorkbook();

        void WriteHeaders(IXLWorksheet sheet, string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
                sheet.Cell(1, i + 1).Value = headers[i];
        }

        void AutoAdjust(IXLWorksheet sheet)
        {
            sheet.Columns().AdjustToContents();
        }

        // Sheet 1: Cumulative Contracts
        var contractSheet = workbook.Worksheets.Add("Contracts");
        string[] contractHeaders = { "ContractID", "Ginner", "Mill", "Total Bales", "Price Per Batch", "Commission %", "Total Amount", "Date", "Delivery Notes", "Payment Notes" };
        WriteHeaders(contractSheet, contractHeaders);

        int row = 2;
        foreach (var c in contracts)
        {
            var g = ginners.FirstOrDefault(g => g.GinnerID == c.GinnerID);
            var m = mills.FirstOrDefault(m => m.MillID == c.MillID);
            contractSheet.Cell(row, 1).Value = c.ContractID;
            contractSheet.Cell(row, 2).Value = $"{g?.GinnerName} ({g?.GinnerID})";
            contractSheet.Cell(row, 3).Value = $"{m?.MillName} ({m?.MillID})";
            contractSheet.Cell(row, 4).Value = c.TotalBales;
            contractSheet.Cell(row, 5).Value = c.PricePerBatch;
            contractSheet.Cell(row, 6).Value = c.CommissionPercentage;
            contractSheet.Cell(row, 7).Value = c.TotalAmount;
            contractSheet.Cell(row, 8).Value = c.DateCreated.ToString("yyyy-MM-dd");
            contractSheet.Cell(row, 9).Value = c.DeliveryNotes;
            contractSheet.Cell(row, 10).Value = c.PaymentNotes;
            row++;
        }
        AutoAdjust(contractSheet);

        // Sheet 2: Cumulative Deliveries
        var deliverySheet = workbook.Worksheets.Add("Deliveries");
        string[] deliveryHeaders = { "DeliveryID", "ContractID", "Amount", "Total Bales", "Factory Weight", "Mill Weight", "Truck Number", "Driver Contact", "Departure Date", "Delivery Date" };
        WriteHeaders(deliverySheet, deliveryHeaders);

        row = 2;
        foreach (var d in allDeliveries)
        {
            deliverySheet.Cell(row, 1).Value = d.DeliveryID;
            deliverySheet.Cell(row, 2).Value = d.ContractID;
            deliverySheet.Cell(row, 3).Value = d.Amount;
            deliverySheet.Cell(row, 4).Value = d.TotalBales;
            deliverySheet.Cell(row, 5).Value = d.FactoryWeight;
            deliverySheet.Cell(row, 6).Value = d.MillWeight;
            deliverySheet.Cell(row, 7).Value = d.TruckNumber;
            deliverySheet.Cell(row, 8).Value = d.DriverContact;
            deliverySheet.Cell(row, 9).Value = d.DepartureDate.ToString("yyyy-MM-dd");
            deliverySheet.Cell(row, 10).Value = d.DeliveryDate.ToString("yyyy-MM-dd");
            row++;
        }
        AutoAdjust(deliverySheet);

        // Sheet 3: Cumulative Payments
        var paymentSheet = workbook.Worksheets.Add("Payments");
        string[] paymentHeaders = { "PaymentID", "ContractID", "Total Amount", "Amount Paid", "Total Bales", "Date" };
        WriteHeaders(paymentSheet, paymentHeaders);

        row = 2;
        foreach (var p in allPayments)
        {
            paymentSheet.Cell(row, 1).Value = p.PaymentID;
            paymentSheet.Cell(row, 2).Value = p.ContractID;
            paymentSheet.Cell(row, 3).Value = p.TotalAmount;
            paymentSheet.Cell(row, 4).Value = p.AmountPaid;
            paymentSheet.Cell(row, 5).Value = p.TotalBales;
            paymentSheet.Cell(row, 6).Value = p.Date.ToString("yyyy-MM-dd");
            row++;
        }
        AutoAdjust(paymentSheet);

        // Sheet for each Ginner with their Contracts, Payments, Deliveries
        foreach (var ginner in ginners)
        {
            var ginnerContracts = contracts.Where(c => c.GinnerID == ginner.GinnerID).ToList();
            if (ginnerContracts.Count == 0) continue;

            var sheet = workbook.Worksheets.Add($"Ginner-{ginner.GinnerID}");
            string[] ginnerHeaders = { "ContractID", "Mill", "Total Bales", "Price/Batch", "Commission %", "Total Amount", "Payments (Total)", "Deliveries (Total)", "Date Created" };
            WriteHeaders(sheet, ginnerHeaders);

            row = 2;
            foreach (var c in ginnerContracts)
            {
                var mill = mills.FirstOrDefault(m => m.MillID == c.MillID);
                var deliveries = allDeliveries.Where(d => d.ContractID == c.ContractID).ToList();
                var payments = allPayments.Where(p => p.ContractID == c.ContractID).ToList();

                double deliveryAmount = deliveries.Sum(d => d.Amount);
                double paymentAmount = payments.Sum(p => p.AmountPaid);

                sheet.Cell(row, 1).Value = c.ContractID;
                sheet.Cell(row, 2).Value = $"{mill?.MillName} ({mill?.MillID})";
                sheet.Cell(row, 3).Value = c.TotalBales;
                sheet.Cell(row, 4).Value = c.PricePerBatch;
                sheet.Cell(row, 5).Value = c.CommissionPercentage;
                sheet.Cell(row, 6).Value = c.TotalAmount;
                sheet.Cell(row, 7).Value = paymentAmount;
                sheet.Cell(row, 8).Value = deliveryAmount;
                sheet.Cell(row, 9).Value = c.DateCreated.ToString("yyyy-MM-dd");
                row++;
            }

            AutoAdjust(sheet);
        }

        workbook.SaveAs(filePath);
        return filePath;
    }
}
