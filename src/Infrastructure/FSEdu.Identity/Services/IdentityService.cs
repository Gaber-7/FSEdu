using FSEdu.Application.Abstractions;
using FSEdu.Identity.Entities;
using FSEdu.Identity.Otp;
using FSEdu.Identity.Persistence;
using FSEdu.Identity.Tokens;
using FSEdu.Shared.Kernel.Results;
using FSEdu.Shared.Kernel.Time;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FSEdu.Identity.Services;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IdentityDbContext _idDb;
    private readonly IApplicationDbContext _appDb;
    private readonly IOtpService _otp;
    private readonly IDateTimeProvider _clock;
    private readonly JwtOptions _jwtOpts;

    public IdentityService(
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        SignInManager<ApplicationUser> signIn,
        IJwtTokenGenerator jwt,
        IdentityDbContext idDb,
        IApplicationDbContext appDb,
        IOtpService otp,
        IDateTimeProvider clock,
        IOptions<JwtOptions> jwtOpts)
    {
        _users = users;
        _roles = roles;
        _signIn = signIn;
        _jwt = jwt;
        _idDb = idDb;
        _appDb = appDb;
        _otp = otp;
        _clock = clock;
        _jwtOpts = jwtOpts.Value;
    }

    private async Task<string?> LookupAvatarUrlAsync(Guid userId, CancellationToken ct)
    {
        try
        {
            return await _appDb.Users
                .Where(u => u.Id == userId)
                .Select(u => u.AvatarUrl)
                .FirstOrDefaultAsync(ct);
        }
        catch { return null; }
    }

    public async Task<Result<Guid>> CreateUserAsync(string phone, string? email, string fullName,
                                                    string password, string role, CancellationToken ct = default)
    {
        var existing = await _users.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, ct);
        if (existing is not null)
            return Error.Conflict("USER.PHONE_EXISTS", "رقم الهاتف مُسجّل مسبقًا", "Phone already registered");

        if (!string.IsNullOrEmpty(email))
        {
            var existingEmail = await _users.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (existingEmail is not null)
                return Error.Conflict("USER.EMAIL_EXISTS", "البريد الإلكتروني مُسجّل مسبقًا", "Email already registered");
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = phone,
            PhoneNumber = phone,
            Email = email,
            FullName = fullName,
        };

        var createResult = await _users.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var first = createResult.Errors.FirstOrDefault();
            return Error.Validation(first?.Code ?? "USER.CREATE_FAILED",
                first?.Description ?? "فشل إنشاء المستخدم",
                first?.Description ?? "User creation failed");
        }

        if (!await _roles.RoleExistsAsync(role))
            await _roles.CreateAsync(new ApplicationRole(role));

        await _users.AddToRoleAsync(user, role);

        return Result.Success(user.Id);
    }

    public async Task<Result<AuthTokens>> SignInAsync(string phone, string password, CancellationToken ct = default)
    {
        var verify = await VerifyPasswordAsync(phone, password, ct);
        if (verify.IsFailure) return verify.Error;
        return await IssueTokensForUserAsync(verify.Value, ct);
    }

    public async Task<Result<Guid>> VerifyPasswordAsync(string phone, string password, CancellationToken ct = default)
    {
        var user = await _users.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, ct);
        if (user is null)
            return Error.Unauthorized("AUTH.INVALID", "بيانات الدخول غير صحيحة", "Invalid credentials");

        var ok = await _users.CheckPasswordAsync(user, password);
        if (!ok)
            return Error.Unauthorized("AUTH.INVALID", "بيانات الدخول غير صحيحة", "Invalid credentials");

        return Result.Success(user.Id);
    }

    public async Task<Result<AuthTokens>> IssueTokensForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null)
            return Error.Unauthorized("AUTH.INVALID", "بيانات الدخول غير صحيحة", "Invalid credentials");

        var roles = await _users.GetRolesAsync(user);
        var tokens = await _jwt.GenerateAsync(user, roles);
        var avatar = await LookupAvatarUrlAsync(user.Id, ct);
        tokens = tokens with { AvatarUrl = avatar };

        _idDb.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = tokens.RefreshToken,
            ExpiresAtUtc = _clock.UtcNow.AddDays(_jwtOpts.RefreshTokenDays)
        });

        user.LastLoginAtUtc = _clock.UtcNow;
        await _idDb.SaveChangesAsync(ct);

        return Result.Success(tokens);
    }

    public async Task<Result<AuthTokens>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var rt = await _idDb.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken, ct);
        if (rt is null || !rt.IsActive)
            return Error.Unauthorized("TOKEN.INVALID", "رمز التحديث غير صالح", "Invalid refresh token");

        var user = await _users.FindByIdAsync(rt.UserId.ToString());
        if (user is null)
            return Error.Unauthorized("TOKEN.USER_NOT_FOUND", "المستخدم غير موجود", "User not found");

        var roles = await _users.GetRolesAsync(user);
        var tokens = await _jwt.GenerateAsync(user, roles);
        var avatar = await LookupAvatarUrlAsync(user.Id, ct);
        tokens = tokens with { AvatarUrl = avatar };

        rt.RevokedAtUtc = _clock.UtcNow;
        rt.ReplacedByToken = tokens.RefreshToken;

        _idDb.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = tokens.RefreshToken,
            ExpiresAtUtc = _clock.UtcNow.AddDays(_jwtOpts.RefreshTokenDays)
        });

        await _idDb.SaveChangesAsync(ct);
        return Result.Success(tokens);
    }

    public async Task<Result> SendOtpAsync(string phone, CancellationToken ct = default)
    {
        var result = await _otp.GenerateAsync(phone, ct);
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
    }

    public Task<Result> VerifyOtpAsync(string phone, string code, CancellationToken ct = default)
        => _otp.VerifyAsync(phone, code, ct);

    public async Task<Result> AssignRoleAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString());
        if (user is null)
            return Error.NotFound("USER.NOT_FOUND", "المستخدم غير موجود", "User not found");

        if (!await _roles.RoleExistsAsync(role))
            await _roles.CreateAsync(new ApplicationRole(role));

        await _users.AddToRoleAsync(user, role);
        return Result.Success();
    }
}
