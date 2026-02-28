using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MadinaEnterprises.Modules.Views;

public partial class CreateAccountPage : ContentPage
{
    private readonly IRegistrationService _registrationService;
    private bool _showPassword = false;
    private bool _showConfirm = false;

    public CreateAccountPage(IRegistrationService? service = null)
    {
        InitializeComponent();
        _registrationService = service ?? RegistrationService.Instance;
    }

    private async void OnCreateAccountClicked(object sender, EventArgs e)
    {
        errorMessageLabel.IsVisible = false;

        var name = nameEntry.Text?.Trim() ?? string.Empty;
        var email = emailEntry.Text?.Trim() ?? string.Empty;
        var password = passwordEntry.Text ?? string.Empty;
        var confirmPassword = confirmPasswordEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            ShowError("All fields are required.");
            return;
        }

        if (name.Length < 2)
        {
            ShowError("Please enter your full name.");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowError("Please enter a valid email.");
            return;
        }

        if (!IsStrongPassword(password))
        {
            ShowError("Password must be 8+ chars and include letters, numbers, and symbols.");
            return;
        }

        if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
        {
            ShowError("Passwords do not match.");
            return;
        }

        if (!(termsCheckBox?.IsChecked ?? false))
        {
            ShowError("You must agree to the Terms of Service and Privacy Policy.");
            return;
        }

        ToggleBusy(true);

        try
        {
            var result = await _registrationService.RegisterAsync(new RegistrationRequest
            {
                Name = name,
                Email = email,
                Password = password
            });

            if (!result.Success)
            {
                ShowError(result.ErrorMessage ?? "Could not create account. Please try again.");
                return;
            }

            var code = await DisplayPromptAsync("Email Verification", "Enter the verification code sent to your email.", "Verify", "Cancel", keyboard: Keyboard.Numeric);
            if (string.IsNullOrWhiteSpace(code))
            {
                ShowError("Verification is required before login.");
                return;
            }

            var verified = await _registrationService.VerifyEmailCodeAsync(email, code);
            if (!verified)
            {
                ShowError("Invalid or expired verification code.");
                return;
            }

            var message = result.NeedsAdminApproval
                ? "Email verified. Your account now awaits admin approval before login."
                : "Email verified. Your account is active and ready to use.";

            if (result.IsFirstAdmin)
            {
                message = "Email verified. You are the first account and have permanent admin access.";
            }

            await DisplayAlert("Account Created", message, "OK");
            await App.NavigateToPage(new LoginPage());
        }
        catch
        {
            ShowError("Something went wrong. Please try again in a moment.");
        }
        finally
        {
            ToggleBusy(false);
        }
    }

    private void ToggleBusy(bool on)
    {
        busyIndicator.IsVisible = on;
        busyIndicator.IsRunning = on;

        createButton.IsEnabled = !on;
        nameEntry.IsEnabled = !on;
        emailEntry.IsEnabled = !on;
        passwordEntry.IsEnabled = !on;
        confirmPasswordEntry.IsEnabled = !on;
        termsCheckBox.IsEnabled = !on;
    }

    private void ShowError(string message)
    {
        errorMessageLabel.Text = message;
        errorMessageLabel.IsVisible = true;
    }

    private bool IsValidEmail(string email)
        => Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private bool IsStrongPassword(string pwd)
    {
        if (string.IsNullOrWhiteSpace(pwd) || pwd.Length < 8) return false;
        bool hasLetter = false, hasDigit = false, hasSymbol = false;

        foreach (var c in pwd)
        {
            if (char.IsLetter(c)) hasLetter = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else hasSymbol = true;
        }

        return hasLetter && hasDigit && hasSymbol;
    }

    private void OnTogglePasswordVisibility(object sender, EventArgs e)
    {
        _showPassword = !_showPassword;
        passwordEntry.IsPassword = !_showPassword;
        if (sender is Button b) b.Text = _showPassword ? "Hide" : "Show";
    }

    private void OnToggleConfirmVisibility(object sender, EventArgs e)
    {
        _showConfirm = !_showConfirm;
        confirmPasswordEntry.IsPassword = !_showConfirm;
        if (sender is Button b) b.Text = _showConfirm ? "Hide" : "Show";
    }

    private async void OnLoginRedirectClicked(object sender, EventArgs e)
    {
        await App.NavigateToPage(new LoginPage());
    }
}

