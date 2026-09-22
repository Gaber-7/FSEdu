using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Courses;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// ─── List my markers for a lesson ─────────────────────
public sealed record GetLessonMarkersQuery(Guid LessonId) : IQuery<List<LessonMarkerDto>>;

public sealed class GetLessonMarkersHandler : IQueryHandler<GetLessonMarkersQuery, List<LessonMarkerDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetLessonMarkersHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<LessonMarkerDto>>> Handle(GetLessonMarkersQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Success(new List<LessonMarkerDto>());

        var studentId = _currentUser.UserId.Value;
        var list = await _db.LessonMarkers
            .Where(m => m.StudentId == studentId && m.LessonId == request.LessonId)
            .OrderBy(m => m.PositionSec)
            .Select(m => new LessonMarkerDto(m.Id, m.LessonId, m.PositionSec, m.Label, m.CreatedAtUtc))
            .ToListAsync(ct);

        return Result.Success(list);
    }
}

// ─── Add a marker ──────────────────────────────────────
public sealed record AddLessonMarkerCommand(Guid LessonId, int PositionSec, string? Label)
    : ICommand<Guid>;

public sealed class AddLessonMarkerValidator : AbstractValidator<AddLessonMarkerCommand>
{
    public AddLessonMarkerValidator()
    {
        RuleFor(x => x.PositionSec).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Label).MaximumLength(255);
    }
}

public sealed class AddLessonMarkerHandler : ICommandHandler<AddLessonMarkerCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public AddLessonMarkerHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<Guid>> Handle(AddLessonMarkerCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var isStudent = await _db.Students.AnyAsync(s => s.Id == studentId, ct);
        if (!isStudent)
            return Error.Forbidden("MARKERS.STUDENTS_ONLY", "العلامات للطلاب فقط", "Students only");

        var hasAccess = await _access.CanAccessLessonAsync(studentId, request.LessonId, ct);
        if (!hasAccess)
            return Error.Forbidden("ACCESS.DENIED", "ليس لديك صلاحية الوصول لهذا الدرس", "No access");

        var marker = new LessonMarker(Guid.NewGuid(), studentId, request.LessonId,
            request.PositionSec, request.Label);
        _db.LessonMarkers.Add(marker);
        await _db.SaveChangesAsync(ct);
        return Result.Success(marker.Id);
    }
}

// ─── Delete a marker ──────────────────────────────────
public sealed record DeleteLessonMarkerCommand(Guid MarkerId) : ICommand;

public sealed class DeleteLessonMarkerHandler : ICommandHandler<DeleteLessonMarkerCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeleteLessonMarkerHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteLessonMarkerCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var marker = await _db.LessonMarkers.FirstOrDefaultAsync(m => m.Id == request.MarkerId, ct);
        if (marker is null)
            return Error.NotFound("MARKER.NOT_FOUND", "العلامة غير موجودة", "Not found");
        if (marker.StudentId != studentId)
            return Error.Forbidden("MARKER.FORBIDDEN", "ليست علامتك", "Forbidden");

        _db.LessonMarkers.Remove(marker);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
