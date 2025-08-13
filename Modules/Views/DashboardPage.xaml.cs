using MadinaEnterprises.Modules.Models;
using System.Linq;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;

namespace MadinaEnterprises.Modules.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DatabaseService _db = App.DatabaseService!;
    private System.Timers.Timer? _refreshTimer;

    public class ActivityItem
    {
        public string Icon { get; set; } = "";
        public string Description { get; set; } = "";
        public string Timestamp { get; set; } = "";
        public string Amount { get; set; } = "";
        public Color AmountColor { get; set; } = Colors.Black;
    }

    public DashboardPage()
    {
        InitializeComponent();
        _ = LoadDashboardData();
        SetupAutoRefresh();
        UpdateDateTime();
    }

    private void SetupAutoRefresh()
    {
        // Auto-refresh every 30 seconds
        _refreshTimer = new System.Timers.Timer(30000);
        _refreshTimer.Elapsed += async (s, e) =>
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await LoadDashboardData();
            });
        };
        _refreshTimer.Start();
    }

    private void UpdateDateTime()
    {
        dateLabel.Text = $"Today is {DateTime.Now:dddd, MMMM dd, yyyy} • Last updated: {DateTime.Now:HH:mm:ss}";
    }

    private async Task LoadDashboardData()
    {
        try
        {
            UpdateDateTime();

            // Get all data
            var stats = await _db.GetDashboardStatistics();
            var contracts = await _db.GetAllContracts();
            var payments = await _db.GetAllPayments();
            var deliveries = await _db.GetAllDeliveries();
            var ginners = await _db.GetAllGinners();
            var mills = await _db.GetAllMills();
            var ledgerEntries = await _db.GetAllGinnerLedger();

            // Calculate additional metrics
            double totalRevenue = contracts.Sum(c => c.TotalAmount);
            double totalCommission = (double)stats["TotalCommission"];
            double totalPaid = (double)stats["TotalPaid"];
            double totalDue = (double)stats["TotalDue"];
            int totalBalesContracted = (int)stats["TotalBalesContracted"];
            int totalBalesDelivered = (int)stats["TotalBalesSold"];
            double avgCommission = (double)stats["AvgCommissionRate"];

            // Calculate payment progress
            double paymentProgress = totalRevenue > 0 ? totalPaid / totalRevenue : 0;

            // Calculate delivery progress
            double deliveryProgress = totalBalesContracted > 0 ?
                (double)totalBalesDelivered / totalBalesContracted : 0;

            // Count active entities (with contracts)
            var activeGinners = contracts.Select(c => c.GinnerID).Distinct().Count();
            var activeMills = contracts.Select(c => c.MillID).Distinct().Count();

            // Contracts this month
            var contractsThisMonth = contracts.Where(c =>
                c.DateCreated.Month == DateTime.Now.Month &&
                c.DateCreated.Year == DateTime.Now.Year).Count();

            // Update UI - Revenue Card
            totalRevenueLabel.Text = $"${totalRevenue:N0}";
            if (totalRevenue > 0)
            {
                var lastMonthRevenue = contracts
                    .Where(c => c.DateCreated.Month == DateTime.Now.AddMonths(-1).Month)
                    .Sum(c => c.TotalAmount);
                var revenueChange = lastMonthRevenue > 0 ?
                    ((totalRevenue - lastMonthRevenue) / lastMonthRevenue * 100) : 0;
                revenueChangeLabel.Text = revenueChange >= 0 ?
                    $"↑ {revenueChange:F1}% from last month" :
                    $"↓ {Math.Abs(revenueChange):F1}% from last month";
                revenueChangeLabel.TextColor = revenueChange >= 0 ? Colors.Green : Colors.Red;
            }

            // Update UI - Commission Card
            totalCommissionLabel.Text = $"${totalCommission:N0}";
            commissionPercentLabel.Text = $"Avg Rate: {avgCommission:F2}%";

            // Update UI - Payments Card
            paymentMadeLabel.Text = $"${totalPaid:N0}";
            paymentProgressBar.Progress = paymentProgress;

            // Update UI - Outstanding Card
            paymentDueLabel.Text = $"${totalDue:N0}";
            var overdueCount = contracts.Where(c =>
            {
                var contractPayments = payments.Where(p => p.ContractID == c.ContractID).Sum(p => p.AmountPaid);
                return contractPayments < c.TotalAmount &&
                       (DateTime.Now - c.DateCreated).TotalDays > 30;
            }).Count();
            overdueCountLabel.Text = overdueCount > 0 ?
                $"⚠️ {overdueCount} overdue contracts" :
                "✅ No overdue payments";
            overdueCountLabel.TextColor = overdueCount > 0 ? Colors.Red : Colors.Green;

            // Update UI - Bales Card
            balesSoldLabel.Text = totalBalesContracted.ToString("N0");
            balesDeliveredLabel.Text = $"{totalBalesDelivered:N0} delivered ({deliveryProgress:P0})";

            // Update UI - Contracts Card
            activeContractsLabel.Text = contracts.Count.ToString();
            contractsThisMonthLabel.Text = $"{contractsThisMonth} this month";

            // Update UI - Ginners Card
            totalGinnersLabel.Text = ginners.Count.ToString();
            activeGinnersLabel.Text = $"{activeGinners} active";

            // Update UI - Mills Card
            totalMillsLabel.Text = mills.Count.ToString();
            activeMillsLabel.Text = $"{activeMills} active";

            // Update Commission Progress Bar
            commissionProgressBar.Progress = avgCommission / 100;
            avgCommissionRateLabel.Text = $"{avgCommission:F2}%";
            var minCommission = contracts.Any() ? contracts.Min(c => c.CommissionPercentage) : 0;
            var maxCommission = contracts.Any() ? contracts.Max(c => c.CommissionPercentage) : 0;
            commissionDetailsLabel.Text = $"Range: {minCommission:F1}% - {maxCommission:F1}%";

            // Update Delivery Progress Bar
            deliveryProgressBar.Progress = deliveryProgress;
            deliveryPercentLabel.Text = $"{deliveryProgress:P0}";
            deliveryDetailsLabel.Text = $"{totalBalesDelivered:N0} of {totalBalesContracted:N0} bales";

            // Load Recent Activity
            await LoadRecentActivity(contracts, payments, deliveries);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load dashboard data: {ex.Message}", "OK");
        }
    }

    private async Task LoadRecentActivity(List<Contracts> contracts, List<Payment> payments, List<Deliveries> deliveries)
    {
        var activities = new ObservableCollection<ActivityItem>();

        // Add recent contracts
        var recentContracts = contracts
            .OrderByDescending(c => c.DateCreated)
            .Take(3)
            .Select(c => new ActivityItem
            {
                Icon = "📄",
                Description = $"New contract {c.ContractID}",
                Timestamp = GetRelativeTime(c.DateCreated),
                Amount = $"${c.TotalAmount:N0}",
                AmountColor = Colors.Green
            });

        // Add recent payments
        var recentPayments = payments
            .OrderByDescending(p => p.Date)
            .Take(3)
            .Select(p => new ActivityItem
            {
                Icon = "💳",
                Description = $"Payment received for {p.ContractID}",
                Timestamp = GetRelativeTime(p.Date),
                Amount = $"${p.AmountPaid:N0}",
                AmountColor = Colors.Blue
            });

        // Add recent deliveries
        var recentDeliveries = deliveries
            .OrderByDescending(d => d.DeliveryDate)
            .Take(3)
            .Select(d => new ActivityItem
            {
                Icon = "🚚",
                Description = $"Delivery {d.DeliveryID} completed",
                Timestamp = GetRelativeTime(d.DeliveryDate),
                Amount = $"{d.TotalBales} bales",
                AmountColor = Colors.Orange
            });

        // Combine and sort by timestamp
        var allActivities = recentContracts
            .Concat(recentPayments)
            .Concat(recentDeliveries)
            .OrderByDescending(a => ParseRelativeTime(a.Timestamp))
            .Take(10);

        foreach (var activity in allActivities)
        {
            activities.Add(activity);
        }

        recentActivityList.ItemsSource = activities;
    }

    private string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.Now - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} days ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} months ago";

        return dateTime.ToString("MMM dd, yyyy");
    }

    private DateTime ParseRelativeTime(string relativeTime)
    {
        // Simple parser for sorting - returns approximate DateTime
        if (relativeTime == "Just now")
            return DateTime.Now;
        if (relativeTime.Contains("minutes ago"))
            return DateTime.Now.AddMinutes(-int.Parse(relativeTime.Split(' ')[0]));
        if (relativeTime.Contains("hours ago"))
            return DateTime.Now.AddHours(-int.Parse(relativeTime.Split(' ')[0]));
        if (relativeTime.Contains("days ago"))
            return DateTime.Now.AddDays(-int.Parse(relativeTime.Split(' ')[0]));
        if (relativeTime.Contains("weeks ago"))
            return DateTime.Now.AddDays(-int.Parse(relativeTime.Split(' ')[0]) * 7);
        if (relativeTime.Contains("months ago"))
            return DateTime.Now.AddDays(-int.Parse(relativeTime.Split(' ')[0]) * 30);

        return DateTime.MinValue;
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        await LoadDashboardData();
        await DisplayAlert("Refreshed", "Dashboard data has been updated.", "OK");
    }

    private async void OnBackupClicked(object sender, EventArgs e)
    {
        try
        {
            var backupPath = await CreateBackup();
            await DisplayAlert("Backup Complete", $"Database backed up to:\n{backupPath}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Backup Failed", $"Error: {ex.Message}", "OK");
        }
    }

    private async Task<string> CreateBackup()
    {
        var sourceDbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "madina.db3");
        var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Backups");

        if (!Directory.Exists(backupDir))
            Directory.CreateDirectory(backupDir);

        var backupFileName = $"madina_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db3";
        var backupPath = Path.Combine(backupDir, backupFileName);

        File.Copy(sourceDbPath, backupPath, true);

        // Clean old backups (keep only last 10)
        var backupFiles = Directory.GetFiles(backupDir, "madina_backup_*.db3")
            .OrderByDescending(f => File.GetCreationTime(f))
            .Skip(10);

        foreach (var oldBackup in backupFiles)
        {
            File.Delete(oldBackup);
        }

        return backupPath;
    }

    // Quick Action Methods
    private async void OnNewContractClicked(object sender, EventArgs e)
        => await App.NavigateToPage(new ContractsPage());

    private async void OnViewReportsClicked(object sender, EventArgs e)
        => await App.NavigateToPage(new ReportsPage());

    private async void OnAddPaymentClicked(object sender, EventArgs e)
        => await App.NavigateToPage(new PaymentsPage());

    private async void OnNewDeliveryClicked(object sender, EventArgs e)
        => await App.NavigateToPage(new DeliveriesPage());

    // Navigation Methods
    private async void OnGinnersPageButtonClicked(object sender, EventArgs e)
        => await App.NavigateToPage(new GinnersPage());

    private async void OnMillsPageButtonClicked(object sender, EventArgs e)
        => await App.NavigateToPage(new MillsPage());

    private async void OnContractsPageButtonClicked(object sender, EventArgs e)
        => await App.NavigateToPage(new ContractsPage());

    private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e)
        => await App.NavigateToPage(new DeliveriesPage());

    private async void OnPaymentsPageButtonClicked(object sender, EventArgs e)
        => await App.NavigateToPage(new PaymentsPage());

    private async void OnGinnerLedgerPageButtonClicked(object sender, EventArgs e)
        => await App.NavigateToPage(new GinnerLedgerPage());

    private async void OnReportsPageButtonClicked(object sender, EventArgs e)
        => await App.NavigateToPage(new ReportsPage());

    private async void OnLogOutButtonClicked(object sender, EventArgs e)
    {
        var result = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (result)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            await App.NavigateToPage(new LoginPage());
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
    }
}