using Microsoft.Maui.Controls;

namespace MadinaEnterprises.Modules.Views;

public partial class LoginPage : ContentPage
{
    private const string HardcodedUsername = "Anees";
    private const string HardcodedPassword = "4081";

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
        errorMessageLabel.IsVisible = false;

        try
        {
            var username = emailEntry.Text?.Trim() ?? string.Empty;
            var password = passwordEntry.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ShowError("Enter username and password.");
                return;
            }

            var usernameMatches = string.Equals(username, HardcodedUsername, StringComparison.OrdinalIgnoreCase);
            var passwordMatches = string.Equals(password, HardcodedPassword, StringComparison.Ordinal);

            if (!usernameMatches || !passwordMatches)
            {
                ShowError("Invalid login credentials.");
                passwordEntry.Text = string.Empty;
                return;
            }

            await App.NavigateToPage(new DashboardPage());

            // Reset form so a later logout returns to a clean login screen.
            passwordEntry.Text = string.Empty;
        }
        catch (Exception ex)
        {
            ShowError($"Login failed: {ex.Message}");
        }
        finally
        {
            loginButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        errorMessageLabel.Text = message;
        errorMessageLabel.IsVisible = true;
    }

    private void OnUsernameCompleted(object sender, EventArgs e) => passwordEntry.Focus();

    private void OnPasswordCompleted(object sender, EventArgs e) => OnLoginButtonClicked(sender, e);
}
