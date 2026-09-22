using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Admin;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Admin;

// Admin-only: reads the audit log with optional filters. Page size is
// capped to keep payloads bounded; the UI does pagination by page number.
public sealed record GetAuditLogQuery(
    string? Action = null,
    Guid? ActorUserId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 50
) : IQuery<AuditLogResponse>;

public sealed class GetAuditLogHandler : IQueryHandler<GetAuditLogQuery, AuditLogResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetAuditLogHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<AuditLogResponse>> Handle(GetAuditLogQuery request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Error.Forbidden("AUDIT.FORBIDDEN", "للأدمن فقط", "Admin only");

        var q = _db.AuditEntries.AsQueryable();
        if (!string.IsNullOrWhiteSpace(request.Action))
            q = q.Where(x => x.Action == request.Action);
        if (request.ActorUserId.HasValue)
            q = q.Where(x => x.ActorUserId == request.ActorUserId.Value);
        if (request.FromUtc.HasValue)
            q = q.Where(x => x.AtUtc >= request.FromUtc.Value);
        if (request.ToUtc.HasValue)
            q = q.Where(x => x.AtUtc <= request.ToUtc.Value);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 10, 200);

        var rows = await q
            .OrderByDescending(x => x.AtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => new AuditEntryDto(
                x.Id, x.ActorUserId, x.ActorName, x.ActorRole,
                x.Action, x.EntityType, x.EntityId, x.DetailsJson,
                x.IpAddress, x.AtUtc))
            .ToListAsync(ct);

        return Result.Success(new AuditLogResponse(total, rows));
    }
}