public record RegistrationRequest
{
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string Password { get; init; } = "";
}

public sealed class RegistrationResult
{
    public bool Success { get; set; }
    public bool IsFirstAdmin { get; set; }
    public bool NeedsAdminApproval { get; set; }
    public string? ErrorMessage { get; set; }

    public static RegistrationResult Fail(string msg) => new() { Success = false, ErrorMessage = msg };
}

public interface IRegistrationService
{
    Task<RegistrationResult> RegisterAsync(RegistrationRequest request);
    Task<bool> VerifyEmailCodeAsync(string email, string code);
}

public interface IEmailService
{
    Task SendVerificationCodeAsync(string email, string code);
}

public sealed class RegistrationService : IRegistrationService
{
    private static readonly Lazy<RegistrationService> _lazy = new(() => new RegistrationService());
    public static RegistrationService Instance => _lazy.Value;

    public string? ApiEndpoint { get; set; } = null;

    private readonly HttpClient _http = new();
    private readonly IEmailService _emailService = new SmtpEmailService();

    private RegistrationService() { }

    public async Task<RegistrationResult> RegisterAsync(RegistrationRequest request)
    {
        if (string.IsNullOrWhiteSpace(ApiEndpoint))
        {
            var created = await App.DatabaseService.RegisterUser(request.Name, request.Email, request.Password);
            if (!created.Success)
            {
                return RegistrationResult.Fail(created.ErrorMessage ?? "Could not create account.");
            }

            var code = await App.DatabaseService.GetVerificationCodeForEmail(request.Email);
            if (string.IsNullOrWhiteSpace(code))
            {
                return RegistrationResult.Fail("Could not generate verification code.");
            }

            await _emailService.SendVerificationCodeAsync(request.Email, code);
            return new RegistrationResult
            {
                Success = true,
                IsFirstAdmin = created.IsFirstAdmin,
                NeedsAdminApproval = created.NeedsAdminApproval
            };
        }

        try
        {
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(ApiEndpoint, content);

            if (resp.IsSuccessStatusCode)
                return new RegistrationResult { Success = true };

            var body = await resp.Content.ReadAsStringAsync();
            if ((int)resp.StatusCode == 409) return RegistrationResult.Fail("This email is already in use.");
            if ((int)resp.StatusCode == 400) return RegistrationResult.Fail("Invalid registration details.");
            return RegistrationResult.Fail(string.IsNullOrWhiteSpace(body) ? "Registration failed." : body);
        }
        catch
        {
            return RegistrationResult.Fail("Network error. Please try again.");
        }
    }

    public Task<bool> VerifyEmailCodeAsync(string email, string code)
        => App.DatabaseService.VerifyEmailCode(email, code);
}

public sealed class SmtpEmailService : IEmailService
{
    public async Task SendVerificationCodeAsync(string email, string code)
    {
        var host = Environment.GetEnvironmentVariable("MADINA_SMTP_HOST");
        var user = Environment.GetEnvironmentVariable("MADINA_SMTP_USER");
        var pass = Environment.GetEnvironmentVariable("MADINA_SMTP_PASS");
        var from = Environment.GetEnvironmentVariable("MADINA_SMTP_FROM") ?? user;

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(pass) || string.IsNullOrWhiteSpace(from))
        {
            // Fallback to local debug output when SMTP is not configured.
            System.Diagnostics.Debug.WriteLine($"[EmailFallback] verification code for {email}: {code}");
            return;
        }

        using var client = new SmtpClient(host)
        {
            Port = int.TryParse(Environment.GetEnvironmentVariable("MADINA_SMTP_PORT"), out var p) ? p : 587,
            EnableSsl = true,
            Credentials = new NetworkCredential(user, pass)
        };

        using var message = new MailMessage(from, email)
        {
            Subject = "Madina Enterprises verification code",
            Body = $"Your verification code is {code}. It expires in 15 minutes.",
            IsBodyHtml = false
        };

        await client.SendMailAsync(message);
    }
}
