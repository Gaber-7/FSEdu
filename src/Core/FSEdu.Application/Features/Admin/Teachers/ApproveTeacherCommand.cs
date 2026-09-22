using FSEdu.Application.Abstractions;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Admin.Teachers;

public sealed record ApproveTeacherCommand(Guid TeacherId, string? Notes) : ICommand;

public sealed class ApproveTeacherHandler : ICommandHandler<ApproveTeacherCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public ApproveTeacherHandler(IApplicationDbContext db, ICurrentUser currentUser, INotificationService notifications)
    {
        _db = db; _currentUser = currentUser; _notifications = notifications;
    }

    public async Task<Result> Handle(ApproveTeacherCommand request, CancellationToken ct)
    {
        var teacher = await _db.Teachers.FirstOrDefaultAsync(t => t.Id == request.TeacherId, ct);
        if (teacher is null)
            return Error.NotFound("TEACHER.NOT_FOUND", "المدرس غير موجود", "Teacher not found");

        if (teacher.Verified)
            return Error.Conflict("TEACHER.ALREADY_VERIFIED", "المدرس موافَق عليه مسبقًا", "Already verified");

        var adminId = _currentUser.UserId ?? Guid.Empty;
        teacher.Verify(adminId);

        await _db.SaveChangesAsync(ct);

        await _notifications.SendAsync(teacher.Id,
            NotificationTypes.TeacherApproved,
            "تم اعتماد حسابك! 🎉",
            "تمت الموافقة على حسابك كمدرس. يمكنك الآن إنشاء دورات وبدء التدريس.",
            new { url = "/teacher/courses/new" },
            ct);

        return Result.Success();
    }
}

public sealed record RejectTeacherCommand(Guid TeacherId, string Reason) : ICommand;

public sealed class RejectTeacherHandler : ICommandHandler<RejectTeacherCommand>
{
    private readonly IApplicationDbContext _db;
    public RejectTeacherHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(RejectTeacherCommand request, CancellationToken ct)
    {
        var teacher = await _db.Teachers.FirstOrDefaultAsync(t => t.Id == request.TeacherId, ct);
        if (teacher is null)
            return Error.NotFound("TEACHER.NOT_FOUND", "المدرس غير موجود", "Teacher not found");

        teacher.Deactivate();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
