using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Security;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Security;

// ─── Status ─────────────────────────────────────
public sealed record GetTotpStatusQuery() : IQuery<TotpStatusDto>;

public sealed class GetTotpStatusHandler : IQueryHandler<GetTotpStatusQuery, TotpStatusDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetTotpStatusHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result<TotpStatusDto>> Handle(GetTotpStatusQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var u = await _db.Users
            .Where(x => x.Id == _currentUser.UserId.Value)
            .Select(x => new { x.TotpEnabled, x.TotpEnabledAtUtc, x.TotpRecoveryCodesHashed })
            .FirstOrDefaultAsync(ct);
        if (u is null)
            return Error.NotFound("USER.NOT_FOUND", "المستخدم غير موجود", "Not found");

        int remainingRecovery = string.IsNullOrEmpty(u.TotpRecoveryCodesHashed)
            ? 0
            : u.TotpRecoveryCodesHashed.Split('|', StringSplitOptions.RemoveEmptyEntries).Length;

        return Result.Success(new TotpStatusDto(u.TotpEnabled, u.TotpEnabledAtUtc, remainingRecovery));
    }
}

// ─── Begin setup ────────────────────────────────
public sealed record BeginTotpSetupCommand() : ICommand<TotpSetupDto>;

public sealed class BeginTotpSetupHandler : ICommandHandler<BeginTotpSetupCommand, TotpSetupDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ITotpService _totp;

    public BeginTotpSetupHandler(IApplicationDbContext db, ICurrentUser currentUser, ITotpService totp)
    { _db = db; _currentUser = currentUser; _totp = totp; }

    public async Task<Result<TotpSetupDto>> Handle(BeginTotpSetupCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == _currentUser.UserId.Value, ct);
        if (user is null)
            return Error.NotFound("USER.NOT_FOUND", "المستخدم غير موجود", "Not found");

        if (user.TotpEnabled)
            return Error.Validation("TOTP.ALREADY_ENABLED",
                "المصادقة الثنائية مفعّلة بالفعل. عطّلها أولًا لإعادة الإعداد.", "Already enabled");

        var secret = _totp.GenerateSecretBase32();
        user.SetTotpSecretPending(secret);
        await _db.SaveChangesAsync(ct);

        var account = user.Email?.Value ?? user.Phone.Value;
        var uri = _totp.BuildProvisioningUri("FSEdu", account, secret);
        return Result.Success(new TotpSetupDto(secret, uri, account));
    }
}

// ─── Confirm setup (verify code, enable, issue recovery codes) ──
public sealed record ConfirmTotpSetupCommand(string Code) : ICommand<TotpEnabledDto>;

public sealed class ConfirmTotpSetupValidator : AbstractValidator<ConfirmTotpSetupCommand>
{
    public ConfirmTotpSetupValidator() => RuleFor(x => x.Code).NotEmpty().Length(6);
}

public sealed class ConfirmTotpSetupHandler : ICommandHandler<ConfirmTotpSetupCommand, TotpEnabledDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ITotpService _totp;

    public ConfirmTotpSetupHandler(IApplicationDbContext db, ICurrentUser currentUser, ITotpService totp)
    { _db = db; _currentUser = currentUser; _totp = totp; }

    public async Task<Result<TotpEnabledDto>> Handle(ConfirmTotpSetupCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == _currentUser.UserId.Value, ct);
        if (user is null)
            return Error.NotFound("USER.NOT_FOUND", "المستخدم غير موجود", "Not found");

        if (string.IsNullOrEmpty(user.TotpSecret))
            return Error.Validation("TOTP.NO_PENDING_SETUP",
                "لم تبدأ إعداد المصادقة الثنائية. ابدأ من جديد.", "No pending setup");

        if (!_totp.VerifyCode(user.TotpSecret, request.Code))
            return Error.Validation("TOTP.INVALID_CODE",
                "الرمز غير صحيح. تأكد من الوقت في هاتفك ثم حاول مرة أخرى.", "Invalid code");

        var (plain, hashed) = _totp.GenerateRecoveryCodes(10);
        user.ConfirmTotp(hashed);
        await _db.SaveChangesAsync(ct);

        return Result.Success(new TotpEnabledDto(plain));
    }
}

// ─── Disable ────────────────────────────────────
public sealed record DisableTotpCommand(string Code) : ICommand;

public sealed class DisableTotpValidator : AbstractValidator<DisableTotpCommand>
{
    public DisableTotpValidator() =>
        RuleFor(x => x.Code).NotEmpty().Must(c => c.Length is 6 or 16 or >= 19)
            .WithMessage("أدخل رمز TOTP (6 أرقام) أو رمز استرداد");
}

public sealed class DisableTotpHandler : ICommandHandler<DisableTotpCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ITotpService _totp;

    public DisableTotpHandler(IApplicationDbContext db, ICurrentUser currentUser, ITotpService totp)
    { _db = db; _currentUser = currentUser; _totp = totp; }

    public async Task<Result> Handle(DisableTotpCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == _currentUser.UserId.Value, ct);
        if (user is null)
            return Error.NotFound("USER.NOT_FOUND", "المستخدم غير موجود", "Not found");
        if (!user.TotpEnabled || string.IsNullOrEmpty(user.TotpSecret))
            return Error.Validation("TOTP.NOT_ENABLED", "المصادقة الثنائية غير مفعّلة", "Not enabled");

        var validTotp = _totp.VerifyCode(user.TotpSecret, request.Code);
        var validRecovery = false;
        if (!validTotp)
        {
            var hash = _totp.HashRecoveryCode(request.Code);
            validRecovery = user.ConsumeRecoveryCode(hash) is not null;
        }

        if (!validTotp && !validRecovery)
            return Error.Validation("TOTP.INVALID_CODE", "الرمز غير صحيح", "Invalid code");

        user.DisableTotp();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Regenerate recovery codes ──────────────────
public sealed record RegenerateTotpRecoveryCommand(string Code) : ICommand<TotpEnabledDto>;

public sealed class RegenerateTotpRecoveryHandler : ICommandHandler<RegenerateTotpRecoveryCommand, TotpEnabledDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ITotpService _totp;

    public RegenerateTotpRecoveryHandler(IApplicationDbContext db, ICurrentUser currentUser, ITotpService totp)
    { _db = db; _currentUser = currentUser; _totp = totp; }

    public async Task<Result<TotpEnabledDto>> Handle(RegenerateTotpRecoveryCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == _currentUser.UserId.Value, ct);
        if (user is null || !user.TotpEnabled || string.IsNullOrEmpty(user.TotpSecret))
            return Error.Validation("TOTP.NOT_ENABLED", "المصادقة الثنائية غير مفعّلة", "Not enabled");

        if (!_totp.VerifyCode(user.TotpSecret, request.Code))
            return Error.Validation("TOTP.INVALID_CODE", "الرمز غير صحيح", "Invalid code");

        var (plain, hashed) = _totp.GenerateRecoveryCodes(10);
        user.ConfirmTotp(hashed); // overwrites hashed list, keeps enabled
        await _db.SaveChangesAsync(ct);
        return Result.Success(new TotpEnabledDto(plain));
    }
}
