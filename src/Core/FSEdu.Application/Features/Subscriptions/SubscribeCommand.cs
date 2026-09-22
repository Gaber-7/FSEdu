using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Billing;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Subscriptions;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Subscriptions;

public sealed record SubscribeCommand(
    string Type,
    int? SubjectId,
    int? StageId,
    string? Term
) : ICommand<SubscribeResponse>;

public sealed class SubscribeValidator : AbstractValidator<SubscribeCommand>
{
    public SubscribeValidator()
    {
        RuleFor(x => x.Type).NotEmpty()
            .Must(t => t is "SubjectMonthly" or "SubjectTerm" or "StageFullTerm")
            .WithMessage("نوع الاشتراك غير صالح");

        When(x => x.Type == "SubjectMonthly" || x.Type == "SubjectTerm", () =>
        {
            RuleFor(x => x.SubjectId).NotNull().GreaterThan(0).WithMessage("معرّف المادة مطلوب");
        });

        When(x => x.Type == "StageFullTerm", () =>
        {
            RuleFor(x => x.StageId).NotNull().GreaterThan(0).WithMessage("معرّف المرحلة مطلوب");
        });

        When(x => x.Type == "SubjectTerm" || x.Type == "StageFullTerm", () =>
        {
            RuleFor(x => x.Term).NotEmpty().WithMessage("الترم مطلوب");
        });
    }
}

public sealed class SubscribeHandler : ICommandHandler<SubscribeCommand, SubscribeResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public SubscribeHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<SubscribeResponse>> Handle(SubscribeCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Authentication required");

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
                    now, amount, PaymentProvider.Manual, providerRef: "DEV_MOCK");
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
                    starts, ends, amount, PaymentProvider.Manual, "DEV_MOCK");
                break;
            }

            case "StageFullTerm":
            {
                var stage = await _db.Stages.FirstOrDefaultAsync(s => s.Id == request.StageId, ct);
                if (stage is null)
                    return Error.NotFound("STAGE.NOT_FOUND", "المرحلة غير موجودة", "Stage not found");

                if (stage.FullTermPriceEgp is null)
                    return Error.Validation("STAGE.NO_PRICE", "لا يوجد سعر محدّد لهذه المرحلة", "Stage has no bundle price");

                var term = ParseTerm(request.Term!);
                var (starts, ends) = TermDates(term, now);
                amount = stage.FullTermPriceEgp.Value;
                sub = Subscription.CreateStageFullTerm(
                    Guid.NewGuid(), userId, stage.Id, term,
                    starts, ends, amount, PaymentProvider.Manual, "DEV_MOCK");
                break;
            }

            default:
                return Error.Validation("SUB.TYPE_INVALID", "نوع غير مدعوم", "Unsupported type");
        }

        _db.Subscriptions.Add(sub);
        await _db.SaveChangesAsync(ct);

        return Result.Success(new SubscribeResponse(
            sub.Id, sub.Type.ToString(), sub.AmountPaid,
            sub.StartsAtUtc, sub.EndsAtUtc, sub.Status.ToString()));
    }

    private static AcademicTerm ParseTerm(string s) => s switch
    {
        "FirstTerm" or "First" => AcademicTerm.First,
        "SecondTerm" or "Second" => AcademicTerm.Second,
        "Annual" => AcademicTerm.Annual,
        _ => AcademicTerm.First
    };

    // Egyptian academic calendar approximation
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
