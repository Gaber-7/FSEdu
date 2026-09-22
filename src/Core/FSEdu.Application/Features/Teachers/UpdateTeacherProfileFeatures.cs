using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Teachers;

// ─── Update bio + years of experience ─────────────────
public sealed record UpdateTeacherProfileCommand(string? Bio, int YearsOfExperience) : ICommand;

public sealed class UpdateTeacherProfileValidator : AbstractValidator<UpdateTeacherProfileCommand>
{
    public UpdateTeacherProfileValidator()
    {
        RuleFor(x => x.Bio).MaximumLength(2000);
        RuleFor(x => x.YearsOfExperience).InclusiveBetween(0, 60);
    }
}

public sealed class UpdateTeacherProfileHandler : ICommandHandler<UpdateTeacherProfileCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateTeacherProfileHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateTeacherProfileCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var teacher = await _db.Teachers.FirstOrDefaultAsync(t => t.Id == userId, ct);
        if (teacher is null)
            return Error.Forbidden("TEACHER.NOT_FOUND", "الحساب ليس مدرّسًا", "Not a teacher");

        teacher.UpdateBio(request.Bio?.Trim(), request.YearsOfExperience);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Add a qualification ──────────────────────────────
public sealed record AddTeacherQualificationCommand(string Title, string Institution, int Year) : ICommand<long>;

public sealed class AddTeacherQualificationValidator : AbstractValidator<AddTeacherQualificationCommand>
{
    public AddTeacherQualificationValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(2).MaximumLength(255);
        RuleFor(x => x.Institution).NotEmpty().MinimumLength(2).MaximumLength(255);
        RuleFor(x => x.Year).InclusiveBetween(1950, DateTime.UtcNow.Year + 1);
    }
}

public sealed class AddTeacherQualificationHandler : ICommandHandler<AddTeacherQualificationCommand, long>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AddTeacherQualificationHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<long>> Handle(AddTeacherQualificationCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var teacher = await _db.Teachers
            .Include(t => t.Qualifications)
            .FirstOrDefaultAsync(t => t.Id == userId, ct);
        if (teacher is null)
            return Error.Forbidden("TEACHER.NOT_FOUND", "الحساب ليس مدرّسًا", "Not a teacher");

        teacher.AddQualification(request.Title.Trim(), request.Institution.Trim(), request.Year, null);
        await _db.SaveChangesAsync(ct);

        var added = teacher.Qualifications.OrderByDescending(q => q.Id).FirstOrDefault();
        return Result.Success(added?.Id ?? 0L);
    }
}

// ─── Delete a qualification ───────────────────────────
public sealed record DeleteTeacherQualificationCommand(long QualificationId) : ICommand;

public sealed class DeleteTeacherQualificationHandler : ICommandHandler<DeleteTeacherQualificationCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeleteTeacherQualificationHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteTeacherQualificationCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var qual = await _db.TeacherQualifications
            .FirstOrDefaultAsync(q => q.Id == request.QualificationId, ct);
        if (qual is null)
            return Error.NotFound("QUAL.NOT_FOUND", "المؤهل غير موجود", "Not found");

        var isAdmin = _currentUser.IsInRole("Admin");
        if (!isAdmin && qual.TeacherId != userId)
            return Error.Forbidden("QUAL.FORBIDDEN", "غير مسموح بالحذف", "Forbidden");

        _db.TeacherQualifications.Remove(qual);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
