using FSEdu.Application.Abstractions;
using FSEdu.Domain.Billing;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Payments;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Admin.Payments;

// ─── Get Pending Payments ─────────────────────
public sealed record GetPendingPaymentsQuery() : IQuery<List<PendingPaymentDto>>;

public sealed class GetPendingPaymentsHandler : IQueryHandler<GetPendingPaymentsQuery, List<PendingPaymentDto>>
{
    private readonly IApplicationDbContext _db;
    public GetPendingPaymentsHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<List<PendingPaymentDto>>> Handle(GetPendingPaymentsQuery request, CancellationToken ct)
    {
        var result = await (
            from p in _db.Payments
            where p.Status == PaymentStatus.AwaitingReview
            join s in _db.Subscriptions on p.SubscriptionId equals s.Id
            join u in _db.Users on p.UserId equals u.Id
            orderby p.SubmittedAtUtc ascending
            select new
            {
                Payment = p,
                Subscription = s,
                User = u,
                Subject = s.Subject,
                Stage = s.Stage
            }).ToListAsync(ct);

        var list = result.Select(x => new PendingPaymentDto(
            x.Payment.Id,
            x.Subscription.Id,
            x.User.Id,
            x.User.FullName,
            x.User.Phone.Value,
            x.Subscription.Type.ToString(),
            x.Subject?.NameAr,
            x.Stage?.NameAr,
            x.Subscription.Term?.ToString(),
            x.Payment.Amount,
            x.Payment.Currency,
            x.Payment.Method?.ToString(),
            x.Payment.ReferenceNumber,
            x.Payment.ReceiptImageUrl,
            x.Payment.SenderName,
            x.Payment.Notes,
            x.Payment.SubmittedAtUtc
        )).ToList();

        return Result.Success(list);
    }
}

// ─── Approve ─────────────────────────────────
public sealed record ApprovePaymentCommand(Guid PaymentId, string? Note) : ICommand;

public sealed class ApprovePaymentHandler : ICommandHandler<ApprovePaymentCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public ApprovePaymentHandler(IApplicationDbContext db, ICurrentUser currentUser, INotificationService notifications)
    {
        _db = db; _currentUser = currentUser; _notifications = notifications;
    }

    public async Task<Result> Handle(ApprovePaymentCommand request, CancellationToken ct)
    {
        var adminId = _currentUser.UserId ?? Guid.Empty;

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == request.PaymentId, ct);
        if (payment is null)
            return Error.NotFound("PAYMENT.NOT_FOUND", "الدفعة غير موجودة", "Payment not found");

        if (payment.Status != PaymentStatus.AwaitingReview)
            return Error.Validation("PAYMENT.INVALID_STATE",
                "هذه الدفعة تمت مراجعتها مسبقًا", "Already reviewed");

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == payment.SubscriptionId, ct);
        if (subscription is null)
            return Error.NotFound("SUB.NOT_FOUND", "الاشتراك غير موجود", "Not found");

        payment.ApproveByAdmin(adminId, request.Note);
        subscription.Activate();

        // ─── Referral reward (only on this student's first approved payment) ──
        var rewardEgp = await IssueReferralRewardIfFirstPaymentAsync(payment, subscription, ct);

        await _db.SaveChangesAsync(ct);

        await _notifications.SendAsync(payment.UserId,
            NotificationTypes.PaymentApproved,
            "تم تفعيل اشتراكك! 🎉",
            $"تمت الموافقة على دفعتك بمبلغ {payment.Amount} {payment.Currency}. اشتراكك مفعّل الآن — استمتع بالتعلّم!",
            new { paymentId = payment.Id, subscriptionId = subscription.Id, url = "/student/subscriptions" },
            ct);

        if (rewardEgp > 0)
        {
            // Notify both parties when the referral pays out
            var referral = await _db.Referrals.FirstOrDefaultAsync(r => r.TriggerSubscriptionId == subscription.Id, ct);
            if (referral is not null)
            {
                await _notifications.SendAsync(referral.ReferrerStudentId,
                    "referral.rewarded",
                    "🎁 مكافأة إحالة جديدة!",
                    $"صديق دعوته اشترك للمرة الأولى. تم إضافة {rewardEgp} جنيه إلى رصيدك في FSEdu.",
                    new { url = "/my/referral" },
                    ct);

                await _notifications.SendAsync(referral.ReferredStudentId,
                    "referral.rewarded",
                    "✨ شكرًا لقبول دعوة صديقك",
                    "تم تأكيد اشتراكك الأول، وحصل صديقك على مكافأة. اعرف المزيد عن خصومات الإحالة من صفحتك.",
                    new { url = "/my/referral" },
                    ct);
            }
        }

        return Result.Success();
    }

    private const int ReferralRewardEgp = 50;

    private async Task<int> IssueReferralRewardIfFirstPaymentAsync(
        Payment payment, Subscription subscription, CancellationToken ct)
    {
        // Skip if student has no inviter
        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == payment.UserId, ct);
        if (student is null || string.IsNullOrEmpty(student.ReferredByCode)) return 0;

        // Find the pending referral row for this student
        var referral = await _db.Referrals.FirstOrDefaultAsync(r => r.ReferredStudentId == student.Id, ct);
        if (referral is null || referral.RewardedAtUtc is not null) return 0;

        // Only on the FIRST approved payment for this student
        var priorApproved = await _db.Payments.AnyAsync(p =>
            p.UserId == student.Id &&
            p.Id != payment.Id &&
            p.Status == PaymentStatus.Succeeded, ct);
        if (priorApproved) return 0;

        var referrer = await _db.Students.FirstOrDefaultAsync(s => s.Id == referral.ReferrerStudentId, ct);
        if (referrer is null) return 0;

        referrer.AddReferralCredit(ReferralRewardEgp);
        referral.MarkRewarded(ReferralRewardEgp, subscription.Id);
        return ReferralRewardEgp;
    }
}

// ─── Reject ──────────────────────────────────
public sealed record RejectPaymentCommand(Guid PaymentId, string Reason) : ICommand;

public sealed class RejectPaymentHandler : ICommandHandler<RejectPaymentCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public RejectPaymentHandler(IApplicationDbContext db, ICurrentUser currentUser, INotificationService notifications)
    {
        _db = db; _currentUser = currentUser; _notifications = notifications;
    }

    public async Task<Result> Handle(RejectPaymentCommand request, CancellationToken ct)
    {
        var adminId = _currentUser.UserId ?? Guid.Empty;

        var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == request.PaymentId, ct);
        if (payment is null)
            return Error.NotFound("PAYMENT.NOT_FOUND", "الدفعة غير موجودة", "Payment not found");

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == payment.SubscriptionId, ct);

        payment.RejectByAdmin(adminId, request.Reason);
        subscription?.Cancel();

        await _db.SaveChangesAsync(ct);

        await _notifications.SendAsync(payment.UserId,
            NotificationTypes.PaymentRejected,
            "لم يتم تأكيد دفعتك",
            $"السبب: {request.Reason} — يرجى مراجعة بياناتك أو التواصل مع الدعم.",
            new { paymentId = payment.Id, url = "/student/subscriptions" },
            ct);

        return Result.Success();
    }
}
