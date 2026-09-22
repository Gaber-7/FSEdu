using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FSEdu.Infrastructure.LiveClassroom;

// Periodically sends a one-time reminder to enrolled students for live sessions
// starting in the next 10–25 minutes. Dedup uses the notifications table so we
// don't spam if the service runs more often than the window slides.
public sealed class SessionReminderService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Lookahead = TimeSpan.FromMinutes(25);
    private static readonly TimeSpan MinLead = TimeSpan.FromMinutes(10);
    private const string ReminderType = "live.starting_soon";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SessionReminderService> _log;

    public SessionReminderService(IServiceScopeFactory scopeFactory, ILogger<SessionReminderService> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken); } catch { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await RunOnceAsync(stoppingToken); }
            catch (OperationCanceledException) { return; }
            catch (Exception ex) { _log.LogError(ex, "SessionReminderService run failed"); }

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
        var minStart = now.Add(MinLead);
        var maxStart = now.Add(Lookahead);

        // Sessions starting in [now+10min, now+25min] that are still scheduled
        var sessions = await db.LiveSessions
            .Where(s => s.Status == LiveSessionStatus.Scheduled
                     && s.ScheduledAtUtc >= minStart
                     && s.ScheduledAtUtc <= maxStart
                     && s.SubjectId != null)
            .ToListAsync(ct);
        if (sessions.Count == 0) return;

        foreach (var s in sessions)
        {
            // Find enrolled students (active subscription matching subject or stage)
            var subjectId = s.SubjectId!.Value;
            var stageId = s.StageId;
            var enrolledIds = await db.Subscriptions
                .Where(x => x.Status == SubscriptionStatus.Active
                         && x.StartsAtUtc <= now && x.EndsAtUtc >= now
                         && ((x.Type != SubscriptionType.StageFullTerm && x.SubjectId == subjectId)
                             || (x.Type == SubscriptionType.StageFullTerm && x.StageId == stageId)))
                .Select(x => x.UserId)
                .Distinct()
                .ToListAsync(ct);
            if (enrolledIds.Count == 0) continue;

            // Dedup: skip users who already received a reminder for THIS session.
            // Match by sessionId embedded in DataJson.
            var marker = $"\"sessionId\":\"{s.Id}\"";
            var alreadyReminded = await db.Notifications
                .Where(n => n.Type == ReminderType && enrolledIds.Contains(n.UserId)
                            && n.DataJson != null && n.DataJson.Contains(marker))
                .Select(n => n.UserId)
                .ToListAsync(ct);
            var skipSet = alreadyReminded.ToHashSet();

            var minutesAway = (int)Math.Round((s.ScheduledAtUtc - now).TotalMinutes);
            foreach (var uid in enrolledIds)
            {
                if (skipSet.Contains(uid)) continue;
                await notifications.SendAsync(uid,
                    ReminderType,
                    "⏰ حصتك تبدأ قريبًا!",
                    $"حصة \"{s.Title}\" تبدأ خلال {minutesAway} دقيقة. استعد للانضمام.",
                    new { sessionId = s.Id, url = $"/live/{s.Id}", minutesAway }, ct);
            }
        }
    }
}
