using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Students;

// Comprehensive learning analytics for the current student:
//   - total minutes watched, plus last-7/last-30 windows
//   - assessment averages
//   - breakdowns by subject and by date
//
// Watched-minutes are approximated from LessonProgress.WatchedPct × Lesson.DurationSeconds.
// This avoids having to store a cumulative watch-time counter.
public sealed record GetMyDetailedStatsQuery() : IQuery<StudentDetailedStatsDto>;

public sealed class GetMyDetailedStatsHandler : IQueryHandler<GetMyDetailedStatsQuery, StudentDetailedStatsDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyDetailedStatsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<StudentDetailedStatsDto>> Handle(GetMyDetailedStatsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var now = DateTime.UtcNow;
        var d7 = now.AddDays(-7);
        var d30 = now.AddDays(-30);

        // ─── Pull progress + lesson metadata in one round-trip ───
        var progressRows = await (
            from lp in _db.LessonProgress
            where lp.StudentId == studentId
            join l in _db.Lessons on lp.LessonId equals l.Id
            join ch in _db.Chapters on l.ChapterId equals ch.Id
            join c in _db.Courses on ch.CourseId equals c.Id
            join sub in _db.Subjects on c.SubjectId equals sub.Id
            select new
            {
                lp.LastViewedAtUtc,
                lp.WatchedPct,
                lp.Completed,
                LessonDurationSec = l.DurationSeconds,
                SubjectId = sub.Id,
                SubjectName = sub.NameAr,
                sub.ColorHex
            }
        ).ToListAsync(ct);

        // Helper: secs watched for one progress row
        static int SecsOf(decimal watchedPct, int duration)
            => (int)Math.Round((double)watchedPct / 100.0 * duration);

        var totalSecs = progressRows.Sum(r => SecsOf(r.WatchedPct, r.LessonDurationSec));
        var secs7 = progressRows.Where(r => r.LastViewedAtUtc >= d7).Sum(r => SecsOf(r.WatchedPct, r.LessonDurationSec));
        var secs30 = progressRows.Where(r => r.LastViewedAtUtc >= d30).Sum(r => SecsOf(r.WatchedPct, r.LessonDurationSec));

        // ─── Assessments ─────────────────────────────────
        var attempts = await (
            from a in _db.AssessmentAttempts
            where a.StudentId == studentId && a.SubmittedAtUtc != null
            join asm in _db.Assessments on a.AssessmentId equals asm.Id
            where asm.CourseId != null
            join c in _db.Courses on asm.CourseId equals c.Id
            select new { a.Score, asm.TotalMarks, CourseSubjectId = c.SubjectId }
        ).ToListAsync(ct);

        decimal pctOf(decimal score, decimal total) => total > 0 ? score / total * 100m : 0m;

        var assessmentPcts = attempts.Select(a => pctOf(a.Score, a.TotalMarks)).ToList();
        var avgPct = assessmentPcts.Count == 0 ? 0m : assessmentPcts.Average();
        var highCount = assessmentPcts.Count(p => p >= 90m);
        var lowCount = assessmentPcts.Count(p => p < 50m);

        // ─── Breakdown by Subject ────────────────────────
        var attemptBySubject = attempts
            .GroupBy(a => a.CourseSubjectId)
            .ToDictionary(g => g.Key, g => new
            {
                Count = g.Count(),
                AvgPct = g.Average(x => pctOf(x.Score, x.TotalMarks))
            });

        var bySubject = progressRows
            .GroupBy(r => new { r.SubjectId, r.SubjectName, r.ColorHex })
            .Select(g =>
            {
                var watchedSec = g.Sum(r => SecsOf(r.WatchedPct, r.LessonDurationSec));
                var completed = g.Count(r => r.Completed);
                var asmStats = attemptBySubject.TryGetValue(g.Key.SubjectId, out var x) ? x : null;
                return new StatsBySubjectDto(
                    g.Key.SubjectId, g.Key.SubjectName, g.Key.ColorHex,
                    watchedSec / 60,
                    completed,
                    asmStats?.Count ?? 0,
                    Math.Round(asmStats?.AvgPct ?? 0m, 1)
                );
            })
            .OrderByDescending(s => s.WatchedMinutes)
            .ToArray();

        // ─── Breakdown by Date (last 30 days) ────────────
        var byDate = progressRows
            .Where(r => r.LastViewedAtUtc >= d30)
            .GroupBy(r => r.LastViewedAtUtc.Date)
            .Select(g => new StatsByDateDto(
                g.Key,
                g.Sum(r => SecsOf(r.WatchedPct, r.LessonDurationSec)) / 60,
                g.Count()
            ))
            .OrderBy(s => s.DateLocal)
            .ToArray();

        var dto = new StudentDetailedStatsDto(
            TotalWatchedMinutes: totalSecs / 60,
            MinutesLast7Days: secs7 / 60,
            MinutesLast30Days: secs30 / 60,
            AssessmentsSubmitted: assessmentPcts.Count,
            CorrectAnswerPct: Math.Round(avgPct, 1),
            HighScoreCount: highCount,
            LowScoreCount: lowCount,
            BySubject: bySubject,
            ByDate: byDate
        );

        return Result.Success(dto);
    }
}
