using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Engagement;

// One row per (student, day) — created lazily the first time the student
// loads their dashboard on a new day.
public sealed class DailyChallenge : AggregateRoot<Guid>
{
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;
    public DateOnly Day { get; private set; }            // student's local day (UTC date)
    public DailyChallengeType Type { get; private set; }
    public int TargetValue { get; private set; }        // e.g. minutes to watch, lessons to complete
    public int ProgressValue { get; private set; }
    public int XpReward { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public bool RewardedShield { get; private set; }    // did completing this also grant a shield?

    private DailyChallenge() { }

    public DailyChallenge(Guid id, Guid studentId, DateOnly day,
                          DailyChallengeType type, int targetValue, int xpReward) : base(id)
    {
        StudentId = studentId;
        Day = day;
        Type = type;
        TargetValue = Math.Max(1, targetValue);
        XpReward = Math.Max(0, xpReward);
    }

    public bool IsCompleted => CompletedAtUtc.HasValue;

    // Increases progress and returns true on the transition from incomplete → completed.
    public bool RecordProgress(int amount)
    {
        if (amount <= 0 || IsCompleted) return false;
        ProgressValue = Math.Min(TargetValue, ProgressValue + amount);
        if (ProgressValue >= TargetValue)
        {
            CompletedAtUtc = DateTime.UtcNow;
            return true;
        }
        return false;
    }

    public void MarkShieldRewarded() => RewardedShield = true;
}

public enum DailyChallengeType
{
    WatchMinutes = 1,           // watch N minutes of any lesson today
    CompleteLessons = 2,        // mark N lessons as completed today
    SubmitHomework = 3,         // submit 1 homework today
    SolveAssessment = 4,        // submit 1 assessment/quiz today
    SolvePastPaper = 5,         // attempt 1 past paper today
}
