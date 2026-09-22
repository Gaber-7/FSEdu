using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Courses;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// ─── List attachments for a lesson ────────────────────
public sealed record GetLessonAttachmentsQuery(Guid LessonId) : IQuery<List<LessonAttachmentDto>>;

public sealed class GetLessonAttachmentsHandler : IQueryHandler<GetLessonAttachmentsQuery, List<LessonAttachmentDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public GetLessonAttachmentsHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<List<LessonAttachmentDto>>> Handle(GetLessonAttachmentsQuery request, CancellationToken ct)
    {
        var lesson = await _db.Lessons
            .Include(l => l.Chapter).ThenInclude(c => c.Course)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId, ct);
        if (lesson is null)
            return Error.NotFound("LESSON.NOT_FOUND", "الدرس غير موجود", "Not found");

        // Visible to:
        //  - free preview viewers
        //  - the course teacher
        //  - admins
        //  - users with access to the lesson
        bool canView = lesson.IsFreePreview;
        if (!canView && _currentUser.UserId is Guid uid)
        {
            var isAdmin = _currentUser.IsInRole("Admin");
            var isCourseTeacher = lesson.Chapter.Course.TeacherId == uid;
            canView = isAdmin || isCourseTeacher
                       || await _access.CanAccessLessonAsync(uid, request.LessonId, ct);
        }
        if (!canView)
            return Error.Forbidden("ACCESS.DENIED", "ليس لديك صلاحية الوصول لهذا الدرس", "No access");

        var list = await _db.LessonAttachments
            .Where(a => a.LessonId == request.LessonId)
            .OrderBy(a => a.Title)
            .Select(a => new LessonAttachmentDto(a.Id, a.LessonId, a.Title, a.FileUrl, a.FileType, a.SizeBytes))
            .ToListAsync(ct);

        return Result.Success(list);
    }
}

// ─── Add an attachment (teacher of course only) ───────
public sealed record AddLessonAttachmentCommand(Guid LessonId, string Title, string FileUrl, string FileType, long SizeBytes)
    : ICommand<Guid>;

public sealed class AddLessonAttachmentValidator : AbstractValidator<AddLessonAttachmentCommand>
{
    public AddLessonAttachmentValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(2).MaximumLength(255);
        RuleFor(x => x.FileUrl).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.FileType).NotEmpty().MaximumLength(20);
        RuleFor(x => x.SizeBytes).GreaterThan(0);
    }
}

public sealed class AddLessonAttachmentHandler : ICommandHandler<AddLessonAttachmentCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AddLessonAttachmentHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(AddLessonAttachmentCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var lesson = await _db.Lessons
            .Include(l => l.Chapter).ThenInclude(c => c.Course)
            .Include(l => l.Attachments)
            .FirstOrDefaultAsync(l => l.Id == request.LessonId, ct);
        if (lesson is null)
            return Error.NotFound("LESSON.NOT_FOUND", "الدرس غير موجود", "Not found");

        var isAdmin = _currentUser.IsInRole("Admin");
        if (!isAdmin && lesson.Chapter.Course.TeacherId != userId)
            return Error.Forbidden("ATTACH.FORBIDDEN", "فقط مدرّس الدورة يستطيع الإضافة", "Teacher only");

        var attachment = new LessonAttachment(
            Guid.NewGuid(), request.LessonId, request.Title.Trim(),
            request.FileUrl, request.FileType, request.SizeBytes);
        lesson.AddAttachment(attachment);
        await _db.SaveChangesAsync(ct);

        return Result.Success(attachment.Id);
    }
}

// ─── Delete an attachment ─────────────────────────────
public sealed record DeleteLessonAttachmentCommand(Guid AttachmentId) : ICommand;

public sealed class DeleteLessonAttachmentHandler : ICommandHandler<DeleteLessonAttachmentCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeleteLessonAttachmentHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteLessonAttachmentCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var att = await _db.LessonAttachments.FirstOrDefaultAsync(a => a.Id == request.AttachmentId, ct);
        if (att is null)
            return Error.NotFound("ATTACH.NOT_FOUND", "المرفق غير موجود", "Not found");

        var courseTeacherId = await _db.Lessons
            .Where(l => l.Id == att.LessonId)
            .Select(l => (Guid?)l.Chapter.Course.TeacherId)
            .FirstOrDefaultAsync(ct);

        var isAdmin = _currentUser.IsInRole("Admin");
        if (!isAdmin && courseTeacherId != userId)
            return Error.Forbidden("ATTACH.FORBIDDEN", "غير مسموح بالحذف", "Forbidden");

        _db.LessonAttachments.Remove(att);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
