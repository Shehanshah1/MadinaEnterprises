using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MadinaEnterprises.Modules.Models;
using System.Globalization;

namespace MadinaEnterprises.Modules.Util
{
    public static class ExportHelper
    {
        public static string ExportContractToWord(Contracts contract, Ginners ginner, Mills mill)
        {
            string fileName = $"Contract_{contract.ContractID}.docx";
            string path = Path.Combine(FileSystem.AppDataDirectory, fileName);

            using var stream = new MemoryStream();
            using var doc = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true);

            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            void AddParagraph(string text)
            {
                var para = new Paragraph(new Run(new Text(text)));
                mainPart.Document.Body.Append(para);
            }

            AddParagraph("=== Contract Summary ===");
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
            File.WriteAllBytes(path, stream.ToArray());
            return path;
        }

        public static string ExportContractToExcel(Contracts contract, Ginners ginner, Mills mill)
        {
            string fileName = $"Contract_{contract.ContractID}.xlsx";
            string path = Path.Combine(FileSystem.AppDataDirectory, fileName);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Contract");

            sheet.Cell("A1").Value = "Field";
            sheet.Cell("B1").Value = "Value";

            int row = 2;
            void AddRow(string label, string value)
            {
                sheet.Cell($"A{row}").Value = label;
                sheet.Cell($"B{row}").Value = value;
                row++;
            }

            AddRow("Contract ID", contract.ContractID);
            AddRow("Date Created", contract.DateCreated.ToString("yyyy-MM-dd"));
            AddRow("Ginner", $"{ginner.GinnerName} ({ginner.GinnerID})");
            AddRow("Mill", $"{mill.MillName} ({mill.MillID})");
            AddRow("Total Bales", contract.TotalBales.ToString());
            AddRow("Price Per Batch", contract.PricePerBatch.ToString(CultureInfo.InvariantCulture));
            AddRow("Total Amount", contract.TotalAmount.ToString(CultureInfo.InvariantCulture));
            AddRow("Commission (%)", contract.CommissionPercentage.ToString(CultureInfo.InvariantCulture));
            AddRow("Delivery Notes", contract.DeliveryNotes);
            AddRow("Payment Notes", contract.PaymentNotes);

            workbook.SaveAs(path);
            return path;
        }
    }
}
