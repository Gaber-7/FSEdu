using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Gamification;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Gamification;

// ─── Leaderboard ─────────────────────────────────
public sealed record GetLeaderboardQuery(int? StageId, int Take = 50) : IQuery<List<LeaderboardEntryDto>>;

public sealed class GetLeaderboardHandler : IQueryHandler<GetLeaderboardQuery, List<LeaderboardEntryDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetLeaderboardHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<LeaderboardEntryDto>>> Handle(GetLeaderboardQuery request, CancellationToken ct)
    {
        var myId = _currentUser.UserId;

        var query = _db.Students.Include(s => s.Stage).AsQueryable();
        if (request.StageId.HasValue)
            query = query.Where(s => s.StageId == request.StageId.Value);

        var students = await query
            .OrderByDescending(s => s.XpPoints)
            .ThenByDescending(s => s.CurrentStreakDays)
            .Take(Math.Min(request.Take, 100))
            .Select(s => new
            {
                s.Id,
                s.FullName,
                StageName = s.Stage.NameAr,
                s.XpPoints,
                s.CurrentStreakDays,
                BadgesCount = _db.StudentBadges.Count(sb => sb.StudentId == s.Id)
            })
            .ToListAsync(ct);

        var list = students.Select((s, i) => new LeaderboardEntryDto(
            Rank: i + 1,
            StudentId: s.Id,
            FullName: s.FullName,
            StageName: s.StageName,
            XpPoints: s.XpPoints,
            CurrentStreakDays: s.CurrentStreakDays,
            BadgesCount: s.BadgesCount,
            IsMe: s.Id == myId
        )).ToList();

        return Result.Success(list);
    }
}

// ─── My Achievements ─────────────────────────────
public sealed record GetMyAchievementsQuery() : IQuery<MyAchievementsDto>;

public sealed class GetMyAchievementsHandler : IQueryHandler<GetMyAchievementsQuery, MyAchievementsDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyAchievementsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<MyAchievementsDto>> Handle(GetMyAchievementsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == userId, ct);
        if (student is null)
            return Error.NotFound("STUDENT.NOT_FOUND", "الطالب غير موجود", "Not found");

        // Compute global rank (rank > my XP count)
        var globalRank = await _db.Students.CountAsync(s => s.XpPoints > student.XpPoints, ct) + 1;

        // Stage rank
        var stageRank = await _db.Students
            .Where(s => s.StageId == student.StageId && s.XpPoints > student.XpPoints)
            .CountAsync(ct) + 1;
        var totalInStage = await _db.Students.CountAsync(s => s.StageId == student.StageId, ct);

        // All badges + which ones earned
        var allBadges = await _db.Badges.OrderBy(b => b.Id).ToListAsync(ct);
        var earnedSet = await _db.StudentBadges
            .Where(sb => sb.StudentId == userId)
            .Select(sb => new { sb.BadgeId, sb.AwardedAtUtc })
            .ToListAsync(ct);
        var earnedMap = earnedSet.ToDictionary(e => e.BadgeId, e => e.AwardedAtUtc);

        var earned = new List<BadgeDto>();
        var available = new List<BadgeDto>();

        foreach (var b in allBadges)
        {
            var isEarned = earnedMap.TryGetValue(b.Id, out var when);
            var dto = new BadgeDto(b.Id, b.Code, b.NameAr, b.IconUrl, isEarned, isEarned ? when : null);
            if (isEarned) earned.Add(dto);
            else available.Add(dto);
        }

        // Award top-ten badge if rank <= 10
        // (handled outside — but flag it here)

        return Result.Success(new MyAchievementsDto(
            student.Id, student.FullName,
            student.XpPoints, student.CurrentStreakDays, student.LongestStreakDays,
            globalRank, stageRank, totalInStage,
            earned.ToArray(), available.ToArray()));
    }
}
