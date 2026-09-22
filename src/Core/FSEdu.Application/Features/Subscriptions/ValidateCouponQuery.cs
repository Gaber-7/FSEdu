using FSEdu.Application.Abstractions;
using FSEdu.Domain.Billing;
using FSEdu.Shared.Contracts.Payments;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Subscriptions;

public sealed record ValidateCouponQuery(string Code, decimal Amount)
    : IQuery<ValidateCouponResponse>;

public sealed class ValidateCouponHandler : IQueryHandler<ValidateCouponQuery, ValidateCouponResponse>
{
    private readonly IApplicationDbContext _db;

    public ValidateCouponHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<ValidateCouponResponse>> Handle(ValidateCouponQuery request, CancellationToken ct)
    {
        var code = (request.Code ?? "").Trim();
        if (string.IsNullOrEmpty(code))
            return Result.Success(new ValidateCouponResponse(false, null, request.Amount, 0m, request.Amount, "أدخل كود الكوبون"));

        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code, ct);
        if (coupon is null)
            return Result.Success(new ValidateCouponResponse(false, code, request.Amount, 0m, request.Amount, "الكوبون غير موجود"));

        if (!coupon.IsValid())
            return Result.Success(new ValidateCouponResponse(false, code, request.Amount, 0m, request.Amount, "الكوبون منتهي الصلاحية أو غير مفعّل"));

        if (request.Amount <= 0)
            return Result.Success(new ValidateCouponResponse(false, code, request.Amount, 0m, request.Amount, "المبلغ غير صالح"));

        var (discount, final) = CouponPricing.Apply(coupon, request.Amount);
        return Result.Success(new ValidateCouponResponse(true, code, request.Amount, discount, final, "تم تطبيق الخصم"));
    }
}

// Shared math helper used by both validation and the actual subscription handler.
public static class CouponPricing
{
    public static (decimal discount, decimal final) Apply(Coupon coupon, decimal original)
    {
        decimal discount = 0m;
        if (coupon.DiscountPct.HasValue)
            discount += original * (coupon.DiscountPct.Value / 100m);
        if (coupon.DiscountFixed.HasValue)
            discount += coupon.DiscountFixed.Value;

        if (discount < 0) discount = 0;
        if (discount > original) discount = original;

        var final = Math.Max(0m, original - discount);
        return (Math.Round(discount, 2), Math.Round(final, 2));
    }
}
