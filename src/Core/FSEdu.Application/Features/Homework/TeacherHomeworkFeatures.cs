using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Courses;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Homework;

// ─── Create ───────────────────────────────────
public sealed record CreateHomeworkCommand(
    Guid CourseId,
    string Title,
    string? Description,
    string? AttachmentUrl,
    DateTime DueDateUtc,
    decimal MaxScore,
    bool PublishNow
) : ICommand<Guid>;

public sealed class CreateHomeworkValidator : AbstractValidator<CreateHomeworkCommand>
{
    public CreateHomeworkValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(255);
        RuleFor(x => x.MaxScore).GreaterThan(0);
        RuleFor(x => x.DueDateUtc).Must(d => d > DateTime.UtcNow)
            .WithMessage("تاريخ التسليم يجب أن يكون فى المستقبل");
    }
}

public sealed class CreateHomeworkHandler : ICommandHandler<CreateHomeworkCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public CreateHomeworkHandler(IApplicationDbContext db, ICurrentUser currentUser,
        INotificationService notifications)
    { _db = db; _currentUser = currentUser; _notifications = notifications; }

    public async Task<Result<Guid>> Handle(CreateHomeworkCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.UserId ?? Guid.Empty;
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId && c.TeacherId == teacherId, ct);
        if (course is null)
            return Error.NotFound("COURSE.NOT_FOUND_OR_NOT_OWNER", "الدورة غير موجودة أو ليست دورتك", "Not your course");

        var hw = new Domain.Courses.Homework(Guid.NewGuid(), course.Id, teacherId,
            request.Title, request.DueDateUtc, request.MaxScore);
        hw.UpdateDetails(request.Title, request.Description, request.AttachmentUrl,
            request.DueDateUtc, request.MaxScore);
        if (request.PublishNow) hw.Publish();
        _db.Homeworks.Add(hw);
        await _db.SaveChangesAsync(ct);

        // Fan out a notification to every enrolled student when published immediately
        if (request.PublishNow)
        {
            var studentIds = await _db.Enrollments
                .Where(e => e.CourseId == course.Id)
                .Select(e => e.StudentId)
                .ToListAsync(ct);
            if (studentIds.Count > 0)
            {
                await _notifications.SendBulkAsync(studentIds,
                    "homework.new",
                    "📝 واجب جديد",
                    $"{course.Title} — {hw.Title}. آخر موعد للتسليم: {hw.DueDateUtc.ToLocalTime():yyyy/MM/dd HH:mm}",
                    new { url = $"/my/homework/{hw.Id}" },
                    ct);
            }
        }

        return Result.Success(hw.Id);
    }
}

// ─── Update ───────────────────────────────────
public sealed record UpdateHomeworkCommand(
    Guid Id,
    string Title,
    string? Description,
    string? AttachmentUrl,
    DateTime DueDateUtc,
    decimal MaxScore
) : ICommand;

public sealed class UpdateHomeworkHandler : ICommandHandler<UpdateHomeworkCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    public UpdateHomeworkHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result> Handle(UpdateHomeworkCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.UserId ?? Guid.Empty;
        var hw = await _db.Homeworks.FirstOrDefaultAsync(h => h.Id == request.Id && h.TeacherId == teacherId, ct);
        if (hw is null) return Error.NotFound("HOMEWORK.NOT_FOUND", "الواجب غير موجود", "Not found");

        hw.UpdateDetails(request.Title, request.Description, request.AttachmentUrl,
            request.DueDateUtc, request.MaxScore);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Publish / Unpublish ──────────────────────
public sealed record ToggleHomeworkPublishCommand(Guid Id, bool Publish) : ICommand;

public sealed class ToggleHomeworkPublishHandler : ICommandHandler<ToggleHomeworkPublishCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;
    public ToggleHomeworkPublishHandler(IApplicationDbContext db, ICurrentUser currentUser, INotificationService notifications)
    { _db = db; _currentUser = currentUser; _notifications = notifications; }

    public async Task<Result> Handle(ToggleHomeworkPublishCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.UserId ?? Guid.Empty;
        var hw = await _db.Homeworks
            .Include(h => h.Course)
            .FirstOrDefaultAsync(h => h.Id == request.Id && h.TeacherId == teacherId, ct);
        if (hw is null) return Error.NotFound("HOMEWORK.NOT_FOUND", "الواجب غير موجود", "Not found");

        var wasPublished = hw.Published;
        if (request.Publish) hw.Publish(); else hw.Unpublish();
        await _db.SaveChangesAsync(ct);

        // Notify on first publish only
        if (!wasPublished && request.Publish)
        {
            var studentIds = await _db.Enrollments
                .Where(e => e.CourseId == hw.CourseId)
                .Select(e => e.StudentId)
                .ToListAsync(ct);
            if (studentIds.Count > 0)
            {
                await _notifications.SendBulkAsync(studentIds,
                    "homework.new",
                    "📝 واجب جديد",
                    $"{hw.Course.Title} — {hw.Title}. آخر موعد: {hw.DueDateUtc.ToLocalTime():yyyy/MM/dd HH:mm}",
                    new { url = $"/my/homework/{hw.Id}" }, ct);
            }
        }
        return Result.Success();
    }
}

// ─── Delete ───────────────────────────────────
public sealed record DeleteHomeworkCommand(Guid Id) : ICommand;

