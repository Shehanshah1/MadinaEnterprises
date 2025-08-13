using MadinaEnterprises.Modules.Models;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;
using MadinaEnterprises.Modules.Util;

namespace MadinaEnterprises.Modules.Views;

public partial class ContractsPage : ContentPage
{
    private readonly DatabaseService _db = App.DatabaseService!;
    private List<Contracts> contracts = new();
    private List<Ginners> ginners = new();
    private List<Mills> mills = new();
    private ObservableCollection<Contracts> filteredContracts = new();
    private Contracts? selectedContract = null;

    public ContractsPage()
    {
        InitializeComponent();
        _ = LoadData();
        SetupEventHandlers();
    }

    private void SetupEventHandlers()
    {
        // Auto-calculate total amount when bales or price changes
        totalBalesEntry.TextChanged += (s, e) => CalculateTotalAmount();
        pricePerBatchEntry.TextChanged += (s, e) => CalculateTotalAmount();
    }

    private void CalculateTotalAmount()
    {
        if (int.TryParse(totalBalesEntry.Text, out var bales) &&
            double.TryParse(pricePerBatchEntry.Text, out var price))
        {
            var total = bales * price;
            // Update a label to show calculated total (you'll need to add this to XAML)
            Device.BeginInvokeOnMainThread(() =>
            {
                // You can add a label in XAML to show this, or just use it internally
                // totalAmountLabel.Text = $"Total: ${total:F2}";
            });
        }
    }

    private async Task LoadData()
    {
        try
        {
            contracts = await _db.GetAllContracts();
            ginners = await _db.GetAllGinners();
            mills = await _db.GetAllMills();

            // Setup pickers
            contractPicker.ItemsSource = contracts.Select(c => c.ContractID).ToList();
            ginnerPicker.ItemsSource = ginners.Select(g => $"{g.GinnerName} ({g.GinnerID})").ToList();
            millPicker.ItemsSource = mills.Select(m => $"{m.MillName} ({m.MillID})").ToList();

            // Populate GinnerName for display
            foreach (var contract in contracts)
            {
                var ginner = ginners.FirstOrDefault(g => g.GinnerID == contract.GinnerID);
                if (ginner != null)
                {
                    contract.GinnerName = ginner.GinnerName;
                }
            }

            filteredContracts = new ObservableCollection<Contracts>(contracts);
            contractListView.ItemsSource = filteredContracts;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load data: {ex.Message}", "OK");
        }
    }

    private void PopulateForm(Contracts c)
    {
        selectedContract = c;
        contractIDEntry.Text = c.ContractID;
        ginnerPicker.SelectedIndex = ginners.FindIndex(g => g.GinnerID == c.GinnerID);
        millPicker.SelectedIndex = mills.FindIndex(m => m.MillID == c.MillID);
        totalBalesEntry.Text = c.TotalBales.ToString();
        pricePerBatchEntry.Text = c.PricePerBatch.ToString("F2");
        commissionEntry.Text = c.CommissionPercentage.ToString("F2");
        contractDatePicker.Date = c.DateCreated;
        deliveryNotesEditor.Text = c.DeliveryNotes;
        paymentNotesEditor.Text = c.PaymentNotes;
    }

    private void ClearForm()
    {
        selectedContract = null;
        contractIDEntry.Text = GenerateContractID();
        ginnerPicker.SelectedIndex = -1;
        millPicker.SelectedIndex = -1;
        totalBalesEntry.Text = "";
        pricePerBatchEntry.Text = "";
        commissionEntry.Text = "";
        contractDatePicker.Date = DateTime.Today;
        deliveryNotesEditor.Text = "";
        paymentNotesEditor.Text = "";
        contractPicker.SelectedItem = null;
        contractListView.SelectedItem = null;
    }

