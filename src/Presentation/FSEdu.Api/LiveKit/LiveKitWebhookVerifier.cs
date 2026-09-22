using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace FSEdu.Api.LiveKit;

// Verifies an incoming LiveKit webhook request:
//   1. Authorization header carries a JWT signed (HS256) with the API secret.
//   2. Issuer (iss) matches our API key.
//   3. The JWT's `sha256` claim equals base64(sha256(rawBody)).
public static class LiveKitWebhookVerifier
{
    public static bool Verify(string authHeader, string body, string apiKey, string apiSecret)
    {
        if (string.IsNullOrEmpty(authHeader) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
            return false;

        // Some senders prefix with "Bearer "
        var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader.Substring(7).Trim()
            : authHeader.Trim();

        var handler = new JwtSecurityTokenHandler();
        var validation = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = apiKey,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(apiSecret))
        };

        try
        {
            var principal = handler.ValidateToken(token, validation, out var validated);
            if (validated is not JwtSecurityToken jwt) return false;

            var shaClaim = jwt.Claims.FirstOrDefault(c => c.Type == "sha256")?.Value;
            if (string.IsNullOrEmpty(shaClaim)) return false;

            var expected = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(body)));
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(shaClaim),
                Encoding.UTF8.GetBytes(expected));
        }
        catch
        {
            return false;
        }
    }
}
