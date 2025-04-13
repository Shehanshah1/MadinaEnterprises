using MadinaEnterprises.Modules.Models;
using System.Linq;
using Microsoft.Maui.Controls;

namespace MadinaEnterprises.Modules.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DatabaseService _db = App.DatabaseService!;

    public DashboardPage()
    {
        InitializeComponent();
        _ = LoadDashboardData();
    }

    private async Task LoadDashboardData()
    {
        try
        {
            var contracts = await _db.GetAllContracts();
            var payments = await _db.GetAllPayments();
            var ginners = await _db.GetAllGinners();
            var mills = await _db.GetAllMills();

            double totalCommission = contracts.Sum(c => c.TotalAmount * (c.CommissionPercentage / 100));
            double totalPaid = payments.Sum(p => p.AmountPaid);
            double totalDue = payments.Sum(p => p.TotalAmount - p.AmountPaid);
            int balesSold = contracts.Sum(c => c.TotalBales);
            int ginnerCount = ginners.Count;
            int millCount = mills.Count;
            double avgCommission = contracts.Any() ? contracts.Average(c => c.CommissionPercentage) : 0;

            TotalCommissionLabel.Text = $"${totalCommission:F2}";
            PaymentMadeLabel.Text = $"${totalPaid:F2}";
            PaymentDueLabel.Text = $"${totalDue:F2}";
            BalesSoldLabel.Text = balesSold.ToString();
            TotalGinnersLabel.Text = ginnerCount.ToString();
            TotalMillsLabel.Text = millCount.ToString();
            AvgCommissionRateLabel.Text = $"{avgCommission:F2}%";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load dashboard data: {ex.Message}", "OK");
        }
    }

    // Navigation
    private async void OnGinnersPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnersPage());
    private async void OnMillsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new MillsPage());
    private async void OnContractsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new ContractsPage());
    private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
    private async void OnPaymentsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new PaymentsPage());
    private async void OnLogOutButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new LoginPage());
}
