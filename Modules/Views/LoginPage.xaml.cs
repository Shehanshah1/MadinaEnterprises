using Microsoft.Maui.Controls;

namespace MadinaEnterprises.Modules.Views;

public partial class LoginPage : ContentPage
{
    private const string HardcodedUsername = "Anees";
    private const string HardcodedPassword = "0000";

    private bool _isPasswordVisible;

    public LoginPage()
    {
        InitializeComponent();
    }

    private void showHidePasswordButton_Clicked(object sender, EventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        passwordEntry.IsPassword = !_isPasswordVisible;
        showHidePasswordButton.Text = _isPasswordVisible ? "Hide" : "Show";
    }

    private async void OnLoginButtonClicked(object sender, EventArgs e)
    {
        loginButton.IsEnabled = false;
        HideError();

        var username = emailEntry.Text?.Trim() ?? string.Empty;
        var password = passwordEntry.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ShowError("Enter username and password.");
            loginButton.IsEnabled = true;
            return;
        }

        var usernameMatches = string.Equals(username, HardcodedUsername, StringComparison.OrdinalIgnoreCase);
        var passwordMatches = string.Equals(password, HardcodedPassword, StringComparison.Ordinal);

        if (!usernameMatches || !passwordMatches)
        {
            ShowError("Invalid login credentials.");
            loginButton.IsEnabled = true;
            return;
        }

        await App.NavigateToPage(new DashboardPage());
        loginButton.IsEnabled = true;
    }

    private void ShowError(string message)
    {
        errorMessageLabel.Text = message;
        errorMessageLabel.IsVisible = true;
        errorFrame.IsVisible = true;
    }

    private void HideError()
    {
        errorMessageLabel.IsVisible = false;
        errorFrame.IsVisible = false;
    }
}
