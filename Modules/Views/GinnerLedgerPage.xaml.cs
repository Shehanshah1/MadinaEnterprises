using MadinaEnterprises.Modules.Models;
using MadinaEnterprises.Modules.Util;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MadinaEnterprises.Modules.Views;

public partial class GinnerLedgerPage : ContentPage
{
    private readonly DatabaseService _db = App.DatabaseService!;
    private List<GinnerLedger> allLedgerEntries = new();
    private ObservableCollection<GinnerLedger> filteredLedgerEntries = new();
    private List<Contracts> contracts = new();
    private List<Mills> mills = new();
    private GinnerLedger? selectedEntry = null;

    public GinnerLedgerPage()
    {
        InitializeComponent();
        _ = LoadData();
    }

    private async Task LoadData()
    {
        try
        {
            // Load all data
            allLedgerEntries = await _db.GetAllGinnerLedger();
            contracts = await _db.GetAllContracts();
            mills = await _db.GetAllMills();

            // Setup pickers
            contractPicker.ItemsSource = contracts.Select(c => $"{c.ContractID} - {c.GinnerID}").ToList();
            filterPicker.ItemsSource = contracts.Select(c => c.ContractID).ToList();
            filterPicker.Items.Insert(0, "All Contracts");
            filterPicker.SelectedIndex = 0;

            millsDueToEntry.ItemsSource = mills.Select(m => $"{m.MillName} ({m.MillID})").ToList();

            // Update summary cards
            UpdateSummaryCards();

            // Display ledger entries
            filteredLedgerEntries = new ObservableCollection<GinnerLedger>(allLedgerEntries);
            ledgerListView.ItemsSource = filteredLedgerEntries;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load data: {ex.Message}", "OK");
        }
    }

    private void UpdateSummaryCards()
    {
        var uniqueContracts = allLedgerEntries.Select(l => l.ContractID).Distinct().Count();
        var totalPaid = allLedgerEntries.Sum(l => l.AmountPaid);
        var pendingDeals = allLedgerEntries.Where(l => string.IsNullOrEmpty(l.MillsDueTo)).Count();

        totalContractsLabel.Text = uniqueContracts.ToString();
        totalAmountPaidLabel.Text = $"${totalPaid:F2}";
        pendingDealsLabel.Text = pendingDeals.ToString();
    }

