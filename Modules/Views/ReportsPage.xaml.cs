using MadinaEnterprises.Modules.Models;
using MadinaEnterprises.Modules.Util;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MadinaEnterprises.Modules.Views;

public partial class ReportsPage : ContentPage
{
    private readonly DatabaseService _db = App.DatabaseService!;
    private string currentReportContent = "";
    private string currentReportType = "Financial Summary";

    public ReportsPage()
    {
        InitializeComponent();
        InitializeDatePickers();
        _ = LoadQuickStats();
    }

    private void InitializeDatePickers()
    {
        // Set default date range (current month)
        startDatePicker.Date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        endDatePicker.Date = DateTime.Now;
        quickDatePicker.SelectedIndex = 3; // This Month

        quickDatePicker.SelectedIndexChanged += OnQuickDateChanged;
    }

    private async Task LoadQuickStats()
    {
        try
        {
            var contracts = await _db.GetAllContracts();
            var payments = await _db.GetAllPayments();
            var deliveries = await _db.GetAllDeliveries();

            var totalRevenue = contracts.Sum(c => c.TotalAmount);
            var totalPaid = payments.Sum(p => p.AmountPaid);
            var pendingPayments = totalRevenue - totalPaid;
            var activeContracts = contracts.Count;
            var totalDeliveries = deliveries.Count;

            totalRevenueStatLabel.Text = $"${totalRevenue:N0}";
            pendingPaymentsStatLabel.Text = $"${pendingPayments:N0}";
            activeContractsStatLabel.Text = activeContracts.ToString();
            totalDeliveriesStatLabel.Text = totalDeliveries.ToString();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load statistics: {ex.Message}", "OK");
        }
    }

