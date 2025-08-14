using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MadinaEnterprises.Modules.Models;

namespace MadinaEnterprises.Services
{
    public class PdfGenerationService
    {
        private static PdfGenerationService? _instance;
        private readonly DatabaseService _db;
        private readonly string _outputDirectory;

        public static PdfGenerationService Instance
        {
            get
            {
                _instance ??= new PdfGenerationService();
                return _instance;
            }
        }

        private PdfGenerationService()
        {
            _db = App.DatabaseService!;
            _outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PDFs");

            if (!Directory.Exists(_outputDirectory))
                Directory.CreateDirectory(_outputDirectory);
        }

        public async Task<string> GenerateContractPdf(string contractId)
        {
            var contract = await _db.GetContractById(contractId);
            if (contract == null) throw new Exception("Contract not found");

            var ginner = await _db.GetGinnerById(contract.GinnerID);
            var mill = await _db.GetMillById(contract.MillID);
            var deliveries = await _db.GetDeliveriesByContractId(contractId);
            var payments = await _db.GetPaymentsByContractId(contractId);

            var fileName = $"Contract_{contractId}_{DateTime.Now:yyyyMMdd}.html";
            var filePath = Path.Combine(_outputDirectory, fileName);

            var html = GenerateContractHtml(contract, ginner!, mill!, deliveries, payments);
            await File.WriteAllTextAsync(filePath, html);

            return filePath;
        }

