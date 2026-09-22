namespace FSEdu.Shared.Contracts.Subscriptions;

public sealed record PricingResponse(
    StagePricingDto[] Stages,
    ActiveCampaignBannerDto[] Campaigns
);

public sealed record StagePricingDto(
    int StageId,
    string StageName,
    decimal? FullTermPriceEgp,
    decimal StageDiscountPct,    // best campaign that applies to this stage (or All)
    SubjectPricingDto[] Subjects
);

public sealed record SubjectPricingDto(
    int SubjectId,
    string SubjectName,
    string? ColorHex,
    decimal MonthlyPriceEgp,
    decimal TermPriceEgp,
    decimal SubjectDiscountPct   // best campaign that applies to this subject (or stage or All)
);

// A site-wide banner shown above the pricing grid when a campaign is live.
public sealed record ActiveCampaignBannerDto(
    int Id,
    string Title,
    decimal DiscountPct,
    string Scope,                // "All" | "Stage" | "Subject"
    string? ScopeName,
    DateTime ValidUntilUtc
);

public sealed record SubscribeRequest(
    string Type,        // "SubjectMonthly" | "SubjectTerm" | "StageFullTerm"
    int? SubjectId,
    int? StageId,
    string? Term        // "FirstTerm" | "SecondTerm" | "Annual"
);

public sealed record SubscribeResponse(
    Guid SubscriptionId,
    string Type,
    decimal AmountPaid,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    string Status
);

public sealed record MySubscriptionDto(
    Guid Id,
    string Type,
    string TypeLabel,
    int? SubjectId,
    string? SubjectName,
    int? StageId,
    string? StageName,
    string? Term,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    decimal AmountPaid,
    string Status,
    bool IsActive
);

public sealed record AccessCheckResponse(bool HasAccess, string? Reason);
