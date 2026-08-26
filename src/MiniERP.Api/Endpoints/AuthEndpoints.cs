using MiniERP.Api.Security;
using MiniERP.Application.Abstractions;

namespace MiniERP.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        // Gọi THẲNG OAuth2/Token của IdP ngoài (mẫu iNOS) — không qua wrapper trung gian.
        group.MapPost("/login-sso", async (LoginRequest req, IIdentityProviderClient idp, CancellationToken ct) =>
        {
            var token = await idp.LoginAsync(req.Email, req.Password, ct);
            return Results.Ok(token);
        }).WithSummary("Login trực tiếp vào IdP ngoài (OAuth2 ROPC, không qua hàm trung gian)");

        // Phát JWT nội bộ mang role=PartnerType để test RBAC trên các endpoint ERP (demo, không gọi IdP ngoài).
        group.MapPost("/token", (TokenRequest req, JwtTokenIssuer issuer) =>
        {
            var token = issuer.IssueToken(req.Email, req.PartnerCode, req.Role);
            return Results.Ok(new { accessToken = token });
        }).WithSummary("Phát JWT nội bộ theo role (demo RBAC) — email/partnerCode/role bất kỳ cho môi trường dev");
    }

    public sealed record LoginRequest(string Email, string Password);
    public sealed record TokenRequest(string Email, string PartnerCode, string Role);
}
