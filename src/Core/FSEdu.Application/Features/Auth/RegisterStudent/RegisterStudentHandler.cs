using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Domain.Users;
using FSEdu.Shared.Contracts.Auth;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Auth.RegisterStudent;

public sealed class RegisterStudentHandler : ICommandHandler<RegisterStudentCommand, AuthResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IIdentityService _identity;

    public RegisterStudentHandler(IApplicationDbContext db, IIdentityService identity)
    {
        _db = db;
        _identity = identity;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterStudentCommand request, CancellationToken ct)
    {
        var phoneVo = PhoneNumber.Create(request.Phone);
        if (phoneVo.IsFailure) return phoneVo.Error;

        Email? emailVo = null;
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var emailResult = Email.Create(request.Email);
            if (emailResult.IsFailure) return emailResult.Error;
            emailVo = emailResult.Value;
        }

        // Validate academic references
        var stageExists = await _db.Stages.AnyAsync(s => s.Id == request.StageId, ct);
        if (!stageExists)
            return Error.Validation("STAGE.NOT_FOUND", "المرحلة الدراسية غير موجودة", "Stage not found");

        var regionExists = await _db.Regions.AnyAsync(r => r.Id == request.RegionId, ct);
        if (!regionExists)
            return Error.Validation("REGION.NOT_FOUND", "المحافظة غير موجودة", "Region not found");

        if (request.SchoolId.HasValue)
        {
            var schoolExists = await _db.Schools.AnyAsync(s => s.Id == request.SchoolId.Value, ct);
            if (!schoolExists)
                return Error.Validation("SCHOOL.NOT_FOUND", "المدرسة غير موجودة", "School not found");
        }

        // 1. Create Identity user (Auth DB)
        var identityResult = await _identity.CreateUserAsync(
            phoneVo.Value.Value,
            emailVo?.Value,
            request.FullName,
            request.Password,
            "Student",
            ct);

        if (identityResult.IsFailure) return identityResult.Error;

        var userId = identityResult.Value;

        // 2. Create Student domain entity (Domain DB)
        var student = new Student(
            userId,
            request.FullName,
            phoneVo.Value,
            request.StageId,
            request.RegionId,
            request.SchoolId,
            emailVo);

        _db.Students.Add(student);
        await _db.SaveChangesAsync(ct);

        // 3. Generate tokens
        var tokens = await _identity.SignInAsync(phoneVo.Value.Value, request.Password, ct);
        if (tokens.IsFailure) return tokens.Error;

        var t = tokens.Value;
        return Result.Success(new AuthResponse(
            t.AccessToken, t.RefreshToken, t.ExpiresAtUtc,
            new UserSummary(t.UserId, t.FullName, t.Phone, t.Email, t.Roles, t.AvatarUrl)
        ));
    }
}
