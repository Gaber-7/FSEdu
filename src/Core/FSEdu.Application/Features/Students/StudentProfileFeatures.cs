using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Auth;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Students;

// ─── Get current student's profile ─────────────────────
public sealed record GetMyStudentProfileQuery() : IQuery<StudentProfileDto>;

public sealed class GetMyStudentProfileHandler : IQueryHandler<GetMyStudentProfileQuery, StudentProfileDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyStudentProfileHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<StudentProfileDto>> Handle(GetMyStudentProfileQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var student = await _db.Students
            .Include(s => s.Stage)
            .Include(s => s.Region)
            .Include(s => s.School)
            .FirstOrDefaultAsync(s => s.Id == userId, ct);

        if (student is null)
            return Error.NotFound("STUDENT.NOT_FOUND", "الحساب ليس طالبًا", "Not a student");

        return Result.Success(new StudentProfileDto(
            student.Id,
            student.FullName,
            student.Phone.Value,
            student.WhatsAppNumber,
            student.Email?.Value,
            student.EmailVerified,
            student.StageId, student.Stage.NameAr,
            student.RegionId, student.Region.NameAr,
            student.SchoolId, student.School?.Name,
            student.AvatarUrl,
            student.XpPoints,
            student.CurrentStreakDays,
            student.LongestStreakDays
        ));
    }
}

// ─── Update profile ────────────────────────────────────
public sealed record UpdateStudentProfileCommand(
    string FullName,
    string? Email,
    string? Phone,
    string? WhatsAppNumber) : ICommand;

public sealed class UpdateStudentProfileValidator : AbstractValidator<UpdateStudentProfileCommand>
{
    public UpdateStudentProfileValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(3).MaximumLength(200);
        RuleFor(x => x.Email).MaximumLength(200);
        RuleFor(x => x.WhatsAppNumber).MaximumLength(20);
        When(x => !string.IsNullOrWhiteSpace(x.Phone), () =>
            RuleFor(x => x.Phone!).Matches(@"^\+?[1-9]\d{7,14}$")
                .WithMessage("رقم الجوّال يجب أن يكون بصيغة دولية صحيحة"));
    }
}

public sealed class UpdateStudentProfileHandler : ICommandHandler<UpdateStudentProfileCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UpdateStudentProfileHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(UpdateStudentProfileCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == userId, ct);
        if (student is null)
            return Error.Forbidden("STUDENT.NOT_FOUND", "الحساب ليس طالبًا", "Not a student");

        // Email
        Email? newEmail = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailResult = Email.Create(request.Email.Trim());
            if (emailResult.IsFailure)
                return Error.Validation("EMAIL.INVALID", "البريد الإلكتروني غير صالح", "Invalid email");
            newEmail = emailResult.Value;
        }

        // Phone change (if provided and different)
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var newPhoneText = request.Phone.Trim();
            if (newPhoneText != student.Phone.Value)
            {
                // Ensure phone uniqueness across users (Phone is the login identifier)
                var inUse = await _db.Users.AnyAsync(u =>
                    u.Id != student.Id && u.Phone.Value == newPhoneText, ct);
                if (inUse)
                    return Error.Conflict("PHONE.IN_USE", "رقم الجوّال مستخدم في حساب آخر", "Phone in use");

                var phoneResult = PhoneNumber.Create(newPhoneText);
                if (phoneResult.IsFailure)
                    return Error.Validation("PHONE.INVALID", "رقم الجوّال غير صحيح", "Invalid phone");
                student.ChangePhone(phoneResult.Value);
            }
        }

        // WhatsApp (free-form, optional, light validation)
        student.SetWhatsAppNumber(request.WhatsAppNumber);

        student.UpdateProfile(request.FullName.Trim(), student.AvatarUrl, student.Gender, student.BirthDate);
        student.SetEmail(newEmail);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
