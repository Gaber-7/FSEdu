using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Billing;
using FSEdu.Shared.Contracts.Admin;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Admin;

// ─── List coupons ─────────────────────────────────────
public sealed record GetCouponsListQuery() : IQuery<List<CouponDto>>;

public sealed class GetCouponsListHandler : IQueryHandler<GetCouponsListQuery, List<CouponDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCouponsListHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<CouponDto>>> Handle(GetCouponsListQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Error.Forbidden("COUPONS.FORBIDDEN", "للأدمن فقط", "Admin only");

        var list = await _db.Coupons
            .OrderByDescending(c => c.Active)
            .ThenBy(c => c.Code)
            .Select(c => new CouponDto(
                c.Id, c.Code,
                c.DiscountPct, c.DiscountFixed,
                c.ValidUntilUtc, c.MaxUses, c.UsedCount, c.Active))
            .ToListAsync(ct);

        return Result.Success(list);
    }
}

// ─── Create coupon ────────────────────────────────────
public sealed record CreateCouponCommand(
    string Code,
    decimal? DiscountPct,
    decimal? DiscountFixed,
    DateTime? ValidUntilUtc,
    int? MaxUses
) : ICommand<int>;

public sealed class CreateCouponValidator : AbstractValidator<CreateCouponCommand>
{
    public CreateCouponValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MinimumLength(3).MaximumLength(50)
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("الكود حروف وأرقام إنجليزية وشُرَط فقط");

        RuleFor(x => x).Must(x => x.DiscountPct.HasValue || x.DiscountFixed.HasValue)
            .WithMessage("اختر إما خصم نسبة أو خصم ثابت");

        When(x => x.DiscountPct.HasValue, () =>
            RuleFor(x => x.DiscountPct!.Value).GreaterThan(0).LessThanOrEqualTo(100));

        When(x => x.DiscountFixed.HasValue, () =>
            RuleFor(x => x.DiscountFixed!.Value).GreaterThan(0));

        When(x => x.MaxUses.HasValue, () =>
            RuleFor(x => x.MaxUses!.Value).GreaterThan(0));
    }
}

public sealed class CreateCouponHandler : ICommandHandler<CreateCouponCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;

    public CreateCouponHandler(IApplicationDbContext db, ICurrentUser currentUser, IAuditLog audit)
    {
        _db = db; _currentUser = currentUser; _audit = audit;
    }

    public async Task<Result<int>> Handle(CreateCouponCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Error.Forbidden("COUPONS.FORBIDDEN", "للأدمن فقط", "Admin only");

        var code = request.Code.Trim().ToUpperInvariant();
        var exists = await _db.Coupons.AnyAsync(c => c.Code == code, ct);
        if (exists)
            return Error.Conflict("COUPON.DUPLICATE", "كود الكوبون موجود مسبقًا", "Code exists");

        var coupon = new Coupon(code, request.DiscountPct, request.DiscountFixed,
            request.ValidUntilUtc, request.MaxUses);
        _db.Coupons.Add(coupon);
        await _db.SaveChangesAsync(ct);

        await _audit.RecordAsync("coupon.create", "Coupon", coupon.Id.ToString(),
            new { code, request.DiscountPct, request.DiscountFixed, request.MaxUses }, ct);

        return Result.Success(coupon.Id);
    }
}

// ─── Deactivate coupon ────────────────────────────────
public sealed record DeactivateCouponCommand(int CouponId) : ICommand;

public sealed class DeactivateCouponHandler : ICommandHandler<DeactivateCouponCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeactivateCouponHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeactivateCouponCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Error.Forbidden("COUPONS.FORBIDDEN", "للأدمن فقط", "Admin only");

        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Id == request.CouponId, ct);
        if (coupon is null)
            return Error.NotFound("COUPON.NOT_FOUND", "الكوبون غير موجود", "Not found");

        coupon.Deactivate();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
