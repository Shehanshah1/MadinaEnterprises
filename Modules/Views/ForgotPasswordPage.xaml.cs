using System.Text.RegularExpressions;

namespace MadinaEnterprises.Modules.Views;

public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage()
    {
        InitializeComponent();
    }

    private async void OnSendLinkClicked(object sender, EventArgs e)
    {
        errorMessageLabel.IsVisible = false;

        var email = emailEntry.Text?.Trim();
        if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
        {
            ShowError("Please enter a valid email address.");
            return;
        }

        // TODO: Integrate with actual backend service
        await DisplayAlert("Reset Sent", "Password reset link has been sent to your email.", "OK");
        await App.NavigateToPage(new LoginPage());
    }

    private void ShowError(string message)
    {
        errorMessageLabel.Text = message;
        errorMessageLabel.IsVisible = true;
    }

    private bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    private async void OnBackToLoginClicked(object sender, EventArgs e)
    {
        await App.NavigateToPage(new LoginPage());
    }
}
