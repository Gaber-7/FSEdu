using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Billing;

// Auto-applied discount rule. Unlike Coupons (manual code) and SalesCampaigns
// (date-bound site-wide), DiscountRules are evaluated per-subscription based on
// the student's profile and history. Examples:
//   - SiblingsCount: parent has 2+ children subscribed → 10% off
//   - SubjectsCount: subscribing to 3+ subjects this term → 15% off
//   - LongDuration: Annual subscription → 20% off
//   - HighAchiever: XP >= 1000 from last year → 15% off
//   - ReturningStudent: had an active sub in the last 12 months → 10% off
public sealed class DiscountRule : AggregateRoot<int>
{
    public string Code { get; private set; } = default!;           // e.g., "siblings"
    public string TitleAr { get; private set; } = default!;        // e.g., "خصم الإخوة"
    public string? DescriptionAr { get; private set; }
    public DiscountRuleType Type { get; private set; }
    public decimal DiscountPct { get; private set; }               // 1..100
    public int? ThresholdInt { get; private set; }                 // generic threshold
    public bool Active { get; private set; } = true;
    public DateTime? ValidFromUtc { get; private set; }            // optional date window
    public DateTime? ValidUntilUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private DiscountRule() { }

    public DiscountRule(string code, string titleAr, string? descriptionAr,
                         DiscountRuleType type, decimal discountPct, int? thresholdInt,
                         DateTime? validFromUtc, DateTime? validUntilUtc)
    {
        Code = code.Trim();
        TitleAr = titleAr.Trim();
        DescriptionAr = descriptionAr?.Trim();
        Type = type;
        DiscountPct = Math.Clamp(discountPct, 0.01m, 100m);
        ThresholdInt = thresholdInt;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public bool IsCurrentlyValid()
    {
        if (!Active) return false;
        var now = DateTime.UtcNow;
        if (ValidFromUtc.HasValue && now < ValidFromUtc.Value) return false;
        if (ValidUntilUtc.HasValue && now > ValidUntilUtc.Value) return false;
        return true;
    }

    public void Update(string titleAr, string? descriptionAr, decimal discountPct,
                       int? thresholdInt, DateTime? validFromUtc, DateTime? validUntilUtc)
    {
        TitleAr = titleAr.Trim();
        DescriptionAr = descriptionAr?.Trim();
        DiscountPct = Math.Clamp(discountPct, 0.01m, 100m);
        ThresholdInt = thresholdInt;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
    }

    public void Deactivate() => Active = false;
    public void Reactivate() => Active = true;
}

public enum DiscountRuleType
{
    SiblingsCount = 1,        // requires parent link + N siblings with active subscriptions
    SubjectsCount = 2,        // requires N+ subject subscriptions (incl. the new one)
    LongDuration = 3,         // applies on Annual / Term subscriptions
    HighAchiever = 4,         // student's XP >= threshold
    ReturningStudent = 5,     // student had an active subscription in last 12 months
    ReferralReferee = 6,      // student was invited by another student (first paid sub only)
}
