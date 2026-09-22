using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Billing;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Payments;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Subscriptions;

public sealed record SubscribeWithPaymentCommand(
    string Type,
    int? SubjectId,
    int? StageId,
    string? Term,
    string PaymentMethod,
    string? ReferenceNumber,
    string? ReceiptImageUrl,
    string? SenderName,
    string? Notes,
    string? CouponCode = null
) : ICommand<SubscribeWithPaymentResponse>;

public sealed class SubscribeWithPaymentValidator : AbstractValidator<SubscribeWithPaymentCommand>
{
    public SubscribeWithPaymentValidator()
    {
        RuleFor(x => x.Type).NotEmpty()
            .Must(t => t is "SubjectMonthly" or "SubjectTerm" or "StageFullTerm")
            .WithMessage("نوع الاشتراك غير صالح");

        RuleFor(x => x.PaymentMethod).NotEmpty()
            .Must(m => m is "InstaPay" or "VodafoneCash" or "BankTransfer" or "Cash")
            .WithMessage("طريقة الدفع غير صالحة");

        // Reference number is required for digital methods, optional for Cash
        // (admin records it when the student pays in-person).
        When(x => x.PaymentMethod != "Cash", () =>
            RuleFor(x => x.ReferenceNumber).NotEmpty().MinimumLength(3)
                .WithMessage("رقم المرجع/العملية مطلوب"));

        When(x => x.Type == "SubjectMonthly" || x.Type == "SubjectTerm", () =>
            RuleFor(x => x.SubjectId).NotNull().GreaterThan(0).WithMessage("معرّف المادة مطلوب"));

        When(x => x.Type == "StageFullTerm", () =>
            RuleFor(x => x.StageId).NotNull().GreaterThan(0).WithMessage("معرّف المرحلة مطلوب"));

        When(x => x.Type != "SubjectMonthly", () =>
            RuleFor(x => x.Term).NotEmpty().WithMessage("الترم مطلوب"));
    }
}

