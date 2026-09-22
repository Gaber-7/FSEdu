using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

public sealed record BrowseCoursesQuery(int? StageId, int? SubjectId) : IQuery<List<CourseListItemDto>>;

public sealed class BrowseCoursesHandler : IQueryHandler<BrowseCoursesQuery, List<CourseListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public BrowseCoursesHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<List<CourseListItemDto>>> Handle(BrowseCoursesQuery request, CancellationToken ct)
    {
        var query = _db.Courses.Where(c => c.Status == CourseStatus.Published);

        if (request.StageId.HasValue) query = query.Where(c => c.StageId == request.StageId.Value);
        if (request.SubjectId.HasValue) query = query.Where(c => c.SubjectId == request.SubjectId.Value);

        var courses = await query
            .Include(c => c.Subject)
            .Include(c => c.Stage)
            .Include(c => c.Chapters).ThenInclude(ch => ch.Lessons)
            .OrderByDescending(c => c.EnrollmentCount)
            .ThenByDescending(c => c.RatingAvg)
            .ToListAsync(ct);

        return Result.Success(courses.Select(c => new CourseListItemDto(
            c.Id, c.Title, c.ThumbnailUrl,
            c.SubjectId, c.Subject.NameAr,
            c.StageId, c.Stage.NameAr,
            c.Status.ToString(),
            c.EnrollmentCount, c.RatingAvg,
            c.Chapters.Sum(ch => ch.Lessons.Count)
        )).ToList());
    }
}

// ─── Lesson Playback ─────────────────────────
public sealed record WatchLessonQuery(Guid LessonId) : IQuery<LessonPlaybackDto>;

public sealed class WatchLessonHandler : IQueryHandler<WatchLessonQuery, LessonPlaybackDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public WatchLessonHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<LessonPlaybackDto>> Handle(WatchLessonQuery request, CancellationToken ct)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Chapter).ThenInclude(ch => ch.Course)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId, ct);

        if (lesson is null)
            return Error.NotFound("LESSON.NOT_FOUND", "الدرس غير موجود", "Lesson not found");

        var userId = _currentUser.UserId;
        var hasAccess = lesson.IsFreePreview
            || (userId.HasValue && (
                lesson.Chapter.Course.TeacherId == userId.Value
                || _currentUser.IsInRole("Admin")
                || await _access.CanAccessLessonAsync(userId.Value, lesson.Id, ct)
            ));

        var isCourseTeacher = userId.HasValue && lesson.Chapter.Course.TeacherId == userId.Value;

        return Result.Success(new LessonPlaybackDto(
            lesson.Id,
            lesson.Title,
            lesson.Type.ToString(),
            lesson.DurationSeconds,
            hasAccess ? lesson.VideoUrl : null,
            hasAccess ? lesson.VideoDrmKeyId : null,
            lesson.Chapter.CourseId,
            lesson.Chapter.Course.Title,
            lesson.OrderNum,
            hasAccess,
            lesson.IsFreePreview,
            isCourseTeacher
        ));
    }
}
