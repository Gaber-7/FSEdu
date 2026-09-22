using FSEdu.Application.Abstractions;
using FSEdu.Domain.Engagement;
using FSEdu.Shared.Contracts.Gamification;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSEdu.Infrastructure.Gamification;

public sealed class DailyChallengeService : IDailyChallengeService
{
    private readonly IApplicationDbContext _db;
    private readonly IGamificationService _gamification;
    private readonly INotificationService _notifications;
    private readonly ILogger<DailyChallengeService> _logger;

    // One new shield every N completed challenges
    private const int ShieldCadence = 5;

    public DailyChallengeService(IApplicationDbContext db, IGamificationService gamification,
                                 INotificationService notifications, ILogger<DailyChallengeService> logger)
    { _db = db; _gamification = gamification; _notifications = notifications; _logger = logger; }

    public async Task<DailyChallenge> EnsureTodayChallengeAsync(Guid studentId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var existing = await _db.DailyChallenges
            .FirstOrDefaultAsync(c => c.StudentId == studentId && c.Day == today, ct);
        if (existing is not null) return existing;

        // Deterministic-ish pick keyed by (student, day) — same student gets variety across days
        // but doesn't change mid-day on refresh.
        var seed = HashCode.Combine(studentId, today.DayNumber);
        var rnd = new Random(seed);
        var (type, target, xp) = PickChallenge(rnd);

        var challenge = new DailyChallenge(Guid.NewGuid(), studentId, today, type, target, xp);
        _db.DailyChallenges.Add(challenge);
        await _db.SaveChangesAsync(ct);
        return challenge;
    }

    public async Task RecordProgressAsync(Guid studentId, DailyChallengeType type, int amount = 1, CancellationToken ct = default)
    {
        if (amount <= 0) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var challenge = await _db.DailyChallenges
            .FirstOrDefaultAsync(c => c.StudentId == studentId && c.Day == today, ct);
        // Only records progress if today's challenge is of this type and still open.
        // Don't auto-create here — the student must have visited the dashboard first
        // to "see" their challenge. (Avoids stealth XP from background actions.)
        if (challenge is null || challenge.Type != type || challenge.IsCompleted) return;

        var transitioned = challenge.RecordProgress(amount);
        await _db.SaveChangesAsync(ct);

        if (transitioned)
        {
            // Award XP via the existing gamification service
            await _gamification.AwardXpAsync(studentId, challenge.XpReward, "daily-challenge", ct);

            // Every N completed daily challenges, grant a shield
            var completedCount = await _db.DailyChallenges
                .CountAsync(c => c.StudentId == studentId && c.CompletedAtUtc != null, ct);
            if (completedCount > 0 && completedCount % ShieldCadence == 0)
            {
                var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
                if (student is not null)
                {
                    student.AddStreakShield();
                    challenge.MarkShieldRewarded();
                    await _db.SaveChangesAsync(ct);
                }
            }

            await _notifications.SendAsync(studentId,
                "daily-challenge.completed",
                "🎯 أكملت تحدّى اليوم!",
                $"حصلت على {challenge.XpReward} XP." +
                (challenge.RewardedShield ? " ومكافأة: درع سلسلة جديد 🛡️" : ""),
                new { url = "/dashboard/student" },
                ct);

            _logger.LogInformation("Daily challenge completed by {Sid} ({Type}, +{Xp} XP)",
                studentId, type, challenge.XpReward);
        }
    }

    public async Task<DailyChallengeStatusDto> GetTodayStatusAsync(Guid studentId, CancellationToken ct = default)
    {
        var challenge = await EnsureTodayChallengeAsync(studentId, ct);
        var student = await _db.Students
            .Where(s => s.Id == studentId)
            .Select(s => new { s.CurrentStreakDays, s.StreakShields })
            .FirstOrDefaultAsync(ct);

        var (title, desc, icon) = LocalizeChallenge(challenge.Type, challenge.TargetValue);
        return new DailyChallengeStatusDto(
            challenge.Type.ToString(),
            title, desc, icon,
            challenge.TargetValue, challenge.ProgressValue, challenge.XpReward,
            challenge.IsCompleted, challenge.CompletedAtUtc,
            student?.CurrentStreakDays ?? 0,
            student?.StreakShields ?? 0);
    }

    // ─── Random challenge picker ────────────────────────
    private static (DailyChallengeType, int, int) PickChallenge(Random rnd)
    {
        // (type, target, xp) — keep targets achievable
        var options = new (DailyChallengeType, int, int)[]
        {
            (DailyChallengeType.WatchMinutes, 20, 30),
            (DailyChallengeType.WatchMinutes, 30, 40),
            (DailyChallengeType.CompleteLessons, 1, 25),
            (DailyChallengeType.CompleteLessons, 2, 40),
            (DailyChallengeType.SolveAssessment, 1, 35),
            (DailyChallengeType.SolvePastPaper, 1, 50),
            (DailyChallengeType.SubmitHomework, 1, 40),
        };
        return options[rnd.Next(options.Length)];
    }

    private static (string Title, string Desc, string Icon) LocalizeChallenge(DailyChallengeType type, int target) =>
        type switch
        {
            DailyChallengeType.WatchMinutes =>
                ($"شاهد {target} دقيقة اليوم", "أكمل دروسًا متواصلة لرفع تركيزك", "⏱"),
            DailyChallengeType.CompleteLessons =>
                ($"أنهِ {target} درس{(target > 1 ? "ين" : "ًا")} اليوم", "خطوة جديدة فى رحلتك التعليمية", "📚"),
            DailyChallengeType.SubmitHomework =>
                ("سلِّم واجبًا واحدًا اليوم", "لا تترك الواجبات تتراكم", "📝"),
            DailyChallengeType.SolveAssessment =>
                ("اجتز اختبارًا اليوم", "اختبر فهمك للدرس", "🎯"),
            DailyChallengeType.SolvePastPaper =>
                ("حلّ امتحانًا سابقًا اليوم", "ابنِ قوّتك على امتحانات حقيقية", "📜"),
            _ => ("تحدّى اليوم", "", "🌟"),
        };
}