public sealed class SubscribeWithPaymentHandler : ICommandHandler<SubscribeWithPaymentCommand, SubscribeWithPaymentResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly DiscountEvaluator _discountEvaluator;

    public SubscribeWithPaymentHandler(IApplicationDbContext db, ICurrentUser currentUser,
        DiscountEvaluator discountEvaluator)
    {
        _db = db; _currentUser = currentUser; _discountEvaluator = discountEvaluator;
    }

    public async Task<Result<SubscribeWithPaymentResponse>> Handle(SubscribeWithPaymentCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var now = DateTime.UtcNow;

        Subscription sub;
        decimal amount;

        switch (request.Type)
        {
            case "SubjectMonthly":
            {
                var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == request.SubjectId && s.IsActive, ct);
                if (subject is null)
                    return Error.NotFound("SUBJECT.NOT_FOUND", "المادة غير موجودة", "Subject not found");

                amount = subject.MonthlyPriceEgp;
                sub = Subscription.CreateSubjectMonthly(
                    Guid.NewGuid(), userId, subject.Id, subject.StageId,
                    now, amount, PaymentProvider.Manual, request.ReferenceNumber);
                break;
            }

            case "SubjectTerm":
            {
                var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == request.SubjectId && s.IsActive, ct);
                if (subject is null)
                    return Error.NotFound("SUBJECT.NOT_FOUND", "المادة غير موجودة", "Subject not found");

                var term = ParseTerm(request.Term!);
                var (starts, ends) = TermDates(term, now);
                amount = subject.TermPriceEgp;
                sub = Subscription.CreateSubjectTerm(
                    Guid.NewGuid(), userId, subject.Id, subject.StageId, term,
                    starts, ends, amount, PaymentProvider.Manual, request.ReferenceNumber);
                break;
            }

            case "StageFullTerm":
            {
                var stage = await _db.Stages.FirstOrDefaultAsync(s => s.Id == request.StageId, ct);
                if (stage is null)
                    return Error.NotFound("STAGE.NOT_FOUND", "المرحلة غير موجودة", "Stage not found");

                if (stage.FullTermPriceEgp is null)
                    return Error.Validation("STAGE.NO_PRICE", "لا يوجد سعر محدّد لهذه المرحلة", "No price");

                var term = ParseTerm(request.Term!);
                var (starts, ends) = TermDates(term, now);
                amount = stage.FullTermPriceEgp.Value;
                sub = Subscription.CreateStageFullTerm(
                    Guid.NewGuid(), userId, stage.Id, term,
                    starts, ends, amount, PaymentProvider.Manual, request.ReferenceNumber);
                break;
            }

            default:
                return Error.Validation("SUB.TYPE_INVALID", "نوع غير مدعوم", "Unsupported");
        }

        // ─── Auto-apply best active campaign (no code required) ─────
        decimal campaignDiscount = 0m;
        SalesCampaign? appliedCampaign = null;
        if (sub.StageId.HasValue)
        {
            var now2 = DateTime.UtcNow;
            var candidates = await _db.SalesCampaigns
                .Where(c => c.Active && c.ValidFromUtc <= now2 && c.ValidUntilUtc >= now2)
                .ToListAsync(ct);
            // Pick the campaign with the highest discount among those that match.
            appliedCampaign = candidates
                .Where(c => c.AppliesTo(sub.StageId.Value, sub.SubjectId))
                .OrderByDescending(c => c.DiscountPct)
                .FirstOrDefault();
            if (appliedCampaign is not null)
            {
                campaignDiscount = Math.Round(amount * appliedCampaign.DiscountPct / 100m, 2);
                amount = Math.Max(0m, amount - campaignDiscount);
            }
        }

        // ─── Auto-apply best matching DiscountRule (no code required) ────
        // Picks the SINGLE highest-pct rule the student qualifies for —
        // we do not stack multiple rules to keep totals predictable.
        decimal ruleDiscount = 0m;
        ApplicableDiscount? appliedRule = null;
        var subTypeEnum = Enum.Parse<SubscriptionType>(request.Type);
        var applicableRules = await _discountEvaluator.EvaluateAsync(
            userId, subTypeEnum, request.SubjectId, request.StageId, ct);
        if (applicableRules.Count > 0)
        {
            appliedRule = applicableRules.OrderByDescending(r => r.Pct).First();
            ruleDiscount = Math.Round(amount * appliedRule.Pct / 100m, 2);
            amount = Math.Max(0m, amount - ruleDiscount);
        }

        // ─── Apply coupon discount on top (if any) ─────────
        decimal couponDiscount = 0m;
        Coupon? appliedCoupon = null;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            var code = request.CouponCode.Trim();
            appliedCoupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code, ct);
            if (appliedCoupon is null)
                return Error.Validation("COUPON.NOT_FOUND", "الكوبون غير موجود", "Coupon not found");
            if (!appliedCoupon.IsValid())
                return Error.Validation("COUPON.INVALID", "الكوبون منتهي الصلاحية أو غير مفعّل", "Coupon invalid");

            (couponDiscount, amount) = CouponPricing.Apply(appliedCoupon, amount);
            appliedCoupon.IncrementUsage();
        }

        // ─── Apply student's referral credit balance (last, off the top) ─
        decimal creditApplied = 0m;
        var studentForCredit = await _db.Students.FirstOrDefaultAsync(s => s.Id == userId, ct);
        if (studentForCredit is not null && studentForCredit.ReferralCredits > 0 && amount > 0)
        {
            var available = studentForCredit.ReferralCredits;
            creditApplied = Math.Min(amount, available);
            amount = Math.Max(0m, amount - creditApplied);
            studentForCredit.SpendReferralCredit((int)Math.Floor(creditApplied));
        }

        var totalDiscount = campaignDiscount + ruleDiscount + couponDiscount + creditApplied;
        var method = Enum.Parse<PaymentMethod>(request.PaymentMethod);

        var payment = new Payment(Guid.NewGuid(), sub.Id, userId, amount, sub.Currency);

        // Build notes recording discounts for audit trail
        var notesBits = new List<string>();
        if (!string.IsNullOrWhiteSpace(request.Notes)) notesBits.Add(request.Notes.Trim());
        if (appliedCampaign is not null)
            notesBits.Add($"حملة خصم: {appliedCampaign.Title} (-{appliedCampaign.DiscountPct:0.##}% = {campaignDiscount:0.##} {sub.Currency})");
        if (appliedRule is not null)
            notesBits.Add($"خصم آلي ({appliedRule.TitleAr}): -{appliedRule.Pct:0.##}% = {ruleDiscount:0.##} {sub.Currency}");
        if (appliedCoupon is not null)
            notesBits.Add($"كوبون: {appliedCoupon.Code} (-{couponDiscount:0.##} {sub.Currency})");
        if (creditApplied > 0)
            notesBits.Add($"رصيد إحالة: -{creditApplied:0.##} {sub.Currency}");
        var notes = notesBits.Count > 0 ? string.Join("\n", notesBits) : null;

        payment.SubmitReceipt(method, request.ReferenceNumber, request.ReceiptImageUrl,
                              request.SenderName, notes);

        _db.Subscriptions.Add(sub);
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(ct);

        string msg;
        if (appliedCampaign is null && appliedCoupon is null && appliedRule is null)
            msg = "تم استلام طلب الاشتراك. سيتم تفعيله فور التحقق من الدفع خلال 24 ساعة.";
        else
            msg = $"تم استلام طلب الاشتراك مع خصم إجمالي {totalDiscount:0.##} {sub.Currency}. سيتم تفعيله فور التحقق من الدفع خلال 24 ساعة.";

        return Result.Success(new SubscribeWithPaymentResponse(
            sub.Id, payment.Id, amount, sub.Currency,
            sub.Status.ToString(),
            msg));
    }

    private static AcademicTerm ParseTerm(string s) => s switch
    {
        "FirstTerm" or "First" => AcademicTerm.First,
        "SecondTerm" or "Second" => AcademicTerm.Second,
        "Annual" => AcademicTerm.Annual,
        _ => AcademicTerm.First
    };

    private static (DateTime starts, DateTime ends) TermDates(AcademicTerm term, DateTime now)
    {
        var year = now.Month >= 9 ? now.Year : now.Year - 1;
        return term switch
        {
            AcademicTerm.First  => (new DateTime(year, 9, 1, 0,0,0, DateTimeKind.Utc),
                                    new DateTime(year + 1, 1, 31, 23,59,0, DateTimeKind.Utc)),
            AcademicTerm.Second => (new DateTime(year + 1, 2, 1, 0,0,0, DateTimeKind.Utc),
                                    new DateTime(year + 1, 6, 30, 23,59,0, DateTimeKind.Utc)),
            AcademicTerm.Annual => (new DateTime(year, 9, 1, 0,0,0, DateTimeKind.Utc),
                                    new DateTime(year + 1, 6, 30, 23,59,0, DateTimeKind.Utc)),
            _ => (now, now.AddMonths(4))
        };
    }
}
