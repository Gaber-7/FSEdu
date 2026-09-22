using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Students;

// Today's "daily mission" — three lightweight engagement goals computed
// from existing data. No new entity; pure read-only aggregation.
public sealed record GetDailyMissionQuery() : IQuery<DailyMissionDto>;

public sealed class GetDailyMissionHandler : IQueryHandler<GetDailyMissionQuery, DailyMissionDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetDailyMissionHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<DailyMissionDto>> Handle(GetDailyMissionQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var student = await _db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == userId, ct);
        if (student is null)
            return Error.NotFound("STUDENT.NOT_FOUND", "الحساب ليس طالبًا", "Not a student");

        // "Today" — uses local Cairo time (system UTC + 2/3 hours typically); fall back to UTC.
        // We approximate with the user's stored Timezone, but for simplicity here use UTC day boundaries.
        var nowUtc = DateTime.UtcNow;
        var todayStart = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc);
        var tomorrowStart = todayStart.AddDays(1);

        // Goal 1: watch any 2 lessons today
        var lessonsViewedToday = await _db.LessonProgress
            .CountAsync(lp => lp.StudentId == userId
                              && lp.LastViewedAtUtc >= todayStart
                              && lp.LastViewedAtUtc < tomorrowStart, ct);

        // Goal 2: complete a lesson (watchedPct ≥ 90%) viewed today
        var lessonsCompletedToday = await _db.LessonProgress
            .CountAsync(lp => lp.StudentId == userId
                              && lp.Completed
                              && lp.LastViewedAtUtc >= todayStart
                              && lp.LastViewedAtUtc < tomorrowStart, ct);

        // Goal 3: submit any assessment today
        var assessmentsToday = await _db.AssessmentAttempts
            .CountAsync(a => a.StudentId == userId
                             && a.SubmittedAtUtc != null
                             && a.SubmittedAtUtc >= todayStart
                             && a.SubmittedAtUtc < tomorrowStart, ct);

        var goals = new[]
        {
            new DailyMissionGoalDto(
                "watch_lesson", "شاهد درسين", "▶",
                Target: 2, Progress: Math.Min(2, lessonsViewedToday), Done: lessonsViewedToday >= 2),
            new DailyMissionGoalDto(
                "complete_lesson", "أكمل درسًا", "✅",
                Target: 1, Progress: Math.Min(1, lessonsCompletedToday), Done: lessonsCompletedToday >= 1),
            new DailyMissionGoalDto(
                "submit_assessment", "حلّ اختبارًا", "📝",
                Target: 1, Progress: Math.Min(1, assessmentsToday), Done: assessmentsToday >= 1),
        };

        var done = goals.Count(g => g.Done);
        return Result.Success(new DailyMissionDto(
            DateLocal: todayStart,
            CompletedCount: done,
            TotalCount: goals.Length,
            AllDone: done == goals.Length,
            CurrentStreakDays: student.CurrentStreakDays,
            Goals: goals));
    }
}
