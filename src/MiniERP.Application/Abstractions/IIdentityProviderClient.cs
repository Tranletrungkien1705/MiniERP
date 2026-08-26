namespace MiniERP.Application.Abstractions;

// Trừu tượng hoá cho SSO ngoài (mẫu iNOS: OAuth2 Resource-Owner-Password-Credentials).
// Implementation thật nằm ở Infrastructure, gọi thẳng {AuthorityUrl}/OAuth/Token — không qua wrapper trung gian.
public interface IIdentityProviderClient
{
    Task<IdentityTokenResult> LoginAsync(string email, string password, CancellationToken ct = default);
    Task<IdentityUserInfo> GetCurrentUserAsync(string accessToken, CancellationToken ct = default);
}

public sealed record IdentityTokenResult(string AccessToken, string? RefreshToken, DateTimeOffset ExpiresAt);

public sealed record IdentityUserInfo(string Email, string Name, string[] Roles);
