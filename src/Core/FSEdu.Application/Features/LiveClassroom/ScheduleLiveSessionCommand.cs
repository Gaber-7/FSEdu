using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Domain.LiveClassroom;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.LiveClassroom;

public sealed record ScheduleLiveSessionCommand(
    int SubjectId,
    int StageId,
    string Title,
    string? Description,
    DateTime ScheduledAtUtc,
    int DurationMinutes
) : ICommand<Guid>;

public sealed class ScheduleLiveSessionValidator : AbstractValidator<ScheduleLiveSessionCommand>
{
    public ScheduleLiveSessionValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(255);
        RuleFor(x => x.SubjectId).GreaterThan(0);
        RuleFor(x => x.StageId).GreaterThan(0);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 240)
            .WithMessage("المدة يجب أن تكون بين 15 و240 دقيقة");
        RuleFor(x => x.ScheduledAtUtc).GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("لا يمكن جدولة حصة في الماضي");
    }
}

public sealed class ScheduleLiveSessionHandler : ICommandHandler<ScheduleLiveSessionCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public ScheduleLiveSessionHandler(IApplicationDbContext db, ICurrentUser currentUser, INotificationService notifications)
    {
        _db = db; _currentUser = currentUser; _notifications = notifications;
    }

    public async Task<Result<Guid>> Handle(ScheduleLiveSessionCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var teacherId = _currentUser.UserId.Value;
        var teacher = await _db.Teachers.FirstOrDefaultAsync(t => t.Id == teacherId, ct);
        if (teacher is null || !teacher.Verified)
            return Error.Forbidden("TEACHER.NOT_VERIFIED", "حسابك لم يُعتمد بعد", "Not verified");

        var subjectExists = await _db.Subjects.AnyAsync(s => s.Id == request.SubjectId, ct);
        if (!subjectExists)
            return Error.NotFound("SUBJECT.NOT_FOUND", "المادة غير موجودة", "Subject not found");

        var roomId = $"room_{Guid.NewGuid():N}".Substring(0, 24);
        var session = new LiveSession(
            Guid.NewGuid(), teacherId, request.Title.Trim(),
            request.SubjectId, request.StageId,
            roomId, request.ScheduledAtUtc, request.DurationMinutes,
            lessonId: null, description: request.Description);

        _db.LiveSessions.Add(session);
        await _db.SaveChangesAsync(ct);

        return Result.Success(session.Id);
    }
}

// ─── Start ─────────────────────────────
public sealed record StartLiveSessionCommand(Guid SessionId) : ICommand;

public sealed class StartLiveSessionHandler : ICommandHandler<StartLiveSessionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public StartLiveSessionHandler(IApplicationDbContext db, ICurrentUser currentUser, INotificationService notifications)
    {
        _db = db; _currentUser = currentUser; _notifications = notifications;
    }

    public async Task<Result> Handle(StartLiveSessionCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var session = await _db.LiveSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId, ct);
        if (session is null)
            return Error.NotFound("LIVE.NOT_FOUND", "الحصة غير موجودة", "Not found");
        if (session.TeacherId != userId)
            return Error.Forbidden("LIVE.NOT_OWNER", "ليست حصتك", "Not yours");

        session.Start();
        await _db.SaveChangesAsync(ct);

        // Notify enrolled students (those with active subscription to this subject)
        var now = DateTime.UtcNow;
        var enrolledUserIds = await _db.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active
                     && s.StartsAtUtc <= now && s.EndsAtUtc >= now
                     && ((s.Type != SubscriptionType.StageFullTerm && s.SubjectId == session.SubjectId)
                         || (s.Type == SubscriptionType.StageFullTerm && s.StageId == session.StageId)))
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var uid in enrolledUserIds)
        {
            await _notifications.SendAsync(uid,
                "live.started",
                "🎥 الحصة المباشرة بدأت!",
                $"الحصة \"{session.Title}\" بدأت الآن — انضم فورًا.",
                new { sessionId = session.Id, url = $"/live/{session.Id}" }, ct);
        }

        return Result.Success();
    }
}

// ─── Edit (scheduled only) ────────────
public sealed record EditLiveSessionCommand(
    Guid SessionId,
    string Title,
    string? Description,
    int SubjectId,
    int StageId,
    DateTime ScheduledAtUtc,
    int DurationMinutes
) : ICommand;

public sealed class EditLiveSessionValidator : AbstractValidator<EditLiveSessionCommand>
{
    public EditLiveSessionValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(255);
        RuleFor(x => x.SubjectId).GreaterThan(0);
        RuleFor(x => x.StageId).GreaterThan(0);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 240);
        RuleFor(x => x.ScheduledAtUtc).GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("لا يمكن جدولة حصة في الماضي");
    }
}

