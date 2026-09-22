namespace FSEdu.Shared.Contracts.Gamification;

public sealed record LeaderboardEntryDto(
    int Rank,
    Guid StudentId,
    string FullName,
    string StageName,
    int XpPoints,
    int CurrentStreakDays,
    int BadgesCount,
    bool IsMe
);

public sealed record MyAchievementsDto(
    Guid StudentId,
    string FullName,
    int XpPoints,
    int CurrentStreakDays,
    int LongestStreakDays,
    int? GlobalRank,
    int? StageRank,
    int TotalStudentsInStage,
    BadgeDto[] EarnedBadges,
    BadgeDto[] AvailableBadges
);

public sealed record BadgeDto(
    int Id,
    string Code,
    string NameAr,
    string IconUrl,
    bool Earned,
    DateTime? EarnedAtUtc
);
