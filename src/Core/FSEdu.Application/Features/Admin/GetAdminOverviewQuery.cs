using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Domain.Courses;
using FSEdu.Domain.Support;
using FSEdu.Shared.Contracts.Admin;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Admin;

public sealed record GetAdminOverviewQuery() : IQuery<AdminOverviewDto>;

public sealed class GetAdminOverviewHandler : IQueryHandler<GetAdminOverviewQuery, AdminOverviewDto>
{
    private readonly IApplicationDbContext _db;

    public GetAdminOverviewHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<AdminOverviewDto>> Handle(GetAdminOverviewQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var in7Days = now.AddDays(7);

        // Users
        var totalStudents = await _db.Students.CountAsync(ct);
        var totalParents = await _db.Parents.CountAsync(ct);
        var totalTeachersVerified = await _db.Teachers.CountAsync(t => t.Verified, ct);
        var totalTeachersPending = await _db.Teachers.CountAsync(t => !t.Verified, ct);
        var newSignupsThisWeek =
            await _db.Students.CountAsync(s => s.CreatedAtUtc >= weekAgo, ct)
          + await _db.Parents.CountAsync(p => p.CreatedAtUtc >= weekAgo, ct)
          + await _db.Teachers.CountAsync(t => t.CreatedAtUtc >= weekAgo, ct);
        var users = new AdminUserStats(totalStudents, totalParents,
            totalTeachersVerified, totalTeachersPending, newSignupsThisWeek);

        // Subscriptions
        var activeSubs = await _db.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Active && s.EndsAtUtc >= now, ct);
        var expiringSoon = await _db.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Active
                          && s.EndsAtUtc >= now && s.EndsAtUtc <= in7Days, ct);
        var expiredSubs = await _db.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Expired, ct);
        var pendingPaySubs = await _db.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.PendingPayment, ct);
        var totalRevenue = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        var revenueThisMonth = await _db.Payments
            .Where(p => p.Status == PaymentStatus.Succeeded && p.PaidAtUtc != null && p.PaidAtUtc >= monthStart)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        var subs = new AdminSubscriptionStats(activeSubs, expiringSoon, expiredSubs,
            pendingPaySubs, totalRevenue, revenueThisMonth);

        // Live sessions
        var scheduledUpcoming = await _db.LiveSessions
            .CountAsync(s => s.Status == LiveSessionStatus.Scheduled && s.ScheduledAtUtc >= now, ct);
        var liveNow = await _db.LiveSessions
            .CountAsync(s => s.Status == LiveSessionStatus.Live, ct);
        var endedThisWeek = await _db.LiveSessions
            .CountAsync(s => s.Status == LiveSessionStatus.Ended && s.EndedAtUtc != null && s.EndedAtUtc >= weekAgo, ct);
        var recordings = await _db.LiveSessions
            .CountAsync(s => s.RecordingUrl != null, ct);
        var live = new AdminLiveStats(scheduledUpcoming, liveNow, endedThisWeek, recordings);

        // Content
        var publishedCourses = await _db.Courses.CountAsync(c => c.Status == CourseStatus.Published, ct);
        var pendingCourses = await _db.Courses.CountAsync(c => c.Status == CourseStatus.PendingReview, ct);
        var totalLessons = await _db.Lessons.CountAsync(ct);
        var totalAssessments = await _db.Assessments.CountAsync(ct);
        var content = new AdminContentStats(publishedCourses, pendingCourses, totalLessons, totalAssessments);

        // Tickets
        var openTickets = await _db.AskTickets.CountAsync(t => t.Status == TicketStatus.Open, ct);
        var resolvedThisWeek = await _db.AskTickets
            .CountAsync(t => t.Status == TicketStatus.Resolved && t.ResolvedAtUtc != null && t.ResolvedAtUtc >= weekAgo, ct);
        var oldestOpen = await _db.AskTickets
            .Where(t => t.Status == TicketStatus.Open)
            .OrderBy(t => t.CreatedAtUtc)
            .Select(t => (DateTime?)t.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
        var oldestOpenDays = oldestOpen.HasValue ? (int)(now - oldestOpen.Value).TotalDays : 0;
        var tickets = new AdminTicketStats(openTickets, resolvedThisWeek, oldestOpenDays);

        // Payments
        var awaitingReview = await _db.Payments.CountAsync(p => p.Status == PaymentStatus.AwaitingReview, ct);
        var awaitingAmount = await _db.Payments
            .Where(p => p.Status == PaymentStatus.AwaitingReview)
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;
        var approvedThisWeek = await _db.Payments
            .CountAsync(p => p.Status == PaymentStatus.Succeeded && p.ReviewedAtUtc != null && p.ReviewedAtUtc >= weekAgo, ct);
        var rejectedThisWeek = await _db.Payments
            .CountAsync(p => p.Status == PaymentStatus.Failed && p.ReviewedAtUtc != null && p.ReviewedAtUtc >= weekAgo, ct);
        var payments = new AdminPaymentStats(awaitingReview, awaitingAmount, approvedThisWeek, rejectedThisWeek);

        // Recent activity (last 10 mixed)
        var recent = new List<AdminRecentActivityItem>();

        var lastPayments = await _db.Payments
            .Where(p => p.Status == PaymentStatus.AwaitingReview && p.SubmittedAtUtc != null)
            .OrderByDescending(p => p.SubmittedAtUtc)
            .Take(5)
            .Select(p => new { p.Id, p.Amount, p.SubmittedAtUtc, p.SenderName })
            .ToListAsync(ct);
        foreach (var p in lastPayments)
        {
            recent.Add(new AdminRecentActivityItem(
                "payment",
                $"دفعة جديدة: {p.Amount:N0} ج.م",
                $"من: {p.SenderName ?? "غير محدّد"} — في انتظار المراجعة",
                p.SubmittedAtUtc!.Value,
                "/admin/payments/pending"));
        }

        var lastTickets = await _db.AskTickets
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(5)
            .Select(t => new { t.Id, t.CreatedAtUtc, t.Status, StudentName = t.Student.FullName })
            .ToListAsync(ct);
        foreach (var t in lastTickets)
        {
            recent.Add(new AdminRecentActivityItem(
                "ticket",
                $"تذكرة من: {t.StudentName}",
                $"الحالة: {(t.Status == TicketStatus.Open ? "مفتوحة" : t.Status == TicketStatus.Resolved ? "مُحلولة" : "مغلقة")}",
                t.CreatedAtUtc,
                "/admin/tickets"));
        }

        var lastSubs = await _db.Subscriptions
            .OrderByDescending(s => s.CreatedAtUtc)
            .Take(5)
            .Select(s => new { s.Id, s.CreatedAtUtc, s.AmountPaid, s.Status })
            .ToListAsync(ct);
        foreach (var s in lastSubs)
        {
            var statusAr = s.Status switch
            {
                SubscriptionStatus.Active => "نشط",
                SubscriptionStatus.PendingPayment => "بانتظار الدفع",
                SubscriptionStatus.Expired => "منتهي",
                _ => s.Status.ToString()
            };
            recent.Add(new AdminRecentActivityItem(
                "subscription",
                $"اشتراك جديد: {s.AmountPaid:N0} ج.م",
                statusAr,
                s.CreatedAtUtc,
                null));
        }

        var orderedRecent = recent.OrderByDescending(r => r.AtUtc).Take(10).ToList();

        return Result.Success(new AdminOverviewDto(
            users, subs, live, content, tickets, payments, orderedRecent));
    }
}