public sealed class EditLiveSessionHandler : ICommandHandler<EditLiveSessionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public EditLiveSessionHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(EditLiveSessionCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var session = await _db.LiveSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId, ct);
        if (session is null)
            return Error.NotFound("LIVE.NOT_FOUND", "الحصة غير موجودة", "Not found");
        if (session.TeacherId != userId)
            return Error.Forbidden("LIVE.NOT_OWNER", "ليست حصتك", "Not yours");
        if (session.Status != LiveSessionStatus.Scheduled)
            return Error.Validation("LIVE.NOT_SCHEDULED", "لا يمكن تعديل حصة بدأت أو انتهت", "Already started/ended");

        var subjectExists = await _db.Subjects.AnyAsync(s => s.Id == request.SubjectId, ct);
        if (!subjectExists)
            return Error.NotFound("SUBJECT.NOT_FOUND", "المادة غير موجودة", "Subject not found");

        session.UpdateScheduleDetails(
            request.Title.Trim(), request.Description?.Trim(),
            request.SubjectId, request.StageId,
            request.ScheduledAtUtc, request.DurationMinutes);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Bulk schedule recurring sessions ───────────────
// Creates N scheduled sessions, one per week starting at FirstStartUtc.
// Useful for teachers who run a weekly class for a whole term — instead
// of opening the schedule form 12 times, they schedule 12 weeks at once.
public sealed record BulkScheduleLiveSessionsCommand(
    int SubjectId,
    int StageId,
    string TitlePattern,            // e.g., "حصة الرياضيات — الأسبوع {N}"
    string? Description,
    DateTime FirstStartUtc,
    int DurationMinutes,
    int Occurrences                 // 2..30
) : ICommand<Guid[]>;

public sealed class BulkScheduleLiveSessionsValidator : AbstractValidator<BulkScheduleLiveSessionsCommand>
{
    public BulkScheduleLiveSessionsValidator()
    {
        RuleFor(x => x.TitlePattern).NotEmpty().MinimumLength(3).MaximumLength(220);
        RuleFor(x => x.SubjectId).GreaterThan(0);
        RuleFor(x => x.StageId).GreaterThan(0);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(15, 240);
        RuleFor(x => x.FirstStartUtc).GreaterThan(DateTime.UtcNow.AddMinutes(-5));
        RuleFor(x => x.Occurrences).InclusiveBetween(2, 30);
    }
}

public sealed class BulkScheduleLiveSessionsHandler
    : ICommandHandler<BulkScheduleLiveSessionsCommand, Guid[]>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public BulkScheduleLiveSessionsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<Guid[]>> Handle(BulkScheduleLiveSessionsCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var teacherId = _currentUser.UserId.Value;
        var teacher = await _db.Teachers.FirstOrDefaultAsync(t => t.Id == teacherId, ct);
        if (teacher is null || !teacher.Verified)
            return Error.Forbidden("TEACHER.NOT_VERIFIED", "حسابك لم يُعتمد بعد", "Not verified");

        var subjectExists = await _db.Subjects.AnyAsync(s => s.Id == request.SubjectId, ct);
        if (!subjectExists)
            return Error.NotFound("SUBJECT.NOT_FOUND", "المادة غير موجودة", "Subject not found");

        var ids = new Guid[request.Occurrences];
        for (int i = 0; i < request.Occurrences; i++)
        {
            var weekNum = i + 1;
            // {N} -> 1..Occurrences, also {n} as alias
            var title = request.TitlePattern
                .Replace("{N}", weekNum.ToString())
                .Replace("{n}", weekNum.ToString())
                .Trim();
            var startsAt = request.FirstStartUtc.AddDays(7 * i);
            var roomId = $"room_{Guid.NewGuid():N}".Substring(0, 24);

            var session = new Domain.LiveClassroom.LiveSession(
                Guid.NewGuid(), teacherId, title,
                request.SubjectId, request.StageId,
                roomId, startsAt, request.DurationMinutes,
                lessonId: null, description: request.Description);

            _db.LiveSessions.Add(session);
            ids[i] = session.Id;
        }

        await _db.SaveChangesAsync(ct);
        return Result.Success(ids);
    }
}

