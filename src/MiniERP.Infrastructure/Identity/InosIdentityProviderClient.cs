using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Options;
using MiniERP.Application.Abstractions;

namespace MiniERP.Infrastructure.Identity;

// Client_id/secret theo mẫu ROPC: KHÔNG hardcode, luôn đọc từ config/secret-store.
public sealed class IdentityProviderOptions
{
    public required string AuthorityUrl { get; init; }   // vd https://sso.example.vn
    public required string ClientId { get; init; }
    public required string ClientSecret { get; init; }
    public string Scope { get; init; } = "api";
}

// Gọi THẲNG endpoint OAuth2 Token của IdP ngoài (mẫu iNOS: ExchangeUserCredentialForToken),
// KHÔNG qua controller trung gian tự dựng lại WebServerClient mỗi request (đó là nguồn gốc chậm ở hệ cũ).
// HttpClient được cấp qua IHttpClientFactory + AddStandardResilienceHandler (retry/circuit-breaker/timeout built-in .NET 8+).
public sealed class InosIdentityProviderClient(HttpClient httpClient, IOptions<IdentityProviderOptions> options)
    : IIdentityProviderClient
{
    private readonly IdentityProviderOptions _options = options.Value;

    public async Task<IdentityTokenResult> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var authBytes = Encoding.ASCII.GetBytes($"{_options.ClientId}:{_options.ClientSecret}");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/OAuth/Token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = email,
                ["password"] = password,
                ["scope"] = _options.Scope,
            }),
            Headers = { Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes)) },
        };

        using var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(ct)
            ?? throw new InvalidOperationException("IdP trả về response rỗng.");

        return new IdentityTokenResult(
            payload.access_token,
            payload.refresh_token,
            DateTimeOffset.UtcNow.AddSeconds(payload.expires_in));
    }

    public async Task<IdentityUserInfo> GetCurrentUserAsync(string accessToken, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/accountapi/getcurrentuser")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) },
        };

        using var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<UserResponse>(ct)
            ?? throw new InvalidOperationException("IdP trả về response rỗng.");

        return new IdentityUserInfo(payload.email, payload.name, payload.roles ?? []);
    }

    private sealed record TokenResponse(string access_token, string? refresh_token, int expires_in);
    private sealed record UserResponse(string email, string name, string[]? roles);
}
