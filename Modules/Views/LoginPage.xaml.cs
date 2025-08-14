using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using MadinaEnterprises.Services;

namespace MadinaEnterprises.Modules.Views
{
    public partial class LoginPage : ContentPage
    {
        private readonly UserAuthenticationService _authService = UserAuthenticationService.Instance;
        private bool _isPasswordVisible = false;
        private int _failedAttempts = 0;
        private DateTime _lastFailedAttempt = DateTime.MinValue;

        public LoginPage()
        {
            InitializeComponent();
            CheckRememberedUser();
        }

        private void CheckRememberedUser()
        {
            // Check if user credentials are saved (in secure storage)
            var savedUsername = Preferences.Get("RememberedUsername", "");
            if (!string.IsNullOrEmpty(savedUsername))
            {
                emailEntry.Text = savedUsername;
                // In production, use SecureStorage for sensitive data
            }
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
            // Check for too many failed attempts
            if (_failedAttempts >= 5 && (DateTime.Now - _lastFailedAttempt).TotalMinutes < 15)
            {
                var remainingTime = 15 - (DateTime.Now - _lastFailedAttempt).TotalMinutes;
                await DisplayAlert("Account Locked",
                    $"Too many failed attempts. Please try again in {remainingTime:F0} minutes.", "OK");
                return;
            }

            loginButton.IsEnabled = false;
            errorMessageLabel.IsVisible = false;

            string username = emailEntry.Text?.Trim() ?? "";
            string password = passwordEntry.Text ?? "";

            // Validation
            if (string.IsNullOrEmpty(username))
            {
                errorMessageLabel.Text = "Please enter your username or email.";
                errorMessageLabel.IsVisible = true;
                loginButton.IsEnabled = true;
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                errorMessageLabel.Text = "Please enter your password.";
                errorMessageLabel.IsVisible = true;
                loginButton.IsEnabled = true;
                return;
            }

            // Show loading indicator
            var loadingIndicator = new ActivityIndicator
            {
                IsRunning = true,
                Color = Color.FromHex("#98cb00"),
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };

            // Add loading indicator to the page (you'd need to add a container in XAML)

            try
            {
                // Attempt login
                var result = await _authService.LoginAsync(username, password);

                if (result.Success)
                {
                    _failedAttempts = 0;

                    // Save username if remember me is checked (add checkbox in XAML)
                    Preferences.Set("RememberedUsername", username);

                    // Navigate based on user role
                    if (result.User?.Role == "Admin")
                    {
                        await App.NavigateToPage(new DashboardPage());
                    }
                    else
                    {
                        await App.NavigateToPage(new DashboardPage());
                    }

                    // Clear the login page from navigation stack
                    if (Application.Current?.MainPage is NavigationPage navPage)
                    {
                        navPage.Navigation.RemovePage(this);
                    }
                }
                else
                {
                    _failedAttempts++;
                    _lastFailedAttempt = DateTime.Now;

                    errorMessageLabel.Text = result.Message;
                    errorMessageLabel.IsVisible = true;

                    // Shake animation for error
                    await ShakeAnimation(errorMessageLabel);

                    if (_failedAttempts >= 3)
                    {
                        errorMessageLabel.Text += $"\n{5 - _failedAttempts} attempts remaining.";
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                loginButton.IsEnabled = true;
            }
        }

        private async Task ShakeAnimation(View view)
        {
            await view.TranslateTo(-10, 0, 50);
            await view.TranslateTo(10, 0, 50);
            await view.TranslateTo(-10, 0, 50);
            await view.TranslateTo(10, 0, 50);
            await view.TranslateTo(0, 0, 50);
        }

        private async void OnCreateAccountButtonClicked(object sender, EventArgs e)
        {
            await App.NavigateToPage(new CreateAccountPage());
        }

        private async void OnForgotPasswordButtonClicked(object sender, EventArgs e)
        {
            await App.NavigateToPage(new ForgotPasswordPage());
        }

        protected override bool OnBackButtonPressed()
        {
            // Prevent going back from login page
            return true;
        }
    }
}