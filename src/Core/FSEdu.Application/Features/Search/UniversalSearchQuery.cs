using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Search;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Search;

public sealed record UniversalSearchQuery(string Q, int PerSection = 10) : IQuery<SearchResponseDto>;

public sealed class UniversalSearchHandler : IQueryHandler<UniversalSearchQuery, SearchResponseDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public UniversalSearchHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<SearchResponseDto>> Handle(UniversalSearchQuery request, CancellationToken ct)
    {
        var q = (request.Q ?? "").Trim();
        if (q.Length < 2)
            return Result.Success(new SearchResponseDto(q, 0, new(), new(), new(), new()));

        var perSection = Math.Clamp(request.PerSection, 1, 25);
        var pattern = $"%{q}%";

        var userId = _currentUser.UserId;
        var isAdmin = _currentUser.IsInRole("Admin");
        var isTeacher = _currentUser.IsInRole("Teacher");

        // Determine which subjects the caller has access to (for students),
        // or unrestricted (admin/teacher).
        HashSet<int>? accessibleSubjects = null;
        if (userId is Guid uid && !isAdmin && !isTeacher)
        {
            var summary = await _access.GetAccessSummaryAsync(uid, ct);
            accessibleSubjects = new HashSet<int>(summary.AccessibleSubjectIds);
        }

        // ─── Courses ─────────────────────────────────────────
        var courseQuery = _db.Courses
            .Include(c => c.Subject)
            .Include(c => c.Teacher)
            .Where(c => c.Status == CourseStatus.Published
                     && (EF.Functions.Like(c.Title, pattern)
                         || (c.Description != null && EF.Functions.Like(c.Description, pattern))
                         || EF.Functions.Like(c.Subject.NameAr, pattern)));

        var coursesRaw = await courseQuery
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(perSection * 2)   // over-fetch then filter for access
            .Select(c => new
            {
                c.Id, c.Title, c.Description, c.ThumbnailUrl, c.SubjectId, c.CreatedAtUtc,
                SubjectName = c.Subject.NameAr,
                TeacherName = c.Teacher.FullName
            })
            .ToListAsync(ct);

        var courses = coursesRaw
            .Where(c => accessibleSubjects is null || accessibleSubjects.Contains(c.SubjectId))
            .Take(perSection)
            .Select(c => new SearchResultDto(
                "course", c.Id, c.Title,
                $"{c.SubjectName} · أ/ {c.TeacherName}",
                Trim(c.Description, 120),
                $"/courses/{c.Id}",
                c.ThumbnailUrl,
                c.CreatedAtUtc))
            .ToList();

        // ─── Live sessions (upcoming + live) ─────────────────
        var now = DateTime.UtcNow;
        var liveQuery = _db.LiveSessions
            .Include(s => s.Teacher)
            .Where(s => (s.Status == LiveSessionStatus.Scheduled || s.Status == LiveSessionStatus.Live)
                     && s.ScheduledAtUtc >= now.AddHours(-2)
                     && (EF.Functions.Like(s.Title, pattern)
                         || (s.Description != null && EF.Functions.Like(s.Description, pattern))));

        var liveRaw = await liveQuery
            .OrderBy(s => s.ScheduledAtUtc)
            .Take(perSection * 2)
            .Select(s => new
            {
                s.Id, s.Title, s.Description, s.SubjectId, s.ScheduledAtUtc, s.Status,
                TeacherName = s.Teacher.FullName
            })
            .ToListAsync(ct);

        var live = liveRaw
            .Where(s => accessibleSubjects is null
                     || (s.SubjectId is int sid && accessibleSubjects.Contains(sid)))
            .Take(perSection)
            .Select(s => new SearchResultDto(
                "live", s.Id, s.Title,
                $"أ/ {s.TeacherName} · {s.ScheduledAtUtc.ToLocalTime():yyyy/MM/dd HH:mm}",
                s.Status == LiveSessionStatus.Live ? "🔴 مباشرة الآن" : Trim(s.Description, 100),
                $"/live/{s.Id}",
                null,
                s.ScheduledAtUtc))
            .ToList();

        // ─── Recordings (ended sessions with recording) ──────
        var recQuery = _db.LiveSessions
            .Include(s => s.Teacher)
            .Where(s => s.Status == LiveSessionStatus.Ended
                     && s.RecordingUrl != null
                     && (EF.Functions.Like(s.Title, pattern)
                         || (s.Description != null && EF.Functions.Like(s.Description, pattern))));

        var recRaw = await recQuery
            .OrderByDescending(s => s.EndedAtUtc)
            .Take(perSection * 2)
            .Select(s => new
            {
                s.Id, s.Title, s.Description, s.SubjectId, s.EndedAtUtc,
                TeacherName = s.Teacher.FullName
            })
            .ToListAsync(ct);

        var recordings = recRaw
            .Where(s => accessibleSubjects is null
                     || (s.SubjectId is int sid && accessibleSubjects.Contains(sid)))
            .Take(perSection)
            .Select(s => new SearchResultDto(
                "recording", s.Id, s.Title,
                $"أ/ {s.TeacherName}",
                Trim(s.Description, 120),
                "/student/recordings",
                null,
                s.EndedAtUtc))
            .ToList();

        // ─── Lessons (by title only) ─────────────────────────
        var lessonRaw = await _db.Lessons
            .Where(l => EF.Functions.Like(l.Title, pattern))
            .OrderBy(l => l.Title)
            .Take(perSection * 2)
            .Select(l => new
            {
                l.Id, l.Title, l.ChapterId,
                CourseId = l.Chapter.CourseId,
                CourseTitle = l.Chapter.Course.Title,
                SubjectId = l.Chapter.Course.SubjectId,
                CourseStatus = l.Chapter.Course.Status
            })
            .ToListAsync(ct);

        var lessons = lessonRaw
            .Where(l => l.CourseStatus == CourseStatus.Published
                     && (accessibleSubjects is null || accessibleSubjects.Contains(l.SubjectId)))
            .Take(perSection)
            .Select(l => new SearchResultDto(
                "lesson", l.Id, l.Title,
                $"درس · {l.CourseTitle}",
                null,
                $"/courses/{l.CourseId}",
                null,
                null))
            .ToList();

        var total = courses.Count + live.Count + recordings.Count + lessons.Count;
        return Result.Success(new SearchResponseDto(q, total, courses, live, recordings, lessons));
    }

    private static string? Trim(string? s, int max)
    {
        if (string.IsNullOrEmpty(s)) return null;
        return s.Length <= max ? s : s.Substring(0, max).TrimEnd() + "…";
    }
}
