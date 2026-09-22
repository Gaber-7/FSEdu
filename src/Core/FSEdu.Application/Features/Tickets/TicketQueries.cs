using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Tickets;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Tickets;

public sealed record GetMyTicketsQuery() : IQuery<List<TicketListItemDto>>;

public sealed class GetMyTicketsHandler : IQueryHandler<GetMyTicketsQuery, List<TicketListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyTicketsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<TicketListItemDto>>> Handle(GetMyTicketsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;

        var tickets = await _db.AskTickets
            .Where(t => t.StudentId == userId)
            .Include(t => t.Subject)
            .Include(t => t.Student)
            .Include(t => t.Assignee)
            .Include(t => t.Messages)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(ct);

        var list = tickets.Select(t => new TicketListItemDto(
            t.Id,
            FirstLine(t.Messages.OrderBy(m => m.SentAtUtc).FirstOrDefault()?.Body) ?? "(بدون عنوان)",
            t.Subject.NameAr,
            t.Status.ToString(),
            t.Priority.ToString(),
            t.Student.FullName,
            t.Assignee?.FullName,
            t.CreatedAtUtc,
            t.ResolvedAtUtc,
            t.Messages.Count,
            t.Messages.Any(m => m.SenderId != t.StudentId)
        )).ToList();

        return Result.Success(list);
    }

    private static string? FirstLine(string? body)
        => string.IsNullOrEmpty(body) ? null : body.Split('\n')[0].Trim();
}

// ─── Teacher / Assistant / Admin: assigned + unassigned tickets ───
public sealed record GetAssignedTicketsQuery() : IQuery<List<TicketListItemDto>>;

public sealed class GetAssignedTicketsHandler : IQueryHandler<GetAssignedTicketsQuery, List<TicketListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetAssignedTicketsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<TicketListItemDto>>> Handle(GetAssignedTicketsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var isAdmin = _currentUser.IsInRole("Admin");
        var isAssistant = _currentUser.IsInRole("Assistant");

        IQueryable<Domain.Support.AskTicket> q = _db.AskTickets;

        if (isAdmin || isAssistant)
        {
            // Admin/Assistant see ALL open tickets
            q = q.Where(t => t.Status != TicketStatus.Closed);
        }
        else
        {
            // Teachers see assigned to them OR unassigned tickets in their subjects
            var mySubjectIds = _db.TeacherSubjects.Where(ts => ts.TeacherId == userId).Select(ts => ts.SubjectId);
            q = q.Where(t => t.AssignedTo == userId
                          || (t.AssignedTo == null && mySubjectIds.Contains(t.SubjectId)));
        }

        var tickets = await q
            .Include(t => t.Subject)
            .Include(t => t.Student)
            .Include(t => t.Assignee)
            .Include(t => t.Messages)
            .OrderByDescending(t => t.Priority)
            .ThenByDescending(t => t.CreatedAtUtc)
            .Take(200)
            .ToListAsync(ct);

        var list = tickets.Select(t => new TicketListItemDto(
            t.Id,
            FirstLine(t.Messages.OrderBy(m => m.SentAtUtc).FirstOrDefault()?.Body) ?? "(بدون عنوان)",
            t.Subject.NameAr,
            t.Status.ToString(),
            t.Priority.ToString(),
            t.Student.FullName,
            t.Assignee?.FullName,
            t.CreatedAtUtc,
            t.ResolvedAtUtc,
            t.Messages.Count,
            t.Messages.Any(m => m.SenderId != t.StudentId)
        )).ToList();

        return Result.Success(list);
    }

    private static string? FirstLine(string? body)
        => string.IsNullOrEmpty(body) ? null : body.Split('\n')[0].Trim();
}

// ─── Get Ticket Details (full conversation) ───
public sealed record GetTicketDetailsQuery(Guid TicketId) : IQuery<TicketDetailDto>;

public sealed class GetTicketDetailsHandler : IQueryHandler<GetTicketDetailsQuery, TicketDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetTicketDetailsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<TicketDetailDto>> Handle(GetTicketDetailsQuery request, CancellationToken ct)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;

        var ticket = await _db.AskTickets
            .Include(t => t.Subject)
            .Include(t => t.Student)
            .Include(t => t.Assignee)
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, ct);

        if (ticket is null)
            return Error.NotFound("TICKET.NOT_FOUND", "السؤال غير موجود", "Not found");

        var canView = ticket.StudentId == userId
                    || ticket.AssignedTo == userId
                    || _currentUser.IsInRole("Admin")
                    || _currentUser.IsInRole("Assistant");
        if (!canView)
            return Error.Forbidden("ACCESS.DENIED", "ليس لديك صلاحية", "Access denied");

        // Resolve sender names (cache to avoid N+1)
        var senderIds = ticket.Messages.Select(m => m.SenderId).Distinct().ToList();
        var senders = await _db.Users
            .Where(u => senderIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToListAsync(ct);
        var senderMap = senders.ToDictionary(s => s.Id, s => s.FullName);

        var messages = ticket.Messages
            .OrderBy(m => m.SentAtUtc)
            .Select(m =>
            {
                var images = string.IsNullOrEmpty(m.ImageUrlsCsv)
                    ? null
                    : m.ImageUrlsCsv.Split('|', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                return new TicketMessageDto(
                    m.Id, m.SenderId,
                    senderMap.GetValueOrDefault(m.SenderId, "مستخدم"),
                    m.SenderId == ticket.StudentId ? "Student" : "Staff",
                    m.Body, images, m.AudioUrl, m.SentAtUtc);
            })
            .ToArray();

        var title = FirstLine(messages.FirstOrDefault()?.Body) ?? "(بدون عنوان)";

        return Result.Success(new TicketDetailDto(
            ticket.Id, title,
            ticket.Status.ToString(),
            ticket.Priority.ToString(),
            ticket.SubjectId, ticket.Subject.NameAr,
            ticket.StudentId, ticket.Student.FullName,
            ticket.AssignedTo, ticket.Assignee?.FullName,
            ticket.CreatedAtUtc, ticket.ResolvedAtUtc,
            messages));
    }

    private static string? FirstLine(string? body)
        => string.IsNullOrEmpty(body) ? null : body.Split('\n')[0].Trim();
}
