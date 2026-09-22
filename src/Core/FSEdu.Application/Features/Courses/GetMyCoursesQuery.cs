using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

public sealed record GetMyCoursesQuery() : IQuery<List<CourseListItemDto>>;

public sealed class GetMyCoursesHandler : IQueryHandler<GetMyCoursesQuery, List<CourseListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyCoursesHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<CourseListItemDto>>> Handle(GetMyCoursesQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var teacherId = _currentUser.UserId.Value;

        var courses = await _db.Courses
            .Where(c => c.TeacherId == teacherId)
            .Include(c => c.Subject)
            .Include(c => c.Stage)
            .Include(c => c.Chapters).ThenInclude(ch => ch.Lessons)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(ct);

        var result = courses.Select(c => new CourseListItemDto(
            c.Id, c.Title, c.ThumbnailUrl,
            c.SubjectId, c.Subject.NameAr,
            c.StageId, c.Stage.NameAr,
            c.Status.ToString(),
            c.EnrollmentCount, c.RatingAvg,
            c.Chapters.Sum(ch => ch.Lessons.Count)
        )).ToList();

        return Result.Success(result);
    }
}

// ─── Get Course Details (for teacher editing OR student viewing) ───
public sealed record GetCourseDetailsQuery(Guid CourseId) : IQuery<CourseDetailDto>;

public sealed class GetCourseDetailsHandler : IQueryHandler<GetCourseDetailsQuery, CourseDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public GetCourseDetailsHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<CourseDetailDto>> Handle(GetCourseDetailsQuery request, CancellationToken ct)
    {
        var course = await _db.Courses
            .Where(c => c.Id == request.CourseId)
            .Include(c => c.Subject)
            .Include(c => c.Stage)
            .Include(c => c.Teacher)
            .Include(c => c.Chapters.OrderBy(ch => ch.OrderNum))
                .ThenInclude(ch => ch.Lessons.OrderBy(l => l.OrderNum))
            .FirstOrDefaultAsync(ct);

        if (course is null)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Course not found");

        var hasAccess = false;
        if (_currentUser.UserId.HasValue)
        {
            hasAccess = course.TeacherId == _currentUser.UserId.Value
                     || _currentUser.IsInRole("Admin")
                     || await _access.CanAccessSubjectAsync(_currentUser.UserId.Value, course.SubjectId, ct);
        }

        var chapters = course.Chapters.Select(ch => new ChapterDto(
            ch.Id, ch.Title, ch.OrderNum,
            ch.Lessons.Select(l => new LessonSummaryDto(
                l.Id, l.Title, l.Type.ToString(),
                l.DurationSeconds, l.IsFreePreview, l.OrderNum, l.ScheduledAtUtc
            )).ToArray()
        )).ToArray();

        return Result.Success(new CourseDetailDto(
            course.Id, course.Title, course.Description, course.ThumbnailUrl,
            course.PreviewVideoUrl,
            course.SubjectId, course.Subject.NameAr, course.Subject.ColorHex,
            course.StageId, course.Stage.NameAr,
            course.TeacherId, course.Teacher.FullName,
            course.Status.ToString(),
            course.Term.ToString(),
            course.EnrollmentCount, course.RatingAvg,
            hasAccess, chapters));
    }
}