public sealed class DeleteHomeworkHandler : ICommandHandler<DeleteHomeworkCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    public DeleteHomeworkHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result> Handle(DeleteHomeworkCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.UserId ?? Guid.Empty;
        var hw = await _db.Homeworks.FirstOrDefaultAsync(h => h.Id == request.Id && h.TeacherId == teacherId, ct);
        if (hw is null) return Error.NotFound("HOMEWORK.NOT_FOUND", "الواجب غير موجود", "Not found");
        _db.Homeworks.Remove(hw);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── List (teacher view) ──────────────────────
public sealed record ListMyTeacherHomeworkQuery() : IQuery<List<TeacherHomeworkRowDto>>;

public sealed class ListMyTeacherHomeworkHandler : IQueryHandler<ListMyTeacherHomeworkQuery, List<TeacherHomeworkRowDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    public ListMyTeacherHomeworkHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result<List<TeacherHomeworkRowDto>>> Handle(ListMyTeacherHomeworkQuery request, CancellationToken ct)
    {
        var teacherId = _currentUser.UserId ?? Guid.Empty;

        var rows = await (
            from h in _db.Homeworks
            where h.TeacherId == teacherId
            join c in _db.Courses on h.CourseId equals c.Id
            orderby h.DueDateUtc descending
            select new
            {
                h.Id, h.CourseId, CourseTitle = c.Title, h.Title, h.DueDateUtc,
                h.MaxScore, h.Published
            }).ToListAsync(ct);

        var hwIds = rows.Select(r => r.Id).ToList();
        var enrollByCourse = await _db.Enrollments
            .Where(e => rows.Select(r => r.CourseId).Distinct().Contains(e.CourseId))
            .GroupBy(e => e.CourseId)
            .Select(g => new { CourseId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourseId, x => x.Count, ct);

        var subsByHw = await _db.HomeworkSubmissions
            .Where(s => hwIds.Contains(s.HomeworkId))
            .GroupBy(s => s.HomeworkId)
            .Select(g => new
            {
                HomeworkId = g.Key,
                Submitted = g.Count(),
                Graded = g.Count(x => x.GradedAtUtc != null),
            })
            .ToDictionaryAsync(x => x.HomeworkId, x => x, ct);

        return Result.Success(rows.Select(r => new TeacherHomeworkRowDto(
            r.Id, r.CourseId, r.CourseTitle, r.Title, r.DueDateUtc, r.MaxScore, r.Published,
            enrollByCourse.GetValueOrDefault(r.CourseId, 0),
            subsByHw.TryGetValue(r.Id, out var s) ? s.Submitted : 0,
            subsByHw.TryGetValue(r.Id, out var g) ? g.Graded : 0
        )).ToList());
    }
}

// ─── Detail (teacher view — with submissions) ─
public sealed record GetTeacherHomeworkDetailQuery(Guid Id) : IQuery<TeacherHomeworkDetailDto>;

public sealed class GetTeacherHomeworkDetailHandler : IQueryHandler<GetTeacherHomeworkDetailQuery, TeacherHomeworkDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    public GetTeacherHomeworkDetailHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result<TeacherHomeworkDetailDto>> Handle(GetTeacherHomeworkDetailQuery request, CancellationToken ct)
    {
        var teacherId = _currentUser.UserId ?? Guid.Empty;
        var hw = await _db.Homeworks
            .Include(h => h.Course)
            .FirstOrDefaultAsync(h => h.Id == request.Id && h.TeacherId == teacherId, ct);
        if (hw is null) return Error.NotFound("HOMEWORK.NOT_FOUND", "الواجب غير موجود", "Not found");

        var subs = await (
            from s in _db.HomeworkSubmissions
            where s.HomeworkId == hw.Id
            join u in _db.Users on s.StudentId equals u.Id
            orderby s.SubmittedAtUtc descending
            select new TeacherHomeworkSubmissionRowDto(
                s.Id, s.StudentId, u.FullName,
                s.SubmittedAtUtc, s.Late, s.AttachmentUrl, s.Body,
                s.GradedAtUtc != null, s.Score, s.FeedbackBody
            )).ToListAsync(ct);

        return Result.Success(new TeacherHomeworkDetailDto(
            hw.Id, hw.CourseId, hw.Course.Title, hw.Title, hw.Description, hw.AttachmentUrl,
            hw.DueDateUtc, hw.MaxScore, hw.Published, subs.ToArray()));
    }
}

// ─── Grade submission ─────────────────────────
public sealed record GradeHomeworkSubmissionCommand(Guid SubmissionId, decimal Score, string? Feedback) : ICommand;

public sealed class GradeHomeworkSubmissionHandler : ICommandHandler<GradeHomeworkSubmissionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public GradeHomeworkSubmissionHandler(IApplicationDbContext db, ICurrentUser currentUser,
        INotificationService notifications)
    { _db = db; _currentUser = currentUser; _notifications = notifications; }

    public async Task<Result> Handle(GradeHomeworkSubmissionCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.UserId ?? Guid.Empty;
        var sub = await _db.HomeworkSubmissions
            .Include(s => s.Homework)
            .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, ct);
        if (sub is null) return Error.NotFound("SUB.NOT_FOUND", "التسليم غير موجود", "Not found");
        if (sub.Homework.TeacherId != teacherId)
            return Error.Forbidden("SUB.NOT_OWNER", "ليس واجبك", "Not your homework");
        if (request.Score < 0 || request.Score > sub.Homework.MaxScore)
            return Error.Validation("SCORE.OUT_OF_RANGE",
                $"الدرجة يجب أن تكون بين 0 و {sub.Homework.MaxScore}", "Score out of range");

        sub.Grade(teacherId, request.Score, request.Feedback);
        await _db.SaveChangesAsync(ct);

        await _notifications.SendAsync(sub.StudentId,
            "homework.graded",
            "📝 تم تصحيح واجبك",
            $"{sub.Homework.Title} — حصلت على {request.Score} / {sub.Homework.MaxScore}",
            new { url = $"/my/homework/{sub.HomeworkId}" }, ct);

        return Result.Success();
    }
}
