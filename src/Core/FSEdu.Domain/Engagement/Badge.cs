using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Engagement;

public sealed class Badge : AggregateRoot<int>
{
    public string Code { get; private set; } = default!;
    public string NameAr { get; private set; } = default!;
    public string? NameEn { get; private set; }
    public string IconUrl { get; private set; } = default!;
    public string CriteriaJson { get; private set; } = default!;

    private Badge() { }

    public Badge(string code, string nameAr, string iconUrl, string criteriaJson)
    {
        Code = code;
        NameAr = nameAr;
        IconUrl = iconUrl;
        CriteriaJson = criteriaJson;
    }
}

public sealed class StudentBadge
{
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;
    public int BadgeId { get; private set; }
    public Badge Badge { get; private set; } = default!;
    public DateTime AwardedAtUtc { get; private set; }

    private StudentBadge() { }

    public StudentBadge(Guid studentId, int badgeId)
    {
        StudentId = studentId;
        BadgeId = badgeId;
        AwardedAtUtc = DateTime.UtcNow;
    }
}

public sealed class WeeklyChallenge : AggregateRoot<Guid>
{
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public DateTime StartsAtUtc { get; private set; }
    public DateTime EndsAtUtc { get; private set; }
    public string TargetMetric { get; private set; } = default!;
    public int TargetValue { get; private set; }
    public int RewardXp { get; private set; }

    private WeeklyChallenge() { }

    public WeeklyChallenge(Guid id, string title, string description, DateTime starts, DateTime ends,
                            string metric, int targetValue, int rewardXp) : base(id)
    {
        Title = title;
        Description = description;
        StartsAtUtc = starts;
        EndsAtUtc = ends;
        TargetMetric = metric;
        TargetValue = targetValue;
        RewardXp = rewardXp;
    }
}
