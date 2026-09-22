using FSEdu.Domain.Academic;
using FSEdu.Domain.Common;
using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Support;

public sealed class AskTicket : AggregateRoot<Guid>, IAuditable
{
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;
    public int SubjectId { get; private set; }
    public Subject Subject { get; private set; } = default!;
    public Guid? AssignedTo { get; private set; }
    public AppUser? Assignee { get; private set; }
    public TicketStatus Status { get; private set; } = TicketStatus.Open;
    public TicketPriority Priority { get; private set; } = TicketPriority.Normal;
    public DateTime? ResolvedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    private readonly List<TicketMessage> _messages = new();
    public IReadOnlyCollection<TicketMessage> Messages => _messages.AsReadOnly();

    private AskTicket() { }

    public AskTicket(Guid id, Guid studentId, int subjectId, TicketPriority priority = TicketPriority.Normal)
        : base(id)
    {
        StudentId = studentId;
        SubjectId = subjectId;
        Priority = priority;
    }

    public void Assign(Guid userId) => AssignedTo = userId;

    public void AddMessage(Guid senderId, string? body, string? audioUrl = null, string[]? imageUrls = null)
    {
        _messages.Add(new TicketMessage(Id, senderId, body, audioUrl, imageUrls));
        if (Status == TicketStatus.Open && senderId != StudentId) Status = TicketStatus.Answered;
    }

    public void Resolve() { Status = TicketStatus.Resolved; ResolvedAtUtc = DateTime.UtcNow; }
    public void Close() => Status = TicketStatus.Closed;
}

public sealed class TicketMessage : Entity<long>
{
    public Guid TicketId { get; private set; }
    public Guid SenderId { get; private set; }
    public string? Body { get; private set; }
    public string? AudioUrl { get; private set; }
    public string? ImageUrlsCsv { get; private set; }
    public DateTime SentAtUtc { get; private set; }

    private TicketMessage() { }

    public TicketMessage(Guid ticketId, Guid senderId, string? body, string? audioUrl, string[]? imageUrls)
    {
        TicketId = ticketId;
        SenderId = senderId;
        Body = body;
        AudioUrl = audioUrl;
        ImageUrlsCsv = imageUrls is { Length: > 0 } ? string.Join("|", imageUrls) : null;
        SentAtUtc = DateTime.UtcNow;
    }
}
