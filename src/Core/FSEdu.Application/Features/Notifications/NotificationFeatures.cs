using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Notifications;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Notifications;

// ─── List my notifications ─────────────────────
public sealed record GetMyNotificationsQuery(int Take = 30, bool UnreadOnly = false)
    : IQuery<List<NotificationDto>>;

public sealed class GetMyNotificationsHandler : IQueryHandler<GetMyNotificationsQuery, List<NotificationDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyNotificationsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<NotificationDto>>> Handle(GetMyNotificationsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var q = _db.Notifications.Where(n => n.UserId == userId);
        if (request.UnreadOnly) q = q.Where(n => n.ReadAtUtc == null);

        var list = await q
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(Math.Min(request.Take, 100))
            .Select(n => new NotificationDto(
                n.Id, n.Type, n.Title, n.Body, n.DataJson,
                n.ReadAtUtc != null, n.CreatedAtUtc))
            .ToListAsync(ct);

        return Result.Success(list);
    }
}

// ─── Unread Count ──────────────────────────────
public sealed record GetUnreadCountQuery() : IQuery<UnreadCountDto>;

public sealed class GetUnreadCountHandler : IQueryHandler<GetUnreadCountQuery, UnreadCountDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetUnreadCountHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<UnreadCountDto>> Handle(GetUnreadCountQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Success(new UnreadCountDto(0));

        var userId = _currentUser.UserId.Value;
        var count = await _db.Notifications
            .CountAsync(n => n.UserId == userId && n.ReadAtUtc == null, ct);

        return Result.Success(new UnreadCountDto(count));
    }
}

// ─── Mark Read (single) ────────────────────────
public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand;

public sealed class MarkNotificationReadHandler : ICommandHandler<MarkNotificationReadCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public MarkNotificationReadHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId && n.UserId == userId, ct);

        if (notification is null)
            return Error.NotFound("NOTIF.NOT_FOUND", "الإشعار غير موجود", "Not found");

        notification.MarkRead();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Mark All Read ─────────────────────────────
public sealed record MarkAllNotificationsReadCommand() : ICommand;

public sealed class MarkAllNotificationsReadHandler : ICommandHandler<MarkAllNotificationsReadCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public MarkAllNotificationsReadHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(MarkAllNotificationsReadCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null) return Result.Success();
        var userId = _currentUser.UserId.Value;

        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && n.ReadAtUtc == null)
            .ToListAsync(ct);

        foreach (var n in unread) n.MarkRead();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
