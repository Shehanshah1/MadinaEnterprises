using MadinaEnterprises.Modules.Services;
using Microsoft.Maui.Controls;

namespace MadinaEnterprises.Modules.Views;

public partial class CloudSyncPage : ContentPage
{
    private readonly DatabaseService _db = App.DatabaseService!;

    public CloudSyncPage()
    {
        InitializeComponent();
        UrlEntry.Text = CloudConfig.Url;
        AnonKeyEntry.Text = CloudConfig.AnonKey;
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        if (_db.Cloud.IsEnabled)
        {
            StatusLabel.Text = "Connected — auto-sync enabled.";
            StatusLabel.TextColor = Color.FromArgb("#98cb00");
        }
        else
        {
            StatusLabel.Text = "Not configured. Save Supabase credentials below, then Pull from Cloud.";
            StatusLabel.TextColor = Color.FromArgb("#FBBF24");
        }

        LastSyncLabel.Text = _db.Cloud.LastSyncedAt.HasValue
            ? $"Last synced: {_db.Cloud.LastSyncedAt:yyyy-MM-dd HH:mm:ss}"
            : "Never synced in this session.";

        if (!string.IsNullOrWhiteSpace(_db.Cloud.LastError))
        {
            LastErrorLabel.Text = _db.Cloud.LastError;
            LastErrorLabel.IsVisible = true;
        }
        else
        {
            LastErrorLabel.IsVisible = false;
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var url = UrlEntry.Text?.Trim() ?? string.Empty;
        var key = AnonKeyEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
        {
            await DisplayAlert("Missing values", "Both URL and Anon Key are required.", "OK");
            return;
        }

        SaveButton.IsEnabled = false;
        try
        {
            await CloudConfig.SaveOverrideAsync(url, key);
            await DisplayAlert("Saved", "Credentials stored locally. Use Pull from Cloud to sync now.", "OK");
            RefreshStatus();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private async void OnPullClicked(object sender, EventArgs e)
    {
        if (!_db.Cloud.IsEnabled)
        {
            await DisplayAlert("Not configured", "Save Supabase credentials first.", "OK");
            return;
        }

        PullButton.IsEnabled = false;
        PullButton.Text = "Pulling...";
        try
        {
            var ok = await _db.SyncFromCloudAsync();
            RefreshStatus();
            await DisplayAlert(ok ? "Pull complete" : "Pull failed",
                ok ? "Local database synced with the cloud." : (_db.Cloud.LastError ?? "Unknown error."),
                "OK");
        }
        finally
        {
            PullButton.IsEnabled = true;
            PullButton.Text = "Pull from Cloud";
        }
    }

    private async void OnPushClicked(object sender, EventArgs e)
    {
        if (!_db.Cloud.IsEnabled)
        {
            await DisplayAlert("Not configured", "Save Supabase credentials first.", "OK");
            return;
        }

        PushButton.IsEnabled = false;
        PushButton.Text = "Pushing...";
        try
        {
            var ok = await _db.PushAllToCloudAsync();
            RefreshStatus();
            await DisplayAlert(ok ? "Push complete" : "Push failed",
                ok ? "All local rows uploaded to Supabase." : (_db.Cloud.LastError ?? "Unknown error."),
                "OK");
        }
        finally
        {
            PushButton.IsEnabled = true;
            PushButton.Text = "Push to Cloud";
        }
    }

    private async void OnBackClicked(object sender, EventArgs e) => await App.GoBack();
}
