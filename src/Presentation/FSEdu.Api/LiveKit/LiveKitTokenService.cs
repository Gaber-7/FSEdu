using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FSEdu.Api.LiveKit;

public interface ILiveKitTokenService
{
    string GenerateToken(string roomName, string userIdentity, string userName,
                         bool canPublish, TimeSpan? validFor = null);
}

public sealed class LiveKitTokenService : ILiveKitTokenService
{
    private readonly LiveKitOptions _options;

    public LiveKitTokenService(IOptions<LiveKitOptions> options) => _options = options.Value;

    public string GenerateToken(string roomName, string userIdentity, string userName,
                                 bool canPublish, TimeSpan? validFor = null)
    {
        var validity = validFor ?? TimeSpan.FromHours(4);

        // LiveKit "video" grants — this is the special claim LiveKit reads
        var videoGrant = new
        {
            room = roomName,
            roomJoin = true,
            canPublish = canPublish,
            canSubscribe = true,
            canPublishData = true,
            canUpdateOwnMetadata = true
        };

        var videoJson = JsonSerializer.Serialize(videoGrant);

        var claims = new List<Claim>
        {
            new("sub", userIdentity),
            new("name", userName),
            new("video", videoJson, JsonClaimValueTypes.Json)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.ApiSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _options.ApiKey,
            audience: null,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.Add(validity),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