    private void OnQuickDateChanged(object sender, EventArgs e)
    {
        var selected = quickDatePicker.SelectedItem?.ToString();
        var today = DateTime.Now;

        switch (selected)
        {
            case "Today":
                startDatePicker.Date = today;
                endDatePicker.Date = today;
                break;
            case "This Week":
                var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                startDatePicker.Date = startOfWeek;
                endDatePicker.Date = today;
                break;
            case "This Month":
                startDatePicker.Date = new DateTime(today.Year, today.Month, 1);
                endDatePicker.Date = today;
                break;
            case "Last Month":
                var lastMonth = today.AddMonths(-1);
                startDatePicker.Date = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                endDatePicker.Date = new DateTime(lastMonth.Year, lastMonth.Month,
                    DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                break;
            case "This Quarter":
                var quarter = (today.Month - 1) / 3;
                var firstMonthOfQuarter = quarter * 3 + 1;
                startDatePicker.Date = new DateTime(today.Year, firstMonthOfQuarter, 1);
                endDatePicker.Date = today;
                break;
            case "This Year":
                startDatePicker.Date = new DateTime(today.Year, 1, 1);
                endDatePicker.Date = today;
                break;
            case "All Time":
                startDatePicker.Date = new DateTime(2020, 1, 1);
                endDatePicker.Date = today;
                break;
        }
    }

    private void OnReportTypeChanged(object sender, CheckedChangedEventArgs e)
    {
        if (e.Value)
        {
            var radioButton = sender as RadioButton;
            currentReportType = radioButton?.Content?.ToString()?.Substring(2) ?? "Financial Summary";
        }
    }

    private async void OnGenerateReportClicked(object sender, EventArgs e)
    {
        try
        {
            reportFrame.IsVisible = false;
            reportContentLayout.Children.Clear();

            // Generate report based on selected type
            if (financialSummaryRadio.IsChecked == true)
                await GenerateFinancialSummaryReport();
            else if (contractAnalysisRadio.IsChecked == true)
                await GenerateContractAnalysisReport();
            else if (ginnerPerformanceRadio.IsChecked == true)
                await GenerateGinnerPerformanceReport();
            else if (millAnalysisRadio.IsChecked == true)
                await GenerateMillAnalysisReport();
            else if (deliveryReportRadio.IsChecked == true)
                await GenerateDeliveryReport();
            else if (paymentStatusRadio.IsChecked == true)
                await GeneratePaymentStatusReport();

            reportFrame.IsVisible = true;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to generate report: {ex.Message}", "OK");
        }
    }

    private async Task GenerateFinancialSummaryReport()
    {
        var contracts = await _db.GetAllContracts();
        var payments = await _db.GetAllPayments();
        var deliveries = await _db.GetAllDeliveries();

        // Filter by date range
        contracts = contracts.Where(c => c.DateCreated >= startDatePicker.Date &&
                                        c.DateCreated <= endDatePicker.Date).ToList();
        payments = payments.Where(p => p.Date >= startDatePicker.Date &&
                                      p.Date <= endDatePicker.Date).ToList();

        reportTitleLabel.Text = "Financial Summary Report";
        reportDateRangeLabel.Text = $"{startDatePicker.Date:MMM dd, yyyy} - {endDatePicker.Date:MMM dd, yyyy}";

        // Calculate metrics
        var totalRevenue = contracts.Sum(c => c.TotalAmount);
        var totalCommission = contracts.Sum(c => c.TotalAmount * (c.CommissionPercentage / 100));
        var totalPaid = payments.Sum(p => p.AmountPaid);
        var totalDue = totalRevenue - totalPaid;
        var avgContractValue = contracts.Any() ? contracts.Average(c => c.TotalAmount) : 0;

        // Build report content
        var sb = new StringBuilder();
        sb.AppendLine("FINANCIAL SUMMARY REPORT");
        sb.AppendLine($"Period: {startDatePicker.Date:yyyy-MM-dd} to {endDatePicker.Date:yyyy-MM-dd}");
        sb.AppendLine(new string('=', 50));
        sb.AppendLine();
        sb.AppendLine($"Total Revenue: ${totalRevenue:N2}");
        sb.AppendLine($"Total Commission: ${totalCommission:N2}");
        sb.AppendLine($"Total Payments Received: ${totalPaid:N2}");
        sb.AppendLine($"Outstanding Amount: ${totalDue:N2}");
        sb.AppendLine($"Average Contract Value: ${avgContractValue:N2}");
        sb.AppendLine($"Number of Contracts: {contracts.Count}");
        sb.AppendLine($"Number of Payments: {payments.Count}");
        sb.AppendLine();
        sb.AppendLine("Payment Collection Rate: " + (totalRevenue > 0 ? $"{(totalPaid / totalRevenue * 100):F2}%" : "N/A"));

        currentReportContent = sb.ToString();

        // Display in UI
        AddReportSection("Revenue Overview", new Dictionary<string, string>
        {
            { "Total Revenue", $"${totalRevenue:N2}" },
            { "Total Commission", $"${totalCommission:N2}" },
            { "Net Revenue", $"${totalRevenue - totalCommission:N2}" }
        });

        AddReportSection("Payment Status", new Dictionary<string, string>
        {
            { "Payments Received", $"${totalPaid:N2}" },
            { "Outstanding Amount", $"${totalDue:N2}" },
            { "Collection Rate", totalRevenue > 0 ? $"{(totalPaid / totalRevenue * 100):F2}%" : "N/A" }
        });

        AddReportSection("Contract Statistics", new Dictionary<string, string>
        {
            { "Total Contracts", contracts.Count.ToString() },
            { "Average Contract Value", $"${avgContractValue:N2}" },
            { "Total Payments", payments.Count.ToString() }
        });

        reportSummaryLabel.Text = $"Summary: Total revenue of ${totalRevenue:N2} with ${totalDue:N2} outstanding. " +
                                 $"Collection rate: {(totalRevenue > 0 ? (totalPaid / totalRevenue * 100) : 0):F2}%";
    }

    private async Task GenerateContractAnalysisReport()
    {
        var contracts = await _db.GetAllContracts();
        var ginners = await _db.GetAllGinners();
        var mills = await _db.GetAllMills();

        contracts = contracts.Where(c => c.DateCreated >= startDatePicker.Date &&
                                        c.DateCreated <= endDatePicker.Date).ToList();

        reportTitleLabel.Text = "Contract Analysis Report";
        reportDateRangeLabel.Text = $"{startDatePicker.Date:MMM dd, yyyy} - {endDatePicker.Date:MMM dd, yyyy}";

        var totalBales = contracts.Sum(c => c.TotalBales);
        var avgBalesPerContract = contracts.Any() ? contracts.Average(c => c.TotalBales) : 0;
        var avgPricePerBatch = contracts.Any() ? contracts.Average(c => c.PricePerBatch) : 0;

        // Build report
        var sb = new StringBuilder();
        sb.AppendLine("CONTRACT ANALYSIS REPORT");
        sb.AppendLine($"Period: {startDatePicker.Date:yyyy-MM-dd} to {endDatePicker.Date:yyyy-MM-dd}");
        sb.AppendLine(new string('=', 50));
        sb.AppendLine();
        sb.AppendLine($"Total Contracts: {contracts.Count}");
        sb.AppendLine($"Total Bales: {totalBales:N0}");
        sb.AppendLine($"Average Bales per Contract: {avgBalesPerContract:F0}");
        sb.AppendLine($"Average Price per Batch: ${avgPricePerBatch:F2}");

        currentReportContent = sb.ToString();

        // Display top contracts
        var topContracts = contracts.OrderByDescending(c => c.TotalAmount).Take(5);

        AddReportSection("Contract Overview", new Dictionary<string, string>
        {
            { "Total Contracts", contracts.Count.ToString() },
            { "Total Bales", totalBales.ToString("N0") },
            { "Avg Bales/Contract", avgBalesPerContract.ToString("F0") },
            { "Avg Price/Batch", $"${avgPricePerBatch:F2}" }
        });

        // Add top contracts table
        var topContractsSection = new Frame
        {
            BackgroundColor = Colors.White,
            Padding = 10,
            CornerRadius = 5
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };

        grid.Add(new Label { Text = "Top Contracts", FontAttributes = FontAttributes.Bold }, 0, 0);
        grid.Add(new Label { Text = "Bales", FontAttributes = FontAttributes.Bold }, 1, 0);
        grid.Add(new Label { Text = "Amount", FontAttributes = FontAttributes.Bold }, 2, 0);

        int row = 1;
        foreach (var contract in topContracts)
        {
            grid.Add(new Label { Text = contract.ContractID }, 0, row);
            grid.Add(new Label { Text = contract.TotalBales.ToString() }, 1, row);
            grid.Add(new Label { Text = $"${contract.TotalAmount:N0}" }, 2, row);
            row++;
        }

        topContractsSection.Content = grid;
        reportContentLayout.Children.Add(topContractsSection);

        reportSummaryLabel.Text = $"Summary: {contracts.Count} contracts with {totalBales:N0} total bales. " +
                                 $"Average contract size: {avgBalesPerContract:F0} bales at ${avgPricePerBatch:F2} per batch.";
    }

    private async Task GenerateGinnerPerformanceReport()
    {
        var contracts = await _db.GetAllContracts();
        var ginners = await _db.GetAllGinners();
        var deliveries = await _db.GetAllDeliveries();

        contracts = contracts.Where(c => c.DateCreated >= startDatePicker.Date &&
                                        c.DateCreated <= endDatePicker.Date).ToList();

        reportTitleLabel.Text = "Ginner Performance Report";
        reportDateRangeLabel.Text = $"{startDatePicker.Date:MMM dd, yyyy} - {endDatePicker.Date:MMM dd, yyyy}";

        var ginnerPerformance = new List<(string GinnerId, string GinnerName, int Contracts, double Revenue, int Bales)>();

        foreach (var ginner in ginners)
        {
            var ginnerContracts = contracts.Where(c => c.GinnerID == ginner.GinnerID).ToList();
            if (ginnerContracts.Any())
            {
                ginnerPerformance.Add((
                    ginner.GinnerID,
                    ginner.GinnerName,
                    ginnerContracts.Count,
                    ginnerContracts.Sum(c => c.TotalAmount),
                    ginnerContracts.Sum(c => c.TotalBales)
                ));
            }
        }

        // Sort by revenue
        ginnerPerformance = ginnerPerformance.OrderByDescending(g => g.Revenue).ToList();

        // Build report
        currentReportContent = BuildGinnerReport(ginnerPerformance);

        // Display in UI
        foreach (var ginner in ginnerPerformance.Take(10))
        {
            AddReportSection($"{ginner.GinnerName} ({ginner.GinnerId})", new Dictionary<string, string>
            {
                { "Contracts", ginner.Contracts.ToString() },
                { "Total Revenue", $"${ginner.Revenue:N2}" },
                { "Total Bales", ginner.Bales.ToString("N0") },
                { "Avg Contract Value", $"${(ginner.Revenue / ginner.Contracts):N2}" }
            });
        }

        var totalRevenue = ginnerPerformance.Sum(g => g.Revenue);
        reportSummaryLabel.Text = $"Summary: {ginnerPerformance.Count} active ginners generated ${totalRevenue:N2} in revenue. " +
                                 $"Top performer: {ginnerPerformance.FirstOrDefault().GinnerName} with ${ginnerPerformance.FirstOrDefault().Revenue:N2}.";
    }

    private async Task GenerateMillAnalysisReport()
    {
        var contracts = await _db.GetAllContracts();
        var mills = await _db.GetAllMills();
        var deliveries = await _db.GetAllDeliveries();

        contracts = contracts.Where(c => c.DateCreated >= startDatePicker.Date &&
                                        c.DateCreated <= endDatePicker.Date).ToList();

        reportTitleLabel.Text = "Mill Analysis Report";
        reportDateRangeLabel.Text = $"{startDatePicker.Date:MMM dd, yyyy} - {endDatePicker.Date:MMM dd, yyyy}";

        var millAnalysis = new List<(string MillId, string MillName, int Contracts, double Revenue, int Bales)>();

        foreach (var mill in mills)
        {
            var millContracts = contracts.Where(c => c.MillID == mill.MillID).ToList();
            if (millContracts.Any())
            {
                millAnalysis.Add((
                    mill.MillID,
                    mill.MillName,
                    millContracts.Count,
                    millContracts.Sum(c => c.TotalAmount),
                    millContracts.Sum(c => c.TotalBales)
                ));
            }
        }

        millAnalysis = millAnalysis.OrderByDescending(m => m.Revenue).ToList();

        // Display in UI
        foreach (var mill in millAnalysis.Take(10))
        {
            AddReportSection($"{mill.MillName} ({mill.MillId})", new Dictionary<string, string>
            {
                { "Contracts", mill.Contracts.ToString() },
                { "Total Business", $"${mill.Revenue:N2}" },
                { "Total Bales", mill.Bales.ToString("N0") },
                { "Avg Contract", $"${(mill.Revenue / mill.Contracts):N2}" }
            });
        }

        var totalMillRevenue = millAnalysis.Sum(m => m.Revenue);
        reportSummaryLabel.Text = $"Summary: {millAnalysis.Count} active mills with ${totalMillRevenue:N2} in total business. " +
                                 $"Top mill: {millAnalysis.FirstOrDefault().MillName}.";
    }

    private async Task GenerateDeliveryReport()
    {
        var deliveries = await _db.GetAllDeliveries();
        var contracts = await _db.GetAllContracts();

        deliveries = deliveries.Where(d => d.DeliveryDate >= startDatePicker.Date &&
                                          d.DeliveryDate <= endDatePicker.Date).ToList();

        reportTitleLabel.Text = "Delivery Report";
        reportDateRangeLabel.Text = $"{startDatePicker.Date:MMM dd, yyyy} - {endDatePicker.Date:MMM dd, yyyy}";

        var totalDeliveries = deliveries.Count;
        var totalBalesDelivered = deliveries.Sum(d => d.TotalBales);
        var totalFactoryWeight = deliveries.Sum(d => d.FactoryWeight);
        var totalMillWeight = deliveries.Sum(d => d.MillWeight);
        var avgDeliveryTime = deliveries.Any() ?
            deliveries.Average(d => (d.DeliveryDate - d.DepartureDate).TotalDays) : 0;

        AddReportSection("Delivery Overview", new Dictionary<string, string>
        {
            { "Total Deliveries", totalDeliveries.ToString() },
            { "Total Bales Delivered", totalBalesDelivered.ToString("N0") },
            { "Total Factory Weight", $"{totalFactoryWeight:N2} kg" },
            { "Total Mill Weight", $"{totalMillWeight:N2} kg" }
        });

        AddReportSection("Performance Metrics", new Dictionary<string, string>
        {
            { "Avg Delivery Time", $"{avgDeliveryTime:F1} days" },
            { "Weight Variance", $"{Math.Abs(totalFactoryWeight - totalMillWeight):N2} kg" },
            { "Deliveries/Day", $"{(totalDeliveries / Math.Max(1, (endDatePicker.Date - startDatePicker.Date).TotalDays)):F2}" }
        });

        // Recent deliveries
        var recentDeliveries = deliveries.OrderByDescending(d => d.DeliveryDate).Take(5);
        var recentSection = new Frame
        {
            BackgroundColor = Colors.White,
            Padding = 10,
            CornerRadius = 5
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
            }
        };

        grid.Add(new Label { Text = "Recent Deliveries", FontAttributes = FontAttributes.Bold }, 0, 0);
        grid.Add(new Label { Text = "Bales", FontAttributes = FontAttributes.Bold }, 1, 0);
        grid.Add(new Label { Text = "Date", FontAttributes = FontAttributes.Bold }, 2, 0);

        int row = 1;
        foreach (var delivery in recentDeliveries)
        {
            grid.Add(new Label { Text = delivery.DeliveryID }, 0, row);
            grid.Add(new Label { Text = delivery.TotalBales.ToString() }, 1, row);
            grid.Add(new Label { Text = delivery.DeliveryDate.ToString("MMM dd") }, 2, row);
            row++;
        }

        recentSection.Content = grid;
        reportContentLayout.Children.Add(recentSection);

        reportSummaryLabel.Text = $"Summary: {totalDeliveries} deliveries completed with {totalBalesDelivered:N0} bales. " +
                                 $"Average delivery time: {avgDeliveryTime:F1} days.";
    }

