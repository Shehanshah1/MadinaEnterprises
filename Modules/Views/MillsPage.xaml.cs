using MadinaEnterprises.Modules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace MadinaEnterprises.Modules.Views
{
    public partial class MillsPage : ContentPage
    {
        private readonly DatabaseService _db = App.DatabaseService!;
        private List<Mills> _mills = new();

        public MillsPage()
        {
            InitializeComponent();
            _ = LoadMills();
        }

        private async Task LoadMills()
        {
            _mills = await _db.GetAllMills();
            millListView.ItemsSource = _mills;
        }

        private void OnMillSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is not Mills selected) return;

            millNameEntry.Text = selected.MillName;
            millIDEntry.Text = selected.MillID;
            millAddressEntry.Text = selected.Address;
            millOwnerNameEntry.Text = selected.OwnerName;
        }

        private async void OnAddMillClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(millNameEntry.Text))
            {
                await DisplayAlert("Error", "Mill name cannot be empty.", "OK");
                return;
            }

            var mill = new Mills
            {
                MillName = millNameEntry.Text,
                MillID = millIDEntry.Text,
                Address = millAddressEntry.Text,
                OwnerName = millOwnerNameEntry.Text
            };  

            var existing = _mills.FirstOrDefault(m => m.MillID == mill.MillID);
            if (existing == null)
                await _db.AddMill(mill);
            else
                await DisplayAlert("Exists", "Mill already exists. Try updating instead.", "OK");

            await DisplayAlert("Success", "Mill saved successfully!", "OK");
            ClearForm();
            await LoadMills();
        }

        private async void OnUpdateMillClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(millNameEntry.Text)) return;

            var mill = new Mills
            {
                MillName = millNameEntry.Text,
                MillID = millIDEntry.Text,
                Address = millAddressEntry.Text,
                OwnerName = millOwnerNameEntry.Text
            };

            await _db.UpdateMill(mill);
            await DisplayAlert("Updated", "Mill updated successfully.", "OK");
            ClearForm();
            await LoadMills();
        }

        private async void OnDeleteMillClicked(object sender, EventArgs e)
        {
            if (millListView.SelectedItem is not Mills selected) return;

            bool confirm = await DisplayAlert("Confirm", $"Delete mill '{selected.MillName}'?", "Yes", "No");
            if (!confirm) return;

            await _db.DeleteMill(selected.MillName);
            await DisplayAlert("Deleted", "Mill deleted successfully.", "OK");
            ClearForm();
            await LoadMills();
        }

        private void ClearForm()
        {
            millNameEntry.Text = "";
            millIDEntry.Text = "";
            millAddressEntry.Text = "";
            millOwnerNameEntry.Text = "";
            millListView.SelectedItem = null;
        }

        // Navigation
        private async void OnDashboardPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DashboardPage());
        private async void OnGinnersPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnersPage());
        private async void OnContractsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new ContractsPage());
        private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
        private async void OnPaymentsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new PaymentsPage());
        private async void OnLogOutButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new LoginPage());
    }
}
