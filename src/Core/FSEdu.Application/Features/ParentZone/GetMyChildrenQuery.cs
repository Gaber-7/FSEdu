using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Parent;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.ParentZone;

public sealed record GetMyChildrenQuery() : IQuery<List<ChildSummaryDto>>;

public sealed class GetMyChildrenHandler : IQueryHandler<GetMyChildrenQuery, List<ChildSummaryDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyChildrenHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<List<ChildSummaryDto>>> Handle(GetMyChildrenQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Authentication required");

        var parentId = _currentUser.UserId.Value;

        var links = await _db.ParentStudentLinks
            .Where(l => l.ParentId == parentId)
            .Include(l => l.Student).ThenInclude(s => s.Stage)
            .Include(l => l.Student).ThenInclude(s => s.Region)
            .ToListAsync(ct);

        var children = links.Select(l => new ChildSummaryDto(
            l.StudentId,
            l.Student.FullName,
            l.Student.Phone.Value,
            l.Student.Stage.NameAr,
            l.Student.Region.NameAr,
            l.Relation.ToString(),
            l.IsPrimary,
            l.Student.XpPoints,
            l.Student.CurrentStreakDays
        )).ToList();

        return Result.Success(children);
    }
}
