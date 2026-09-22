using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Notifications;

// One push subscription per browser per user. A user can have many — phone,
// laptop, tablet — and each gets the push independently.
public sealed class PushSubscription : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Endpoint { get; private set; } = default!;
    public string P256dh { get; private set; } = default!;   // browser's public key (for encryption)
    public string Auth { get; private set; } = default!;     // shared secret
    public string? UserAgent { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? LastUsedAtUtc { get; private set; }
    public DateTime? FailedAtUtc { get; private set; }
    public int FailCount { get; private set; }

    private PushSubscription() { }

    public PushSubscription(Guid id, Guid userId, string endpoint, string p256dh, string auth, string? userAgent)
        : base(id)
    {
        UserId = userId;
        Endpoint = endpoint;
        P256dh = p256dh;
        Auth = auth;
        UserAgent = userAgent;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkUsed()
    {
        LastUsedAtUtc = DateTime.UtcNow;
        FailCount = 0;
    }

    public void MarkFailed()
    {
        FailedAtUtc = DateTime.UtcNow;
        FailCount++;
    }
}
