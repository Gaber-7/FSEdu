using FSEdu.Application.Abstractions;
using FSEdu.Domain.Engagement;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Homework;

// ─── List my homework ─────────────────────────
public sealed record ListMyStudentHomeworkQuery() : IQuery<List<StudentHomeworkRowDto>>;

public sealed class ListMyStudentHomeworkHandler : IQueryHandler<ListMyStudentHomeworkQuery, List<StudentHomeworkRowDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ListMyStudentHomeworkHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result<List<StudentHomeworkRowDto>>> Handle(ListMyStudentHomeworkQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var now = DateTime.UtcNow;

        // Get all homework from courses the student is enrolled in
        var enrolledCourseIds = await _db.Enrollments
            .Where(e => e.StudentId == studentId)
            .Select(e => e.CourseId)
            .ToListAsync(ct);

        var rows = await (
            from h in _db.Homeworks
            where h.Published && enrolledCourseIds.Contains(h.CourseId)
            join c in _db.Courses on h.CourseId equals c.Id
            join sub in _db.Subjects on c.SubjectId equals sub.Id
            join s in _db.HomeworkSubmissions.Where(x => x.StudentId == studentId)
                on h.Id equals s.HomeworkId into subj
            from mySub in subj.DefaultIfEmpty()
            orderby h.DueDateUtc
            select new
            {
                HomeworkId = h.Id,
                h.CourseId, CourseTitle = c.Title,
                SubjectColor = sub.ColorHex,
                h.Title, h.DueDateUtc, h.MaxScore,
                MySubmissionAt = (DateTime?)(mySub != null ? mySub.SubmittedAtUtc : (DateTime?)null),
                MyGradedAt = (DateTime?)(mySub != null ? mySub.GradedAtUtc : (DateTime?)null),
                MyScore = (decimal?)(mySub != null ? mySub.Score : (decimal?)null),
                MyLate = (bool)(mySub != null && mySub.Late)
            }).ToListAsync(ct);

        return Result.Success(rows.Select(r =>
        {
            string status;
            if (r.MyGradedAt is not null) status = "Graded";
            else if (r.MySubmissionAt is not null) status = r.MyLate ? "Late" : "Submitted";
            else if (r.DueDateUtc < now) status = "Overdue";
            else status = "Pending";
            return new StudentHomeworkRowDto(
                r.HomeworkId, r.CourseId, r.CourseTitle, r.SubjectColor,
                r.Title, r.DueDateUtc, r.MaxScore,
                status, r.MySubmissionAt, r.MyScore);
        }).ToList());
    }
}

// ─── Get detail (student view) ────────────────
public sealed record GetStudentHomeworkDetailQuery(Guid Id) : IQuery<StudentHomeworkDetailDto>;

public sealed class GetStudentHomeworkDetailHandler : IQueryHandler<GetStudentHomeworkDetailQuery, StudentHomeworkDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetStudentHomeworkDetailHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result<StudentHomeworkDetailDto>> Handle(GetStudentHomeworkDetailQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var hw = await _db.Homeworks
            .Include(h => h.Course)
            .FirstOrDefaultAsync(h => h.Id == request.Id && h.Published, ct);
        if (hw is null) return Error.NotFound("HOMEWORK.NOT_FOUND", "الواجب غير موجود", "Not found");

        // Must be enrolled in the course
        var enrolled = await _db.Enrollments.AnyAsync(e => e.CourseId == hw.CourseId && e.StudentId == studentId, ct);
        if (!enrolled) return Error.Forbidden("HOMEWORK.NOT_ENROLLED", "غير مشترك فى هذه الدورة", "Not enrolled");

        var sub = await _db.HomeworkSubmissions
            .FirstOrDefaultAsync(s => s.HomeworkId == hw.Id && s.StudentId == studentId, ct);

        var subDto = sub is null ? null : new StudentHomeworkSubmissionDto(
            sub.Id, sub.SubmittedAtUtc, sub.Body, sub.AttachmentUrl, sub.Late,
            sub.GradedAtUtc, sub.Score, sub.FeedbackBody);

        return Result.Success(new StudentHomeworkDetailDto(
            hw.Id, hw.CourseId, hw.Course.Title, hw.Title, hw.Description, hw.AttachmentUrl,
            hw.DueDateUtc, hw.MaxScore, hw.IsOverdue(DateTime.UtcNow), subDto));
    }
}

// ─── Submit (or update) ───────────────────────
public sealed record SubmitHomeworkCommand(Guid HomeworkId, string? Body, string? AttachmentUrl) : ICommand<Guid>;

public sealed class SubmitHomeworkHandler : ICommandHandler<SubmitHomeworkCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDailyChallengeService _dailyChallenge;

    public SubmitHomeworkHandler(IApplicationDbContext db, ICurrentUser currentUser,
        IDailyChallengeService dailyChallenge)
    { _db = db; _currentUser = currentUser; _dailyChallenge = dailyChallenge; }

    public async Task<Result<Guid>> Handle(SubmitHomeworkCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var hw = await _db.Homeworks.FirstOrDefaultAsync(h => h.Id == request.HomeworkId && h.Published, ct);
        if (hw is null) return Error.NotFound("HOMEWORK.NOT_FOUND", "الواجب غير موجود", "Not found");

        var enrolled = await _db.Enrollments.AnyAsync(e => e.CourseId == hw.CourseId && e.StudentId == studentId, ct);
        if (!enrolled) return Error.Forbidden("HOMEWORK.NOT_ENROLLED", "غير مشترك فى هذه الدورة", "Not enrolled");

        if (string.IsNullOrWhiteSpace(request.Body) && string.IsNullOrWhiteSpace(request.AttachmentUrl))
            return Error.Validation("SUB.EMPTY", "أضف نصًا أو مرفقًا واحدًا على الأقل", "Empty submission");

        var existing = await _db.HomeworkSubmissions
            .FirstOrDefaultAsync(s => s.HomeworkId == hw.Id && s.StudentId == studentId, ct);

        var late = DateTime.UtcNow > hw.DueDateUtc;

        if (existing is null)
        {
            var sub = new Domain.Courses.HomeworkSubmission(Guid.NewGuid(), hw.Id, studentId,
                request.Body, request.AttachmentUrl, late);
            _db.HomeworkSubmissions.Add(sub);
            await _db.SaveChangesAsync(ct);
            await _dailyChallenge.RecordProgressAsync(studentId, DailyChallengeType.SubmitHomework, 1, ct);
            return Result.Success(sub.Id);
        }
        else
        {
            if (existing.GradedAtUtc is not null)
                return Error.Validation("SUB.ALREADY_GRADED", "تم تصحيح هذا التسليم — لا يمكن التعديل", "Already graded");
            existing.UpdateContent(request.Body, request.AttachmentUrl);
            await _db.SaveChangesAsync(ct);
            return Result.Success(existing.Id);
        }
    }
}
