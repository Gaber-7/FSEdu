using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Domain.Users;
using FSEdu.Shared.Contracts.Auth;
using FSEdu.Shared.Kernel.Results;

namespace FSEdu.Application.Features.Auth.RegisterParent;

public sealed record RegisterParentCommand(
    string FullName,
    string Phone,
    string? Email,
    string Password,
    string? NationalId,
    string? Occupation
) : ICommand<AuthResponse>;

public sealed class RegisterParentValidator : AbstractValidator<RegisterParentCommand>
{
    public RegisterParentValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MinimumLength(3).MaximumLength(200)
            .WithMessage("الاسم الكامل مطلوب (3 أحرف على الأقل)");

        RuleFor(x => x.Phone).NotEmpty().Matches(@"^\+[1-9]\d{7,14}$")
            .WithMessage("صيغة رقم الهاتف غير صحيحة");

        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).Matches(@"\d")
            .WithMessage("كلمة المرور يجب ألا تقل عن 8 أحرف وتحتوي على رقم");

        When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
        {
            RuleFor(x => x.Email!).EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");
        });

        When(x => !string.IsNullOrWhiteSpace(x.NationalId), () =>
        {
            RuleFor(x => x.NationalId!).Length(14).WithMessage("الرقم القومي يجب أن يكون 14 رقمًا");
        });
    }
}

public sealed class RegisterParentHandler : ICommandHandler<RegisterParentCommand, AuthResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IIdentityService _identity;

    public RegisterParentHandler(IApplicationDbContext db, IIdentityService identity)
    {
        _db = db;
        _identity = identity;
    }

    public async Task<Result<AuthResponse>> Handle(RegisterParentCommand request, CancellationToken ct)
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

        var identityResult = await _identity.CreateUserAsync(
            phoneVo.Value.Value, emailVo?.Value, request.FullName,
            request.Password, "Parent", ct);
        if (identityResult.IsFailure) return identityResult.Error;

        var userId = identityResult.Value;

        var parent = new Parent(userId, request.FullName, phoneVo.Value, emailVo,
                                 request.NationalId, request.Occupation);
        parent.VerifyPhone();
        parent.Activate();

        _db.Parents.Add(parent);
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
