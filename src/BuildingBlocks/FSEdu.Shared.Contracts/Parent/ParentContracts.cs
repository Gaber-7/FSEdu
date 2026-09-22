namespace FSEdu.Shared.Contracts.Parent;

public sealed record LinkChildRequest(string StudentPhone, string Relation);

public sealed record ChildSummaryDto(
    Guid StudentId,
    string FullName,
    string Phone,
    string StageName,
    string RegionName,
    string Relation,
    bool IsPrimary,
    int XpPoints,
    int CurrentStreak
);
