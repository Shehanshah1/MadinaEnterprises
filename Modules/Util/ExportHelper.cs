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
        string filePath = Path.Combine(FileSystem.AppDataDirectory, $"1 Cotton Purchase {DateTime.Now.Year}.xlsx");
        using var workbook = new XLWorkbook();
        var sheet1 = workbook.Worksheets.Add("Sheet1");

        string[] headers = { "ContractID", "Ginner", "Mill", "Total Bales", "Price Per Batch", "Commission %", "Total Amount", "Date", "Delivery Notes", "Payment Notes" };
        for (int i = 0; i < headers.Length; i++)
            sheet1.Cell(1, i + 1).Value = headers[i];

        int row = 2;
        foreach (var c in contracts)
        {
            var g = ginners.FirstOrDefault(g => g.GinnerID == c.GinnerID);
            var m = mills.FirstOrDefault(m => m.MillID == c.MillID);

            sheet1.Cell(row, 1).Value = c.ContractID;
            sheet1.Cell(row, 2).Value = $"{g?.GinnerName} ({g?.GinnerID})";
            sheet1.Cell(row, 3).Value = $"{m?.MillName} ({m?.MillID})";
            sheet1.Cell(row, 4).Value = c.TotalBales;
            sheet1.Cell(row, 5).Value = c.PricePerBatch;
            sheet1.Cell(row, 6).Value = c.CommissionPercentage;
            sheet1.Cell(row, 7).Value = c.TotalAmount;
            sheet1.Cell(row, 8).Value = c.DateCreated.ToString("yyyy-MM-dd");
            sheet1.Cell(row, 9).Value = c.DeliveryNotes;
            sheet1.Cell(row, 10).Value = c.PaymentNotes;
            row++;
        }

        // Create separate sheet for each Ginner
        var grouped = contracts.GroupBy(c => c.GinnerID);
        foreach (var group in grouped)
        {
            var ginner = ginners.FirstOrDefault(g => g.GinnerID == group.Key);
            if (ginner == null) continue;

            var ginnerSheet = workbook.Worksheets.Add(ginner.GinnerID);
            for (int i = 0; i < headers.Length; i++)
                ginnerSheet.Cell(1, i + 1).Value = headers[i];

            int gRow = 2;
            foreach (var c in group)
            {
                var m = mills.FirstOrDefault(m => m.MillID == c.MillID);
                ginnerSheet.Cell(gRow, 1).Value = c.ContractID;
                ginnerSheet.Cell(gRow, 2).Value = $"{ginner.GinnerName} ({ginner.GinnerID})";
                ginnerSheet.Cell(gRow, 3).Value = $"{m?.MillName} ({m?.MillID})";
                ginnerSheet.Cell(gRow, 4).Value = c.TotalBales;
                ginnerSheet.Cell(gRow, 5).Value = c.PricePerBatch;
                ginnerSheet.Cell(gRow, 6).Value = c.CommissionPercentage;
                ginnerSheet.Cell(gRow, 7).Value = c.TotalAmount;
                ginnerSheet.Cell(gRow, 8).Value = c.DateCreated.ToString("yyyy-MM-dd");
                ginnerSheet.Cell(gRow, 9).Value = c.DeliveryNotes;
                ginnerSheet.Cell(gRow, 10).Value = c.PaymentNotes;
                gRow++;
            }
        }

        workbook.SaveAs(filePath);
        return filePath;
    }
}
