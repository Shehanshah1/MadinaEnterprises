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

    public ContractsPage()
    {
        InitializeComponent();
        _ = LoadData();
    }

    private async Task LoadData()
    {
        contracts = await _db.GetAllContracts();
        ginners = await _db.GetAllGinners();
        mills = await _db.GetAllMills();

        contractPicker.ItemsSource = contracts.Select(c => c.ContractID).ToList();
        ginnerPicker.ItemsSource = ginners.Select(g => $"{g.GinnerName} ({g.GinnerID})").ToList();
        millPicker.ItemsSource = mills.Select(m => $"{m.MillName} ({m.MillID})").ToList();

        foreach (var contract in contracts)
        {
            var ginner = ginners.FirstOrDefault(g => g.GinnerID == contract.GinnerID);
            if (ginner != null)
            {
                contract.GinnerName = ginner.GinnerName;
                contract.GinnerID = ginner.GinnerID;
            }
        }

        filteredContracts = new ObservableCollection<Contracts>(contracts);
        contractListView.ItemsSource = filteredContracts;
    }

    private void PopulateForm(Contracts c)
    {
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
        contractIDEntry.Text = "";
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
        if (ginnerPicker.SelectedIndex == -1 || millPicker.SelectedIndex == -1)
        {
            await DisplayAlert("Validation Error", "Please select a Ginner and a Mill.", "OK");
            return;
        }

        var contract = new Contracts
        {
            ContractID = contractIDEntry.Text ?? "",
            GinnerID = ginners[ginnerPicker.SelectedIndex].GinnerID,
            MillID = mills[millPicker.SelectedIndex].MillID,
            TotalBales = int.TryParse(totalBalesEntry.Text, out var bales) ? bales : 0,
            PricePerBatch = double.TryParse(pricePerBatchEntry.Text, out var price) ? price : 0,
            CommissionPercentage = double.TryParse(commissionEntry.Text, out var comm) ? comm : 0,
            DateCreated = contractDatePicker.Date,
            DeliveryNotes = deliveryNotesEditor.Text ?? "",
            PaymentNotes = paymentNotesEditor.Text ?? ""
        };

        if (contracts.Any(c => c.ContractID == contract.ContractID))
        {
            await DisplayAlert("Notice", "Contract already exists. Use Update to modify.", "OK");
            return;
        }

        await _db.AddContract(contract);
        await DisplayAlert("Success", "Contract saved.", "OK");
        ClearForm();
        await LoadData();
    }

    private async void OnUpdateContractClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(contractIDEntry.Text))
        {
            await DisplayAlert("Validation Error", "Contract ID is required to update.", "OK");
            return;
        }

        var contract = new Contracts
        {
            ContractID = contractIDEntry.Text ?? "",
            GinnerID = ginners[ginnerPicker.SelectedIndex].GinnerID,
            MillID = mills[millPicker.SelectedIndex].MillID,
            TotalBales = int.TryParse(totalBalesEntry.Text, out var bales) ? bales : 0,
            PricePerBatch = double.TryParse(pricePerBatchEntry.Text, out var price) ? price : 0,
            CommissionPercentage = double.TryParse(commissionEntry.Text, out var comm) ? comm : 0,
            DateCreated = contractDatePicker.Date,
            DeliveryNotes = deliveryNotesEditor.Text ?? "",
            PaymentNotes = paymentNotesEditor.Text ?? ""
        };

        await _db.UpdateContract(contract);
        await DisplayAlert("Updated", "Contract updated successfully.", "OK");
        ClearForm();
        await LoadData();
    }

    private async void OnDeleteContractClicked(object sender, EventArgs e)
    {
        if (contractPicker.SelectedItem is string id)
        {
            await _db.DeleteContract(id);
            await DisplayAlert("Deleted", "Contract deleted.", "OK");
            ClearForm();
            await LoadData();
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var search = e.NewTextValue?.ToLower() ?? "";
        contractListView.ItemsSource = new ObservableCollection<Contracts>(
            contracts.Where(c => c.ContractID.ToLower().Contains(search))
        );
    }

    private async void OnExportDocClicked(object sender, EventArgs e)
    {
        if (contractPicker.SelectedItem is not string selectedId) return;
        var contract = contracts.FirstOrDefault(c => c.ContractID == selectedId);
        if (contract == null) return;

        var ginner = ginners.FirstOrDefault(g => g.GinnerID == contract.GinnerID);
        var mill = mills.FirstOrDefault(m => m.MillID == contract.MillID);

        var path = ExportHelper.ExportContractToWord(contract, ginner!, mill!);
        await DisplayAlert("Export", $"DOCX export complete: {path}", "OK");
        ClearForm();
    }

    private async void OnExportExcelClicked(object sender, EventArgs e)
    {
        var path = ExportHelper.ExportAllContractsToExcel(contracts, ginners, mills);
        await DisplayAlert("Export", $"Excel exported: {path}", "OK");
        ClearForm();
    }

    // Navigation
    private async void OnDashboardPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DashboardPage());
    private async void OnGinnersPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnersPage());
    private async void OnMillsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new MillsPage());
    private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
    private async void OnPaymentsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new PaymentsPage());
    private async void OnLogOutButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new LoginPage());
}
