using MadinaEnterprises.Modules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace MadinaEnterprises.Modules.Views
{
    public partial class DeliveriesPage : ContentPage
    {
        private readonly DatabaseService _db = App.DatabaseService!;
        private List<Deliveries> deliveries = new();
        private List<Contracts> contracts = new();

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
        }

        private void OnDeliverySelected(object sender, EventArgs e)
        {
            if (deliveryPicker.SelectedIndex == -1) return;

            var selected = deliveries.FirstOrDefault(d => d.DeliveryID == deliveryPicker.SelectedItem?.ToString());
            if (selected == null) return;

            deliveryIDEntry.Text = selected.DeliveryID;
            contractPicker.SelectedIndex = contracts.FindIndex(c => c.ContractID == selected.ContractID);
            amountEntry.Text = selected.Amount.ToString("F2");
            totalBalesEntry.Text = selected.TotalBales.ToString();
            factoryWeightEntry.Text = selected.FactoryWeight.ToString("F2");
            millWeightEntry.Text = selected.MillWeight.ToString("F2");
            truckNoEntry.Text = selected.TruckNumber;
            driverContactEntry.Text = selected.DriverContact;
            departureDatePicker.Date = selected.DepartureDate;
            deliveryDatePicker.Date = selected.DeliveryDate;
        }

        private async void OnSaveDeliveryClicked(object sender, EventArgs e)
        {
            if (contractPicker.SelectedIndex == -1)
            {
                await DisplayAlert("Validation Error", "Please select a Contract.", "OK");
                return;
            }

            try
            {
                var delivery = new Deliveries
                {
                    DeliveryID = deliveryIDEntry.Text ?? "",
                    ContractID = contracts[contractPicker.SelectedIndex].ContractID,
                    Amount = double.Parse(amountEntry.Text),
                    TotalBales = int.Parse(totalBalesEntry.Text),
                    FactoryWeight = double.Parse(factoryWeightEntry.Text),
                    MillWeight = double.Parse(millWeightEntry.Text),
                    TruckNumber = truckNoEntry.Text ?? "",
                    DriverContact = driverContactEntry.Text ?? "",
                    DepartureDate = departureDatePicker.Date,
                    DeliveryDate = deliveryDatePicker.Date
                };

                var exists = deliveries.Any(d => d.DeliveryID == delivery.DeliveryID);
                if (exists)
                    await _db.UpdateDelivery(delivery);
                else
                    await _db.AddDelivery(delivery);

                await DisplayAlert("Success", "Delivery saved.", "OK");
                await LoadData();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to save delivery. Details: {ex.Message}", "OK");
            }
        }

        private async void OnDeleteDeliveryClicked(object sender, EventArgs e)
        {
            if (deliveryPicker.SelectedIndex == -1) return;

            var deliveryID = deliveryPicker.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(deliveryID))
            {
                await _db.DeleteDelivery(deliveryID);
                await DisplayAlert("Deleted", "Delivery deleted.", "OK");
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
}
