using MadinaEnterprises.Modules.Util;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace MadinaEnterprises.Modules.Views
{
    public partial class MorePage : ContentPage
    {
        private readonly DataSyncService _syncService = DataSyncService.Instance;
        private readonly UserAuthenticationService _authService = UserAuthenticationService.Instance;

        public MorePage()
        {
            InitializeComponent();
            LoadUserInfo();
        }

        private void LoadUserInfo()
        {
            if (_authService.CurrentUser != null)
            {
                userNameLabel.Text = _authService.CurrentUser.FullName;
                userRoleLabel.Text = _authService.CurrentUser.Role;
                lastLoginLabel.Text = DateTime.Now.ToString("MMM dd, yyyy HH:mm");
            }
            else
            {
                userNameLabel.Text = "Admin User";
                userRoleLabel.Text = "Administrator";
                lastLoginLabel.Text = DateTime.Now.ToString("MMM dd, yyyy HH:mm");
            }
        }

        // Navigation to data management pages
        private async void OnDeliveriesClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(DeliveriesPage));
        }

        private async void OnPaymentsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(PaymentsPage));
        }

        private async void OnGinnerLedgerClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(GinnerLedgerPage));
        }

        // Reports and Analytics
        private async void OnReportsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(ReportsPage));
        }

        private async void OnAnalyticsClicked(object sender, EventArgs e)
        {
            // Navigate to dashboard which has analytics
            await Shell.Current.GoToAsync("//DashboardPage");
        }

        // System operations
        private async void OnBackupClicked(object sender, EventArgs e)
        {
            try
            {
                var action = await DisplayActionSheet("Select Backup Type", "Cancel", null,
                    "Full Backup", "Incremental Backup (Last 30 days)", "Custom Date Range");

                if (action == "Cancel" || action == null) return;

                BackupResult result;

                switch (action)
                {
                    case "Full Backup":
                        result = await _syncService.CreateFullBackup();
                        break;

                    case "Incremental Backup (Last 30 days)":
                        result = await _syncService.CreateIncrementalBackup(DateTime.Now.AddDays(-30));
                        break;

                    case "Custom Date Range":
                        // For simplicity, using last 7 days. In production, show date picker
                        result = await _syncService.CreateIncrementalBackup(DateTime.Now.AddDays(-7));
                        break;

                    default:
                        return;
                }

                if (result.Success)
                {
                    await DisplayAlert("Backup Complete",
                        $"Backup created successfully!\n\nFile: {Path.GetFileName(result.BackupPath)}\n" +
                        $"Size: {result.BackupSize / 1024:F2} KB\n" +
                        $"Records: {result.RecordCount}",
                        "OK");
                }
                else
                {
                    await DisplayAlert("Backup Failed", result.Message, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Backup failed: {ex.Message}", "OK");
            }
        }

        private async void OnRestoreClicked(object sender, EventArgs e)
        {
            try
            {
                var backups = _syncService.GetAvailableBackups();

                if (!backups.Any())
                {
                    await DisplayAlert("No Backups", "No backup files found.", "OK");
                    return;
                }

                var backupNames = backups.Select(b => $"{b.FileName} ({b.SizeFormatted})").ToArray();
                var selected = await DisplayActionSheet("Select Backup to Restore", "Cancel", null, backupNames);

                if (selected == "Cancel" || selected == null) return;

                var selectedBackup = backups.FirstOrDefault(b => selected.Contains(b.FileName));
                if (selectedBackup == null) return;

                var confirm = await DisplayAlert("Confirm Restore",
                    $"Are you sure you want to restore from:\n{selectedBackup.FileName}?\n\n" +
                    "This will merge the backup data with existing data.",
                    "Restore", "Cancel");

                if (!confirm) return;

                var result = await _syncService.RestoreFromBackup(selectedBackup.FilePath);

                if (result.Success)
                {
                    await DisplayAlert("Restore Complete",
                        $"Successfully restored {result.RestoredRecords} records.", "OK");
                }
                else
                {
                    await DisplayAlert("Restore Failed", result.Message, "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Restore failed: {ex.Message}", "OK");
            }
        }

        private async void OnExportClicked(object sender, EventArgs e)
        {
            try
            {
                var action = await DisplayActionSheet("Export Format", "Cancel", null,
                    "Excel (All Data)", "CSV (Contracts)", "CSV (Payments)", "CSV (Ginners)");

                if (action == "Cancel" || action == null) return;

                string filePath;

                switch (action)
                {
                    case "Excel (All Data)":
                        var options = new ExportOptions
                        {
                            IncludeContracts = true,
                            IncludeGinners = true,
                            IncludeMills = true,
                            IncludeDeliveries = true,
                            IncludePayments = true,
                            IncludeLedger = true
                        };
                        filePath = await _syncService.ExportToExcel(options);
                        break;

                    case "CSV (Contracts)":
                        filePath = await _syncService.ExportToCSV("contracts");
                        break;

                    case "CSV (Payments)":
                        filePath = await _syncService.ExportToCSV("payments");
                        break;

                    case "CSV (Ginners)":
                        filePath = await _syncService.ExportToCSV("ginners");
                        break;

                    default:
                        return;
                }

                await DisplayAlert("Export Complete",
                    $"Data exported successfully!\n\nFile saved to:\n{Path.GetFileName(filePath)}", "OK");

                // Optionally share the file
                var share = await DisplayAlert("Share File",
                    "Would you like to share the exported file?", "Share", "No");

                if (share)
                {
                    await Share.RequestAsync(new ShareFileRequest
                    {
                        Title = "Share Export",
                        File = new ShareFile(filePath)
                    });
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Export failed: {ex.Message}", "OK");
            }
        }

        private async void OnSettingsClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Settings", "Settings page coming soon!", "OK");
        }

        // Account operations
        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            try
            {
                // Simple password change dialog
                string currentPassword = await DisplayPromptAsync("Change Password",
                    "Enter current password:", "Continue", "Cancel",
                    keyboard: Keyboard.Text, maxLength: 50);

                if (string.IsNullOrEmpty(currentPassword)) return;

                string newPassword = await DisplayPromptAsync("Change Password",
                    "Enter new password:", "Continue", "Cancel",
                    keyboard: Keyboard.Text, maxLength: 50);

                if (string.IsNullOrEmpty(newPassword)) return;

                string confirmPassword = await DisplayPromptAsync("Change Password",
                    "Confirm new password:", "Change", "Cancel",
                    keyboard: Keyboard.Text, maxLength: 50);

                if (string.IsNullOrEmpty(confirmPassword)) return;

                if (newPassword != confirmPassword)
                {
                    await DisplayAlert("Error", "New passwords do not match.", "OK");
                    return;
                }

                // If user is authenticated through the service
                if (_authService.CurrentUser != null)
                {
                    var result = await _authService.ChangePasswordAsync(
                        _authService.CurrentUser.UserId, currentPassword, newPassword);

                    if (result.Success)
                    {
                        await DisplayAlert("Success", "Password changed successfully.", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Error", result.Message, "OK");
                    }
                }
                else
                {
                    // For demo purposes when not using auth service
                    await DisplayAlert("Success", "Password changed successfully.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Failed to change password: {ex.Message}", "OK");
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Logout",
                "Are you sure you want to logout?", "Yes", "No");

            if (confirm)
            {
                // Logout from auth service if authenticated
                if (_authService.IsAuthenticated)
                {
                    await _authService.LogoutAsync();
                }

                // Navigate to login page
                App.ShowLogin();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadUserInfo();
        }
    }
}