    private string GenerateContractID()
    {
        // Generate a unique contract ID
        return $"CTR-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
    }

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(contractIDEntry.Text))
        {
            DisplayAlert("Validation Error", "Contract ID is required.", "OK");
            return false;
        }

        if (ginnerPicker.SelectedIndex == -1)
        {
            DisplayAlert("Validation Error", "Please select a Ginner.", "OK");
            return false;
        }

        if (millPicker.SelectedIndex == -1)
        {
            DisplayAlert("Validation Error", "Please select a Mill.", "OK");
            return false;
        }

        if (!int.TryParse(totalBalesEntry.Text, out var bales) || bales <= 0)
        {
            DisplayAlert("Validation Error", "Please enter a valid number of bales.", "OK");
            return false;
        }

        if (!double.TryParse(pricePerBatchEntry.Text, out var price) || price <= 0)
        {
            DisplayAlert("Validation Error", "Please enter a valid price per batch.", "OK");
            return false;
        }

        if (!double.TryParse(commissionEntry.Text, out var commission) || commission < 0 || commission > 100)
        {
            DisplayAlert("Validation Error", "Commission must be between 0 and 100.", "OK");
            return false;
        }

        return true;
    }

    private void OnContractSelected(object sender, EventArgs e)
    {
        if (contractPicker.SelectedIndex == -1) return;

        var selected = contracts.FirstOrDefault(c => c.ContractID == contractPicker.SelectedItem?.ToString());
        if (selected != null)
            PopulateForm(selected);
    }

    private void OnContractListSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Contracts selected)
        {
            contractPicker.SelectedItem = selected.ContractID;
            PopulateForm(selected);
        }
    }

    private async void OnSaveContractClicked(object sender, EventArgs e)
    {
        if (!ValidateForm()) return;

        try
        {
            // Calculate total amount
            var bales = int.Parse(totalBalesEntry.Text);
            var price = double.Parse(pricePerBatchEntry.Text);
            var totalAmount = bales * price;

            var contract = new Contracts
            {
                ContractID = contractIDEntry.Text,
                GinnerID = ginners[ginnerPicker.SelectedIndex].GinnerID,
                MillID = mills[millPicker.SelectedIndex].MillID,
                TotalBales = bales,
                PricePerBatch = price,
                TotalAmount = totalAmount,
                CommissionPercentage = double.Parse(commissionEntry.Text),
                DateCreated = contractDatePicker.Date,
                DeliveryNotes = deliveryNotesEditor.Text ?? "",
                PaymentNotes = paymentNotesEditor.Text ?? ""
            };

            // Check if contract already exists
            if (contracts.Any(c => c.ContractID == contract.ContractID))
            {
                var result = await DisplayAlert("Duplicate Contract",
                    "A contract with this ID already exists. Would you like to update it instead?",
                    "Update", "Cancel");

                if (result)
                {
                    await _db.UpdateContract(contract);
                    await DisplayAlert("Success", "Contract updated successfully.", "OK");
                }
                return;
            }

            await _db.AddContract(contract);
            await DisplayAlert("Success", $"Contract saved successfully.\nTotal Amount: ${totalAmount:F2}", "OK");
            ClearForm();
            await LoadData();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save contract: {ex.Message}", "OK");
        }
    }

    private async void OnUpdateContractClicked(object sender, EventArgs e)
    {
        if (!ValidateForm()) return;

        try
        {
            var bales = int.Parse(totalBalesEntry.Text);
            var price = double.Parse(pricePerBatchEntry.Text);
            var totalAmount = bales * price;

            var contract = new Contracts
            {
                ContractID = contractIDEntry.Text,
                GinnerID = ginners[ginnerPicker.SelectedIndex].GinnerID,
                MillID = mills[millPicker.SelectedIndex].MillID,
                TotalBales = bales,
                PricePerBatch = price,
                TotalAmount = totalAmount,
                CommissionPercentage = double.Parse(commissionEntry.Text),
                DateCreated = contractDatePicker.Date,
                DeliveryNotes = deliveryNotesEditor.Text ?? "",
                PaymentNotes = paymentNotesEditor.Text ?? ""
            };

            await _db.UpdateContract(contract);
            await DisplayAlert("Updated", $"Contract updated successfully.\nNew Total: ${totalAmount:F2}", "OK");
            ClearForm();
            await LoadData();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to update contract: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteContractClicked(object sender, EventArgs e)
    {
        if (contractPicker.SelectedItem is not string id)
        {
            await DisplayAlert("Error", "Please select a contract to delete.", "OK");
            return;
        }

        var result = await DisplayAlert("Confirm Delete",
            $"Are you sure you want to delete contract {id}?\nThis will also delete all related deliveries and payments.",
            "Delete", "Cancel");

        if (!result) return;

        try
        {
            await _db.DeleteContract(id);
            await DisplayAlert("Deleted", "Contract and all related records deleted.", "OK");
            ClearForm();
            await LoadData();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to delete contract: {ex.Message}", "OK");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var search = e.NewTextValue?.ToLower() ?? "";

        if (string.IsNullOrWhiteSpace(search))
        {
            contractListView.ItemsSource = new ObservableCollection<Contracts>(contracts);
        }
        else
        {
            var filtered = contracts.Where(c =>
                c.ContractID.ToLower().Contains(search) ||
                (c.GinnerName?.ToLower().Contains(search) ?? false) ||
                c.GinnerID.ToLower().Contains(search)
            );
            contractListView.ItemsSource = new ObservableCollection<Contracts>(filtered);
        }
    }

    private async void OnExportDocClicked(object sender, EventArgs e)
    {
        if (contractPicker.SelectedItem is not string selectedId)
        {
            await DisplayAlert("Error", "Please select a contract to export.", "OK");
            return;
        }

        try
        {
            var contract = contracts.FirstOrDefault(c => c.ContractID == selectedId);
            if (contract == null) return;

            var ginner = ginners.FirstOrDefault(g => g.GinnerID == contract.GinnerID);
            var mill = mills.FirstOrDefault(m => m.MillID == contract.MillID);

            if (ginner == null || mill == null)
            {
                await DisplayAlert("Error", "Cannot export: Missing ginner or mill information.", "OK");
                return;
            }

            var path = ExportHelper.ExportContractToWord(contract, ginner, mill);
            await DisplayAlert("Export Complete", $"Document saved to:\n{path}", "OK");
            ClearForm();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Export failed: {ex.Message}", "OK");
        }
    }

    private async void OnExportExcelClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await DisplayAlert("Export All Data",
                "This will export all contracts, deliveries, and payments to Excel. Continue?",
                "Export", "Cancel");

            if (!result) return;

            var path = ExportHelper.ExportAllContractsToExcel(contracts, ginners, mills);
            await DisplayAlert("Export Complete", $"Excel file saved to:\n{path}", "OK");
            ClearForm();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Export failed: {ex.Message}", "OK");
        }
    }

    // Navigation methods
    private async void OnDashboardPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DashboardPage());
    private async void OnGinnerLedgerPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnerLedgerPage());
    private async void OnGinnersPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnersPage());
    private async void OnMillsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new MillsPage());
    private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
    private async void OnPaymentsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new PaymentsPage());
    private async void OnLogOutButtonClicked(object sender, EventArgs e)
    {
        var result = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
        if (result)
            await App.NavigateToPage(new LoginPage());
    }
}