namespace FSEdu.Application.Abstractions;

public interface IGamificationService
{
    /// <summary>Award XP to a student.</summary>
    Task<int> AwardXpAsync(Guid studentId, int points, string reason, CancellationToken ct = default);

    /// <summary>Try to award a badge by its code (idempotent — won't double-award).</summary>
    /// <returns>true if newly awarded; false if already had it.</returns>
    Task<bool> TryAwardBadgeAsync(Guid studentId, string badgeCode, CancellationToken ct = default);

    /// <summary>Update student streak based on today's activity. Returns new streak count.</summary>
    Task<int> UpdateStreakAsync(Guid studentId, CancellationToken ct = default);
}

public static class BadgeCodes
{
    public const string FirstLesson = "first-lesson";
    public const string WeekStreak = "week-streak";
    public const string MonthStreak = "month-streak";
    public const string PerfectScore = "perfect-score";
    public const string TopTen = "top-ten";
    public const string Helpful = "helpful";
}
