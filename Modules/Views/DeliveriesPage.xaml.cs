using MadinaEnterprises.Modules.Models;
using Microsoft.Maui.Controls;
using MadinaEnterprises.Modules.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MadinaEnterprises.Modules.Views;

public partial class DeliveriesPage : ContentPage
{
    private readonly DatabaseService _db = App.DatabaseService!;
    private List<Deliveries> deliveries = new();
    private List<Contracts> contracts = new();
    private ObservableCollection<Deliveries> filteredDeliveries = new();
    private bool _isSettingSuggestedAmount;
    private bool _isAmountManuallyEdited;


    public DeliveriesPage()
    {
        InitializeComponent();
        _ = LoadData();
    }

    private async Task LoadData()
    {
        deliveries = await _db.GetAllDeliveries();
        contracts = await _db.GetAllContracts();

        deliveryPicker.ItemsSource = deliveries.Select(d => d.DeliveryID).ToList();
        contractPicker.ItemsSource = contracts.Select(c => c.ContractID).ToList();

        filteredDeliveries = new ObservableCollection<Deliveries>(deliveries);
        deliveryListView.ItemsSource = filteredDeliveries;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var search = e.NewTextValue?.ToLower() ?? "";
        deliveryListView.ItemsSource = new ObservableCollection<Deliveries>(
            deliveries.Where(d => d.DeliveryID.ToLower().Contains(search))
        );
    }