    private async void OnContractSelected(object sender, EventArgs e)
    {
        if (contractPicker.SelectedIndex == -1)
        {
            contractDetailsFrame.IsVisible = false;
            return;
        }

        try
        {
            var selectedContractId = contracts[contractPicker.SelectedIndex].ContractID;
            var contract = await _db.GetContractById(selectedContractId);

            if (contract != null)
            {
                var ginner = await _db.GetGinnerById(contract.GinnerID);
                var mill = await _db.GetMillById(contract.MillID);

                contractDetailsLabel.Text = $"Ginner: {ginner?.GinnerName ?? "N/A"}\n" +
                                          $"Mill: {mill?.MillName ?? "N/A"}\n" +
                                          $"Total Bales: {contract.TotalBales}\n" +
                                          $"Total Amount: ${contract.TotalAmount:F2}\n" +
                                          $"Commission: {contract.CommissionPercentage}%";

                contractDetailsFrame.IsVisible = true;

                // Auto-generate Deal ID if empty
                if (string.IsNullOrWhiteSpace(dealIDEntry.Text))
                {
                    dealIDEntry.Text = GenerateDealID(selectedContractId);
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load contract details: {ex.Message}", "OK");
        }
    }

    private string GenerateDealID(string contractId)
    {
        var existingDeals = allLedgerEntries.Where(l => l.ContractID == contractId).Count();
        return $"DEAL-{contractId}-{existingDeals + 1:D3}";
    }

    private void OnAmountChanged(object sender, TextChangedEventArgs e)
    {
        // Validate and format amount as user types
        if (!string.IsNullOrWhiteSpace(e.NewTextValue))
        {
            if (!double.TryParse(e.NewTextValue, out _))
            {
                amountPaidEntry.Text = e.OldTextValue;
            }
        }
    }

    private bool ValidateForm()
    {
        if (contractPicker.SelectedIndex == -1)
        {
            DisplayAlert("Validation Error", "Please select a contract.", "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(dealIDEntry.Text))
        {
            DisplayAlert("Validation Error", "Deal ID is required.", "OK");
            return false;
        }

        if (!double.TryParse(amountPaidEntry.Text, out var amount) || amount <= 0)
        {
            DisplayAlert("Validation Error", "Please enter a valid amount paid.", "OK");
            return false;
        }

        if (millsDueToEntry.SelectedIndex == -1)
        {
            DisplayAlert("Validation Error", "Please select a mill.", "OK");
            return false;
        }

        return true;
    }

    private async void OnSaveEntryClicked(object sender, EventArgs e)
    {
        if (!ValidateForm()) return;

        try
        {
            var contractId = contracts[contractPicker.SelectedIndex].ContractID;
            var millId = mills[millsDueToEntry.SelectedIndex].MillID;

            // Check if entry already exists
            var existing = allLedgerEntries.FirstOrDefault(l =>
                l.ContractID == contractId && l.DealID == dealIDEntry.Text);

            if (existing != null)
            {
                await DisplayAlert("Duplicate Entry",
                    "An entry with this Contract ID and Deal ID already exists. Use Update instead.", "OK");
                return;
            }

            var entry = new GinnerLedger
            {
                ContractID = contractId,
                DealID = dealIDEntry.Text,
                AmountPaid = double.Parse(amountPaidEntry.Text),
                DatePaid = datePaidPicker.Date,
                MillsDueTo = millId
            };

            await _db.AddGinnerLedger(entry);
            await DisplayAlert("Success", "Ledger entry saved successfully.", "OK");
            ClearForm();
            await LoadData();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save entry: {ex.Message}", "OK");
        }
    }

    private async void OnUpdateEntryClicked(object sender, EventArgs e)
    {
        if (!ValidateForm()) return;

        if (selectedEntry == null)
        {
            await DisplayAlert("Error", "Please select an entry to update.", "OK");
            return;
        }

        try
        {
            var contractId = contracts[contractPicker.SelectedIndex].ContractID;
            var millId = mills[millsDueToEntry.SelectedIndex].MillID;

            var entry = new GinnerLedger
            {
                ContractID = contractId,
                DealID = dealIDEntry.Text,
                AmountPaid = double.Parse(amountPaidEntry.Text),
                DatePaid = datePaidPicker.Date,
                MillsDueTo = millId
            };

            await _db.UpdateGinnerLedger(entry);
            await DisplayAlert("Success", "Ledger entry updated successfully.", "OK");
            ClearForm();
            await LoadData();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to update entry: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteEntryClicked(object sender, EventArgs e)
    {
        if (selectedEntry == null)
        {
            await DisplayAlert("Error", "Please select an entry to delete.", "OK");
            return;
        }

        var result = await DisplayAlert("Confirm Delete",
            $"Are you sure you want to delete this ledger entry?\nContract: {selectedEntry.ContractID}\nDeal: {selectedEntry.DealID}",
            "Delete", "Cancel");

        if (!result) return;

        try
        {
            await _db.DeleteGinnerLedger(selectedEntry.ContractID, selectedEntry.DealID);
            await DisplayAlert("Success", "Ledger entry deleted successfully.", "OK");
            ClearForm();
            await LoadData();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to delete entry: {ex.Message}", "OK");
        }
    }

    private void OnClearClicked(object sender, EventArgs e)
    {
        ClearForm();
    }

    private void ClearForm()
    {
        selectedEntry = null;
        contractPicker.SelectedIndex = -1;
        dealIDEntry.Text = "";
        amountPaidEntry.Text = "";
        millsDueToEntry.SelectedIndex = -1;
        datePaidPicker.Date = DateTime.Today;
        contractDetailsFrame.IsVisible = false;
        ledgerListView.SelectedItem = null;
    }

    private void OnLedgerEntrySelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is GinnerLedger entry)
        {
            selectedEntry = entry;
            PopulateForm(entry);
        }
    }

    private void PopulateForm(GinnerLedger entry)
    {
        // Find indices
        var contractIndex = contracts.FindIndex(c => c.ContractID == entry.ContractID);
        var millIndex = mills.FindIndex(m => m.MillID == entry.MillsDueTo);

        if (contractIndex >= 0)
            contractPicker.SelectedIndex = contractIndex;

        dealIDEntry.Text = entry.DealID;
        amountPaidEntry.Text = entry.AmountPaid.ToString("F2");

        if (millIndex >= 0)
            millsDueToEntry.SelectedIndex = millIndex;

        datePaidPicker.Date = entry.DatePaid;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        FilterLedgerEntries();
    }

    private void OnFilterChanged(object sender, EventArgs e)
    {
        FilterLedgerEntries();
    }

    private void FilterLedgerEntries()
    {
        var searchText = searchBar.Text?.ToLower() ?? "";
        var filtered = allLedgerEntries.AsEnumerable();

        // Apply contract filter
        if (filterPicker.SelectedIndex > 0 && filterPicker.SelectedItem != null)
        {
            var selectedContract = filterPicker.SelectedItem.ToString();
            filtered = filtered.Where(l => l.ContractID == selectedContract);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            filtered = filtered.Where(l =>
                l.ContractID.ToLower().Contains(searchText) ||
                l.DealID.ToLower().Contains(searchText) ||
                l.MillsDueTo.ToLower().Contains(searchText));
        }

        ledgerListView.ItemsSource = new ObservableCollection<GinnerLedger>(filtered);
    }

    private void OnResetFilterClicked(object sender, EventArgs e)
    {
        searchBar.Text = "";
        filterPicker.SelectedIndex = 0;
        ledgerListView.ItemsSource = new ObservableCollection<GinnerLedger>(allLedgerEntries);
    }

    private async void OnGenerateReportClicked(object sender, EventArgs e)
    {
        try
        {
            var report = "GINNER LEDGER SUMMARY REPORT\n";
            report += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}\n";
            report += "=" + new string('=', 50) + "\n\n";

            // Group by contract
            var groupedByContract = allLedgerEntries.GroupBy(l => l.ContractID);

            foreach (var group in groupedByContract)
            {
                var contract = await _db.GetContractById(group.Key);
                var ginner = contract != null ? await _db.GetGinnerById(contract.GinnerID) : null;

                report += $"Contract: {group.Key}\n";
                report += $"Ginner: {ginner?.GinnerName ?? "N/A"}\n";
                report += $"Total Deals: {group.Count()}\n";
                report += $"Total Paid: ${group.Sum(l => l.AmountPaid):F2}\n";
                report += "-" + new string('-', 30) + "\n";

                foreach (var entry in group.OrderBy(l => l.DatePaid))
                {
                    report += $"  Deal: {entry.DealID} | ${entry.AmountPaid:F2} | {entry.DatePaid:MM/dd/yyyy}\n";
                }
                report += "\n";
            }

            report += "=" + new string('=', 50) + "\n";
            report += $"GRAND TOTAL: ${allLedgerEntries.Sum(l => l.AmountPaid):F2}\n";
            report += $"Total Contracts: {groupedByContract.Count()}\n";
            report += $"Total Deals: {allLedgerEntries.Count}\n";

            await DisplayAlert("Summary Report", report, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to generate report: {ex.Message}", "OK");
        }
    }

    // Navigation methods
    private async void OnDashboardPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DashboardPage());
    private async void OnGinnersPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnersPage());
    private async void OnMillsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new MillsPage());
    private async void OnContractsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new ContractsPage());
    private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
    private async void OnPaymentsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new PaymentsPage());
    private async void OnLogOutButtonClicked(object sender, EventArgs e)
    {
        var result = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (result)
            await App.NavigateToPage(new LoginPage());
    }
}