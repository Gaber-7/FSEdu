using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Users;
using FSEdu.Shared.Contracts.Students;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Students;

// ─── Get my referral info ────────────────────────
public sealed record GetMyReferralInfoQuery() : IQuery<MyReferralInfoDto>;

public sealed class GetMyReferralInfoHandler : IQueryHandler<GetMyReferralInfoQuery, MyReferralInfoDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyReferralInfoHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result<MyReferralInfoDto>> Handle(GetMyReferralInfoQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null)
            return Error.Forbidden("STUDENT.ONLY", "للطلاب فقط", "Students only");

        // Ensure the student has a referral code (lazily generated on first view)
        if (string.IsNullOrEmpty(student.ReferralCode))
        {
            student.EnsureReferralCode();
            await _db.SaveChangesAsync(ct);
        }

        var referredRows = await (
            from r in _db.Referrals
            where r.ReferrerStudentId == studentId
            join referred in _db.Students on r.ReferredStudentId equals referred.Id
            orderby r.CreatedAtUtc descending
            select new MyReferredRowDto(
                MaskName(referred.FullName),
                r.CreatedAtUtc,
                r.RewardedAtUtc != null,
                r.RewardEgp
            )).ToListAsync(ct);

        var totalRewards = referredRows.Sum(r => r.RewardEgp);

        return Result.Success(new MyReferralInfoDto(
            student.ReferralCode!,
            student.ReferredByCode,
            student.ReferralCredits,
            referredRows.Count,
            referredRows.Count(r => r.Rewarded),
            totalRewards,
            referredRows));
    }

    private static string MaskName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "—";
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "—";
        return parts[0] + (parts.Length > 1 ? " ***" : "");
    }
}

// ─── Redeem referral code ────────────────────────
public sealed record RedeemReferralCodeCommand(string Code) : ICommand;

public sealed class RedeemReferralCodeValidator : AbstractValidator<RedeemReferralCodeCommand>
{
    public RedeemReferralCodeValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(4, 16);
    }
}

public sealed class RedeemReferralCodeHandler : ICommandHandler<RedeemReferralCodeCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public RedeemReferralCodeHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result> Handle(RedeemReferralCodeCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null)
            return Error.Forbidden("STUDENT.ONLY", "للطلاب فقط", "Students only");

        if (!string.IsNullOrEmpty(student.ReferredByCode))
            return Error.Validation("REFERRAL.ALREADY_REDEEMED",
                "لقد قمت باستخدام كود إحالة من قبل ولا يمكن تغييره", "Already redeemed");

        var code = request.Code.Trim().ToUpperInvariant();

        // Cannot redeem own code
        if (code == student.ReferralCode)
            return Error.Validation("REFERRAL.SELF",
                "لا يمكنك استخدام كود الإحالة الخاص بك", "Cannot use your own code");

        var referrer = await _db.Students.FirstOrDefaultAsync(s => s.ReferralCode == code, ct);
        if (referrer is null)
            return Error.NotFound("REFERRAL.INVALID_CODE",
                "كود الإحالة غير صحيح", "Invalid referral code");

        student.SetReferredBy(code);

        var referral = new Referral(Guid.NewGuid(), referrer.Id, student.Id);
        _db.Referrals.Add(referral);

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
