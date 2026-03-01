using MadinaEnterprises.Modules.Models;
using MadinaEnterprises.Modules.Util;
using System.Collections.ObjectModel;

namespace MadinaEnterprises.Modules.Views;

public partial class GinnerLedgerPage : ContentPage
{
    private readonly DatabaseService _db = App.DatabaseService!;
    private List<GinnerLedger> ledgerEntries = new();
    private List<Contracts> contracts = new();
    private ObservableCollection<GinnerLedger> filteredEntries = new();

    public GinnerLedgerPage()
    {
        InitializeComponent();
        _ = LoadData();
    }

    private async Task LoadData()
    {
        ledgerEntries = await _db.GetAllGinnerLedger();
        contracts = await _db.GetAllContracts();

        contractPicker.ItemsSource = contracts.Select(c => c.ContractID).ToList();

        filteredEntries = new ObservableCollection<GinnerLedger>(ledgerEntries);
        ledgerListView.ItemsSource = filteredEntries;
    }

    private void PopulateForm(GinnerLedger entry)
    {
        contractPicker.SelectedItem = entry.ContractID;
        dealIDEntry.Text = entry.DealID;
        amountPaidEntry.Text = entry.AmountPaid.ToString("F2");
        datePaidPicker.Date = entry.DatePaid;
        millsDueToEntry.Text = entry.MillsDueTo;
    }

    private void ClearForm()
    {
        contractPicker.SelectedIndex = -1;
        dealIDEntry.Text = "";
        amountPaidEntry.Text = "";
        datePaidPicker.Date = DateTime.Today;
        millsDueToEntry.Text = "";
        ledgerListView.SelectedItem = null;
    }

    private void OnLedgerListSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is GinnerLedger selected)
        {
            PopulateForm(selected);
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (contractPicker.SelectedIndex == -1 || string.IsNullOrWhiteSpace(dealIDEntry.Text))
        {
            await DisplayAlert("Validation Error", "Contract and Deal ID are required.", "OK");
            return;
        }

        var entry = new GinnerLedger
        {
            ContractID = contractPicker.SelectedItem?.ToString() ?? "",
            DealID = dealIDEntry.Text,
            AmountPaid = RateCalculation.TryParseDouble(amountPaidEntry.Text, out var amt) ? amt : 0,
            DatePaid = datePaidPicker.Date,
            MillsDueTo = millsDueToEntry.Text ?? ""
        };

        if (ledgerEntries.Any(l => l.ContractID == entry.ContractID && l.DealID == entry.DealID))
        {
            await DisplayAlert("Duplicate", "This Contract/Deal combination already exists. Use Update.", "OK");
            return;
        }

        await _db.AddGinnerLedger(entry);
        await DisplayAlert("Success", "Ledger entry saved.", "OK");
        ClearForm();
        await LoadData();
    }

    private async void OnUpdateClicked(object sender, EventArgs e)
    {
        if (contractPicker.SelectedIndex == -1 || string.IsNullOrWhiteSpace(dealIDEntry.Text))
        {
            await DisplayAlert("Validation Error", "Contract and Deal ID are required.", "OK");
            return;
        }

        var entry = new GinnerLedger
        {
            ContractID = contractPicker.SelectedItem?.ToString() ?? "",
            DealID = dealIDEntry.Text,
            AmountPaid = RateCalculation.TryParseDouble(amountPaidEntry.Text, out var amt) ? amt : 0,
            DatePaid = datePaidPicker.Date,
            MillsDueTo = millsDueToEntry.Text ?? ""
        };

        await _db.UpdateGinnerLedger(entry);
        await DisplayAlert("Updated", "Ledger entry updated.", "OK");
        ClearForm();
        await LoadData();
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        if (contractPicker.SelectedIndex == -1 || string.IsNullOrWhiteSpace(dealIDEntry.Text))
        {
            await DisplayAlert("Validation Error", "Select a ledger entry to delete.", "OK");
            return;
        }

        var contractId = contractPicker.SelectedItem?.ToString() ?? "";
        var dealId = dealIDEntry.Text;

        bool confirm = await DisplayAlert("Confirm", $"Delete ledger entry {contractId}/{dealId}?", "Yes", "No");
        if (!confirm) return;

        await _db.DeleteGinnerLedger(contractId, dealId);
        await DisplayAlert("Deleted", "Ledger entry deleted.", "OK");
        ClearForm();
        await LoadData();
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = (e.NewTextValue ?? "").ToLowerInvariant();
        ledgerListView.ItemsSource = new ObservableCollection<GinnerLedger>(
            ledgerEntries.Where(l =>
                l.ContractID.ToLowerInvariant().Contains(query) ||
                l.DealID.ToLowerInvariant().Contains(query))
        );
    }

    // Navigation
    private async void OnDashboardPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DashboardPage());
    private async void OnGinnersPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnersPage());
    private async void OnMillsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new MillsPage());
    private async void OnContractsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new ContractsPage());
    private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
    private async void OnPaymentsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new PaymentsPage());
    private void OnLogOutButtonClicked(object sender, EventArgs e) => App.Logout();
}
