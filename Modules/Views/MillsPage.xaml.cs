using MadinaEnterprises.Modules.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

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
            millPicker.ItemsSource = _mills.Select(m => m.MillName).ToList();
        }

        private void OnMillSelected(object sender, EventArgs e)
        {
            if (millPicker.SelectedItem is not string selectedName) return;

            var mill = _mills.FirstOrDefault(m => m.MillName == selectedName);
            if (mill == null) return;

            millNameEntry.Text = mill.MillName;
            millAddressEntry.Text = mill.Address;
            millOwnerNameEntry.Text = mill.OwnerName;
        }

        private async void OnSaveMillClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(millNameEntry.Text))
            {
                await DisplayAlert("Error", "Mill name cannot be empty.", "OK");
                return;
            }

            var mill = new Mills
            {
                MillName = millNameEntry.Text ?? "",
                Address = millAddressEntry.Text ?? "",
                OwnerName = millOwnerNameEntry.Text ?? ""
            };

            var existing = _mills.FirstOrDefault(m => m.MillName == mill.MillName);
            if (existing == null)
                await _db.AddMill(mill);
            else
                await _db.UpdateMill(mill);

            await DisplayAlert("Success", "Mill saved successfully!", "OK");
            ClearForm();
            await LoadMills();
        }

        private async void OnDeleteMillClicked(object sender, EventArgs e)
        {
            if (millPicker.SelectedItem is not string selectedName) return;

            var confirm = await DisplayAlert("Confirm", $"Delete mill '{selectedName}'?", "Yes", "No");
            if (!confirm) return;

            await _db.DeleteMill(selectedName);
            await DisplayAlert("Deleted", "Mill deleted successfully.", "OK");
            ClearForm();
            await LoadMills();
        }

        private void ClearForm()
        {
            millNameEntry.Text = "";
            millAddressEntry.Text = "";
            millOwnerNameEntry.Text = "";
            millPicker.SelectedItem = null;
        }

        // Navigation Buttons
        private async void OnDashboardPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DashboardPage());
        private async void OnGinnersPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnersPage());
        private async void OnContractsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new ContractsPage());
        private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
        private async void OnPaymentsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new PaymentsPage());
        private async void OnLogOutButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new LoginPage());
    }
}
