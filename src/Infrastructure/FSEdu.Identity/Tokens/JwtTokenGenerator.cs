using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FSEdu.Application.Abstractions;
using FSEdu.Identity.Entities;
using FSEdu.Shared.Kernel.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FSEdu.Identity.Tokens;

public interface IJwtTokenGenerator
{
    Task<AuthTokens> GenerateAsync(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
}

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly IDateTimeProvider _clock;

    public JwtTokenGenerator(IOptions<JwtOptions> options, IDateTimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public Task<AuthTokens> GenerateAsync(ApplicationUser user, IList<string> roles)
    {
        var expires = _clock.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.Name, user.FullName),
            new("phone", user.PhoneNumber ?? ""),
        };

        if (!string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));

        if (user.DomainUserId.HasValue)
            claims.Add(new Claim("domain_user_id", user.DomainUserId.Value.ToString()));

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: _clock.UtcNow,
            expires: expires,
            signingCredentials: creds);

        var access = new JwtSecurityTokenHandler().WriteToken(token);
        var refresh = GenerateRefreshToken();

        return Task.FromResult(new AuthTokens(
            access, refresh, expires,
            user.Id,
            user.FullName,
            user.PhoneNumber ?? string.Empty,
            user.Email,
            roles.ToArray()));
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(bytes);
    }
}
