using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;

namespace GradLink.Client.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    // The API writes ClaimTypes.* URIs; map the short JWT names too in case a token uses them.
    private static readonly Dictionary<string, string> ClaimAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["role"] = ClaimTypes.Role,
        ["nameid"] = ClaimTypes.NameIdentifier,
        ["sub"] = ClaimTypes.NameIdentifier
    };

    private readonly LocalStorageService _localStorage;

    public CustomAuthenticationStateProvider(LocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _localStorage.GetItemAsync("authToken");
        if (string.IsNullOrWhiteSpace(token))
            return Anonymous;

        var user = ParseToken(token);
        if (user == null)
        {
            // Malformed or expired: forget it so the user is sent back to the login page.
            await _localStorage.RemoveItemAsync("authToken");
            return Anonymous;
        }

        return new AuthenticationState(user);
    }

    public void NotifyUserAuthentication(string token)
    {
        var user = ParseToken(token);
        NotifyAuthenticationStateChanged(Task.FromResult(user == null ? Anonymous : new AuthenticationState(user)));
    }

    public void NotifyUserLogout()
    {
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    /// <summary>
    /// Reads the claims from the JWT payload, or returns null if the token is malformed or expired.
    /// The signature is not checked here; the API does that on every request.
    /// </summary>
    private static ClaimsPrincipal? ParseToken(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
            return null;

        try
        {
            using var payload = JsonDocument.Parse(DecodeBase64Url(parts[1]));
            var root = payload.RootElement;

            if (root.TryGetProperty("exp", out var exp) &&
                DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()) <= DateTimeOffset.UtcNow)
                return null;

            var claims = root.EnumerateObject()
                .Select(p => new Claim(ClaimAliases.GetValueOrDefault(p.Name, p.Name), p.Value.ToString()));

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", ClaimTypes.NameIdentifier, ClaimTypes.Role));
        }
        catch (Exception e) when (e is FormatException or JsonException or InvalidOperationException)
        {
            return null;
        }
    }

    // JWTs use unpadded base64url ('-' and '_' instead of '+' and '/').
    private static byte[] DecodeBase64Url(string value)
    {
        var base64 = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '='));
    }
}
