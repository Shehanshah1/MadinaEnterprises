/*
TODO: ForgotPasswordPage – production-readiness checklist

SECURITY & PRIVACY
- [ ] Backend endpoint: implement POST /auth/password-reset-request that ALWAYS returns 200 (no account enumeration).
- [ ] Token: generate single-use, time-boxed (~15–30 min) reset token; bind to user, IP/device hints, and rotation counter.
- [ ] Storage: hash tokens (HMAC or random opaque mapped server-side); never store raw tokens; set max outstanding tokens/user.
- [ ] Transport: enforce HTTPS; consider TLS pinning on mobile builds.
- [ ] Throttling: rate limit by user/email/IP/device; add exponential backoff on repeated requests.
- [ ] CAPTCHA/human verification after N failures (reCAPTCHA/Turnstile) to deter automation.
- [ ] CSRF: if this page is hosted as web, include an anti-CSRF token; not required for native MAUI → API call.
- [ ] Logging: structured, privacy-safe logs (no PII, no raw tokens); log success/failure + reasons for ops/Audit.
- [ ] Alerts: security alerting on spikes (possible attack) and on mail send failures.
- [ ] Secrets: move ApiEndpoint and API keys to secure config (KeyChain/Keystore + server vault), never in code.

EMAIL DELIVERY
- [ ] Mail content: include user name (if available), device/IP (optional), and clear “ignore if not you” text.
- [ ] Link: deep link / universal link scheme (app:// or https://) that opens the in-app Reset page with token.
- [ ] Deliverability: configure SPF, DKIM, DMARC; monitor bounce/complaint rates.
- [ ] Template: locale-aware HTML + plaintext; dark-mode friendly; brand-consistent.

RESET FLOW (downstream page)
- [ ] ResetPasswordPage: accept token; validate + show “set new password” with:
      - [ ] Password policy (length/complexity/breach check with k-Anon HIBP or similar)
      - [ ] Show/Hide, strength meter, confirm field, paste handling
- [ ] On success: invalidate token (single use), rotate auth sessions, prompt re-login.
- [ ] UX: success screen with “Back to Login” and optional auto-redirect.

API CLIENT ROBUSTNESS
- [ ] CancellationToken support on requests; set HttpClient timeout; transient retry with jitter (Polly) but NO retries on 4xx.
- [ ] Resend cooldown UI (e.g., “Resend in 60s”) to prevent spam and rate limits.
- [ ] Offline handling: graceful error if no network.

ACCESSIBILITY & UX
- [ ] Labels linked to inputs; larger touch targets; semantic hints for screen readers.
- [ ] Clear, user-safe copy: “If this email exists…” already done (prevents enumeration).
- [ ] Localization: move strings to resources; support RTL languages.

TESTING & QUALITY
- [ ] Unit tests for email validation, service success/failure paths, and throttle behavior.
- [ ] UI tests: send flow, error banners, spinner state.
- [ ] Contract tests with backend (OpenAPI) to validate payloads/status codes.
- [ ] Telemetry: add events (reset_requested, reset_failed, reset_sent) with privacy budgets.

OPERATIONS
- [ ] Rate-limit configuration & dashboards; error budget + SLO for the endpoint.
- [ ] Run security review + basic pen-test of the auth flows.
*/


using System.Text.RegularExpressions;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MadinaEnterprises.Modules.Views;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly IPasswordResetService _passwordResetService;

    public ForgotPasswordPage(IPasswordResetService? service = null)
    {
        InitializeComponent();
        _passwordResetService = service ?? PasswordResetService.Instance;
    }

    private async void OnSendLinkClicked(object sender, EventArgs e)
    {
        errorMessageLabel.IsVisible = false;

        var email = emailEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(email) || !IsValidEmail(email))
        {
            ShowError("Please enter a valid email address.");
            return;
        }

        ToggleBusy(true);

        try
        {
            var result = await _passwordResetService.SendResetAsync(email);

            if (result.Success)
            {
                await DisplayAlert("Reset Sent",
                    "If this email exists in our system, a reset link has been sent.",
                    "OK");
                await App.NavigateToPage(new LoginPage());
            }
            else
            {
                ShowError(result.ErrorMessage ?? "Unable to send reset link. Please try again.");
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
        emailEntry.IsEnabled = !on;
        sendButton.IsEnabled = !on; // directly reference the button
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

/* ===== Service layer (same as before) ===== */
public interface IPasswordResetService
{
    Task<PasswordResetResult> SendResetAsync(string email);
}

public sealed class PasswordResetResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public static PasswordResetResult Ok() => new() { Success = true };
    public static PasswordResetResult Fail(string msg) => new() { Success = false, ErrorMessage = msg };
}

public sealed class PasswordResetService : IPasswordResetService
{
    private static readonly Lazy<PasswordResetService> _lazy = new(() => new PasswordResetService());
    public static PasswordResetService Instance => _lazy.Value;

    public string? ApiEndpoint { get; set; } = null; // e.g., "https://api.example.com/auth/request-reset"
    private readonly HttpClient _http = new();

    private PasswordResetService() { }

    public async Task<PasswordResetResult> SendResetAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(ApiEndpoint))
        {
            // Keep response generic to avoid user enumeration while still performing a real lookup.
            _ = await App.DatabaseService.UserExists(email);
            await Task.Delay(300);
            return PasswordResetResult.Ok();
        }

        try
        {
            var json = JsonSerializer.Serialize(new { email });
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = await _http.PostAsync(ApiEndpoint, content);
            return resp.IsSuccessStatusCode
                ? PasswordResetResult.Ok()
                : PasswordResetResult.Fail("Unable to send reset link.");
        }
        catch
        {
            return PasswordResetResult.Fail("Network error. Please try again.");
        }
    }
}
