using System.Text.RegularExpressions;

namespace MadinaEnterprises.Modules.Views;

public partial class CreateAccountPage : ContentPage
{
    public CreateAccountPage()
    {
        InitializeComponent();
    }

    private async void OnCreateAccountClicked(object sender, EventArgs e)
    {
        errorMessageLabel.IsVisible = false;

        var name = nameEntry.Text?.Trim();
        var email = emailEntry.Text?.Trim();
        var password = passwordEntry.Text;
        var confirmPassword = confirmPasswordEntry.Text;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            ShowError("All fields are required.");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowError("Please enter a valid email.");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Password must be at least 6 characters.");
            return;
        }

        if (password != confirmPassword)
        {
            ShowError("Passwords do not match.");
            return;
        }

        // TODO: Add user registration logic here (e.g., save to database or API call)
        await DisplayAlert("Success", "Account created successfully!", "OK");
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

    private async void OnLoginRedirectClicked(object sender, EventArgs e)
    {
        await App.NavigateToPage(new LoginPage());
    }
}
