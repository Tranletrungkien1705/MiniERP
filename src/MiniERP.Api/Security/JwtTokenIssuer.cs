using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MiniERP.Api.Security;

// Sau khi xác thực qua IdP ngoài (IIdentityProviderClient), API tự phát JWT nội bộ mang role=PartnerType
// để [Authorize(Roles=...)] hoạt động trên các endpoint — tách biệt "ai bạn là" (SSO ngoài) khỏi
// "bạn được làm gì trong hệ ERP này" (RBAC nội bộ), đúng pattern IdP-ngoài + RBAC-trong của DMSSales/InBrand.
public sealed class JwtTokenIssuer(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public string IssueToken(string email, string partnerCode, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(ClaimTypes.Role, role),
            new Claim("partner_code", partnerCode),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
