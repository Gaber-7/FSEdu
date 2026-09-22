using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Billing;

// Site-wide auto-applied discount. Unlike Coupons, no code is required —
// the discount is found and applied automatically during checkout when
// the campaign's scope matches what the student is subscribing to.
public sealed class SalesCampaign : AggregateRoot<int>
{
    public string Title { get; private set; } = default!;
    public decimal DiscountPct { get; private set; }      // 1..100

    // Scope: "All" applies to every product, "Stage" applies to a specific
    // stage, "Subject" applies to a specific subject.
    public CampaignScope Scope { get; private set; }
    public int? ScopeId { get; private set; }              // null when Scope == All

    public DateTime ValidFromUtc { get; private set; }
    public DateTime ValidUntilUtc { get; private set; }
    public bool Active { get; private set; } = true;
    public DateTime CreatedAtUtc { get; private set; }

    private SalesCampaign() { }

    public SalesCampaign(string title, decimal discountPct, CampaignScope scope, int? scopeId,
                         DateTime validFromUtc, DateTime validUntilUtc)
    {
        Title = title.Trim();
        DiscountPct = Math.Clamp(discountPct, 0.01m, 100m);
        Scope = scope;
        ScopeId = scope == CampaignScope.All ? null : scopeId;
        ValidFromUtc = validFromUtc;
        ValidUntilUtc = validUntilUtc;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public bool IsCurrentlyValid() =>
        Active && DateTime.UtcNow >= ValidFromUtc && DateTime.UtcNow <= ValidUntilUtc;

    // True if this campaign applies to the given (stageId, subjectId?) tuple.
    public bool AppliesTo(int stageId, int? subjectId) => Scope switch
    {
        CampaignScope.All => true,
        CampaignScope.Stage => ScopeId == stageId,
        CampaignScope.Subject => subjectId is int sid && ScopeId == sid,
        _ => false
    };

    public void Deactivate() => Active = false;
    public void Reactivate() => Active = true;
}

public enum CampaignScope { All = 1, Stage = 2, Subject = 3 }
