using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Admin;

// Admin-only fire-and-store-and-push broadcast.
// Target syntax:
//   "all"          → every active user (AppUser)
//   "students"     → all students
//   "teachers"     → all teachers
//   "parents"      → all parents
//   "stage:{id}"   → all students in a given stage
public sealed record BroadcastNotificationCommand(
    string Target,
    string Title,
    string Body,
    string? Url
) : ICommand<int>;

public sealed class BroadcastNotificationValidator : AbstractValidator<BroadcastNotificationCommand>
{
    public BroadcastNotificationValidator()
    {
        RuleFor(x => x.Target).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Title).NotEmpty().MinimumLength(2).MaximumLength(255);
        RuleFor(x => x.Body).NotEmpty().MinimumLength(2).MaximumLength(2000);
        RuleFor(x => x.Url).MaximumLength(500);
    }
}

public sealed class BroadcastNotificationHandler : ICommandHandler<BroadcastNotificationCommand, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;
    private readonly IAuditLog _audit;

    public BroadcastNotificationHandler(IApplicationDbContext db, ICurrentUser currentUser,
        INotificationService notifications, IAuditLog audit)
    {
        _db = db; _currentUser = currentUser; _notifications = notifications; _audit = audit;
    }

    public async Task<Result<int>> Handle(BroadcastNotificationCommand request, CancellationToken ct)
    {
        if (!_currentUser.IsInRole("Admin"))
            return Error.Forbidden("BROADCAST.FORBIDDEN", "للأدمن فقط", "Admin only");

        var target = request.Target.Trim().ToLowerInvariant();
        List<Guid> userIds;

        if (target == "all")
        {
            userIds = await _db.Users.Select(u => u.Id).ToListAsync(ct);
        }
        else if (target == "students")
        {
            userIds = await _db.Students.Select(s => s.Id).ToListAsync(ct);
        }
        else if (target == "teachers")
        {
            userIds = await _db.Teachers.Select(t => t.Id).ToListAsync(ct);
        }
        else if (target == "parents")
        {
            userIds = await _db.Parents.Select(p => p.Id).ToListAsync(ct);
        }
        else if (target.StartsWith("stage:"))
        {
            if (!int.TryParse(target.Substring("stage:".Length), out var stageId) || stageId <= 0)
                return Error.Validation("BROADCAST.BAD_STAGE", "معرّف المرحلة غير صحيح", "Invalid stage id");
            userIds = await _db.Students.Where(s => s.StageId == stageId).Select(s => s.Id).ToListAsync(ct);
        }
        else
        {
            return Error.Validation("BROADCAST.BAD_TARGET",
                "هدف غير معروف. استخدم: all/students/teachers/parents/stage:{id}",
                "Unknown target");
        }

        if (userIds.Count == 0)
            return Result.Success(0);

        await _notifications.SendBulkAsync(userIds, "admin.broadcast",
            request.Title.Trim(), request.Body.Trim(),
            new { url = string.IsNullOrWhiteSpace(request.Url) ? null : request.Url.Trim() }, ct);

        await _audit.RecordAsync("admin.broadcast", "Broadcast", null,
            new { target, recipients = userIds.Count, title = request.Title }, ct);

        return Result.Success(userIds.Count);
    }
}
