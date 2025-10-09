using MadinaEnterprises.Modules.Models;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MadinaEnterprises.Modules.Views
{
    public partial class MillsPage : ContentPage
    {
        private readonly DatabaseService _db = App.DatabaseService!;

        // Cache for searching
        private List<Mills> _allMills = new();

        // Bound to MillsListView
        private readonly ObservableCollection<Mills> _millsList = new();

        public MillsPage()
        {
            InitializeComponent();

            // Bind the list
            MillsListView.ItemsSource = _millsList;

            _ = LoadMills();
        }

        private async Task LoadMills()
        {
            _allMills = await _db.GetAllMills();

            _millsList.Clear();
            foreach (var m in _allMills)
                _millsList.Add(m);
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
            var name = millNameEntry.Text?.Trim() ?? string.Empty;
            var id = millIDEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(id))
            {
                await DisplayAlert("Validation Error", "Mill ID and Mill Name are required.", "OK");
                return;
            }

            if (_allMills.Any(m => string.Equals(m.MillID, id, StringComparison.OrdinalIgnoreCase)))
            {
                await DisplayAlert("Exists", "A mill with this ID already exists. Try updating instead.", "OK");
                return;
            }

            var mill = new Mills
            {
                MillName = name,
                MillID = id,
                Address = millAddressEntry.Text?.Trim() ?? string.Empty,
                OwnerName = millOwnerNameEntry.Text?.Trim() ?? string.Empty
            };

            await _db.AddMill(mill);
            await DisplayAlert("Success", "Mill saved successfully!", "OK");
            ClearForm();
            await LoadMills();
        }

        private async void OnUpdateMillClicked(object sender, EventArgs e)
        {
            var name = millNameEntry.Text?.Trim() ?? string.Empty;
            var id = millIDEntry.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(id))
            {
                await DisplayAlert("Validation Error", "Mill ID is required to update.", "OK");
                return;
            }

            var existing = _allMills.FirstOrDefault(m => string.Equals(m.MillID, id, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                await DisplayAlert("Not Found", "No mill found with this ID to update.", "OK");
                return;
            }

            var mill = new Mills
            {
                MillName = string.IsNullOrWhiteSpace(name) ? existing.MillName : name,
                MillID = id,
                Address = millAddressEntry.Text?.Trim() ?? string.Empty,
                OwnerName = millOwnerNameEntry.Text?.Trim() ?? string.Empty
            };

            await _db.UpdateMill(mill);
            await DisplayAlert("Updated", "Mill updated successfully.", "OK");
            ClearForm();
            await LoadMills();
        }

        private async void OnDeleteMillClicked(object sender, EventArgs e)
        {
            // Prefer selected item; fall back to typed ID
            var selected = MillsListView.SelectedItem as Mills;
            var id = selected?.MillID ?? millIDEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                await DisplayAlert("Validation Error", "Select a mill or enter a Mill ID to delete.", "OK");
                return;
            }

            var millToDelete = _allMills.FirstOrDefault(m => string.Equals(m.MillID, id, StringComparison.OrdinalIgnoreCase));
            if (millToDelete is null)
            {
                await DisplayAlert("Not Found", "No mill found with this ID.", "OK");
                return;
            }

            bool confirm = await DisplayAlert("Confirm", $"Delete mill '{millToDelete.MillName}'?", "Yes", "No");
            if (!confirm) return;

            await _db.DeleteMill(id); // Delete by ID
            await DisplayAlert("Deleted", "Mill deleted successfully.", "OK");
            ClearForm();
            await LoadMills();
        }

        private void ClearForm()
        {
            millNameEntry.Text = string.Empty;
            millIDEntry.Text = string.Empty;
            millAddressEntry.Text = string.Empty;
            millOwnerNameEntry.Text = string.Empty;
            MillsListView.SelectedItem = null;
            // If you have a SearchBar named MillSearchBar, also clear it:
            // MillSearchBar.Text = string.Empty;
        }

        //****************************************************************************
        //                               SEARCH
        //****************************************************************************
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var q = (e.NewTextValue ?? string.Empty).Trim().ToLowerInvariant();

            IEnumerable<Mills> src = _allMills;

            if (!string.IsNullOrEmpty(q))
            {
                src = _allMills.Where(m =>
                    (m.MillName ?? string.Empty).ToLowerInvariant().Contains(q) ||
                    (m.MillID ?? string.Empty).ToLowerInvariant().Contains(q));
            }

            _millsList.Clear();
            foreach (var m in src)
                _millsList.Add(m);
        }

        //****************************************************************************
        //                               NAVIGATION
        //****************************************************************************
        private async void OnDashboardPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DashboardPage());
        private async void OnGinnersPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new GinnersPage());
        private async void OnContractsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new ContractsPage());
        private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new DeliveriesPage());
        private async void OnPaymentsPageButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new PaymentsPage());
        private async void OnLogOutButtonClicked(object sender, EventArgs e) => await App.NavigateToPage(new LoginPage());
    }
}
