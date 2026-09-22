using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// All notes the current student has written across all lessons.
// Used by the consolidated "My Notes" study page.
public sealed record GetMyNotesQuery(string? Search = null, Guid? CourseId = null)
    : IQuery<MyNotesResponse>;

public sealed class GetMyNotesHandler : IQueryHandler<GetMyNotesQuery, MyNotesResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyNotesHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<MyNotesResponse>> Handle(GetMyNotesQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;

        var query = _db.LessonNotes
            .Where(n => n.StudentId == studentId);

        if (request.CourseId.HasValue)
        {
            var cid = request.CourseId.Value;
            query = query.Where(n => n.Lesson.Chapter.CourseId == cid);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim();
            query = query.Where(n =>
                n.Body.Contains(s) ||
                n.Lesson.Title.Contains(s) ||
                n.Lesson.Chapter.Course.Title.Contains(s));
        }

        var notes = await query
            .OrderByDescending(n => n.UpdatedAtUtc)
            .Select(n => new MyNoteRowDto(
                n.LessonId,
                n.Lesson.Title,
                n.Lesson.Chapter.CourseId,
                n.Lesson.Chapter.Course.Title,
                n.Lesson.Chapter.Course.Subject.NameAr,
                n.Body,
                n.UpdatedAtUtc))
            .ToListAsync(ct);

        var total = await _db.LessonNotes.CountAsync(n => n.StudentId == studentId, ct);
        var distinctCourses = await _db.LessonNotes
            .Where(n => n.StudentId == studentId)
            .Select(n => n.Lesson.Chapter.CourseId)
            .Distinct()
            .CountAsync(ct);

        return Result.Success(new MyNotesResponse(total, distinctCourses, notes));
    }
}
