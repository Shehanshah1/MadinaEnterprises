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
        private Mills? _selectedMill = null;

        public MillsPage()
        {
            InitializeComponent();
            _ = LoadMills();
        }

        private async Task LoadMills()
        {
            try
            {
                _mills = await _db.GetAllMills();
                millListView.ItemsSource = _mills;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to load mills: {ex.Message}", "OK");
            }
        }

        private void OnMillSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is not Mills selected) return;

            _selectedMill = selected;
            millNameEntry.Text = selected.MillName;
            millIDEntry.Text = selected.MillID;
            millAddressEntry.Text = selected.Address;
            millOwnerNameEntry.Text = selected.OwnerName;
        }

        private async void OnAddMillClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(millNameEntry.Text) || string.IsNullOrWhiteSpace(millIDEntry.Text))
            {
                await DisplayAlert("Validation Error", "Mill name and Mill ID are required.", "OK");
                return;
            }

            // Generate Mill ID if not provided
            if (string.IsNullOrWhiteSpace(millIDEntry.Text))
            {
                millIDEntry.Text = $"MILL-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            }

            var mill = new Mills
            {
                MillName = millNameEntry.Text.Trim(),
                MillID = millIDEntry.Text.Trim(),
                Address = millAddressEntry.Text?.Trim() ?? "",
                OwnerName = millOwnerNameEntry.Text?.Trim() ?? ""
            };

            // Check if mill already exists
            var existing = _mills.FirstOrDefault(m => m.MillID == mill.MillID);
            if (existing != null)
            {
                await DisplayAlert("Duplicate Mill", "A mill with this ID already exists. Use Update instead.", "OK");
                return;
            }

            try
            {
                await _db.AddMill(mill);
                await DisplayAlert("Success", "Mill added successfully!", "OK");
                ClearForm();
                await LoadMills();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to add mill: {ex.Message}", "OK");
            }
        }

        private async void OnUpdateMillClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(millIDEntry.Text))
            {
                await DisplayAlert("Validation Error", "Please select a mill to update.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(millNameEntry.Text))
            {
                await DisplayAlert("Validation Error", "Mill name is required.", "OK");
                return;
            }

            var mill = new Mills
            {
                MillName = millNameEntry.Text.Trim(),
                MillID = millIDEntry.Text.Trim(),
                Address = millAddressEntry.Text?.Trim() ?? "",
                OwnerName = millOwnerNameEntry.Text?.Trim() ?? ""
            };

            try
            {
                await _db.UpdateMill(mill);
                await DisplayAlert("Success", "Mill updated successfully.", "OK");
                ClearForm();
                await LoadMills();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to update mill: {ex.Message}", "OK");
            }
        }

        private async void OnDeleteMillClicked(object sender, EventArgs e)
        {
            if (_selectedMill == null && string.IsNullOrWhiteSpace(millIDEntry.Text))
            {
                await DisplayAlert("Error", "Please select a mill to delete.", "OK");
                return;
            }

            var millToDelete = _selectedMill ?? new Mills { MillID = millIDEntry.Text };

            bool confirm = await DisplayAlert("Confirm Delete",
                $"Are you sure you want to delete mill '{millToDelete.MillName ?? millToDelete.MillID}'?\n\nNote: This will fail if the mill is referenced in any contracts.",
                "Yes", "No");

            if (!confirm) return;

            try
            {
                await _db.DeleteMill(millToDelete.MillID);
                await DisplayAlert("Success", "Mill deleted successfully.", "OK");
                ClearForm();
                await LoadMills();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to delete mill: {ex.Message}\n\nThe mill may be referenced in existing contracts.", "OK");
            }
        }

        private void ClearForm()
        {
            millNameEntry.Text = "";
            millIDEntry.Text = "";
            millAddressEntry.Text = "";
            millOwnerNameEntry.Text = "";
            millListView.SelectedItem = null;
            _selectedMill = null;
        }

        // Navigation methods
        private async void OnDashboardPageButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//DashboardPage");
        }

        private async void OnGinnersPageButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//GinnersPage");
        }

        private async void OnContractsPageButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//ContractsPage");
        }

        private async void OnDeliveriesPageButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//DeliveriesPage");
        }

        private async void OnPaymentsPageButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//PaymentsPage");
        }

        private async void OnGinnerLedgerPageButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//GinnerLedgerPage");
        }

        private async void OnLogOutButtonClicked(object sender, EventArgs e)
        {
            var result = await DisplayAlert("Logout", "Are you sure you want to logout?", "Yes", "No");
            if (result)
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
    }
}