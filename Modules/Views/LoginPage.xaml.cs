using System.Text.RegularExpressions;
using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
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
            // Disable login button to prevent multiple clicks
            loginButton.IsEnabled = false;
            errorMessageLabel.IsVisible = false; // Hide previous error message

            string enteredEmail = emailEntry.Text?.Trim();
            string enteredPassword = passwordEntry.Text;

            // Validate email format
            if (string.IsNullOrEmpty(enteredEmail) || !IsValidEmail(enteredEmail))
            {
                await DisplayAlert("Invalid Input", "Please enter a valid email address.", "OK");
                loginButton.IsEnabled = true;
                return;
            }

            // Validate password field
            if (string.IsNullOrEmpty(enteredPassword))
            {
                await DisplayAlert("Invalid Input", "Password cannot be empty.", "OK");
                loginButton.IsEnabled = true;
                return;
            }

            // Simulate backend processing delay
            await Task.Delay(1000);

            // Check credentials (for demonstration purposes; replace with actual authentication)
            string correctEmail = "user@example.com";
            string correctPassword = "password123";


            if (enteredEmail == correctEmail && enteredPassword == correctPassword)
            {

                await App.NavigateToPage(new DashboardPage());
            }
            else
            {
                errorMessageLabel.Text = "Incorrect email or password.";
                errorMessageLabel.IsVisible = true;
            }

            // Re-enable login button after processing
            loginButton.IsEnabled = true;
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