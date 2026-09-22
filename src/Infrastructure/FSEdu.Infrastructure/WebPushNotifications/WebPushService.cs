using System.Text.Json;
using FSEdu.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebPush;

namespace FSEdu.Infrastructure.WebPushNotifications;

// Sends VAPID-signed Web Push payloads to every subscription a user has.
// Falls back to a no-op when keys aren't configured (so the app still works
// without push set up).
public sealed class WebPushService : IWebPushService
{
    private readonly WebPushOptions _options;
    private readonly IApplicationDbContext _db;
    private readonly ILogger<WebPushService> _log;
    private readonly WebPushClient? _client;

    public WebPushService(IOptions<WebPushOptions> options, IApplicationDbContext db, ILogger<WebPushService> log)
    {
        _options = options.Value;
        _db = db;
        _log = log;
        if (IsConfigured)
        {
            _client = new WebPushClient();
            _client.SetVapidDetails(_options.Subject, _options.PublicKey, _options.PrivateKey);
        }
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.PublicKey) && !string.IsNullOrWhiteSpace(_options.PrivateKey);

    public string GetPublicKey() => _options.PublicKey ?? "";

    public async Task SendAsync(Guid userId, string title, string body, string? url, CancellationToken ct = default)
    {
        if (!IsConfigured || _client is null) return;

        var subs = await _db.PushSubscriptions
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);
        if (subs.Count == 0) return;

        var payload = JsonSerializer.Serialize(new { title, body, url });

        foreach (var sub in subs)
        {
            var browserSub = new global::WebPush.PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
            try
            {
                await _client.SendNotificationAsync(browserSub, payload, cancellationToken: ct);
                sub.MarkUsed();
            }
            catch (WebPushException ex) when ((int)ex.StatusCode == 404 || (int)ex.StatusCode == 410)
            {
                // Subscription expired/unsubscribed — clean up.
                _db.PushSubscriptions.Remove(sub);
                _log.LogInformation("Removed dead push subscription for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                sub.MarkFailed();
                _log.LogWarning(ex, "Push send failed for user {UserId}", userId);
            }
        }

        try { await _db.SaveChangesAsync(ct); } catch { /* best-effort */ }
    }
}
