using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Courses;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// ─── Get a student's note for a lesson ─────────────────
public sealed record GetLessonNoteQuery(Guid LessonId) : IQuery<LessonNoteDto>;

public sealed class GetLessonNoteHandler : IQueryHandler<GetLessonNoteQuery, LessonNoteDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetLessonNoteHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<LessonNoteDto>> Handle(GetLessonNoteQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var note = await _db.LessonNotes
            .Where(n => n.StudentId == studentId && n.LessonId == request.LessonId)
            .Select(n => new LessonNoteDto(n.LessonId, n.Body, n.UpdatedAtUtc))
            .FirstOrDefaultAsync(ct);

        return Result.Success(note ?? new LessonNoteDto(request.LessonId, string.Empty, null));
    }
}

// ─── Save (upsert) the note ────────────────────────────
public sealed record SaveLessonNoteCommand(Guid LessonId, string Body) : ICommand;

public sealed class SaveLessonNoteValidator : AbstractValidator<SaveLessonNoteCommand>
{
    public SaveLessonNoteValidator()
    {
        RuleFor(x => x.Body).MaximumLength(8000);
    }
}

public sealed class SaveLessonNoteHandler : ICommandHandler<SaveLessonNoteCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public SaveLessonNoteHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result> Handle(SaveLessonNoteCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var isStudent = await _db.Students.AnyAsync(s => s.Id == studentId, ct);
        if (!isStudent)
            return Error.Forbidden("NOTES.STUDENTS_ONLY", "الملاحظات للطلاب فقط", "Students only");

        var hasAccess = await _access.CanAccessLessonAsync(studentId, request.LessonId, ct);
        if (!hasAccess)
            return Error.Forbidden("ACCESS.DENIED", "ليس لديك صلاحية الوصول لهذا الدرس", "No access");

        var existing = await _db.LessonNotes
            .FirstOrDefaultAsync(n => n.StudentId == studentId && n.LessonId == request.LessonId, ct);

        var body = (request.Body ?? string.Empty);
        // If body is whitespace-only, clear the note (delete row).
        if (string.IsNullOrWhiteSpace(body))
        {
            if (existing is not null)
            {
                _db.LessonNotes.Remove(existing);
                await _db.SaveChangesAsync(ct);
            }
            return Result.Success();
        }

        // Cap to schema max as a safety net (validator already enforces)
        if (body.Length > 8000) body = body.Substring(0, 8000);

        if (existing is null)
        {
            _db.LessonNotes.Add(new LessonNote(studentId, request.LessonId, body));
        }
        else
        {
            existing.Update(body);
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
