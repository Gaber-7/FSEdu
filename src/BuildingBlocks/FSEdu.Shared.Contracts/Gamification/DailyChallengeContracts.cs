namespace FSEdu.Shared.Contracts.Gamification;

public sealed record DailyChallengeStatusDto(
    string Type,                // e.g. "WatchMinutes"
    string Title,               // localized prompt to show student
    string Description,         // motivational sub-text
    string IconEmoji,
    int TargetValue,
    int ProgressValue,
    int XpReward,
    bool Completed,
    DateTime? CompletedAtUtc,
    int CurrentStreakDays,
    int StreakShields           // available shields to protect streak
);
