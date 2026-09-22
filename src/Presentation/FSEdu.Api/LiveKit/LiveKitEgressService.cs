using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FSEdu.Application.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FSEdu.Api.LiveKit;

// Calls the LiveKit Egress Twirp HTTP API to start/stop room-composite recordings.
// Tracks active egress IDs in-memory keyed by session id (recording sessions are
// short-lived; on restart we lose the map but the recording finishes naturally and
// the egress_ended webhook still attaches the file URL to the LiveSession).
public sealed class LiveKitEgressService : IEgressService
{
    private readonly LiveKitOptions _options;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<LiveKitEgressService> _log;
    private static readonly ConcurrentDictionary<Guid, string> _active = new();

    public LiveKitEgressService(
        IOptions<LiveKitOptions> options,
        IHttpClientFactory httpFactory,
        ILogger<LiveKitEgressService> log)
    {
        _options = options.Value;
        _httpFactory = httpFactory;
        _log = log;
    }

    public bool IsRecording(Guid sessionId) => _active.ContainsKey(sessionId);

    public async Task<string> StartRoomRecordingAsync(string roomName, Guid sessionId, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("livekit-egress");
        var token = GenerateAdminToken();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var fileName = $"{roomName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.mp4";
        var body = new
        {
            room_name = roomName,
            layout = "grid",
            file_outputs = new[]
            {
                new { filepath = $"/recordings/{fileName}" }
            }
        };

        var url = TwirpUrl("/twirp/livekit.Egress/StartRoomCompositeEgress");
        using var resp = await http.PostAsJsonAsync(url, body, ct);
        var payload = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Egress start failed ({(int)resp.StatusCode}): {payload}");

        using var doc = JsonDocument.Parse(payload);
        var egressId = doc.RootElement.TryGetProperty("egress_id", out var idProp)
            ? idProp.GetString() ?? string.Empty
            : string.Empty;
        if (string.IsNullOrEmpty(egressId))
            throw new InvalidOperationException("Egress response missing egress_id: " + payload);

        _active[sessionId] = egressId;
        _log.LogInformation("Started egress {EgressId} for room {Room} (session {Session})",
            egressId, roomName, sessionId);
        return egressId;
    }

    public async Task StopRecordingAsync(Guid sessionId, CancellationToken ct)
    {
        if (!_active.TryRemove(sessionId, out var egressId)) return;

        var http = _httpFactory.CreateClient("livekit-egress");
        var token = GenerateAdminToken();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var url = TwirpUrl("/twirp/livekit.Egress/StopEgress");
        var body = new { egress_id = egressId };
        using var resp = await http.PostAsJsonAsync(url, body, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var text = await resp.Content.ReadAsStringAsync(ct);
            _log.LogWarning("Stop egress {EgressId} returned {Status}: {Body}",
                egressId, (int)resp.StatusCode, text);
        }
    }

    private string TwirpUrl(string path)
    {
        // _options.Url is the public WSS URL returned to browsers (e.g. wss://host/livekit).
        // For server-to-server Twirp we want the internal HTTP endpoint instead.
        var baseUrl = string.IsNullOrEmpty(_options.HttpUrl) ? ToHttp(_options.Url) : _options.HttpUrl;
        return baseUrl.TrimEnd('/') + path;
    }

    private static string ToHttp(string wsUrl)
    {
        if (wsUrl.StartsWith("wss://", StringComparison.OrdinalIgnoreCase))
            return "https://" + wsUrl.Substring(6);
        if (wsUrl.StartsWith("ws://", StringComparison.OrdinalIgnoreCase))
            return "http://" + wsUrl.Substring(5);
        return wsUrl;
    }

    // Admin JWT for Twirp API access — needs roomRecord (egress) grants.
    private string GenerateAdminToken()
    {
        var videoGrant = new
        {
            roomRecord = true,
            roomCreate = true,
            roomList = true,
            roomAdmin = true,
            room = "*"
        };
        var videoJson = JsonSerializer.Serialize(videoGrant);

        var claims = new List<Claim>
        {
            new("sub", "fsedu-api"),
            new("video", videoJson, JsonClaimValueTypes.Json)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.ApiSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: _options.ApiKey,
            audience: null,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
