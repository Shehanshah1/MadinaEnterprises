using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace MadinaEnterprises.Modules.Views
{
    public partial class LoginPage : ContentPage
    {
        private bool _isPasswordVisible = false;

        public LoginPage()
        {
            InitializeComponent();
        }

        private bool IsValidEmail(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
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

            string email = emailEntry.Text?.Trim() ?? "";
            string password = passwordEntry.Text ?? "";

            if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
            {
                await DisplayAlert("Invalid Input", "Please enter a valid email address.", "OK");
                loginButton.IsEnabled = true;
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                await DisplayAlert("Invalid Input", "Password cannot be empty.", "OK");
                loginButton.IsEnabled = true;
                return;
            }

            await Task.Delay(100); // Simulate API delay

            var loginResult = await App.DatabaseService.ValidateUserCredentials(email, password);

            if (loginResult.IsValid)
            {
                if (loginResult.IsAdmin)
                {
                    await ProcessPendingApprovals(email);
                }

                await App.NavigateToPage(new DashboardPage());
            }
            else
            {
                errorMessageLabel.Text = loginResult.ErrorMessage ?? "Incorrect email or password.";
                errorMessageLabel.IsVisible = true;
            }

            loginButton.IsEnabled = true;
        }
        private async Task ProcessPendingApprovals(string adminEmail)
        {
            var pending = await App.DatabaseService.GetPendingApprovalEmails();
            if (!pending.Any())
            {
                return;
            }

            var shouldReview = await DisplayAlert("Admin Approval", $"There are {pending.Count} pending verified account(s). Approve now?", "Yes", "Later");
            if (!shouldReview)
            {
                return;
            }

            foreach (var pendingEmail in pending)
            {
                var approve = await DisplayAlert("Approve User", $"Approve '{pendingEmail}'?", "Approve", "Skip");
                if (approve)
                {
                    await App.DatabaseService.ApproveUser(adminEmail, pendingEmail);
                }
            }
        }

        private async void OnCreateAccountButtonClicked(object sender, EventArgs e)
        {
            await App.NavigateToPage(new CreateAccountPage());
        }

        private async void OnForgotPasswordButtonClicked(object sender, EventArgs e)
        {
            await App.NavigateToPage(new ForgotPasswordPage());
        }
    }
}
