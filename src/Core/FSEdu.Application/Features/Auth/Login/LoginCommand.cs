using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Auth;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Auth.Login;

public sealed record LoginCommand(string Phone, string Password, string? TotpCode = null)
    : ICommand<AuthResponse>;

public sealed class LoginHandler : ICommandHandler<LoginCommand, AuthResponse>
{
    private readonly IIdentityService _identity;
    private readonly IApplicationDbContext _db;
    private readonly ITotpService _totp;

    public LoginHandler(IIdentityService identity, IApplicationDbContext db, ITotpService totp)
    {
        _identity = identity;
        _db = db;
        _totp = totp;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken ct)
    {
        var phoneVo = PhoneNumber.Create(request.Phone);
        if (phoneVo.IsFailure) return phoneVo.Error;

        // Phase 1: verify password
        var verify = await _identity.VerifyPasswordAsync(phoneVo.Value.Value, request.Password, ct);
        if (verify.IsFailure) return verify.Error;

        var userId = verify.Value;

        // Phase 2: check 2FA
        var twoFa = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => new { u.TotpEnabled, u.TotpSecret, u.TotpRecoveryCodesHashed })
            .FirstOrDefaultAsync(ct);

        if (twoFa is not null && twoFa.TotpEnabled && !string.IsNullOrEmpty(twoFa.TotpSecret))
        {
            // No code supplied → tell the client to prompt for it. Tokens stay empty.
            if (string.IsNullOrWhiteSpace(request.TotpCode))
            {
                return Result.Success(new AuthResponse(
                    AccessToken: "",
                    RefreshToken: "",
                    ExpiresAtUtc: DateTime.MinValue,
                    User: new UserSummary(userId, "", phoneVo.Value.Value, null, Array.Empty<string>()),
                    TwoFactorRequired: true));
            }

            // Validate the supplied code (TOTP first, recovery code as fallback)
            var code = request.TotpCode.Trim();
            var validTotp = _totp.VerifyCode(twoFa.TotpSecret, code);

            if (!validTotp)
            {
                var hash = _totp.HashRecoveryCode(code);
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
                if (user is null || user.ConsumeRecoveryCode(hash) is null)
                    return Error.Unauthorized("AUTH.TOTP_INVALID",
                        "رمز المصادقة الثنائية غير صحيح", "Invalid TOTP code");
                await _db.SaveChangesAsync(ct);
            }
        }

        // Phase 3: issue tokens
        var tokensResult = await _identity.IssueTokensForUserAsync(userId, ct);
        if (tokensResult.IsFailure) return tokensResult.Error;

        var t = tokensResult.Value;
        return Result.Success(new AuthResponse(
            t.AccessToken,
            t.RefreshToken,
            t.ExpiresAtUtc,
            new UserSummary(t.UserId, t.FullName, t.Phone, t.Email, t.Roles, t.AvatarUrl)
        ));
    }
}
