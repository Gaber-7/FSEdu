using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// All lesson questions across the courses owned by the current teacher.
// Used by the teacher's Q&A inbox so they can answer in one place.
public sealed record GetTeacherQuestionsInboxQuery(bool UnansweredOnly = true, int Take = 100)
    : IQuery<TeacherInboxResponse>;

public sealed class GetTeacherQuestionsInboxHandler
    : IQueryHandler<GetTeacherQuestionsInboxQuery, TeacherInboxResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetTeacherQuestionsInboxHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<TeacherInboxResponse>> Handle(
        GetTeacherQuestionsInboxQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var isAdmin = _currentUser.IsInRole("Admin");

        var baseQuery = _db.LessonQuestions
            .Include(q => q.Lesson).ThenInclude(l => l.Chapter).ThenInclude(c => c.Course)
            .AsQueryable();

        if (!isAdmin)
            baseQuery = baseQuery.Where(q => q.Lesson.Chapter.Course.TeacherId == userId);

        var total = await baseQuery.CountAsync(ct);
        var unanswered = await baseQuery.CountAsync(q => q.AnswerBody == null, ct);

        var listQuery = baseQuery;
        if (request.UnansweredOnly)
            listQuery = listQuery.Where(q => q.AnswerBody == null);

        var items = await listQuery
            .OrderBy(q => q.AnswerBody == null ? 0 : 1)
            .ThenByDescending(q => q.CreatedAtUtc)
            .Take(Math.Clamp(request.Take, 1, 500))
            .Select(q => new TeacherInboxQuestionDto(
                q.Id,
                q.LessonId, q.Lesson.Title,
                q.Lesson.Chapter.CourseId, q.Lesson.Chapter.Course.Title,
                q.AskedByUserId, q.AskedByName,
                q.Body, q.CreatedAtUtc,
                q.AnswerBody, q.AnsweredByName, q.AnsweredAtUtc))
            .ToListAsync(ct);

        return Result.Success(new TeacherInboxResponse(total, unanswered, items));
    }
}
