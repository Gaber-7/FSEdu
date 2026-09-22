using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Domain.Support;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Tickets;

public sealed record CreateTicketCommand(
    int SubjectId,
    string Title,
    string Body,
    string? ImageUrl,
    string? Priority
) : ICommand<Guid>;

public sealed class CreateTicketValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(255)
            .WithMessage("عنوان السؤال يجب أن يكون بين 3 و255 حرف");
        RuleFor(x => x.Body).NotEmpty().MinimumLength(5)
            .WithMessage("نص السؤال يجب ألا يقل عن 5 أحرف");
        RuleFor(x => x.SubjectId).GreaterThan(0);
    }
}

public sealed class CreateTicketHandler : ICommandHandler<CreateTicketCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;
    private readonly INotificationService _notifications;

    public CreateTicketHandler(IApplicationDbContext db, ICurrentUser currentUser,
                                IAccessPolicy access, INotificationService notifications)
    {
        _db = db; _currentUser = currentUser; _access = access; _notifications = notifications;
    }

    public async Task<Result<Guid>> Handle(CreateTicketCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;

        var student = await _db.Students.FirstOrDefaultAsync(s => s.Id == studentId, ct);
        if (student is null)
            return Error.Forbidden("AUTH.NOT_STUDENT", "هذا الحساب ليس طالبًا", "Not a student");

        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == request.SubjectId, ct);
        if (subject is null)
            return Error.NotFound("SUBJECT.NOT_FOUND", "المادة غير موجودة", "Subject not found");

        var hasAccess = await _access.CanAccessSubjectAsync(studentId, subject.Id, ct);
        if (!hasAccess)
            return Error.Forbidden("ACCESS.DENIED",
                "يجب أن تكون مشتركًا في المادة لإرسال سؤال",
                "Subscription required to ask in this subject");

        var priority = request.Priority switch
        {
            "Low" => TicketPriority.Low,
            "High" => TicketPriority.High,
            "Urgent" => TicketPriority.Urgent,
            _ => TicketPriority.Normal
        };

        var ticket = new AskTicket(Guid.NewGuid(), studentId, subject.Id, priority);

        // Auto-assign to a verified teacher of this subject (round-robin or first available)
        var teacherId = await _db.TeacherSubjects
            .Where(ts => ts.SubjectId == subject.Id)
            .Join(_db.Teachers, ts => ts.TeacherId, t => t.Id, (ts, t) => t)
            .Where(t => t.Verified && t.Status == UserStatus.Active)
            .OrderBy(t => t.Id) // simple round-robin placeholder
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);

        if (teacherId.HasValue) ticket.Assign(teacherId.Value);

        // Build initial body from title + body
        var fullBody = string.IsNullOrEmpty(request.Body)
            ? request.Title
            : $"{request.Title}\n\n{request.Body}";

        ticket.AddMessage(studentId, fullBody, audioUrl: null,
            imageUrls: string.IsNullOrEmpty(request.ImageUrl) ? null : new[] { request.ImageUrl });

        _db.AskTickets.Add(ticket);
        await _db.SaveChangesAsync(ct);

        // Notify the assigned teacher (if any)
        if (teacherId.HasValue)
        {
            await _notifications.SendAsync(teacherId.Value,
                NotificationTypes.TicketCreated,
                "❓ سؤال جديد من طالب",
                $"\"{request.Title}\" — في مادة {subject.NameAr}",
                new { ticketId = ticket.Id, url = $"/tickets/{ticket.Id}" },
                ct);
        }

        return Result.Success(ticket.Id);
    }
}

// ─── Reply ─────────────────────────────────
public sealed record ReplyToTicketCommand(Guid TicketId, string Body, string? ImageUrl, string? AudioUrl) : ICommand;

public sealed class ReplyToTicketValidator : AbstractValidator<ReplyToTicketCommand>
{
    public ReplyToTicketValidator()
    {
        RuleFor(x => x.Body).NotEmpty().MinimumLength(2);
    }
}

public sealed class ReplyToTicketHandler : ICommandHandler<ReplyToTicketCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public ReplyToTicketHandler(IApplicationDbContext db, ICurrentUser currentUser, INotificationService notifications)
    {
        _db = db; _currentUser = currentUser; _notifications = notifications;
    }

    public async Task<Result> Handle(ReplyToTicketCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var ticket = await _db.AskTickets.FirstOrDefaultAsync(t => t.Id == request.TicketId, ct);
        if (ticket is null)
            return Error.NotFound("TICKET.NOT_FOUND", "السؤال غير موجود", "Not found");

        var canReply = ticket.StudentId == userId
                    || ticket.AssignedTo == userId
                    || _currentUser.IsInRole("Admin")
                    || _currentUser.IsInRole("Assistant");
        if (!canReply)
            return Error.Forbidden("ACCESS.DENIED", "ليس لديك صلاحية الرد", "Cannot reply");

        if (ticket.AssignedTo is null && (_currentUser.IsInRole("Teacher")
                || _currentUser.IsInRole("Assistant") || _currentUser.IsInRole("Admin")))
            ticket.Assign(userId);

        ticket.AddMessage(userId, request.Body,
            audioUrl: request.AudioUrl,
            imageUrls: string.IsNullOrEmpty(request.ImageUrl) ? null : new[] { request.ImageUrl });

        await _db.SaveChangesAsync(ct);

        // Notify the OTHER party (student if staff replied, or teacher if student replied)
        var notifyUserId = userId == ticket.StudentId
            ? ticket.AssignedTo
            : (Guid?)ticket.StudentId;

        if (notifyUserId.HasValue)
        {
            var preview = request.Body.Length > 80 ? request.Body[..80] + "..." : request.Body;
            await _notifications.SendAsync(notifyUserId.Value,
                NotificationTypes.TicketReplied,
                userId == ticket.StudentId ? "💬 رد جديد من الطالب" : "✓ رد جديد على سؤالك",
                preview,
                new { ticketId = ticket.Id, url = $"/tickets/{ticket.Id}" },
                ct);
        }

        return Result.Success();
    }
}

// ─── Resolve ─────────────────────────────────
public sealed record ResolveTicketCommand(Guid TicketId) : ICommand;

public sealed class ResolveTicketHandler : ICommandHandler<ResolveTicketCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ResolveTicketHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(ResolveTicketCommand request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        var ticket = await _db.AskTickets.FirstOrDefaultAsync(t => t.Id == request.TicketId, ct);
        if (ticket is null)
            return Error.NotFound("TICKET.NOT_FOUND", "السؤال غير موجود", "Not found");

        var canResolve = ticket.StudentId == userId
                       || ticket.AssignedTo == userId
                       || _currentUser.IsInRole("Admin");
        if (!canResolve)
            return Error.Forbidden("ACCESS.DENIED", "ليس لديك صلاحية", "Cannot resolve");

        ticket.Resolve();
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