// ─── Reschedule an ended/cancelled session as a new one ────
// Clones an existing session (regardless of status) into a brand-new
// Scheduled session with a new datetime. Useful when a teacher wants
// to re-run a past session (e.g., for absent students) without losing
// the original's attendance, recording, or chat history.
public sealed record RescheduleLiveSessionCommand(
    Guid SourceSessionId,
    DateTime NewScheduledAtUtc,
    int? NewDurationMinutes = null,
    string? TitleOverride = null
) : ICommand<Guid>;

public sealed class RescheduleLiveSessionValidator : AbstractValidator<RescheduleLiveSessionCommand>
{
    public RescheduleLiveSessionValidator()
    {
        RuleFor(x => x.NewScheduledAtUtc).GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("لا يمكن إعادة الجدولة في الماضي");
        When(x => x.NewDurationMinutes.HasValue, () =>
            RuleFor(x => x.NewDurationMinutes!.Value).InclusiveBetween(15, 240));
        RuleFor(x => x.TitleOverride).MaximumLength(255);
    }
}

public sealed class RescheduleLiveSessionHandler : ICommandHandler<RescheduleLiveSessionCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public RescheduleLiveSessionHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(RescheduleLiveSessionCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var src = await _db.LiveSessions.FirstOrDefaultAsync(s => s.Id == request.SourceSessionId, ct);
        if (src is null)
            return Error.NotFound("LIVE.NOT_FOUND", "الحصة غير موجودة", "Not found");

        var isAdmin = _currentUser.IsInRole("Admin");
        if (!isAdmin && src.TeacherId != userId)
            return Error.Forbidden("LIVE.NOT_OWNER", "ليست حصتك", "Not yours");

        if (src.SubjectId is null || src.StageId is null)
            return Error.Validation("LIVE.MISSING_SUBJECT", "الحصة الأصلية تفتقد المادة أو المرحلة", "Missing data");

        var title = string.IsNullOrWhiteSpace(request.TitleOverride)
            ? src.Title
            : request.TitleOverride.Trim();

        var roomId = $"room_{Guid.NewGuid():N}".Substring(0, 24);
        var clone = new LiveSession(
            Guid.NewGuid(), src.TeacherId, title,
            src.SubjectId.Value, src.StageId.Value,
            roomId, request.NewScheduledAtUtc,
            request.NewDurationMinutes ?? src.DurationMinutes,
            lessonId: null, description: src.Description);

        _db.LiveSessions.Add(clone);
        await _db.SaveChangesAsync(ct);

        return Result.Success(clone.Id);
    }
}

// ─── Cancel (scheduled only) ──────────
public sealed record CancelLiveSessionCommand(Guid SessionId) : ICommand;

public sealed class CancelLiveSessionHandler : ICommandHandler<CancelLiveSessionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CancelLiveSessionHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(CancelLiveSessionCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var session = await _db.LiveSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId, ct);
        if (session is null)
            return Error.NotFound("LIVE.NOT_FOUND", "الحصة غير موجودة", "Not found");
        if (session.TeacherId != userId)
            return Error.Forbidden("LIVE.NOT_OWNER", "ليست حصتك", "Not yours");
        if (session.Status != LiveSessionStatus.Scheduled)
            return Error.Validation("LIVE.NOT_SCHEDULED", "لا يمكن إلغاء حصة بدأت أو انتهت", "Already started/ended");

        session.Cancel();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── End ───────────────────────────────
public sealed record EndLiveSessionCommand(Guid SessionId) : ICommand;

public sealed class EndLiveSessionHandler : ICommandHandler<EndLiveSessionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public EndLiveSessionHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(EndLiveSessionCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var session = await _db.LiveSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId, ct);
        if (session is null)
            return Error.NotFound("LIVE.NOT_FOUND", "الحصة غير موجودة", "Not found");
        if (session.TeacherId != userId)
            return Error.Forbidden("LIVE.NOT_OWNER", "ليست حصتك", "Not yours");

        session.End();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}

// ─── Start Recording (Teacher only) ────
public sealed record StartRecordingCommand(Guid SessionId) : ICommand<string>;

