namespace FSEdu.Shared.Contracts.Admin;

public sealed record PendingTeacherDto(
    Guid Id,
    string FullName,
    string Phone,
    string? Email,
    string? Bio,
    int YearsOfExperience,
    string[] SubjectNames,
    string[] RegionNames,
    QualificationSummary[] Qualifications,
    DateTime CreatedAtUtc
);

public sealed record QualificationSummary(string Title, string Institution, int Year, string? DocumentUrl);

public sealed record ApproveTeacherRequest(string? Notes);

public sealed record RejectTeacherRequest(string Reason);

// ─── Admin Overview Dashboard ─────────────────────
public sealed record AdminOverviewDto(
    AdminUserStats Users,
    AdminSubscriptionStats Subscriptions,
    AdminLiveStats LiveSessions,
    AdminContentStats Content,
    AdminTicketStats Tickets,
    AdminPaymentStats Payments,
    List<AdminRecentActivityItem> RecentActivity
);

public sealed record AdminUserStats(
    int TotalStudents,
    int TotalParents,
    int TotalTeachersVerified,
    int TotalTeachersPending,
    int NewSignupsThisWeek
);

public sealed record AdminSubscriptionStats(
    int ActiveCount,
    int ExpiringIn7Days,
    int ExpiredCount,
    int PendingPaymentCount,
    decimal TotalRevenueEgp,
    decimal RevenueThisMonthEgp
);

public sealed record AdminLiveStats(
    int ScheduledUpcoming,
    int LiveNow,
    int EndedThisWeek,
    int RecordingsAvailable
);

public sealed record AdminContentStats(
    int PublishedCourses,
    int PendingCoursesForReview,
    int TotalLessons,
    int TotalAssessments
);

public sealed record AdminTicketStats(
    int OpenCount,
    int ResolvedThisWeek,
    int OldestOpenDays
);

public sealed record AdminPaymentStats(
    int AwaitingReview,
    decimal AwaitingAmountEgp,
    int ApprovedThisWeek,
    int RejectedThisWeek
);

public sealed record AdminRecentActivityItem(
    string Type,        // "subscription" | "payment" | "ticket" | "live" | "user"
    string Title,
    string Subtitle,
    DateTime AtUtc,
    string? Url
);

public sealed record BroadcastNotificationRequest(
    string Target,
    string Title,
    string Body,
    string? Url
);

public sealed record BroadcastNotificationResponse(int Recipients);

// ─── Coupons ────────────────────────────────────────
public sealed record CouponDto(
    int Id,
    string Code,
    decimal? DiscountPct,
    decimal? DiscountFixed,
    DateTime? ValidUntilUtc,
    int? MaxUses,
    int UsedCount,
    bool Active
);

public sealed record CreateCouponRequest(
    string Code,
    decimal? DiscountPct,
    decimal? DiscountFixed,
    DateTime? ValidUntilUtc,
    int? MaxUses
);

// ─── Sales Campaigns ─────────────────────────────────
public sealed record SalesCampaignDto(
    int Id,
    string Title,
    decimal DiscountPct,
    string Scope,            // "All" | "Stage" | "Subject"
    int? ScopeId,
    string? ScopeName,       // resolved label (e.g., "الصف الأول الإعدادي" or "اللغة العربية")
    DateTime ValidFromUtc,
    DateTime ValidUntilUtc,
    bool Active,
    bool IsLive             // computed: Active && in date range
);

public sealed record CreateCampaignRequest(
    string Title,
    decimal DiscountPct,
    string Scope,            // "All" | "Stage" | "Subject"
    int? ScopeId,
    DateTime ValidFromUtc,
    DateTime ValidUntilUtc
);

// ─── Audit Log ──────────────────────────────────────
public sealed record AuditEntryDto(
    long Id,
    Guid ActorUserId,
    string ActorName,
    string ActorRole,
    string Action,
    string? EntityType,
    string? EntityId,
    string? DetailsJson,
    string? IpAddress,
    DateTime AtUtc
);

public sealed record AuditLogResponse(
    int TotalCount,
    List<AuditEntryDto> Items
);

// ─── Discount Rules ─────────────────────────────────
public sealed record DiscountRuleDto(
    int Id,
    string Code,
    string TitleAr,
    string? DescriptionAr,
    string Type,            // "SiblingsCount" | "SubjectsCount" | "LongDuration" | "HighAchiever" | "ReturningStudent"
    decimal DiscountPct,
    int? ThresholdInt,
    bool Active,
    DateTime? ValidFromUtc,
    DateTime? ValidUntilUtc
);

public sealed record CreateDiscountRuleRequest(
    string Code,
    string TitleAr,
    string? DescriptionAr,
    string Type,
    decimal DiscountPct,
    int? ThresholdInt,
    DateTime? ValidFromUtc,
    DateTime? ValidUntilUtc
);