        private string GenerateContractHtml(Contracts contract, Ginners ginner, Mills mill,
            List<Deliveries> deliveries, List<Payment> payments)
        {
            var totalDelivered = deliveries.Sum(d => d.TotalBales);
            var totalPaid = payments.Sum(p => p.AmountPaid);
            var balanceRemaining = contract.TotalAmount - totalPaid;
            var deliveryProgress = contract.TotalBales > 0 ? (double)totalDelivered / contract.TotalBales * 100 : 0;
            var paymentProgress = contract.TotalAmount > 0 ? totalPaid / contract.TotalAmount * 100 : 0;

            var html = new StringBuilder();
            html.AppendLine(@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Contract " + contract.ContractID + @"</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Arial', sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 800px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #98cb00, #7ba600); color: white; padding: 30px; text-align: center; }
        .logo { font-size: 36px; font-weight: bold; margin-bottom: 10px; }
        .subtitle { font-size: 18px; opacity: 0.9; }
        .section { background: white; margin: 20px 0; padding: 25px; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }
        .section-title { color: #98cb00; font-size: 20px; margin-bottom: 15px; border-bottom: 2px solid #98cb00; padding-bottom: 5px; }
        .info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 15px; }
        .info-item { padding: 10px; background: #f9f9f9; border-left: 3px solid #98cb00; }
        .info-label { font-weight: bold; color: #666; font-size: 12px; text-transform: uppercase; }
        .info-value { color: #333; font-size: 16px; margin-top: 5px; }
        table { width: 100%; border-collapse: collapse; margin-top: 15px; }
        th { background: #98cb00; color: white; padding: 10px; text-align: left; }
        td { padding: 10px; border-bottom: 1px solid #ddd; }
        tr:hover { background: #f5f5f5; }
        .status-badge { display: inline-block; padding: 5px 10px; border-radius: 20px; font-size: 12px; font-weight: bold; }
        .status-active { background: #4caf50; color: white; }
        .status-pending { background: #ff9800; color: white; }
        .status-complete { background: #2196f3; color: white; }
        .progress-bar { width: 100%; height: 30px; background: #e0e0e0; border-radius: 15px; overflow: hidden; margin: 10px 0; }
        .progress-fill { height: 100%; background: linear-gradient(90deg, #98cb00, #7ba600); display: flex; align-items: center; justify-content: center; color: white; font-weight: bold; }
        .summary-cards { display: grid; grid-template-columns: repeat(3, 1fr); gap: 15px; margin: 20px 0; }
        .summary-card { background: linear-gradient(135deg, #f5f5f5, #e0e0e0); padding: 20px; border-radius: 10px; text-align: center; }
        .summary-value { font-size: 24px; font-weight: bold; color: #333; }
        .summary-label { font-size: 12px; color: #666; margin-top: 5px; }
        .footer { text-align: center; margin-top: 40px; padding: 20px; background: #f5f5f5; color: #666; font-size: 12px; }
        .signature-section { display: grid; grid-template-columns: 1fr 1fr; gap: 50px; margin-top: 50px; }
        .signature-box { border-top: 2px solid #333; padding-top: 10px; text-align: center; }
        @media print {
            .no-print { display: none; }
            body { print-color-adjust: exact; -webkit-print-color-adjust: exact; }
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>MADINA ENTERPRISES</div>
            <div class='subtitle'>Cotton Brokerage & Trading</div>
        </div>

        <div class='section'>
            <h2 class='section-title'>CONTRACT DETAILS</h2>
            <div class='info-grid'>
                <div class='info-item'>
                    <div class='info-label'>Contract ID</div>
                    <div class='info-value'>" + contract.ContractID + @"</div>
                </div>
                <div class='info-item'>
                    <div class='info-label'>Date Created</div>
                    <div class='info-value'>" + contract.DateCreated.ToString("MMMM dd, yyyy") + @"</div>
                </div>
                <div class='info-item'>
                    <div class='info-label'>Total Amount</div>
                    <div class='info-value'>PKR " + contract.TotalAmount.ToString("N2") + @"</div>
                </div>
                <div class='info-item'>
                    <div class='info-label'>Commission</div>
                    <div class='info-value'>" + contract.CommissionPercentage + @"% (PKR " + (contract.TotalAmount * contract.CommissionPercentage / 100).ToString("N2") + @")</div>
                </div>
            </div>
        </div>

        <div class='section'>
            <h2 class='section-title'>PARTIES INVOLVED</h2>
            <div class='info-grid'>
                <div class='info-item'>
                    <div class='info-label'>Ginner Details</div>
                    <div class='info-value'>
                        <strong>" + ginner.GinnerName + @"</strong><br>
                        ID: " + ginner.GinnerID + @"<br>
                        Contact: " + ginner.Contact + @"<br>
                        Address: " + ginner.Address + @"<br>
                        IBAN: " + ginner.IBAN + @"
                    </div>
                </div>
                <div class='info-item'>
                    <div class='info-label'>Mill Details</div>
                    <div class='info-value'>
                        <strong>" + mill.MillName + @"</strong><br>
                        ID: " + mill.MillID + @"<br>
                        Owner: " + mill.OwnerName + @"<br>
                        Address: " + mill.Address + @"
                    </div>
                </div>
            </div>
        </div>

        <div class='section'>
            <h2 class='section-title'>CONTRACT SUMMARY</h2>
            <div class='summary-cards'>
                <div class='summary-card'>
                    <div class='summary-value'>" + contract.TotalBales + @"</div>
                    <div class='summary-label'>TOTAL BALES</div>
                </div>
                <div class='summary-card'>
                    <div class='summary-value'>PKR " + contract.PricePerBatch.ToString("N0") + @"</div>
                    <div class='summary-label'>PRICE PER BATCH</div>
                </div>
                <div class='summary-card'>
                    <div class='summary-value'>" + deliveryProgress.ToString("F1") + @"%</div>
                    <div class='summary-label'>DELIVERY PROGRESS</div>
                </div>
            </div>
        </div>

        <div class='section'>
            <h2 class='section-title'>DELIVERY STATUS</h2>
            <div class='progress-bar'>
                <div class='progress-fill' style='width: " + deliveryProgress + @"%'>
                    " + totalDelivered + " / " + contract.TotalBales + @" Bales Delivered
                </div>
            </div>");

            if (deliveries.Any())
            {
                html.AppendLine(@"
            <table>
                <thead>
                    <tr>
                        <th>Delivery ID</th>
                        <th>Date</th>
                        <th>Bales</th>
                        <th>Factory Weight</th>
                        <th>Mill Weight</th>
                        <th>Truck #</th>
                    </tr>
                </thead>
                <tbody>");

                foreach (var delivery in deliveries.OrderByDescending(d => d.DeliveryDate))
                {
                    html.AppendLine($@"
                    <tr>
                        <td>{delivery.DeliveryID}</td>
                        <td>{delivery.DeliveryDate:MMM dd, yyyy}</td>
                        <td>{delivery.TotalBales}</td>
                        <td>{delivery.FactoryWeight:N2} kg</td>
                        <td>{delivery.MillWeight:N2} kg</td>
                        <td>{delivery.TruckNumber}</td>
                    </tr>");
                }

                html.AppendLine(@"
                </tbody>
            </table>");
            }
            else
            {
                html.AppendLine("<p style='text-align: center; color: #999; padding: 20px;'>No deliveries recorded yet</p>");
            }

            html.AppendLine(@"
        </div>

        <div class='section'>
            <h2 class='section-title'>PAYMENT STATUS</h2>
            <div class='progress-bar'>
                <div class='progress-fill' style='width: " + paymentProgress + @"%'>
                    PKR " + totalPaid.ToString("N2") + " / " + contract.TotalAmount.ToString("N2") + @" Paid
                </div>
            </div>");

            if (payments.Any())
            {
                html.AppendLine(@"
            <table>
                <thead>
                    <tr>
                        <th>Payment ID</th>
                        <th>Date</th>
                        <th>Amount Paid</th>
                        <th>Balance After Payment</th>
                    </tr>
                </thead>
                <tbody>");

                double runningBalance = contract.TotalAmount;
                foreach (var payment in payments.OrderBy(p => p.Date))
                {
                    runningBalance -= payment.AmountPaid;
                    html.AppendLine($@"
                    <tr>
                        <td>{payment.PaymentID}</td>
                        <td>{payment.Date:MMM dd, yyyy}</td>
                        <td>PKR {payment.AmountPaid:N2}</td>
                        <td>PKR {runningBalance:N2}</td>
                    </tr>");
                }

                html.AppendLine(@"
                </tbody>
            </table>");
            }
            else
            {
                html.AppendLine("<p style='text-align: center; color: #999; padding: 20px;'>No payments recorded yet</p>");
            }

            html.AppendLine($@"
            <div class='info-grid' style='margin-top: 20px;'>
                <div class='info-item'>
                    <div class='info-label'>Total Amount Due</div>
                    <div class='info-value'>PKR {contract.TotalAmount:N2}</div>
                </div>
                <div class='info-item'>
                    <div class='info-label'>Balance Remaining</div>
                    <div class='info-value' style='color: {(balanceRemaining > 0 ? "red" : "green")}'>PKR {balanceRemaining:N2}</div>
                </div>
            </div>
        </div>");

            if (!string.IsNullOrEmpty(contract.DeliveryNotes) || !string.IsNullOrEmpty(contract.PaymentNotes))
            {
                html.AppendLine(@"
        <div class='section'>
            <h2 class='section-title'>ADDITIONAL NOTES</h2>");

                if (!string.IsNullOrEmpty(contract.DeliveryNotes))
                {
                    html.AppendLine($@"
            <div class='info-item'>
                <div class='info-label'>Delivery Notes</div>
                <div class='info-value'>{contract.DeliveryNotes}</div>
            </div>");
                }

                if (!string.IsNullOrEmpty(contract.PaymentNotes))
                {
                    html.AppendLine($@"
            <div class='info-item' style='margin-top: 15px;'>
                <div class='info-label'>Payment Notes</div>
                <div class='info-value'>{contract.PaymentNotes}</div>
            </div>");
                }

                html.AppendLine("</div>");
            }

            html.AppendLine(@"
        <div class='section'>
            <h2 class='section-title'>SIGNATURES</h2>
            <div class='signature-section'>
                <div class='signature-box'>
                    <div>Ginner Representative</div>
                    <div style='margin-top: 5px; font-size: 11px;'>" + ginner.GinnerName + @"</div>
                </div>
                <div class='signature-box'>
                    <div>Mill Representative</div>
                    <div style='margin-top: 5px; font-size: 11px;'>" + mill.MillName + @"</div>
                </div>
            </div>
        </div>

        <div class='footer'>
            <p>Generated on " + DateTime.Now.ToString("MMMM dd, yyyy HH:mm") + @"</p>
            <p>Madina Enterprises - Cotton Brokerage Management System</p>
            <p>This is a computer-generated document</p>
        </div>
    </div>
</body>
</html>");

            return html.ToString();
        }

        public async Task<string> GenerateMonthlyReport(int month, int year)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var contracts = (await _db.GetAllContracts())
                .Where(c => c.DateCreated >= startDate && c.DateCreated <= endDate).ToList();
            var payments = (await _db.GetAllPayments())
                .Where(p => p.Date >= startDate && p.Date <= endDate).ToList();
            var deliveries = (await _db.GetAllDeliveries())
                .Where(d => d.DeliveryDate >= startDate && d.DeliveryDate <= endDate).ToList();

            var fileName = $"MonthlyReport_{year}_{month:D2}.html";
            var filePath = Path.Combine(_outputDirectory, fileName);

            var html = GenerateMonthlyReportHtml(month, year, contracts, payments, deliveries);
            await File.WriteAllTextAsync(filePath, html);

            return filePath;
        }

        private string GenerateMonthlyReportHtml(int month, int year, List<Contracts> contracts,
            List<Payment> payments, List<Deliveries> deliveries)
        {
            var monthName = new DateTime(year, month, 1).ToString("MMMM yyyy");
            var totalRevenue = contracts.Sum(c => c.TotalAmount);
            var totalPayments = payments.Sum(p => p.AmountPaid);
            var totalCommission = contracts.Sum(c => c.TotalAmount * c.CommissionPercentage / 100);
            var totalBales = contracts.Sum(c => c.TotalBales);
            var totalDeliveries = deliveries.Sum(d => d.TotalBales);

            var html = new StringBuilder();
            html.AppendLine($@"<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Monthly Report - {monthName}</title>
    <style>
        /* Same styles as contract PDF */
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{ font-family: 'Arial', sans-serif; color: #333; }}
        .container {{ max-width: 1000px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #98cb00, #7ba600); color: white; padding: 40px; text-align: center; }}
        .section {{ background: white; margin: 20px 0; padding: 25px; box-shadow: 0 2px 5px rgba(0,0,0,0.1); }}
        /* Additional styles... */
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>MONTHLY BUSINESS REPORT</h1>
            <h2>{monthName}</h2>
        </div>
        
        <!-- Report content -->
        <div class='section'>
            <h2>Executive Summary</h2>
            <p>Total Contracts: {contracts.Count}</p>
            <p>Total Revenue: PKR {totalRevenue:N2}</p>
            <p>Total Payments Received: PKR {totalPayments:N2}</p>
            <p>Total Commission: PKR {totalCommission:N2}</p>
        </div>
        
        <!-- More sections... -->
    </div>
</body>
</html>");

            return html.ToString();
        }
    }
}