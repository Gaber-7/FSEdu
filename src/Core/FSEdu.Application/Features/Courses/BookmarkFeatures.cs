using FSEdu.Application.Abstractions;
using FSEdu.Domain.Courses;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// ─── Toggle bookmark ──────────────────────────────────
public sealed record ToggleCourseBookmarkCommand(Guid CourseId)
    : ICommand<ToggleBookmarkResponse>;

public sealed class ToggleCourseBookmarkHandler
    : ICommandHandler<ToggleCourseBookmarkCommand, ToggleBookmarkResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ToggleCourseBookmarkHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<ToggleBookmarkResponse>> Handle(
        ToggleCourseBookmarkCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var isStudent = await _db.Students.AnyAsync(s => s.Id == studentId, ct);
        if (!isStudent)
            return Error.Forbidden("BOOKMARKS.STUDENTS_ONLY", "المحفوظات للطلاب فقط", "Students only");

        var courseExists = await _db.Courses.AnyAsync(c => c.Id == request.CourseId, ct);
        if (!courseExists)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Not found");

        var existing = await _db.CourseBookmarks
            .FirstOrDefaultAsync(b => b.StudentId == studentId && b.CourseId == request.CourseId, ct);

        if (existing is null)
        {
            _db.CourseBookmarks.Add(new CourseBookmark(studentId, request.CourseId));
            await _db.SaveChangesAsync(ct);
            return Result.Success(new ToggleBookmarkResponse(true));
        }
        else
        {
            _db.CourseBookmarks.Remove(existing);
            await _db.SaveChangesAsync(ct);
            return Result.Success(new ToggleBookmarkResponse(false));
        }
    }
}

// ─── List my bookmarks ────────────────────────────────
public sealed record GetMyBookmarksQuery() : IQuery<List<BookmarkedCourseDto>>;

public sealed class GetMyBookmarksHandler : IQueryHandler<GetMyBookmarksQuery, List<BookmarkedCourseDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyBookmarksHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<BookmarkedCourseDto>>> Handle(
        GetMyBookmarksQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;

        var list = await _db.CourseBookmarks
            .Where(b => b.StudentId == studentId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new BookmarkedCourseDto(
                b.CourseId,
                b.Course.Title,
                b.Course.Subject.NameAr,
                b.Course.Stage.NameAr,
                b.Course.Teacher.FullName,
                b.Course.ThumbnailUrl,
                b.Course.Chapters.Sum(ch => ch.Lessons.Count),
                b.Course.RatingAvg,
                b.CreatedAtUtc
            ))
            .ToListAsync(ct);

        return Result.Success(list);
    }
}

// ─── Check if a single course is bookmarked ───────────
public sealed record IsCourseBookmarkedQuery(Guid CourseId) : IQuery<bool>;

public sealed class IsCourseBookmarkedHandler : IQueryHandler<IsCourseBookmarkedQuery, bool>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public IsCourseBookmarkedHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(IsCourseBookmarkedQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Success(false);

        var studentId = _currentUser.UserId.Value;
        var exists = await _db.CourseBookmarks
            .AnyAsync(b => b.StudentId == studentId && b.CourseId == request.CourseId, ct);
        return Result.Success(exists);
    }
}
