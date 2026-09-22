using FSEdu.Domain.Academic;
using FSEdu.Domain.Common;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Billing;

// Legacy - retained for backwards compatibility. New flow uses Subject/Stage pricing directly.
public sealed class Plan : AggregateRoot<int>
{
    public string Code { get; private set; } = default!;
    public string NameAr { get; private set; } = default!;
    public string? NameEn { get; private set; }
    public BillingInterval Interval { get; private set; }
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "EGP";
    public string FeaturesJson { get; private set; } = "[]";
    public bool Active { get; private set; } = true;

    private Plan() { }

    public Plan(string code, string nameAr, BillingInterval interval, decimal price, string currency = "EGP")
    {
        Code = code;
        NameAr = nameAr;
        Interval = interval;
        Price = price;
        Currency = currency;
    }

    public void Update(string nameAr, decimal price, string featuresJson)
    {
        NameAr = nameAr;
        Price = price;
        FeaturesJson = featuresJson;
    }

    public void Deactivate() => Active = false;
}

public sealed class Subscription : AggregateRoot<Guid>, IAuditable
{
    public Guid UserId { get; private set; }
    public SubscriptionType Type { get; private set; }

    // For SubjectMonthly / SubjectTerm
    public int? SubjectId { get; private set; }
    public Subject? Subject { get; private set; }

    // For StageFullTerm (also denormalized from Subject.StageId for SubjectTerm to speed access checks)
    public int? StageId { get; private set; }
    public Stage? Stage { get; private set; }

    // For term-based subscriptions
    public AcademicTerm? Term { get; private set; }

