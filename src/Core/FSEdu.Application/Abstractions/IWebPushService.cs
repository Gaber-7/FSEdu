namespace FSEdu.Application.Abstractions;

public interface IWebPushService
{
    /// <summary>Send a push notification to all of a user's registered browsers.</summary>
    Task SendAsync(Guid userId, string title, string body, string? url, CancellationToken ct = default);

    /// <summary>The VAPID public key (URL-safe base64) the browser needs to subscribe.</summary>
    string GetPublicKey();

    /// <summary>True if web push is configured and operational.</summary>
    bool IsConfigured { get; }
}
