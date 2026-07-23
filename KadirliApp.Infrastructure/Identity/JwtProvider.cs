using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KadirliApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace KadirliApp.Infrastructure.Identity;

/// <summary>
/// Faz 10.2: access + refresh + temp (registration) token üretimi/doğrulaması.
/// GÜVENLİK: refresh ve temp token'lar RefreshSecret ile imzalanır ve token_type claim'i taşır —
/// JwtBearer yalnız AccessSecret'ı kabul ettiğinden bu token'lar [Authorize] uçlarından geçemez;
/// kendi aralarında da token_type kontrolüyle ayrışırlar (refresh, register'da kullanılamaz ve tersi).
/// </summary>
public sealed class JwtProvider : IJwtProvider
{
    private const string RefreshTokenType = "refresh";
    private const string RegistrationTokenType = "registration";

    private readonly IConfiguration _configuration;

    public JwtProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private string AccessSecret => _configuration["Jwt:AccessSecret"]
        ?? throw new InvalidOperationException("JWT AccessSecret is missing");

    private string RefreshSecret => _configuration["Jwt:RefreshSecret"]
        ?? throw new InvalidOperationException("JWT RefreshSecret is missing");

    public AuthTokens GenerateTokens(Guid userId, string role, string phone)
    {
        // double: canlı testte kısa ömür verilebilsin diye (ör. 0.0002 ≈ 17 sn); config'te normalde tam gün.
        var accessLifetime = TimeSpan.FromDays(_configuration.GetValue("Jwt:AccessExpiresDays", 1d));
        var refreshLifetime = TimeSpan.FromDays(_configuration.GetValue("Jwt:RefreshExpiresDays", 90d));

        var accessClaims = new[]
        {
            new Claim("user_id", userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("phone", phone)
        };
        var access = BuildToken(accessClaims, AccessSecret, accessLifetime);

        var refreshClaims = new[]
        {
            new Claim("user_id", userId.ToString()),
            new Claim("phone", phone),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("token_type", RefreshTokenType)
        };
        var refresh = BuildToken(refreshClaims, RefreshSecret, refreshLifetime);

        return new AuthTokens(access, refresh, (int)accessLifetime.TotalSeconds);
    }

    public string GenerateTempToken(string phone)
    {
        var minutes = _configuration.GetValue("Jwt:TempTokenMinutes", 30);
        var claims = new[]
        {
            new Claim("phone", phone),
            new Claim("token_type", RegistrationTokenType)
        };
        return BuildToken(claims, RefreshSecret, TimeSpan.FromMinutes(minutes));
    }

    public string? ValidateTempToken(string tempToken)
    {
        var validated = Validate(tempToken, RefreshSecret);
        if (validated is not { } v || v.Principal.FindFirstValue("token_type") != RegistrationTokenType)
            return null;

        return v.Principal.FindFirstValue("phone");
    }

    public RefreshTokenPayload? ValidateRefreshToken(string refreshToken)
    {
        var validated = Validate(refreshToken, RefreshSecret);
        if (validated is not { } v || v.Principal.FindFirstValue("token_type") != RefreshTokenType)
            return null;

        var jti = v.Principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
        if (jti == null || !Guid.TryParse(v.Principal.FindFirstValue("user_id"), out var userId))
            return null;

        return new RefreshTokenPayload(userId, jti, v.Token.ValidTo);
    }

    private string BuildToken(IEnumerable<Claim> claims, string secret, TimeSpan lifetime)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private (ClaimsPrincipal Principal, SecurityToken Token)? Validate(string token, string secret)
    {
        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear(); // claim adları üretildiği gibi kalsın (user_id, phone, jti)

        try
        {
            var principal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            return (principal, validatedToken);
        }
        catch
        {
            // İmza/issuer/audience/süre hatalarının hepsi "geçersiz token" — çağıran 401'e çevirir.
            return null;
        }
    }
}
