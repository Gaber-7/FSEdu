using FSEdu.Application.Abstractions;
using FSEdu.Domain.Billing;
using FSEdu.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Subscriptions;

// Evaluates which DiscountRules apply to a given (student, subscription) tuple.
// Returns the list of applicable rules with their pct values — the caller
// decides how to apply them (sum, stack, choose-best). Default policy:
// take the SINGLE highest discount across all matched rules — this keeps
// total predictable and prevents abuse via rule-stacking.
public sealed record ApplicableDiscount(int RuleId, string Code, string TitleAr, decimal Pct);

public sealed class DiscountEvaluator
{
    private readonly IApplicationDbContext _db;

    public DiscountEvaluator(IApplicationDbContext db) => _db = db;

    public async Task<List<ApplicableDiscount>> EvaluateAsync(
        Guid studentId, SubscriptionType type, int? subjectId, int? stageId,
        CancellationToken ct = default)
    {
        var rules = await _db.DiscountRules
            .Where(r => r.Active)
            .ToListAsync(ct);
        rules = rules.Where(r => r.IsCurrentlyValid()).ToList();
        if (rules.Count == 0) return new List<ApplicableDiscount>();

        var matched = new List<ApplicableDiscount>();
        foreach (var rule in rules)
        {
            var ok = rule.Type switch
            {
                DiscountRuleType.SiblingsCount => await SiblingsCountAsync(studentId, rule.ThresholdInt ?? 2, ct),
                DiscountRuleType.SubjectsCount => await SubjectsCountAsync(studentId, rule.ThresholdInt ?? 2, ct),
                DiscountRuleType.LongDuration => LongDuration(type),
                DiscountRuleType.HighAchiever => await HighAchieverAsync(studentId, rule.ThresholdInt ?? 1000, ct),
                DiscountRuleType.ReturningStudent => await ReturningAsync(studentId, ct),
                DiscountRuleType.ReferralReferee => await ReferralRefereeAsync(studentId, ct),
                _ => false
            };
            if (ok)
                matched.Add(new ApplicableDiscount(rule.Id, rule.Code, rule.TitleAr, rule.DiscountPct));
        }
        return matched;
    }

    // ─── Rule predicates ─────────────────────────────────────

    // Sibling = another student linked to one of this student's parents
    // AND that sibling currently has an active subscription. ThresholdInt
    // is the *minimum* sibling count required to grant the discount (default 2,
    // i.e., "you + at least 1 sibling already subscribed").
    private async Task<bool> SiblingsCountAsync(Guid studentId, int threshold, CancellationToken ct)
    {
        var parentIds = await _db.ParentStudentLinks
            .Where(l => l.StudentId == studentId)
            .Select(l => l.ParentId)
            .ToListAsync(ct);
        if (parentIds.Count == 0) return false;

        var now = DateTime.UtcNow;
        var activeSiblings = await (
            from l in _db.ParentStudentLinks
            where parentIds.Contains(l.ParentId) && l.StudentId != studentId
            join s in _db.Subscriptions on l.StudentId equals s.UserId
            where s.Status == SubscriptionStatus.Active
                  && s.StartsAtUtc <= now && s.EndsAtUtc >= now
            select l.StudentId
        ).Distinct().CountAsync(ct);

        // Include the current student as "1" → so threshold of 2 means need 1+ sibling
        return (1 + activeSiblings) >= threshold;
    }

    private async Task<bool> SubjectsCountAsync(Guid studentId, int threshold, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var count = await _db.Subscriptions
            .Where(s => s.UserId == studentId
                     && s.Status == SubscriptionStatus.Active
                     && s.StartsAtUtc <= now && s.EndsAtUtc >= now
                     && s.SubjectId != null)
            .Select(s => s.SubjectId!.Value)
            .Distinct()
            .CountAsync(ct);
        // +1 for the subscription about to be created (caller's intent)
        return (count + 1) >= threshold;
    }

    private static bool LongDuration(SubscriptionType type) =>
        type is SubscriptionType.SubjectTerm or SubscriptionType.StageFullTerm;

    private async Task<bool> HighAchieverAsync(Guid studentId, int xpThreshold, CancellationToken ct)
    {
        var xp = await _db.Students.Where(s => s.Id == studentId)
            .Select(s => (int?)s.XpPoints).FirstOrDefaultAsync(ct);
        return (xp ?? 0) >= xpThreshold;
    }

    private async Task<bool> ReturningAsync(Guid studentId, CancellationToken ct)
    {
        var oneYearAgo = DateTime.UtcNow.AddYears(-1);
        return await _db.Subscriptions
            .AnyAsync(s => s.UserId == studentId
                       && (s.Status == SubscriptionStatus.Active
                          || s.Status == SubscriptionStatus.Expired)
                       && s.EndsAtUtc >= oneYearAgo, ct);
    }

    // Student was referred by another student AND this would be their first paid subscription
    private async Task<bool> ReferralRefereeAsync(Guid studentId, CancellationToken ct)
    {
        var student = await _db.Students
            .Where(s => s.Id == studentId)
            .Select(s => new { s.ReferredByCode })
            .FirstOrDefaultAsync(ct);
        if (student is null || string.IsNullOrEmpty(student.ReferredByCode)) return false;

        // First paid subscription only — anyone with a prior succeeded payment doesn't qualify
        var hasPriorPaidSub = await _db.Payments
            .AnyAsync(p => p.UserId == studentId && p.Status == PaymentStatus.Succeeded, ct);
        return !hasPriorPaidSub;
    }
}
