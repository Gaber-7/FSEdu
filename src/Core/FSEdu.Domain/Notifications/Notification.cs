using FSEdu.Domain.Common;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Notifications;

public sealed class Notification : AggregateRoot<Guid>
{
    public Guid UserId { get; private set; }
    public string Type { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public string? DataJson { get; private set; }
    public NotificationChannel Channel { get; private set; } = NotificationChannel.InApp;
    public DateTime? ReadAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Notification() { }

    public Notification(Guid id, Guid userId, string type, string title, string body,
                        NotificationChannel channel = NotificationChannel.InApp, string? dataJson = null)
        : base(id)
    {
        UserId = userId;
        Type = type;
        Title = title;
        Body = body;
        Channel = channel;
        DataJson = dataJson;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkRead() => ReadAtUtc ??= DateTime.UtcNow;
}
