/*
TODO: CreateAccount – production-readiness

SECURITY
- [ ] Backend /auth/register must hash passwords with strong KDF (Argon2id / bcrypt / PBKDF2) and per-user salt.
- [ ] Enforce HTTPS; never log passwords or full tokens; secrets in secure vaults.
- [ ] Rate-limit by IP/user/device; apply CAPTCHA after repeated attempts.
- [ ] Email verification: send signed, time-limited token; block login until verified (configurable).
- [ ] Unique email constraint at DB + normalized email casing; transactional create (user + verification token).
- [ ] Privacy-safe logs/metrics (no PII beyond necessary IDs).

UX & ACCESSIBILITY
- [ ] Localize strings; support RTL; larger hit targets; semantic hints for screen readers.
- [ ] Password strength meter; breached-password check (k-anon HIBP).
- [ ] Terms/Privacy links to actual documents; record consent timestamp/version.

CLIENT ROBUSTNESS
- [ ] CancellationToken, HttpClient timeout, retry (Polly) on transient 5xx only.
- [ ] Offline detection & graceful failure; resend verification flow with cooldown.

TESTING & OPS
- [ ] Unit tests: validators; API client happy/sad paths; error mapping (409/400/5xx).
- [ ] E2E tests: register → verify → login.
- [ ] Observability: events (register_attempt, register_success, register_fail), dashboards & alerting on spikes.
*/

using System.Net.Http;
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

        // Required fields
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) ||
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            ShowError("All fields are required.");
            return;
        }

        // Basic name sanity
        if (name.Length < 2)
        {
            ShowError("Please enter your full name.");
            return;
        }

        // Email
        if (!IsValidEmail(email))
        {
            ShowError("Please enter a valid email.");
            return;
        }

        // Password policy
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

        // Terms
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

            if (result.Success)
            {
                await DisplayAlert("Account Created",
                    "We’ve sent a verification link to your email. Please verify to complete setup.",
                    "OK");
                await App.NavigateToPage(new LoginPage());
            }
            else
            {
                ShowError(result.ErrorMessage ?? "Could not create account. Please try again.");
            }
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

    // 8+ chars, at least one letter, one digit, one symbol
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

/* ===========================
   Service Layer (swappable)
   =========================== */

public record RegistrationRequest
{
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string Password { get; init; } = "";
}

public sealed class RegistrationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public static RegistrationResult Ok() => new() { Success = true };
    public static RegistrationResult Fail(string msg) => new() { Success = false, ErrorMessage = msg };
}

public interface IRegistrationService
{
    Task<RegistrationResult> RegisterAsync(RegistrationRequest request);
}

/// <summary>
/// Default stub implementation.
/// Swap ApiEndpoint to a real URL to integrate.
/// </summary>
public sealed class RegistrationService : IRegistrationService
{
    private static readonly Lazy<RegistrationService> _lazy = new(() => new RegistrationService());
    public static RegistrationService Instance => _lazy.Value;

    // Set to your real API, e.g. "https://api.madinaenterprises.com/auth/register"
    public string? ApiEndpoint { get; set; } = null;

    private readonly HttpClient _http = new();

    private RegistrationService() { }

    public async Task<RegistrationResult> RegisterAsync(RegistrationRequest request)
    {
        if (string.IsNullOrWhiteSpace(ApiEndpoint))
        {
            var created = await App.DatabaseService.RegisterUser(request.Name, request.Email, request.Password);
            return created
                ? RegistrationResult.Ok()
                : RegistrationResult.Fail("This email is already in use.");
        }

        try
        {
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(ApiEndpoint, content);

            if (resp.IsSuccessStatusCode)
                return RegistrationResult.Ok();

            var body = await resp.Content.ReadAsStringAsync();
            // Map typical errors (409: email exists, 400: invalid)
            if ((int)resp.StatusCode == 409) return RegistrationResult.Fail("This email is already in use.");
            if ((int)resp.StatusCode == 400) return RegistrationResult.Fail("Invalid registration details.");
            return RegistrationResult.Fail(string.IsNullOrWhiteSpace(body) ? "Registration failed." : body);
        }
        catch
        {
            return RegistrationResult.Fail("Network error. Please try again.");
        }
    }
}
