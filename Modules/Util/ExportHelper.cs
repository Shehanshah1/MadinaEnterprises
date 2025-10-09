using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using MadinaEnterprises.Modules.Models;
using System.Globalization;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace MadinaEnterprises.Modules.Util;
using W = DocumentFormat.OpenXml.Wordprocessing;

public static class ExportHelper
{
    /* =========================
       WORD: Single Contract DOCX
       ========================= */
    public static string ExportContractToWord(Contracts contract, Ginners ginner, Mills mill)
    {
        var fileName = $"Contract_{SanitizeFileName(contract.ContractID)}.docx";
        string docPath = Path.Combine(FileSystem.AppDataDirectory, fileName);

        using (var wordDoc = WordprocessingDocument.Create(docPath, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new W.Document();
            var body = new W.Body();

            // Title
            var titleRunProps = new W.RunProperties(new W.Bold(), new W.FontSize { Val = "36" }); // 18pt
            var titleParaProps = new W.ParagraphProperties(new W.Justification { Val = W.JustificationValues.Center });
            var title = new W.Paragraph(titleParaProps, new W.Run(titleRunProps, new W.Text("Contract Summary")));
            body.Append(title);

            // Spacer
            body.Append(new W.Paragraph(new W.Run(new W.Text(" "))));

            // Table
            var table = BuildKVTable(new (string, string)[]
            {
            ("Contract ID", contract.ContractID ?? ""),
            ("Date Created", contract.DateCreated.ToString("yyyy-MM-dd")),
            ("Ginner", $"{ginner?.GinnerName} ({ginner?.GinnerID})"),
            ("Mill", $"{mill?.MillName} ({mill?.MillID})"),
            ("Total Bales", contract.TotalBales.ToString(CultureInfo.InvariantCulture)),
            ("Price Per Batch", contract.PricePerBatch.ToString("N2", CultureInfo.InvariantCulture)),
            ("Total Amount", contract.TotalAmount.ToString("N2", CultureInfo.InvariantCulture)),
            ("Commission (%)", contract.CommissionPercentage.ToString("N2", CultureInfo.InvariantCulture)),
            ("Delivery Notes", contract.DeliveryNotes ?? ""),
            ("Payment Notes", contract.PaymentNotes ?? "")
            });

            body.Append(table);
            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        return docPath;
    }

    private static W.Table BuildKVTable((string Key, string Val)[] rows)
    {
        var tbl = new W.Table();

        var props = new W.TableProperties(
            new W.TableBorders(
                new W.TopBorder { Val = new EnumValue<W.BorderValues>(W.BorderValues.Single), Size = 8 },
                new W.BottomBorder { Val = W.BorderValues.Single, Size = 8 },
                new W.LeftBorder { Val = W.BorderValues.Single, Size = 8 },
                new W.RightBorder { Val = W.BorderValues.Single, Size = 8 },
                new W.InsideHorizontalBorder { Val = W.BorderValues.Single, Size = 6 },
                new W.InsideVerticalBorder { Val = W.BorderValues.Single, Size = 6 }
            ),
            new W.TableWidth { Type = W.TableWidthUnitValues.Auto }
        );
        tbl.AppendChild(props);

        foreach (var (k, v) in rows)
        {
            var tr = new W.TableRow();

            var keyCell = new W.TableCell(
                new W.Paragraph(new W.Run(new W.RunProperties(new W.Bold()), new W.Text(k ?? ""))));
            keyCell.TableCellProperties = new W.TableCellProperties(
                new W.Shading { Fill = "EEEEEE", Val = W.ShadingPatternValues.Clear, Color = "auto" });

            var valCell = new W.TableCell(new W.Paragraph(new W.Run(new W.Text(v ?? ""))));

            tr.Append(keyCell, valCell);
            tbl.Append(tr);
        }

        return tbl;
    }


    /* ==========================================
       EXCEL: Master workbook + per-ginner files
       ========================================== */
    public static string ExportAllContractsToExcel(List<Contracts> contracts, List<Ginners> ginners, List<Mills> mills)
    {
        // Pull related data
        var allDeliveries = App.DatabaseService.GetAllDeliveries().Result;
        var allPayments = App.DatabaseService.GetAllPayments().Result;

        // === Master workbook
        string masterPath = Path.Combine(FileSystem.AppDataDirectory, $"Cotton_Contracts_All_{DateTime.Now:yyyyMMdd}.xlsx");
        using (var wb = new XLWorkbook())
        {
            // Contracts sheet
            var wsC = wb.Worksheets.Add("Contracts");
            string[] headersC =
            {
                "ContractID","Ginner","Mill","Total Bales","Price Per Batch","Commission %","Total Amount","Date","Delivery Notes","Payment Notes"
            };
            WriteHeaders(wsC, headersC);

            int r = 2;
            foreach (var c in contracts)
            {
                var g = ginners.FirstOrDefault(x => x.GinnerID == c.GinnerID);
                var m = mills.FirstOrDefault(x => x.MillID == c.MillID);

                wsC.Cell(r, 1).Value = c.ContractID;
                wsC.Cell(r, 2).Value = $"{g?.GinnerName} ({g?.GinnerID})";
                wsC.Cell(r, 3).Value = $"{m?.MillName} ({m?.MillID})";
                wsC.Cell(r, 4).Value = c.TotalBales;
                wsC.Cell(r, 5).Value = c.PricePerBatch;
                wsC.Cell(r, 6).Value = c.CommissionPercentage;
                wsC.Cell(r, 7).Value = c.TotalAmount;
                wsC.Cell(r, 8).Value = c.DateCreated;
                wsC.Cell(r, 9).Value = c.DeliveryNotes;
                wsC.Cell(r, 10).Value = c.PaymentNotes;
                r++;
            }
            StyleContractsSheet(wsC);

            // Deliveries sheet
            var wsD = wb.Worksheets.Add("Deliveries");
            string[] headersD = { "DeliveryID", "ContractID", "Amount", "Total Bales", "Factory Weight", "Mill Weight", "Truck Number", "Driver Contact", "Departure Date", "Delivery Date" };
            WriteHeaders(wsD, headersD);

            r = 2;
            foreach (var d in allDeliveries)
            {
                wsD.Cell(r, 1).Value = d.DeliveryID;
                wsD.Cell(r, 2).Value = d.ContractID;
                wsD.Cell(r, 3).Value = d.Amount;
                wsD.Cell(r, 4).Value = d.TotalBales;
                wsD.Cell(r, 5).Value = d.FactoryWeight;
                wsD.Cell(r, 6).Value = d.MillWeight;
                wsD.Cell(r, 7).Value = d.TruckNumber;
                wsD.Cell(r, 8).Value = d.DriverContact;
                wsD.Cell(r, 9).Value = d.DepartureDate;
                wsD.Cell(r, 10).Value = d.DeliveryDate;
                r++;
            }
            StyleDeliveriesSheet(wsD);

            // Payments sheet
            var wsP = wb.Worksheets.Add("Payments");
            string[] headersP = { "PaymentID", "ContractID", "Total Amount", "Amount Paid", "Total Bales", "Date" };
            WriteHeaders(wsP, headersP);

            r = 2;
            foreach (var p in allPayments)
            {
                wsP.Cell(r, 1).Value = p.PaymentID;
                wsP.Cell(r, 2).Value = p.ContractID;
                wsP.Cell(r, 3).Value = p.TotalAmount;
                wsP.Cell(r, 4).Value = p.AmountPaid;
                wsP.Cell(r, 5).Value = p.TotalBales;
                wsP.Cell(r, 6).Value = p.Date;
                r++;
            }
            StylePaymentsSheet(wsP);

            wb.SaveAs(masterPath);
        }

        // === Per-ginner workbooks
        foreach (var g in ginners)
        {
            var gContracts = contracts.Where(c => c.GinnerID == g.GinnerID).ToList();
            if (gContracts.Count == 0) continue;

            var gContractIds = gContracts.Select(c => c.ContractID).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var gDeliveries = allDeliveries.Where(d => gContractIds.Contains(d.ContractID)).ToList();
            var gPayments = allPayments.Where(p => gContractIds.Contains(p.ContractID)).ToList();

            string gPath = Path.Combine(
                FileSystem.AppDataDirectory,
                $"Ginner_{SanitizeFileName(g.GinnerID)}_{SanitizeFileName(g.GinnerName)}_{DateTime.Now:yyyyMMdd}.xlsx"
            );

            using (var wb = new XLWorkbook())
            {
                // Summary
                var wsS = wb.Worksheets.Add("Summary");
                wsS.Cell(1, 1).Value = "Ginner";
                wsS.Cell(1, 2).Value = $"{g.GinnerName} ({g.GinnerID})";
                wsS.Cell(2, 1).Value = "Contracts";
                wsS.Cell(2, 2).Value = gContracts.Count;
                wsS.Cell(3, 1).Value = "Total Contract Amount";
                wsS.Cell(3, 2).Value = gContracts.Sum(c => c.TotalAmount);
                wsS.Cell(4, 1).Value = "Total Paid";
                wsS.Cell(4, 2).Value = gPayments.Sum(p => p.AmountPaid);
                wsS.Cell(5, 1).Value = "Total Delivered Amount";
                wsS.Cell(5, 2).Value = gDeliveries.Sum(d => d.Amount);
                wsS.Columns().AdjustToContents();
                wsS.Range("A1:A5").Style.Font.Bold = true;
                wsS.Column(2).Style.NumberFormat.Format = "#,##0.00";

                // Contracts
                var wsC = wb.Worksheets.Add("Contracts");
                WriteHeaders(wsC, new[]
                {
                    "ContractID","Mill","Total Bales","Price Per Batch","Commission %","Total Amount","Date Created"
                });
                int r = 2;
                foreach (var c in gContracts)
                {
                    var m = mills.FirstOrDefault(x => x.MillID == c.MillID);
                    wsC.Cell(r, 1).Value = c.ContractID;
                    wsC.Cell(r, 2).Value = $"{m?.MillName} ({m?.MillID})";
                    wsC.Cell(r, 3).Value = c.TotalBales;
                    wsC.Cell(r, 4).Value = c.PricePerBatch;
                    wsC.Cell(r, 5).Value = c.CommissionPercentage;
                    wsC.Cell(r, 6).Value = c.TotalAmount;
                    wsC.Cell(r, 7).Value = c.DateCreated;
                    r++;
                }
                StyleContractsSheet(wsC);

                // Deliveries
                var wsD = wb.Worksheets.Add("Deliveries");
                WriteHeaders(wsD, new[]
                {
                    "DeliveryID","ContractID","Amount","Total Bales","Factory Weight","Mill Weight","Departure Date","Delivery Date","Truck #","Driver Contact"
                });
                r = 2;
                foreach (var d in gDeliveries)
                {
                    wsD.Cell(r, 1).Value = d.DeliveryID;
                    wsD.Cell(r, 2).Value = d.ContractID;
                    wsD.Cell(r, 3).Value = d.Amount;
                    wsD.Cell(r, 4).Value = d.TotalBales;
                    wsD.Cell(r, 5).Value = d.FactoryWeight;
                    wsD.Cell(r, 6).Value = d.MillWeight;
                    wsD.Cell(r, 7).Value = d.DepartureDate;
                    wsD.Cell(r, 8).Value = d.DeliveryDate;
                    wsD.Cell(r, 9).Value = d.TruckNumber;
                    wsD.Cell(r, 10).Value = d.DriverContact;
                    r++;
                }
                StyleDeliveriesSheet(wsD);

                // Payments
                var wsP = wb.Worksheets.Add("Payments");
                WriteHeaders(wsP, new[]
                {
                    "PaymentID","ContractID","Total Amount","Amount Paid","Total Bales","Date"
                });
                r = 2;
                foreach (var p in gPayments)
                {
                    wsP.Cell(r, 1).Value = p.PaymentID;
                    wsP.Cell(r, 2).Value = p.ContractID;
                    wsP.Cell(r, 3).Value = p.TotalAmount;
                    wsP.Cell(r, 4).Value = p.AmountPaid;
                    wsP.Cell(r, 5).Value = p.TotalBales;
                    wsP.Cell(r, 6).Value = p.Date;
                    r++;
                }
                StylePaymentsSheet(wsP);

                wb.SaveAs(gPath);
            }
        }

        return masterPath;
    }

    /* ============= Helpers (Excel) ============= */
    private static void WriteHeaders(IXLWorksheet ws, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var rng = ws.Range(1, 1, 1, headers.Length);
        rng.Style.Font.Bold = true;
        rng.Style.Fill.BackgroundColor = XLColor.FromHtml("#EAEAEA");
        ws.SheetView.FreezeRows(1);
    }

    private static void StyleContractsSheet(IXLWorksheet ws)
    {
        // Set types + formats
        ws.Column(4).Style.NumberFormat.Format = "#,##0";      // Total Bales
        ws.Column(5).Style.NumberFormat.Format = "#,##0.00";   // Price Per Batch
        ws.Column(6).Style.NumberFormat.Format = "0.00%";      // Commission %
        ws.Column(7).Style.NumberFormat.Format = "#,##0.00";   // Total Amount
        ws.Column(8).Style.DateFormat.Format = "yyyy-mm-dd";   // Date
        ws.Columns().AdjustToContents();
    }

    private static void StyleDeliveriesSheet(IXLWorksheet ws)
    {
        ws.Column(3).Style.NumberFormat.Format = "#,##0.00";   // Amount
        ws.Column(4).Style.NumberFormat.Format = "#,##0";      // Total Bales
        ws.Column(5).Style.NumberFormat.Format = "#,##0.00";   // Factory Weight
        ws.Column(6).Style.NumberFormat.Format = "#,##0.00";   // Mill Weight
        ws.Column(9).Style.DateFormat.Format = "yyyy-mm-dd";   // Departure
        ws.Column(10).Style.DateFormat.Format = "yyyy-mm-dd";  // Delivery
        ws.Columns().AdjustToContents();
    }

    private static void StylePaymentsSheet(IXLWorksheet ws)
    {
        ws.Column(3).Style.NumberFormat.Format = "#,##0.00";   // Total Amount
        ws.Column(4).Style.NumberFormat.Format = "#,##0.00";   // Amount Paid
        ws.Column(5).Style.NumberFormat.Format = "#,##0";      // Total Bales
        ws.Column(6).Style.DateFormat.Format = "yyyy-mm-dd";   // Date
        ws.Columns().AdjustToContents();
    }

    private static string SanitizeFileName(string? name)
    {
        name ??= "NA";
        var invalid = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
        return Regex.Replace(name, $"[{invalid}]+", "_");
    }
}
