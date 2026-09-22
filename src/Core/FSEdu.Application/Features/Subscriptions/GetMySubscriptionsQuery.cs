using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Subscriptions;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Subscriptions;

public sealed record GetMySubscriptionsQuery() : IQuery<List<MySubscriptionDto>>;

public sealed class GetMySubscriptionsHandler : IQueryHandler<GetMySubscriptionsQuery, List<MySubscriptionDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMySubscriptionsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<List<MySubscriptionDto>>> Handle(GetMySubscriptionsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Authentication required");

        var userId = _currentUser.UserId.Value;

        var subs = await _db.Subscriptions
            .Where(s => s.UserId == userId)
            .Include(s => s.Subject)
            .Include(s => s.Stage)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var result = subs.Select(s => new MySubscriptionDto(
            s.Id,
            s.Type.ToString(),
            TypeLabel(s.Type),
            s.SubjectId,
            s.Subject?.NameAr,
            s.StageId,
            s.Stage?.NameAr,
            s.Term?.ToString(),
            s.StartsAtUtc,
            s.EndsAtUtc,
            s.AmountPaid,
            s.Status.ToString(),
            s.IsActiveNow(now)
        )).ToList();

        return Result.Success(result);
    }

    private static string TypeLabel(Domain.Common.SubscriptionType t) => t switch
    {
        Domain.Common.SubscriptionType.SubjectMonthly => "مادة / شهر",
        Domain.Common.SubscriptionType.SubjectTerm    => "مادة / ترم",
        Domain.Common.SubscriptionType.StageFullTerm  => "جميع المواد / ترم",
        _ => "غير معروف"
    };
}
