using FSEdu.Application.Abstractions;
using FSEdu.Domain.Courses;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// Teacher-only analytics view: per-course aggregates and per-lesson metrics
// computed from existing data (LessonProgress, LessonQuestions, LessonReactions).
// Used to drive the "what needs my attention?" decisions.
public sealed record GetCourseInsightsQuery(Guid CourseId) : IQuery<CourseInsightsDto>;

public sealed class GetCourseInsightsHandler : IQueryHandler<GetCourseInsightsQuery, CourseInsightsDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCourseInsightsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<CourseInsightsDto>> Handle(GetCourseInsightsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var course = await _db.Courses
            .Include(c => c.Chapters).ThenInclude(ch => ch.Lessons)
            .FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);
        if (course is null)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Not found");

        var isAdmin = _currentUser.IsInRole("Admin");
        if (!isAdmin && course.TeacherId != userId)
            return Error.Forbidden("COURSE.NOT_YOURS", "هذه الدورة ليست لك", "Not yours");

        var lessonIds = course.Chapters.SelectMany(ch => ch.Lessons).Select(l => l.Id).ToList();
        var totalLessons = lessonIds.Count;

        // ─── Per-lesson aggregates ───────────────────────────
        var progressByLesson = await _db.LessonProgress
            .Where(lp => lessonIds.Contains(lp.LessonId))
            .GroupBy(lp => lp.LessonId)
            .Select(g => new
            {
                LessonId = g.Key,
                ViewCount = g.Count(),
                CompletionCount = g.Count(x => x.Completed),
                AvgWatchedPct = g.Average(x => (decimal?)x.WatchedPct) ?? 0m
            })
            .ToDictionaryAsync(x => x.LessonId, ct);

        var questionsByLesson = await _db.LessonQuestions
            .Where(q => lessonIds.Contains(q.LessonId))
            .GroupBy(q => q.LessonId)
            .Select(g => new
            {
                LessonId = g.Key,
                Total = g.Count(),
                Unanswered = g.Count(x => x.AnswerBody == null)
            })
            .ToDictionaryAsync(x => x.LessonId, ct);

        var reactionsByLesson = await _db.LessonReactions
            .Where(r => lessonIds.Contains(r.LessonId))
            .GroupBy(r => new { r.LessonId, r.Type })
            .Select(g => new { g.Key.LessonId, g.Key.Type, Count = g.Count() })
            .ToListAsync(ct);

        var reactionMap = reactionsByLesson
            .GroupBy(r => r.LessonId)
            .ToDictionary(g => g.Key, g => new
            {
                Helpful = g.FirstOrDefault(x => x.Type == ReactionType.Helpful)?.Count ?? 0,
                Confusing = g.FirstOrDefault(x => x.Type == ReactionType.Confusing)?.Count ?? 0,
                Loved = g.FirstOrDefault(x => x.Type == ReactionType.Loved)?.Count ?? 0,
            });

        // ─── Build per-lesson rows ───────────────────────────
        var lessonRows = course.Chapters
            .OrderBy(ch => ch.OrderNum)
            .SelectMany(ch => ch.Lessons.OrderBy(l => l.OrderNum).Select(l => (ch, l)))
            .Select(t =>
            {
                var prog = progressByLesson.TryGetValue(t.l.Id, out var p) ? p : null;
                var qs = questionsByLesson.TryGetValue(t.l.Id, out var q) ? q : null;
                var rx = reactionMap.TryGetValue(t.l.Id, out var r) ? r : null;
                return new LessonInsightDto(
                    t.l.Id,
                    t.l.Title,
                    t.ch.Title,
                    t.l.OrderNum,
                    prog?.ViewCount ?? 0,
                    prog?.CompletionCount ?? 0,
                    Math.Round(prog?.AvgWatchedPct ?? 0m, 1),
                    qs?.Total ?? 0,
                    qs?.Unanswered ?? 0,
                    rx?.Helpful ?? 0,
                    rx?.Confusing ?? 0,
                    rx?.Loved ?? 0
                );
            })
            .ToArray();

        // ─── Course-level aggregates ─────────────────────────
        var enrolled = course.EnrollmentCount;
        var totalReactions = lessonRows.Sum(x => x.HelpfulCount + x.ConfusingCount + x.LovedCount);
        var totalQuestions = lessonRows.Sum(x => x.QuestionsCount);
        var unanswered = lessonRows.Sum(x => x.UnansweredCount);
        var helpful = lessonRows.Sum(x => x.HelpfulCount);
        var confusing = lessonRows.Sum(x => x.ConfusingCount);
        var loved = lessonRows.Sum(x => x.LovedCount);

        var avgWatched = lessonRows.Length > 0
            ? Math.Round(lessonRows.Average(x => x.AvgWatchedPct), 1)
            : 0m;

        // Completion rate = avg (Completion count / Enrollment count) per lesson, when there's data.
        decimal completionRate = 0m;
        if (enrolled > 0 && lessonRows.Length > 0)
        {
            completionRate = Math.Round(
                lessonRows.Average(x => (decimal)x.CompletionCount / enrolled * 100m),
                1);
        }

        var dto = new CourseInsightsDto(
            course.Id, course.Title,
            enrolled, totalLessons,
            avgWatched, completionRate,
            totalQuestions, unanswered,
            totalReactions, helpful, confusing, loved,
            course.RatingAvg, course.RatingsCount,
            lessonRows);

        return Result.Success(dto);
    }
}
