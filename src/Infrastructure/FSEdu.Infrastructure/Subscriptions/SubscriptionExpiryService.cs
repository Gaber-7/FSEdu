using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FSEdu.Infrastructure.Subscriptions;

// Periodically marks expired subscriptions and notifies users whose
// subscription is about to expire (within the next 3 days).
public sealed class SubscriptionExpiryService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan WarningWindow = TimeSpan.FromDays(3);
    private static readonly TimeSpan WarningDedupWindow = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionExpiryService> _log;

    public SubscriptionExpiryService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionExpiryService> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait a few seconds at startup so DI + DB are ready
        try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); } catch { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (OperationCanceledException) { return; }
            catch (Exception ex) { _log.LogError(ex, "SubscriptionExpiryService run failed"); }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;

        // 1) Expire subscriptions whose end date has passed.
        var expired = await db.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active && s.EndsAtUtc < now)
            .ToListAsync(ct);

        foreach (var sub in expired)
        {
            sub.Expire();
        }
        if (expired.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            _log.LogInformation("Expired {Count} subscriptions", expired.Count);

            foreach (var sub in expired)
            {
                await notifications.SendAsync(sub.UserId,
                    "subscription.expired",
                    "⏰ انتهى اشتراكك",
                    "انتهت صلاحية اشتراكك. جدّد الآن لاستعادة الوصول إلى المحتوى.",
                    new { subscriptionId = sub.Id, url = "/subscribe" }, ct);
            }
        }

        // 2) Warn subscriptions expiring within the next 3 days, deduped per 24h.
        var warningCutoff = now.Add(WarningWindow);
        var dedupCutoff = now - WarningDedupWindow;

        var soon = await db.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active
                     && s.EndsAtUtc >= now
                     && s.EndsAtUtc <= warningCutoff)
            .ToListAsync(ct);

        foreach (var sub in soon)
        {
            var marker = $"\"subscriptionId\":\"{sub.Id}\"";
            var alreadyWarned = await db.Notifications.AnyAsync(n =>
                n.UserId == sub.UserId
                && n.Type == NotificationTypes.SubscriptionExpiring
                && n.CreatedAtUtc > dedupCutoff
                && n.DataJson != null
                && n.DataJson.Contains(marker), ct);
            if (alreadyWarned) continue;

            var daysLeft = (int)Math.Ceiling((sub.EndsAtUtc - now).TotalDays);
            await notifications.SendAsync(sub.UserId,
                NotificationTypes.SubscriptionExpiring,
                "⏳ اشتراكك قارب على الانتهاء",
                $"يتبقى {daysLeft} يوم على انتهاء اشتراكك. جدّد الآن لتجنب الانقطاع.",
                new { subscriptionId = sub.Id, daysLeft, url = "/subscribe" }, ct);
        }
    }
}