    private async Task GeneratePaymentStatusReport()
    {
        var payments = await _db.GetAllPayments();
        var contracts = await _db.GetAllContracts();

        payments = payments.Where(p => p.Date >= startDatePicker.Date &&
                                      p.Date <= endDatePicker.Date).ToList();

        reportTitleLabel.Text = "Payment Status Report";
        reportDateRangeLabel.Text = $"{startDatePicker.Date:MMM dd, yyyy} - {endDatePicker.Date:MMM dd, yyyy}";

        var totalPaymentsReceived = payments.Sum(p => p.AmountPaid);
        var totalContractValue = contracts.Sum(c => c.TotalAmount);
        var totalOutstanding = totalContractValue - totalPaymentsReceived;

        // Group payments by contract
        var paymentsByContract = payments.GroupBy(p => p.ContractID)
            .Select(g => new
            {
                ContractID = g.Key,
                TotalPaid = g.Sum(p => p.AmountPaid),
                PaymentCount = g.Count(),
                LastPaymentDate = g.Max(p => p.Date)
            })
            .OrderByDescending(p => p.TotalPaid)
            .ToList();

        AddReportSection("Payment Overview", new Dictionary<string, string>
        {
            { "Total Payments Received", $"${totalPaymentsReceived:N2}" },
            { "Total Outstanding", $"${totalOutstanding:N2}" },
            { "Collection Rate", $"{(totalContractValue > 0 ? (totalPaymentsReceived / totalContractValue * 100) : 0):F2}%" },
            { "Number of Payments", payments.Count.ToString() }
        });

        // Overdue contracts
        var overdueContracts = contracts.Where(c =>
        {
            var contractPayments = payments.Where(p => p.ContractID == c.ContractID).Sum(p => p.AmountPaid);
            return contractPayments < c.TotalAmount && (DateTime.Now - c.DateCreated).TotalDays > 30;
        }).ToList();

        if (overdueContracts.Any())
        {
            AddReportSection("⚠️ Overdue Contracts", new Dictionary<string, string>
            {
                { "Overdue Count", overdueContracts.Count.ToString() },
                { "Total Overdue Amount", $"${overdueContracts.Sum(c => c.TotalAmount):N2}" },
                { "Oldest Overdue", $"{(DateTime.Now - overdueContracts.Min(c => c.DateCreated)).TotalDays:F0} days" }
            });
        }

        reportSummaryLabel.Text = $"Summary: ${totalPaymentsReceived:N2} collected with ${totalOutstanding:N2} outstanding. " +
                                 $"{overdueContracts.Count} contracts are overdue.";
    }