public sealed class StartRecordingHandler : ICommandHandler<StartRecordingCommand, string>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IEgressService _egress;

    public StartRecordingHandler(IApplicationDbContext db, ICurrentUser currentUser, IEgressService egress)
    {
        _db = db; _currentUser = currentUser; _egress = egress;
    }

    public async Task<Result<string>> Handle(StartRecordingCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var session = await _db.LiveSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId, ct);
        if (session is null)
            return Error.NotFound("LIVE.NOT_FOUND", "الحصة غير موجودة", "Not found");
        if (session.TeacherId != userId)
            return Error.Forbidden("LIVE.NOT_OWNER", "ليست حصتك", "Not yours");
        if (session.Status != LiveSessionStatus.Live)
            return Error.Validation("LIVE.NOT_LIVE", "لا يمكن التسجيل قبل بدء الحصة", "Not live");
        if (_egress.IsRecording(session.Id))
            return Error.Validation("LIVE.ALREADY_RECORDING", "التسجيل قيد التشغيل بالفعل", "Already recording");

        try
        {
            var egressId = await _egress.StartRoomRecordingAsync(session.RoomId, session.Id, ct);
            return Result.Success(egressId);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("invalid_argument") || ex.Message.Contains("output"))
        {
            // LiveKit Cloud rejects local file outputs — needs cloud storage configured.
            return Error.Validation("LIVE.EGRESS_STORAGE_MISSING",
                "التسجيل غير مفعّل. على LiveKit Cloud لازم تضبط Cloud Storage من لوحة التحكم، أو انشر السيرفر بنفسك للتسجيل المحلي.",
                "Recording requires cloud storage on LiveKit Cloud or self-hosted Egress");
        }
        catch (Exception ex)
        {
            return Error.Validation("LIVE.EGRESS_FAILED",
                "تعذّر بدء التسجيل: " + ex.Message,
                "Egress start failed: " + ex.Message);
        }
    }
}

// ─── Stop Recording (Teacher only) ─────
public sealed record StopRecordingCommand(Guid SessionId) : ICommand;

public sealed class StopRecordingHandler : ICommandHandler<StopRecordingCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IEgressService _egress;

    public StopRecordingHandler(IApplicationDbContext db, ICurrentUser currentUser, IEgressService egress)
    {
        _db = db; _currentUser = currentUser; _egress = egress;
    }

    public async Task<Result> Handle(StopRecordingCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var session = await _db.LiveSessions.FirstOrDefaultAsync(s => s.Id == request.SessionId, ct);
        if (session is null)
            return Error.NotFound("LIVE.NOT_FOUND", "الحصة غير موجودة", "Not found");
        if (session.TeacherId != userId)
            return Error.Forbidden("LIVE.NOT_OWNER", "ليست حصتك", "Not yours");

        await _egress.StopRecordingAsync(session.Id, ct);
        return Result.Success();
    }
}

// ─── Join (Student) ────────────────────
public sealed record JoinLiveSessionCommand(Guid SessionId) : ICommand<JoinLiveSessionResponse>;

public sealed record JoinLiveSessionResponse(
    Guid SessionId,
    string Title,
    string TeacherName,
    string RoomId,
    string Status,
    bool IsTeacher,
    string CurrentUserName,
    string? LiveKitUrl = null,
    string? LiveKitToken = null);

public sealed class JoinLiveSessionHandler : ICommandHandler<JoinLiveSessionCommand, JoinLiveSessionResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public JoinLiveSessionHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<JoinLiveSessionResponse>> Handle(JoinLiveSessionCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var session = await _db.LiveSessions
            .Include(s => s.Teacher)
            .Include(s => s.Attendance)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, ct);

        if (session is null)
            return Error.NotFound("LIVE.NOT_FOUND", "الحصة غير موجودة", "Not found");

        var isTeacher = session.TeacherId == userId;
        var isAdmin = _currentUser.IsInRole("Admin");

        // Access: teacher always, admin always, student needs subject subscription
        if (!isTeacher && !isAdmin)
        {
            if (session.SubjectId is null)
                return Error.Forbidden("ACCESS.DENIED", "لا توجد مادة محدّدة للحصة", "No subject");
            var hasAccess = await _access.CanAccessSubjectAsync(userId, session.SubjectId.Value, ct);
            if (!hasAccess)
                return Error.Forbidden("ACCESS.DENIED",
                    "يجب أن تكون مشتركًا في المادة لدخول الحصة المباشرة", "Subscription required");
        }

        // Record attendance (student only). Attendance is loaded above so
        // StudentJoined() can correctly detect a re-join vs first-time join.
        if (!isTeacher && session.Status == LiveSessionStatus.Live)
        {
            session.StudentJoined(userId);
            await _db.SaveChangesAsync(ct);
        }

        var fullName = await _db.Users.Where(u => u.Id == userId).Select(u => u.FullName).FirstOrDefaultAsync(ct)
                       ?? "مستخدم";

        return Result.Success(new JoinLiveSessionResponse(
            session.Id, session.Title,
            session.Teacher.FullName,
            session.RoomId,
            session.Status.ToString(),
            isTeacher, fullName));
    }
}
