using FSEdu.Application.Abstractions;
using FSEdu.Domain.Engagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSEdu.Infrastructure.Gamification;

public sealed class GamificationService : IGamificationService
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notifications;
    private readonly ILogger<GamificationService> _logger;

    public GamificationService(IApplicationDbContext db, INotificationService notifications,
                                ILogger<GamificationService> logger)
    {
        _db = db; _notifications = notifications; _logger = logger;
    }

    public async Task<int> AwardXpAsync(Guid studentId, int points, string reason, CancellationToken ct = default)
    {
        if (points <= 0) return 0;

        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null) return 0;

        student.AddXp(points);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Awarded {Points} XP to {StudentId} for {Reason}", points, studentId, reason);
        return points;
    }

    public async Task<bool> TryAwardBadgeAsync(Guid studentId, string badgeCode, CancellationToken ct = default)
    {
        var badge = await _db.Badges.FirstOrDefaultAsync(b => b.Code == badgeCode, ct);
        if (badge is null) return false;

        var alreadyHas = await _db.StudentBadges
            .AnyAsync(sb => sb.StudentId == studentId && sb.BadgeId == badge.Id, ct);
        if (alreadyHas) return false;

        var studentBadge = new StudentBadge(studentId, badge.Id);
        _db.StudentBadges.Add(studentBadge);
        await _db.SaveChangesAsync(ct);

        // Notify student
        await _notifications.SendAsync(studentId,
            "badge.awarded",
            $"🏆 شارة جديدة: {badge.NameAr}",
            "تهانينا! حصلت على شارة جديدة.",
            new { badgeCode = badge.Code, url = "/student/achievements" }, ct);

        _logger.LogInformation("Awarded badge '{Code}' to student {StudentId}", badgeCode, studentId);
        return true;
    }

    public async Task<int> UpdateStreakAsync(Guid studentId, CancellationToken ct = default)
    {
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null) return 0;

        var lastLogin = student.LastLoginAtUtc;
        var now = DateTime.UtcNow;

        if (lastLogin is null)
        {
            // First login ever
            student.IncrementStreak();
        }
        else
        {
            var hoursSinceLast = (now - lastLogin.Value).TotalHours;
            if (hoursSinceLast < 12)
            {
                // Same session — no streak change
                return student.CurrentStreakDays;
            }
            else if (hoursSinceLast < 36)
            {
                // Within 24-36 hours = consecutive day
                student.IncrementStreak();
            }
            else
            {
                // Missed at least a full day. Consume one shield per missed day to preserve
                // the streak; only reset to 1 if shields ran out.
                var daysMissed = (int)Math.Floor((hoursSinceLast - 24) / 24.0);
                var shieldedDays = 0;
                while (shieldedDays < daysMissed && student.TryConsumeStreakShield()) shieldedDays++;
                if (shieldedDays < daysMissed)
                {
                    student.ResetStreak();
                    student.IncrementStreak();
                }
                else
                {
                    student.IncrementStreak();
                }
            }
        }

        student.RecordLogin();
        await _db.SaveChangesAsync(ct);

        // Award streak badges
        if (student.CurrentStreakDays >= 7)
            await TryAwardBadgeAsync(studentId, BadgeCodes.WeekStreak, ct);
        if (student.CurrentStreakDays >= 30)
            await TryAwardBadgeAsync(studentId, BadgeCodes.MonthStreak, ct);

        return student.CurrentStreakDays;
    }
}
