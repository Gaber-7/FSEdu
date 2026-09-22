using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Domain.Users;
using FSEdu.Shared.Contracts.Auth;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Auth.RegisterTeacher;

public sealed record RegisterTeacherCommand(
    string FullName,
    string Phone,
    string? Email,
    string Password,
    string? Bio,
    int YearsOfExperience,
    int[] SubjectIds,
    int[] RegionIds,
    TeacherQualificationDto[] Qualifications
) : ICommand<AuthResponse>;

public sealed class RegisterTeacherValidator : AbstractValidator<RegisterTeacherCommand>
{
    public RegisterTeacherValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(3).MaximumLength(200)
            .WithMessage("الاسم الكامل مطلوب (3 أحرف على الأقل)");

        RuleFor(x => x.Phone).NotEmpty().Matches(@"^\+[1-9]\d{7,14}$")
            .WithMessage("صيغة رقم الهاتف غير صحيحة");

        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).Matches(@"\d")
            .WithMessage("كلمة المرور يجب ألا تقل عن 8 أحرف وتحتوي على رقم");

        RuleFor(x => x.YearsOfExperience).InclusiveBetween(0, 60)
            .WithMessage("سنوات الخبرة غير صحيحة");

        RuleFor(x => x.SubjectIds).NotEmpty().WithMessage("اختر مادة واحدة على الأقل");
        RuleFor(x => x.RegionIds).NotEmpty().WithMessage("اختر منطقة تعليمية واحدة على الأقل");

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
            RuleFor(x => x.Email!).EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة"));
    }
}

public sealed class RegisterTeacherHandler : ICommandHandler<RegisterTeacherCommand, AuthResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IIdentityService _identity;

    public RegisterTeacherHandler(IApplicationDbContext db, IIdentityService identity)
    {
        _db = db;
        _identity = identity;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterTeacherCommand request, CancellationToken ct)
    {
        var phoneVo = PhoneNumber.Create(request.Phone);
        if (phoneVo.IsFailure) return phoneVo.Error;

        Email? emailVo = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var er = Email.Create(request.Email);
            if (er.IsFailure) return er.Error;
            emailVo = er.Value;
        }

        var subjects = await _db.Subjects.Where(s => request.SubjectIds.Contains(s.Id)).Select(s => s.Id).ToListAsync(ct);
        if (subjects.Count != request.SubjectIds.Length)
            return Error.Validation("SUBJECT.INVALID", "بعض المواد المختارة غير صحيحة", "Invalid subjects");

        var regions = await _db.Regions.Where(r => request.RegionIds.Contains(r.Id)).Select(r => r.Id).ToListAsync(ct);
        if (regions.Count != request.RegionIds.Length)
            return Error.Validation("REGION.INVALID", "بعض المناطق المختارة غير صحيحة", "Invalid regions");

        var identityResult = await _identity.CreateUserAsync(
            phoneVo.Value.Value, emailVo?.Value, request.FullName,
            request.Password, "Teacher", ct);
        if (identityResult.IsFailure) return identityResult.Error;

        var userId = identityResult.Value;

        var teacher = new Teacher(userId, request.FullName, phoneVo.Value,
                                   request.YearsOfExperience, request.Bio, emailVo);

        foreach (var subjectId in subjects) teacher.AddSubject(subjectId);
        foreach (var regionId in regions) teacher.AddRegion(regionId);

        foreach (var q in request.Qualifications)
            teacher.AddQualification(q.Title, q.Institution, q.Year, q.DocumentUrl);

        teacher.VerifyPhone();
        // Status remains Pending — awaits admin approval

        _db.Teachers.Add(teacher);
        await _db.SaveChangesAsync(ct);

        var tokens = await _identity.SignInAsync(phoneVo.Value.Value, request.Password, ct);
        if (tokens.IsFailure) return tokens.Error;

        var t = tokens.Value;
        return Result.Success(new AuthResponse(
            t.AccessToken, t.RefreshToken, t.ExpiresAtUtc,
            new UserSummary(t.UserId, t.FullName, t.Phone, t.Email, t.Roles, t.AvatarUrl)
        ));
    }
}
