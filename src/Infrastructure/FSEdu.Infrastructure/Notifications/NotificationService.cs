using System.Text.Json;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Notifications;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Infrastructure.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _db;
    private readonly IWebPushService _push;
    private readonly IEmailSender _email;

    public NotificationService(IApplicationDbContext db, IWebPushService push, IEmailSender email)
    {
        _db = db;
        _push = push;
        _email = email;
    }

    public async Task SendAsync(Guid userId, string type, string title, string body,
                                 object? data = null, CancellationToken ct = default)
    {
        var category = NotificationCategories.CategoryOf(type);
        if (!await IsEnabledAsync(userId, category, ct))
            return; // user opted out of this category — skip storage + push entirely

        var notification = new Notification(
            Guid.NewGuid(), userId, type, title, body,
            FSEdu.Domain.Common.NotificationChannel.InApp,
            data is null ? null : JsonSerializer.Serialize(data));

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        var url = ExtractUrl(data);
        try { await _push.SendAsync(userId, title, body, url, ct); } catch { /* push is best-effort */ }

        // Fan out to email for high-importance event types
        if (ShouldEmail(type))
        {
            var email = await _db.Users
                .Where(u => u.Id == userId && u.Email != null)
                .Select(u => u.Email!.Value)
                .FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(email))
            {
                try { await _email.SendAsync(email, title, BuildHtmlBody(title, body, url), ct); }
                catch { /* best-effort */ }
            }
        }
    }

    public async Task SendBulkAsync(IEnumerable<Guid> userIds, string type, string title, string body,
                                     object? data = null, CancellationToken ct = default)
    {
        var category = NotificationCategories.CategoryOf(type);
        var ids = userIds.Distinct().ToList();

        // Bulk-load opt-outs in one query
        var optedOut = await _db.NotificationPreferences
            .Where(p => ids.Contains(p.UserId) && p.Category == category && !p.Enabled)
            .Select(p => p.UserId)
            .ToListAsync(ct);
        var optedOutSet = optedOut.ToHashSet();
        var targets = ids.Where(id => !optedOutSet.Contains(id)).ToList();
        if (targets.Count == 0) return;

        var dataJson = data is null ? null : JsonSerializer.Serialize(data);

        foreach (var userId in targets)
        {
            _db.Notifications.Add(new Notification(
                Guid.NewGuid(), userId, type, title, body,
                FSEdu.Domain.Common.NotificationChannel.InApp,
                dataJson));
        }

        await _db.SaveChangesAsync(ct);

        var url = ExtractUrl(data);
        foreach (var userId in targets)
        {
            try { await _push.SendAsync(userId, title, body, url, ct); } catch { /* best-effort */ }
        }
    }

    private async Task<bool> IsEnabledAsync(Guid userId, string category, CancellationToken ct)
    {
        // Default: enabled. Only an explicit row with Enabled=false blocks delivery.
        var pref = await _db.NotificationPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Category == category, ct);
        return pref is null || pref.Enabled;
    }

    private static string? ExtractUrl(object? data)
    {
        if (data is null) return null;
        try
        {
            using var doc = JsonDocument.Parse(JsonSerializer.Serialize(data));
            if (doc.RootElement.TryGetProperty("url", out var u) && u.ValueKind == JsonValueKind.String)
                return u.GetString();
        }
        catch { /* ignore */ }
        return null;
    }

    // Only "important" notifications go out by email — chatty types stay in-app only.
    private static bool ShouldEmail(string type) => type switch
    {
        NotificationTypes.PaymentApproved => true,
        NotificationTypes.PaymentRejected => true,
        NotificationTypes.SubscriptionExpiring => true,
        NotificationTypes.CourseApproved => true,
        NotificationTypes.CourseRejected => true,
        NotificationTypes.TeacherApproved => true,
        NotificationTypes.Welcome => true,
        "referral.rewarded" => true,
        _ => false
    };

    // Minimal branded HTML wrapper for the email body
    private static string BuildHtmlBody(string title, string body, string? url)
    {
        var safeTitle = System.Net.WebUtility.HtmlEncode(title);
        var safeBody = System.Net.WebUtility.HtmlEncode(body).Replace("\n", "<br />");
        var ctaHtml = string.IsNullOrEmpty(url)
            ? ""
            : $"<p style='margin:24px 0'><a href='{System.Net.WebUtility.HtmlEncode(url)}' " +
              "style='background:#5B4FE5;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:600'>افتح في المنصة ←</a></p>";
        return $@"<!doctype html><html dir='rtl' lang='ar'><body style='margin:0;background:#F8FAFC;font-family:system-ui,sans-serif;color:#0F172A'>
<div style='max-width:560px;margin:24px auto;background:#fff;border-radius:12px;padding:32px;box-shadow:0 1px 3px rgba(0,0,0,.05)'>
  <div style='font-weight:800;color:#5B4FE5;font-size:18px;margin-bottom:16px'>FSEdu</div>
  <h2 style='margin:0 0 12px;font-size:20px'>{safeTitle}</h2>
  <p style='line-height:1.7;color:#334155;margin:0 0 8px'>{safeBody}</p>
  {ctaHtml}
  <hr style='border:none;border-top:1px solid #E2E8F0;margin:24px 0' />
  <small style='color:#94A3B8'>إذا كنت لا ترغب في تلقّي هذه الرسائل، يمكنك تعديل تفضيلات الإشعارات من حسابك.</small>
</div></body></html>";
    }
}
