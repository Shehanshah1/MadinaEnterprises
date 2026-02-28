using System;
using System.Globalization;
using System.IO;                       // needed for File & streams
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using MadinaEnterprises.Modules.Models;
using W = DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
// Alias System.IO.Path to avoid ambiguity with DocumentFormat.OpenXml.Drawing.Path
using IOPath = System.IO.Path;

namespace MadinaEnterprises.Modules.Util
{
    public sealed class ExcelExportResult
    {
        public string ExportDirectory { get; init; } = string.Empty;
        public string MasterWorkbookPath { get; init; } = string.Empty;
        public IReadOnlyList<string> PerGinnerWorkbookPaths { get; init; } = Array.Empty<string>();
    }

    public static class ExportHelper
    {
        // ---- Calculation constants (for Excel only; DOCX has no math) ----
        private const double KG_PER_BALE = 150.0; // typical bale weight
        private const double KG_PER_MAUND = 40.0;  // 1 maund = 40 kg

        /* =========================================
           WORD: Single Contract DOCX (paragraphs)
           ========================================= */
        public static string ExportContractToWord(Contracts contract, Ginners ginner, Mills mill)
        {
            // Try to infer a Delivery Date similar to your sample: use first (earliest) delivery if present.
            var deliveries = App.DatabaseService.GetAllDeliveries().Result
                .Where(d => d.ContractID == contract.ContractID)
                .OrderBy(d => d.DeliveryDate)
                .ToList();

            string deliveryDateStr = deliveries.Count > 0
                ? deliveries.First().DeliveryDate.ToString("dd/MM/yy", CultureInfo.InvariantCulture)
                : string.Empty;

            // Attention line – prefer owner name; fall back to mill name.
            string attentionTo =
                !string.IsNullOrWhiteSpace(mill?.OwnerName) ? $"Attention: Mr. {mill.OwnerName}" :
                !string.IsNullOrWhiteSpace(mill?.MillName) ? $"Attention: {mill.MillName}" :
                                                              "Attention:";

            // Seller line – ginner name with station/address suffix if available
            string sellerLine = ginner?.GinnerName ?? "";
            if (!string.IsNullOrWhiteSpace(ginner?.Station))
                sellerLine += $", {ginner.Station}";
            else if (!string.IsNullOrWhiteSpace(ginner?.Address))
                sellerLine += $", {ginner.Address}";

            // Buyer line
            string buyerLine = mill?.MillName ?? "N/A";

            // Rate line (no math in DOCX)
            string rateLine = $"Rs.{contract.PricePerBatch.ToString("N0", CultureInfo.InvariantCulture)}/-";

            var fileName = $"Contract_{SanitizeFileName(contract.ContractID)}.docx";
            string docPath = IOPath.Combine(FileSystem.AppDataDirectory, fileName);

            using (var wordDoc = WordprocessingDocument.Create(docPath, DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
            {
                var mainPart = wordDoc.AddMainDocumentPart();
                mainPart.Document = new W.Document();

                // Add top-right logo in header (does not affect body flow)
              //  AddRightLogoHeader(mainPart, "madina_logoc.png", widthPx: 140, heightPx: 140);

                // Set default font (Times New Roman), size (11pt), and double spacing globally
                var stylesPart = mainPart.StyleDefinitionsPart ?? mainPart.AddNewPart<StyleDefinitionsPart>();
                stylesPart.Styles ??= new W.Styles();

                var docDefaults = new W.DocDefaults(
                    new W.RunPropertiesDefault(
                        new W.RunProperties(
                            new W.RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman", EastAsia = "Times New Roman", ComplexScript = "Times New Roman" },
                            new W.FontSize { Val = "22" },                 // 11 pt
                            new W.FontSizeComplexScript { Val = "22" }
                        )
                    ),
                    new W.ParagraphPropertiesDefault(
                        new W.ParagraphProperties(
                            // Global double-spacing
                            new W.SpacingBetweenLines { Before = "0", After = "0", Line = "480", LineRule = W.LineSpacingRuleValues.Auto },
                            new W.ParagraphMarkRunProperties(
                                new W.RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman", EastAsia = "Times New Roman", ComplexScript = "Times New Roman" },
                                new W.FontSize { Val = "22" },
                                new W.FontSizeComplexScript { Val = "22" }
                            )
                        )
                    )
                );

                stylesPart.Styles.RemoveAllChildren<W.DocDefaults>();
                stylesPart.Styles.Append(docDefaults);
                stylesPart.Styles.Save();

                var body = new W.Body();

                // Title
                body.Append(
                    ParaCenter(
                        RunBold("Sales Contract", sizeHalfPoints: 36) // 18pt
                    )
                );

                // Spacer
                body.Append(Para(""));

                // Contract Date / No on one line (simple spacing)
                string left = $"Contract Date: {contract.DateCreated:dd-MM-yyyy}";
                string right = $"Contract No: {contract.ContractID}";
                body.Append(Para(left + Spaces(60) + right));

                // Attention
                body.Append(Para(attentionTo));

                // Preamble
                body.Append(Para("We are pleased to confirm here the selling of cotton on terms and conditions as described below:"));

                // Seller
                body.Append(ParaBoldLabelValue("Seller: ", sellerLine));

                // Account details block (paragraphs; no table)
                if (!string.IsNullOrWhiteSpace(ginner?.GinnerName) ||
                    !string.IsNullOrWhiteSpace(ginner?.IBAN) ||
                    !string.IsNullOrWhiteSpace(ginner?.BankAddress) ||
                    !string.IsNullOrWhiteSpace(ginner?.NTN) ||
                    !string.IsNullOrWhiteSpace(ginner?.STN))
                {
                    body.Append(Para("")); // spacer
                    body.Append(ParaBold("Account details:"));
                    if (!string.IsNullOrWhiteSpace(ginner?.GinnerName))
                        body.Append(Para($"Title: {ginner.GinnerName}"));
                    if (!string.IsNullOrWhiteSpace(ginner?.IBAN) || !string.IsNullOrWhiteSpace(ginner?.BankAddress))
                        body.Append(Para($"Account number: {ginner?.IBAN ?? ""}    Bank: {ginner?.BankAddress ?? ""}"));
                    if (!string.IsNullOrWhiteSpace(ginner?.NTN) || !string.IsNullOrWhiteSpace(ginner?.STN))
                        body.Append(Para($"NTN: {ginner?.NTN ?? ""}    STN: {ginner?.STN ?? ""}"));
                }

                // Buyer & Broker
                body.Append(ParaBoldLabelValue("Buyer: ", buyerLine));
                body.Append(ParaBoldLabelValue("Broker: ", "Madina Enterprises"));

                // Quantity
                body.Append(ParaBoldLabelValue("Quantity: ", $"{contract.TotalBales} Bales"));

                // Description
                if (!string.IsNullOrWhiteSpace(contract.Description))
                    body.Append(ParaBoldLabelValue("Description: ", contract.Description));

                // Rate / Payment / Delivery Date / Address
                body.Append(ParaBoldLabelValue("Rate: ", rateLine));

                if (!string.IsNullOrWhiteSpace(contract.PaymentNotes))
                    body.Append(ParaBoldLabelValue("Payment: ", contract.PaymentNotes));
                else
                    body.Append(ParaBoldLabelValue("Payment: ", "As per contract"));

                var deliveryDate = !string.IsNullOrWhiteSpace(contract.DeliveryNotes) ? contract.DeliveryNotes : deliveryDateStr;
                body.Append(ParaBoldLabelValue("Delivery Date: ", deliveryDate));

                if (!string.IsNullOrWhiteSpace(mill?.Address))
                    body.Append(ParaBoldLabelValue("Delivery Address: ", mill.Address));

                // Closing / signature
                body.Append(Para("Please return to us the copy duly signed by you along with the formal confirmation of the purchase."));
                body.Append(Para(""));
                body.Append(Para("Regards,"));
                body.Append(Para("Anees ur Rehman"));
                body.Append(Para("CEO, Madina Enterprises"));

                mainPart.Document.Append(body);
                mainPart.Document.Save();
            }

            return docPath;
        }

        // -----------------------
        // DOCX helpers (paragraph)
        // -----------------------

        private static W.Paragraph Para(string text)
        {
            return new W.Paragraph(
                new W.Run(
                    new W.Text(text) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve }
                )
            );
        }

        private static W.Paragraph ParaBoldLabelValue(string boldLabel, string value)
        {
            return new W.Paragraph(
                new W.Run(new W.RunProperties(new W.Bold()),
                    new W.Text(boldLabel) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve }),
                new W.Run(
                    new W.Text(value ?? "") { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve })
            );
        }

