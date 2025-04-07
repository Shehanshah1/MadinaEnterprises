namespace MadinaEnterprises.Modules.Views;

using MadinaEnterprises.Modules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

public partial class ContractsPage : ContentPage
{
    private readonly DatabaseService _db = App.DatabaseService!;
    private List<Contracts> contracts = new();
    private List<Ginners> ginners = new();
    private List<Mills> mills = new();

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
        ginnerPicker.ItemsSource = ginners.Select(g => g.GinnerName).ToList();
        millPicker.ItemsSource = mills.Select(m => m.MillName).ToList();
    }

    private void OnContractSelected(object sender, EventArgs e)
    {
        if (contractPicker.SelectedIndex == -1) return;

        var selected = contracts.FirstOrDefault(c => c.ContractID == contractPicker.SelectedItem?.ToString());
        if (selected == null) return;

        contractIDEntry.Text = selected.ContractID;
        ginnerPicker.SelectedIndex = ginners.FindIndex(g => g.GinnerID == selected.GinnerID);
        millPicker.SelectedIndex = mills.FindIndex(m => m.MillID == selected.MillID);
        totalBalesEntry.Text = selected.TotalBales.ToString();
        pricePerBatchEntry.Text = selected.PricePerBatch.ToString("F2");
        commissionEntry.Text = selected.CommissionPercentage.ToString("F2");
        contractDatePicker.Date = selected.DateCreated;
        deliveryNotesEditor.Text = selected.DeliveryNotes;
        paymentNotesEditor.Text = selected.PaymentNotes;
    }

    private async void OnSaveContractClicked(object sender, EventArgs e)
    {
        if (ginnerPicker.SelectedIndex == -1 || millPicker.SelectedIndex == -1)
        {
            await DisplayAlert("Validation Error", "Please select a Ginner and a Mill.", "OK");
            return;
        }

        try
        {
            var contract = new Contracts
            {
                ContractID = contractIDEntry.Text ?? "",
                GinnerID = ginners[ginnerPicker.SelectedIndex].GinnerID,
                MillID = mills[millPicker.SelectedIndex].MillID,
                TotalBales = int.Parse(totalBalesEntry.Text),
                PricePerBatch = double.Parse(pricePerBatchEntry.Text),
                CommissionPercentage = double.Parse(commissionEntry.Text),
                DateCreated = contractDatePicker.Date,
                DeliveryNotes = deliveryNotesEditor.Text ?? "",
                PaymentNotes = paymentNotesEditor.Text ?? ""
            };

            var exists = contracts.Any(c => c.ContractID == contract.ContractID);
            if (exists)
                await _db.UpdateContract(contract);
            else
                await _db.AddContract(contract);

            await DisplayAlert("Success", "Contract saved.", "OK");
            await LoadData();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to save contract. Details: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteContractClicked(object sender, EventArgs e)
    {
        if (contractPicker.SelectedIndex == -1) return;

        var contractID = contractPicker.SelectedItem?.ToString();
        if (!string.IsNullOrWhiteSpace(contractID))
        {
            await _db.DeleteContract(contractID);
            await DisplayAlert("Deleted", "Contract deleted.", "OK");
            await LoadData();
        }
    }

    //*****************************************************************************
    //                       NAVIGATION BUTTONS
    //*****************************************************************************
    private async void OnDashboardPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DashboardPage());
    private async void OnGinnersPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnersPage());
    private async void OnMillsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new MillsPage());
    private async void OnContractsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new ContractsPage());
    private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
    private async void OnPaymentsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new PaymentsPage());
    private async void OnLogOutButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new LoginPage());
}
