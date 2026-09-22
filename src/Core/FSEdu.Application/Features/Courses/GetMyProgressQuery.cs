using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

public sealed record GetMyProgressQuery() : IQuery<StudentProgressDto>;

public sealed class GetMyProgressHandler : IQueryHandler<GetMyProgressQuery, StudentProgressDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public GetMyProgressHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<StudentProgressDto>> Handle(GetMyProgressQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null)
            return Error.Forbidden("STUDENT.NOT_FOUND", "هذه الميزة للطلاب فقط", "Students only");

        // 1) Find courses the student has access to (via subject subscriptions)
        var summary = await _access.GetAccessSummaryAsync(studentId, ct);
        var subjectIds = summary.AccessibleSubjectIds.ToList();

        var courses = await _db.Courses
            .Where(c => subjectIds.Contains(c.SubjectId))
            .Include(c => c.Subject)
            .Include(c => c.Chapters).ThenInclude(ch => ch.Lessons)
            .ToListAsync(ct);

        // 2) Pull all this student's lesson progress in one round-trip
        var lessonIds = courses.SelectMany(c => c.Chapters)
                               .SelectMany(ch => ch.Lessons)
                               .Select(l => l.Id)
                               .ToHashSet();
        var progressEntries = await _db.LessonProgress
            .Where(lp => lp.StudentId == studentId && lessonIds.Contains(lp.LessonId))
            .ToListAsync(ct);
        var progressByLesson = progressEntries.ToDictionary(lp => lp.LessonId);

        // 3) Build per-course rows
        var courseRows = new List<CourseProgressDto>();
        foreach (var c in courses)
        {
            var lessons = c.Chapters.SelectMany(ch => ch.Lessons).ToList();
            var total = lessons.Count;
            var completed = 0;
            DateTime? lastView = null;
            Guid? lastLessonId = null;
            string? lastLessonTitle = null;
            int lastPos = 0;

            foreach (var l in lessons)
            {
                if (progressByLesson.TryGetValue(l.Id, out var lp))
                {
                    if (lp.Completed) completed++;
                    if (lastView is null || lp.LastViewedAtUtc > lastView)
                    {
                        lastView = lp.LastViewedAtUtc;
                        lastLessonId = l.Id;
                        lastLessonTitle = l.Title;
                        lastPos = lp.LastPositionSec;
                    }
                }
            }

            var pct = total == 0 ? 0 : (int)Math.Round(completed * 100.0 / total);
            courseRows.Add(new CourseProgressDto(
                c.Id, c.Title, c.ThumbnailUrl,
                c.SubjectId, c.Subject.NameAr,
                total, completed, pct,
                lastLessonId, lastLessonTitle, lastPos, lastView));
        }

        // 4) Aggregate
        var totalAvailable = courseRows.Sum(r => r.TotalLessons);
        var totalCompleted = courseRows.Sum(r => r.CompletedLessons);
        var overallPct = totalAvailable == 0 ? 0 : (int)Math.Round(totalCompleted * 100.0 / totalAvailable);

        // Resume: most recently viewed lesson across all courses
        var resume = courseRows
            .Where(r => r.LastViewedAtUtc is not null)
            .OrderByDescending(r => r.LastViewedAtUtc)
            .FirstOrDefault();

        var badgesCount = await _db.StudentBadges.CountAsync(b => b.StudentId == studentId, ct);

        // Sort: in-progress first (0% < pct < 100%), then not-started, then completed
        var ordered = courseRows
            .OrderBy(r => r.ProgressPct == 100 ? 2 : r.ProgressPct == 0 ? 1 : 0)
            .ThenByDescending(r => r.LastViewedAtUtc ?? DateTime.MinValue)
            .ToList();

        return Result.Success(new StudentProgressDto(
            TotalCourses: courseRows.Count,
            TotalLessonsAvailable: totalAvailable,
            TotalLessonsCompleted: totalCompleted,
            OverallPct: overallPct,
            CurrentStreakDays: student.CurrentStreakDays,
            LongestStreakDays: student.LongestStreakDays,
            XpPoints: student.XpPoints,
            BadgesEarned: badgesCount,
            ResumeLessonId: resume?.LastLessonId,
            ResumeCourseId: resume?.CourseId,
            ResumeLessonTitle: resume?.LastLessonTitle,
            ResumeCourseTitle: resume?.Title,
            Courses: ordered));
    }
}
