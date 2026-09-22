using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Dashboard;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Dashboard;

public sealed record GetParentDashboardQuery() : IQuery<ParentDashboardDto>;

public sealed class GetParentDashboardHandler : IQueryHandler<GetParentDashboardQuery, ParentDashboardDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public GetParentDashboardHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<ParentDashboardDto>> Handle(GetParentDashboardQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var parentId = _currentUser.UserId.Value;
        var parent = await _db.Parents.FirstOrDefaultAsync(p => p.Id == parentId, ct);
        if (parent is null)
            return Error.Forbidden("AUTH.NOT_PARENT", "هذا الحساب ليس ولي أمر", "Not a parent");

        var links = await _db.ParentStudentLinks
            .Where(l => l.ParentId == parentId)
            .Include(l => l.Student).ThenInclude(s => s.Stage)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var children = new List<ChildProgressDto>();

        foreach (var link in links)
        {
            var s = link.Student;

            var activeSubs = await _db.Subscriptions
                .Where(sub => sub.UserId == s.Id
                           && sub.Status == SubscriptionStatus.Active
                           && sub.StartsAtUtc <= now && sub.EndsAtUtc >= now)
                .ToListAsync(ct);

            var hasSub = activeSubs.Any();
            string? subLabel = null;
            DateTime? expiresAt = null;

            if (hasSub)
            {
                var bundle = activeSubs.FirstOrDefault(x => x.Type == SubscriptionType.StageFullTerm);
                subLabel = bundle is not null
                    ? $"باقة شاملة / ترم"
                    : $"{activeSubs.Count} اشتراك(ات) فردي";
                expiresAt = activeSubs.Max(x => x.EndsAtUtc);
            }

            var summary = await _access.GetAccessSummaryAsync(s.Id, ct);
            var accessibleCount = await _db.Courses
                .Where(c => c.Status == CourseStatus.Published && summary.AccessibleSubjectIds.Contains(c.SubjectId))
                .CountAsync(ct);

            children.Add(new ChildProgressDto(
                s.Id, s.FullName, s.Phone.Value,
                s.Stage.NameAr,
                link.Relation.ToString(),
                link.IsPrimary,
                s.XpPoints, s.CurrentStreakDays,
                hasSub, subLabel, expiresAt,
                accessibleCount
            ));
        }

        return Result.Success(new ParentDashboardDto(
            parent.FullName,
            children.Count,
            children.ToArray()
        ));
    }
}