        private static W.Paragraph ParaCenter(params W.Run[] runs)
        {
            var pPr = new W.ParagraphProperties(new W.Justification { Val = W.JustificationValues.Center });
            var p = new W.Paragraph { ParagraphProperties = pPr };
            foreach (var r in runs) p.Append(r);
            return p;
        }

        private static W.Run RunBold(string text, int sizeHalfPoints = 0)
        {
            var rp = new W.RunProperties(new W.Bold());
            if (sizeHalfPoints > 0) rp.Append(new W.FontSize { Val = sizeHalfPoints.ToString(CultureInfo.InvariantCulture) });
            return new W.Run(rp, new W.Text(text) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve });
        }

        private static W.Paragraph ParaBold(string text)
        {
            return new W.Paragraph(
                new W.Run(new W.RunProperties(new W.Bold()),
                    new W.Text(text) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve })
            );
        }

        private static string Spaces(int n) => new string(' ', n);

  /*      private static void AddRightLogoHeader(MainDocumentPart mainPart, string appImageName, int widthPx, int heightPx)
        {
            // Ensure a document/body exists so SectionProperties are attachable
            if (mainPart.Document == null)
                mainPart.Document = new W.Document();
            mainPart.Document.Body ??= new W.Body();

            // Load the packaged image (try flat name, then Resources/Raw/)
            byte[]? imgBytes = null;
            try
            {
                using var s = FileSystem.OpenAppPackageFileAsync(appImageName).GetAwaiter().GetResult();
                using var ms = new MemoryStream(); s.CopyTo(ms); imgBytes = ms.ToArray();
            }
            catch
            {
                try
                {
                    using var s2 = FileSystem.OpenAppPackageFileAsync($"Resources/Raw/{appImageName}").GetAwaiter().GetResult();
                    using var ms2 = new MemoryStream(); s2.CopyTo(ms2); imgBytes = ms2.ToArray();
                }
                catch { return; } // not found -> skip logo silently
            }

            // Create header and feed image
            var headerPart = mainPart.AddNewPart<HeaderPart>();
            var relId = mainPart.GetIdOfPart(headerPart);

            var imagePart = headerPart.AddImagePart(ImagePartType.Png);
            using (var imgStream = new MemoryStream(imgBytes))
                imagePart.FeedData(imgStream);

            long cx = (long)widthPx * 9525L; // px -> EMUs
            long cy = (long)heightPx * 9525L;

            // ANCHORED picture so it doesn't affect paragraph layout
            var anchor = new DW.Anchor(
                new DW.SimplePosition { X = 0L, Y = 0L },

                // Horizontal: align to the RIGHT of the page margin
                new DW.HorizontalPosition(
                    new DW.PositionOffset("0") // some schema versions want this present
                )
                {
                    RelativeFrom = DW.HorizontalRelativePositionValues.Margin,
                    // 'align' element is HorizontalAlignment in SDK
                    // values are text: "left" | "center" | "right" | "inside" | "outside"
                    // we want top-right
                    HorizontalAlignment = new DW.HorizontalAlignment("right")
                },

                // Vertical: start at top of page (offset 0)
                new DW.VerticalPosition(new DW.PositionOffset("0"))
                {
                    RelativeFrom = DW.VerticalRelativePositionValues.Page
                },

                new DW.Extent { Cx = cx, Cy = cy },
                new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },

                // No wrapping -> body text stays untouched
                new DW.WrapNone(),

                new DW.DocProperties { Id = 1U, Name = "Company Logo" },
                new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),

                new A.Graphic(
                    new A.GraphicData(
                        new A.Picture(
                            new A.NonVisualPictureProperties(
                                new A.NonVisualDrawingProperties { Id = 0U, Name = appImageName },
                                new A.NonVisualPictureDrawingProperties()
                            ),
                            new A.BlipFill(
                                new A.Blip { Embed = headerPart.GetIdOfPart(imagePart) },
                                new A.Stretch(new A.FillRectangle())
                            ),
                            new A.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset { X = 0L, Y = 0L },
                                    new A.Extents { Cx = cx, Cy = cy }
                                ),
                                new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }
                            )
                        )
                    )
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                )
            )
            {
                BehindDoc = true,          // render behind doc—no text movement
                LayoutInCell = true,
                Locked = false,
                AllowOverlap = true,
                DistanceFromTop = 0U,
                DistanceFromBottom = 0U,
                DistanceFromLeft = 0U,
                DistanceFromRight = 0U
            };

            // Put anchor into header
            var headerPara = new W.Paragraph(new W.Run(new W.Drawing(anchor)));
            headerPart.Header = new W.Header(headerPara);
            headerPart.Header.Save();

            // Attach header to current section
            var sectProps = mainPart.Document.Body.Elements<W.SectionProperties>().LastOrDefault();
            if (sectProps == null)
            {
                sectProps = new W.SectionProperties();
                mainPart.Document.Body.Append(sectProps);
            }
            sectProps.RemoveAllChildren<W.HeaderReference>();
            sectProps.PrependChild(new W.HeaderReference { Type = W.HeaderFooterValues.Default, Id = relId });
        }



        // 96 DPI: 1 pixel = 9525 EMUs
        private static long PxToEmu(int px) => (long)px * 9525L;

        */

        /* ==========================================
           EXCEL: Master workbook + per-ginner files
           (includes calculations)
           ========================================== */
        public static ExcelExportResult ExportAllContractsToExcel(
            List<Contracts> contracts,
            List<Ginners> ginners,
            List<Mills> mills)
        {
            var allDeliveries = App.DatabaseService.GetAllDeliveries().GetAwaiter().GetResult();
            var allPayments = App.DatabaseService.GetAllPayments().GetAwaiter().GetResult();

            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var exportDirectory = IOPath.Combine(FileSystem.AppDataDirectory, $"Exports_{stamp}");
            Directory.CreateDirectory(exportDirectory);

            string masterPath = IOPath.Combine(exportDirectory, $"Cotton_Contracts_All_{stamp}.xlsx");
            using (var wb = new XLWorkbook())
            {
                // Contracts (with calculations)
                var wsC = wb.Worksheets.Add("Contracts");
                string[] headersC =
                {
                    "ContractID","Ginner","Mill","Mill Address","Mill Owner",
                    "Description","Total Bales","Rate/Maund",
                    "Est Kg","Est Maunds","Est Amount",
                    "Mill Weight Kg","Final Maunds","Final Amount",
                    "Paid","Balance","Date","Delivery Notes","Payment Notes"
                };
                WriteHeaders(wsC, headersC);

                int r = 2;
                foreach (var c in contracts)
                {
                    var g = ginners.FirstOrDefault(x => x.GinnerID == c.GinnerID);
                    var m = mills.FirstOrDefault(x => x.MillID == c.MillID);

                    var cDeliveries = allDeliveries.Where(d => d.ContractID == c.ContractID).ToList();
                    var cPayments = allPayments.Where(p => p.ContractID == c.ContractID).ToList();

                    double rate = c.PricePerBatch;
                    double estKg = c.TotalBales * KG_PER_BALE;
                    double estMd = estKg / KG_PER_MAUND;
                    double estAmt = estMd * rate;

                    double millKg = cDeliveries.Sum(d => d.MillWeight);
                    double finalMd = millKg / KG_PER_MAUND;
                    double finalAmt = finalMd * rate;

                    double paid = cPayments.Sum(p => p.AmountPaid);
                    double balance = finalAmt - paid;

                    wsC.Cell(r, 1).Value = c.ContractID;
                    wsC.Cell(r, 2).Value = $"{g?.GinnerName} ({g?.GinnerID})";
                    wsC.Cell(r, 3).Value = $"{m?.MillName} ({m?.MillID})";
                    wsC.Cell(r, 4).Value = m?.Address ?? "";
                    wsC.Cell(r, 5).Value = m?.OwnerName ?? "";
                    wsC.Cell(r, 6).Value = c.Description ?? "";
                    wsC.Cell(r, 7).Value = c.TotalBales;
                    wsC.Cell(r, 8).Value = rate;
                    wsC.Cell(r, 9).Value = estKg;
                    wsC.Cell(r, 10).Value = estMd;
                    wsC.Cell(r, 11).Value = estAmt;
                    wsC.Cell(r, 12).Value = millKg;
                    wsC.Cell(r, 13).Value = finalMd;
                    wsC.Cell(r, 14).Value = finalAmt;
                    wsC.Cell(r, 15).Value = paid;
                    wsC.Cell(r, 16).Value = balance;
                    wsC.Cell(r, 17).Value = c.DateCreated;
                    wsC.Cell(r, 18).Value = c.DeliveryNotes;
                    wsC.Cell(r, 19).Value = c.PaymentNotes;
                    r++;
                }
                StyleContractsSheetRich(wsC);

                var wsD = wb.Worksheets.Add("Deliveries");
                WriteHeaders(wsD, new[] {
                    "DeliveryID","ContractID","Amount","Total Bales","Factory Weight","Mill Weight",
                    "Truck Number","Driver Contact","Departure Date","Delivery Date"
                });
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

                var wsP = wb.Worksheets.Add("Payments");
                WriteHeaders(wsP, new[] { "PaymentID", "ContractID", "Total Amount (Legacy)", "Amount Paid", "Total Bales (Legacy)", "Date" });
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

            var ginnerPaths = new List<string>();
            foreach (var g in ginners)
            {
                var gContracts = contracts.Where(c => c.GinnerID == g.GinnerID).ToList();
                if (gContracts.Count == 0) continue;

                var gContractIds = gContracts.Select(c => c.ContractID).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var gDeliveries = allDeliveries.Where(d => gContractIds.Contains(d.ContractID)).ToList();
                var gPayments = allPayments.Where(p => gContractIds.Contains(p.ContractID)).ToList();

                string gPath = IOPath.Combine(
                    exportDirectory,
                    $"Ginner_{SanitizeFileName(g.GinnerID)}_{SanitizeFileName(g.GinnerName)}_{stamp}.xlsx"
                );

                using (var wb = new XLWorkbook())
                {
                    var wsS = wb.Worksheets.Add("Summary");
                    wsS.Cell(1, 1).Value = "Ginner";
                    wsS.Cell(1, 2).Value = $"{g.GinnerName} ({g.GinnerID})";
                    wsS.Cell(2, 1).Value = "Contracts";
                    wsS.Cell(2, 2).Value = gContracts.Count;

                    double finalAmtSum = 0;
                    double paidSum = 0;
                    foreach (var c in gContracts)
                    {
                        var rate = c.PricePerBatch;
                        var mKg = gDeliveries.Where(d => d.ContractID == c.ContractID).Sum(d => d.MillWeight);
                        var fAmt = (mKg / KG_PER_MAUND) * rate;
                        finalAmtSum += fAmt;
                        paidSum += gPayments.Where(p => p.ContractID == c.ContractID).Sum(p => p.AmountPaid);
                    }

                    wsS.Cell(3, 1).Value = "Total Final Amount";
                    wsS.Cell(3, 2).Value = finalAmtSum;
                    wsS.Cell(4, 1).Value = "Total Paid";
                    wsS.Cell(4, 2).Value = paidSum;
                    wsS.Cell(5, 1).Value = "Balance Due";
                    wsS.Cell(5, 2).Value = finalAmtSum - paidSum;
                    wsS.Range("A1:A5").Style.Font.Bold = true;
                    wsS.Columns().AdjustToContents();
                    wsS.Column(2).Style.NumberFormat.Format = "#,##0";

                    var wsC = wb.Worksheets.Add("Contracts");
                    WriteHeaders(wsC, new[]
                    {
                        "ContractID","Mill","Mill Address","Mill Owner","Description",
                        "Total Bales","Rate/Maund",
                        "Est Kg","Est Maunds","Est Amount",
                        "Mill Weight Kg","Final Maunds","Final Amount",
                        "Paid","Balance","Date Created"
                    });

                    int r = 2;
                    foreach (var c in gContracts)
                    {
                        var m = mills.FirstOrDefault(x => x.MillID == c.MillID);
                        var dKg = gDeliveries.Where(d => d.ContractID == c.ContractID).Sum(d => d.MillWeight);
                        var pay = gPayments.Where(p => p.ContractID == c.ContractID).Sum(p => p.AmountPaid);

                        var rate = c.PricePerBatch;
                        var eKg = c.TotalBales * KG_PER_BALE;
                        var eMd = eKg / KG_PER_MAUND;
                        var eAmt = eMd * rate;
                        var fMd = dKg / KG_PER_MAUND;
                        var fAmt = fMd * rate;
                        var bal = fAmt - pay;

                        wsC.Cell(r, 1).Value = c.ContractID;
                        wsC.Cell(r, 2).Value = $"{m?.MillName} ({m?.MillID})";
                        wsC.Cell(r, 3).Value = m?.Address ?? "";
                        wsC.Cell(r, 4).Value = m?.OwnerName ?? "";
                        wsC.Cell(r, 5).Value = c.Description ?? "";
                        wsC.Cell(r, 6).Value = c.TotalBales;
                        wsC.Cell(r, 7).Value = rate;
                        wsC.Cell(r, 8).Value = eKg;
                        wsC.Cell(r, 9).Value = eMd;
                        wsC.Cell(r, 10).Value = eAmt;
                        wsC.Cell(r, 11).Value = dKg;
                        wsC.Cell(r, 12).Value = fMd;
                        wsC.Cell(r, 13).Value = fAmt;
                        wsC.Cell(r, 14).Value = pay;
                        wsC.Cell(r, 15).Value = bal;
                        wsC.Cell(r, 16).Value = c.DateCreated;
                        r++;
                    }
                    StyleContractsSheetRich(wsC);

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

                    var wsP = wb.Worksheets.Add("Payments");
                    WriteHeaders(wsP, new[]
                    {
                        "PaymentID","ContractID","Total Amount (Legacy)","Amount Paid","Total Bales (Legacy)","Date"
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
                    ginnerPaths.Add(gPath);
                }
            }

            return new ExcelExportResult
            {
                ExportDirectory = exportDirectory,
                MasterWorkbookPath = masterPath,
                PerGinnerWorkbookPaths = ginnerPaths
            };
        }

        /* =======================
           Excel styling helpers
           ======================= */
        private static void WriteHeaders(IXLWorksheet ws, string[] headers)
        {
            for (int i = 0; i < headers.Length; i++)
                ws.Cell(1, i + 1).Value = headers[i];

            var rng = ws.Range(1, 1, 1, headers.Length);
            rng.Style.Font.Bold = true;
            rng.Style.Font.FontColor = XLColor.White;
            rng.Style.Fill.BackgroundColor = XLColor.FromHtml("#2F5597");
            rng.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            rng.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            rng.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            ws.SheetView.FreezeRows(1);
            ws.Row(1).Height = 22;
        }

        private static void StyleContractsSheetRich(IXLWorksheet ws)
        {
            ws.Column(7).Style.NumberFormat.Format = "#,##0";
            ws.Column(8).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(9).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(10).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(11).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(12).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(13).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(14).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(15).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(16).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(17).Style.DateFormat.Format = "yyyy-mm-dd";

            ApplySheetPolish(ws);

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;
            if (lastRow >= 2)
            {
                var balanceRange = ws.Range(2, 16, lastRow, 16);
                balanceRange.AddConditionalFormat().WhenLessThan(0).Fill.SetBackgroundColor(XLColor.FromHtml("#FCE4D6"));
                balanceRange.AddConditionalFormat().WhenGreaterThan(0).Fill.SetBackgroundColor(XLColor.FromHtml("#E2F0D9"));
            }
        }

        private static void StyleDeliveriesSheet(IXLWorksheet ws)
        {
            ws.Column(3).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(4).Style.NumberFormat.Format = "#,##0";
            ws.Column(5).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(6).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(9).Style.DateFormat.Format = "yyyy-mm-dd";
            ws.Column(10).Style.DateFormat.Format = "yyyy-mm-dd";
            ApplySheetPolish(ws);
        }

        private static void StylePaymentsSheet(IXLWorksheet ws)
        {
            ws.Column(3).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(4).Style.NumberFormat.Format = "#,##0.00";
            ws.Column(5).Style.NumberFormat.Format = "#,##0";
            ws.Column(6).Style.DateFormat.Format = "yyyy-mm-dd";
            ApplySheetPolish(ws);
        }

        private static void ApplySheetPolish(IXLWorksheet ws)
        {
            var used = ws.RangeUsed();
            if (used is null)
            {
                return;
            }

            used.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            used.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            used.SetAutoFilter();
            ws.Columns().AdjustToContents();
        }

        private static string SanitizeFileName(string? name)
        {
            name ??= "NA";
            var invalid = Regex.Escape(new string(IOPath.GetInvalidFileNameChars()));
            return Regex.Replace(name, $"[{invalid}]+", "_");
        }
    }
}