    private void OnDeliveryListSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Deliveries selected)
        {
            deliveryPicker.SelectedItem = selected.DeliveryID;
            PopulateForm(selected);
        }
    }


    private void PopulateForm(Deliveries d)
    {
        deliveryIDEntry.Text = d.DeliveryID;
        contractPicker.SelectedIndex = contracts.FindIndex(c => c.ContractID == d.ContractID);
        _isSettingSuggestedAmount = true;
        amountEntry.Text = d.Amount.ToString("F2");
        _isSettingSuggestedAmount = false;
        _isAmountManuallyEdited = true;
        totalBalesEntry.Text = d.TotalBales.ToString();
        factoryWeightEntry.Text = d.FactoryWeight.ToString("F2");
        millWeightEntry.Text = d.MillWeight.ToString("F2");
        truckNoEntry.Text = d.TruckNumber;
        driverContactEntry.Text = d.DriverContact;
        departureDatePicker.Date = d.DepartureDate;
        deliveryDatePicker.Date = d.DeliveryDate;
    }

    private void ClearForm()
    {
        deliveryIDEntry.Text = "";
        contractPicker.SelectedIndex = -1;
        amountEntry.Text = "";
        totalBalesEntry.Text = "";
        factoryWeightEntry.Text = "";
        millWeightEntry.Text = "";
        truckNoEntry.Text = "";
        driverContactEntry.Text = "";
        departureDatePicker.Date = DateTime.Today;
        deliveryDatePicker.Date = DateTime.Today;
        deliveryPicker.SelectedIndex = -1;
        _isAmountManuallyEdited = false;
    }

    private void OnDeliverySelected(object sender, EventArgs e)
    {
        if (deliveryPicker.SelectedIndex == -1) return;

        var selected = deliveries.FirstOrDefault(d => d.DeliveryID == deliveryPicker.SelectedItem?.ToString());
        if (selected != null)
            PopulateForm(selected);
    }

    private void OnContractChanged(object sender, EventArgs e)
    {
        _isAmountManuallyEdited = false;
        RecalculateActualAmount();
    }
    private void OnMillWeightChanged(object sender, TextChangedEventArgs e) => RecalculateActualAmount();

    private void OnAmountChanged(object sender, TextChangedEventArgs e)
    {
        if (_isSettingSuggestedAmount)
        {
            return;
        }

        _isAmountManuallyEdited = true;
    }

    private void RecalculateActualAmount()
    {
        if (_isAmountManuallyEdited)
        {
            return;
        }

        if (contractPicker.SelectedIndex < 0 || contractPicker.SelectedIndex >= contracts.Count)
        {
            _isSettingSuggestedAmount = true;
            amountEntry.Text = "0.00";
            _isSettingSuggestedAmount = false;
            return;
        }

        if (!RateCalculation.TryParseDouble(millWeightEntry.Text, out var millWeightKg))
        {
            _isSettingSuggestedAmount = true;
            amountEntry.Text = "0.00";
            _isSettingSuggestedAmount = false;
            return;
        }

        var ratePerMaund = contracts[contractPicker.SelectedIndex].PricePerBatch;
        var actualAmount = RateCalculation.AmountFromKg(millWeightKg, ratePerMaund);

        _isSettingSuggestedAmount = true;
        amountEntry.Text = actualAmount.ToString("F2");
        _isSettingSuggestedAmount = false;
    }

    private async void OnSaveDeliveryClicked(object sender, EventArgs e)
    {
        if (contractPicker.SelectedIndex == -1)
        {
            await DisplayAlert("Validation Error", "Please select a Contract.", "OK");
            return;
        }

        RecalculateActualAmount();

        var delivery = new Deliveries
        {
            DeliveryID = deliveryIDEntry.Text ?? "",
            ContractID = contracts[contractPicker.SelectedIndex].ContractID,
            Amount = RateCalculation.TryParseDouble(amountEntry.Text, out var amt) ? amt : 0,
            TotalBales = int.TryParse(totalBalesEntry.Text, out var bales) ? bales : 0,
            FactoryWeight = RateCalculation.TryParseDouble(factoryWeightEntry.Text, out var fw) ? fw : 0,
            MillWeight = RateCalculation.TryParseDouble(millWeightEntry.Text, out var mw) ? mw : 0,
            TruckNumber = truckNoEntry.Text ?? "",
            DriverContact = driverContactEntry.Text ?? "",
            DepartureDate = departureDatePicker.Date,
            DeliveryDate = deliveryDatePicker.Date
        };

        if (deliveries.Any(d => d.DeliveryID == delivery.DeliveryID))
        {
            await DisplayAlert("Duplicate", "Delivery ID already exists. Use Update to modify.", "OK");
            return;
        }

        await _db.AddDelivery(delivery);
        await DisplayAlert("Success", "Delivery added.", "OK");
        ClearForm();
        await LoadData();
    }

    private async void OnUpdateDeliveryClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(deliveryIDEntry.Text))
        {
            await DisplayAlert("Validation", "Delivery ID is required.", "OK");
            return;
        }

        RecalculateActualAmount();

        var delivery = new Deliveries
        {
            DeliveryID = deliveryIDEntry.Text ?? "",
            ContractID = contracts[contractPicker.SelectedIndex].ContractID,
            Amount = RateCalculation.TryParseDouble(amountEntry.Text, out var amt) ? amt : 0,
            TotalBales = int.TryParse(totalBalesEntry.Text, out var bales) ? bales : 0,
            FactoryWeight = RateCalculation.TryParseDouble(factoryWeightEntry.Text, out var fw) ? fw : 0,
            MillWeight = RateCalculation.TryParseDouble(millWeightEntry.Text, out var mw) ? mw : 0,
            TruckNumber = truckNoEntry.Text ?? "",
            DriverContact = driverContactEntry.Text ?? "",
            DepartureDate = departureDatePicker.Date,
            DeliveryDate = deliveryDatePicker.Date
        };

        await _db.UpdateDelivery(delivery);
        await DisplayAlert("Updated", "Delivery updated successfully.", "OK");
        ClearForm();
        await LoadData();
    }

    private async void OnDeleteDeliveryClicked(object sender, EventArgs e)
    {
        if (deliveryPicker.SelectedItem is not string id) return;

        await _db.DeleteDelivery(id);
        await DisplayAlert("Deleted", "Delivery deleted.", "OK");
        ClearForm();
        await LoadData();
    }

    // Navigation
    private async void OnDashboardPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DashboardPage());
    private async void OnGinnersPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnersPage());
    private async void OnMillsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new MillsPage());
    private async void OnContractsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new ContractsPage());
    private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
    private async void OnPaymentsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new PaymentsPage());
    private async void OnGinnerLedgerPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnerLedgerPage());
    private void OnLogOutButtonClicked(object sender, EventArgs e) => App.Logout();
}