    public DateTime StartsAtUtc { get; private set; }
    public DateTime EndsAtUtc { get; private set; }
    public bool AutoRenew { get; private set; }
    public decimal AmountPaid { get; private set; }
    public string Currency { get; private set; } = "EGP";
    public SubscriptionStatus Status { get; private set; }
    public PaymentProvider PaymentProvider { get; private set; }
    public string? ProviderReference { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    private Subscription() { }

    // Factory: Subject Monthly — starts PENDING until admin approves payment
    public static Subscription CreateSubjectMonthly(
        Guid id, Guid userId, int subjectId, int stageId,
        DateTime startsAtUtc, decimal amount, PaymentProvider provider, string? providerRef = null)
    {
        return new Subscription
        {
            Id = id,
            UserId = userId,
            Type = SubscriptionType.SubjectMonthly,
            SubjectId = subjectId,
            StageId = stageId,
            Term = null,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = startsAtUtc.AddMonths(1),
            AutoRenew = true,
            AmountPaid = amount,
            Status = SubscriptionStatus.PendingPayment,
            PaymentProvider = provider,
            ProviderReference = providerRef
        };
    }

    public static Subscription CreateSubjectTerm(
        Guid id, Guid userId, int subjectId, int stageId, AcademicTerm term,
        DateTime startsAtUtc, DateTime endsAtUtc, decimal amount,
        PaymentProvider provider, string? providerRef = null)
    {
        return new Subscription
        {
            Id = id,
            UserId = userId,
            Type = SubscriptionType.SubjectTerm,
            SubjectId = subjectId,
            StageId = stageId,
            Term = term,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            AutoRenew = false,
            AmountPaid = amount,
            Status = SubscriptionStatus.PendingPayment,
            PaymentProvider = provider,
            ProviderReference = providerRef
        };
    }

    public static Subscription CreateStageFullTerm(
        Guid id, Guid userId, int stageId, AcademicTerm term,
        DateTime startsAtUtc, DateTime endsAtUtc, decimal amount,
        PaymentProvider provider, string? providerRef = null)
    {
        return new Subscription
        {
            Id = id,
            UserId = userId,
            Type = SubscriptionType.StageFullTerm,
            SubjectId = null,
            StageId = stageId,
            Term = term,
            StartsAtUtc = startsAtUtc,
            EndsAtUtc = endsAtUtc,
            AutoRenew = false,
            AmountPaid = amount,
            Status = SubscriptionStatus.PendingPayment,
            PaymentProvider = provider,
            ProviderReference = providerRef
        };
    }

    /// <summary>Activates the subscription (called after admin approves payment).</summary>
    public void Activate() => Status = SubscriptionStatus.Active;

    public void Cancel() => Status = SubscriptionStatus.Cancelled;
    public void MarkPastDue() => Status = SubscriptionStatus.PastDue;
    public void Renew(DateTime newEndDate) { EndsAtUtc = newEndDate; Status = SubscriptionStatus.Active; }
    public void Expire() => Status = SubscriptionStatus.Expired;

    public bool IsActiveNow(DateTime nowUtc) =>
        Status == SubscriptionStatus.Active && nowUtc >= StartsAtUtc && nowUtc <= EndsAtUtc;

    public bool GrantsAccessToSubject(int subjectId, int subjectStageId) =>
        Type switch
        {
            SubscriptionType.SubjectMonthly or SubscriptionType.SubjectTerm => SubjectId == subjectId,
            SubscriptionType.StageFullTerm => StageId == subjectStageId,
            _ => false
        };
}

public sealed class Payment : AggregateRoot<Guid>
{
    public Guid SubscriptionId { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public PaymentStatus Status { get; private set; }
    public PaymentMethod? Method { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? ReceiptImageUrl { get; private set; }
    public string? SenderName { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public Guid? ReviewedBy { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? ReviewNote { get; private set; }
    public string? InvoiceUrl { get; private set; }

    private Payment() { }

    public Payment(Guid id, Guid subscriptionId, Guid userId, decimal amount, string currency) : base(id)
    {
        SubscriptionId = subscriptionId;
        UserId = userId;
        Amount = amount;
        Currency = currency;
        Status = PaymentStatus.Pending;
    }

    public void SubmitReceipt(PaymentMethod method, string? referenceNumber,
                              string? receiptImageUrl, string? senderName, string? notes)
    {
        Method = method;
        ReferenceNumber = string.IsNullOrWhiteSpace(referenceNumber) ? null : referenceNumber;
        ReceiptImageUrl = receiptImageUrl;
        SenderName = senderName;
        Notes = notes;
        Status = PaymentStatus.AwaitingReview;
        SubmittedAtUtc = DateTime.UtcNow;
    }

    public void ApproveByAdmin(Guid adminId, string? note = null)
    {
        Status = PaymentStatus.Succeeded;
        ReviewedBy = adminId;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewNote = note;
        PaidAtUtc = DateTime.UtcNow;
    }

    public void RejectByAdmin(Guid adminId, string reason)
    {
        Status = PaymentStatus.Failed;
        ReviewedBy = adminId;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewNote = reason;
    }

    public void Refund() => Status = PaymentStatus.Refunded;
}

public sealed class Coupon : AggregateRoot<int>
{
    public string Code { get; private set; } = default!;
    public decimal? DiscountPct { get; private set; }
    public decimal? DiscountFixed { get; private set; }
    public DateTime? ValidUntilUtc { get; private set; }
    public int? MaxUses { get; private set; }
    public int UsedCount { get; private set; }
    public bool Active { get; private set; } = true;

    private Coupon() { }

    public Coupon(string code, decimal? discountPct, decimal? discountFixed, DateTime? validUntil, int? maxUses)
    {
        Code = code;
        DiscountPct = discountPct;
        DiscountFixed = discountFixed;
        ValidUntilUtc = validUntil;
        MaxUses = maxUses;
    }

    public bool IsValid() =>
        Active &&
        (ValidUntilUtc is null || ValidUntilUtc > DateTime.UtcNow) &&
        (MaxUses is null || UsedCount < MaxUses);

    public void IncrementUsage() => UsedCount++;
    public void Deactivate() => Active = false;
}