    private void AddReportSection(string title, Dictionary<string, string> data)
    {
        var frame = new Frame
        {
            BackgroundColor = Colors.White,
            Padding = 15,
            CornerRadius = 5,
            Margin = new Thickness(0, 0, 0, 10)
        };

        var stack = new VerticalStackLayout { Spacing = 8 };

        stack.Children.Add(new Label
        {
            Text = title,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Black
        });

        foreach (var item in data)
        {
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                }
            };

            grid.Add(new Label { Text = item.Key, TextColor = Color.FromHex("#666") }, 0, 0);
            grid.Add(new Label { Text = item.Value, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.End }, 1, 0);

            stack.Children.Add(grid);
        }

        frame.Content = stack;
        reportContentLayout.Children.Add(frame);
    }

    private string BuildGinnerReport(List<(string GinnerId, string GinnerName, int Contracts, double Revenue, int Bales)> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("GINNER PERFORMANCE REPORT");
        sb.AppendLine($"Period: {startDatePicker.Date:yyyy-MM-dd} to {endDatePicker.Date:yyyy-MM-dd}");
        sb.AppendLine(new string('=', 50));
        sb.AppendLine();

        foreach (var ginner in data)
        {
            sb.AppendLine($"Ginner: {ginner.GinnerName} ({ginner.GinnerId})");
            sb.AppendLine($"  Contracts: {ginner.Contracts}");
            sb.AppendLine($"  Revenue: ${ginner.Revenue:N2}");
            sb.AppendLine($"  Bales: {ginner.Bales:N0}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private async void OnEmailReportClicked(object sender, EventArgs e)
    {
        try
        {
            var message = new EmailMessage
            {
                Subject = $"Madina Enterprises - {currentReportType} Report",
                Body = currentReportContent,
                To = new List<string> { "admin@madinaenterprises.com" }
            };

            await Email.Default.ComposeAsync(message);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to compose email: {ex.Message}", "OK");
        }
    }

    private async void OnExportReportClicked(object sender, EventArgs e)
    {
        try
        {
            var fileName = $"{currentReportType.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

            await File.WriteAllTextAsync(filePath, currentReportContent);

            await DisplayAlert("Export Complete", $"Report exported to:\n{filePath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to export report: {ex.Message}", "OK");
        }
    }

    private async void OnPrintReportClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Print", "Print functionality would connect to a printer service.\nReport content has been prepared for printing.", "OK");
    }

    // Navigation methods
    private async void OnDashboardPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DashboardPage());
    private async void OnGinnersPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnersPage());
    private async void OnMillsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new MillsPage());
    private async void OnContractsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new ContractsPage());
    private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
    private async void OnPaymentsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new PaymentsPage());
    private async void OnGinnerLedgerPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnerLedgerPage());
    private async void OnLogOutButtonClicked(object sender, EventArgs e)
    {
        var result = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (result)
            await App.NavigateToPage(new LoginPage());
    }
}