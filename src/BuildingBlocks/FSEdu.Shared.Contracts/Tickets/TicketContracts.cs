namespace FSEdu.Shared.Contracts.Tickets;

public sealed record CreateTicketRequest(
    int SubjectId,
    string Title,        // short summary
    string Body,
    string? ImageUrl,
    string? Priority    // "Low" | "Normal" | "High" | "Urgent"
);

public sealed record ReplyToTicketRequest(
    string Body,
    string? ImageUrl,
    string? AudioUrl
);

public sealed record TicketListItemDto(
    Guid Id,
    string Title,
    string SubjectName,
    string Status,
    string Priority,
    string StudentName,
    string? AssigneeName,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc,
    int MessagesCount,
    bool HasReply
);

public sealed record TicketDetailDto(
    Guid Id,
    string Title,
    string Status,
    string Priority,
    int SubjectId,
    string SubjectName,
    Guid StudentId,
    string StudentName,
    Guid? AssigneeId,
    string? AssigneeName,
    DateTime CreatedAtUtc,
    DateTime? ResolvedAtUtc,
    TicketMessageDto[] Messages
);

public sealed record TicketMessageDto(
    long Id,
    Guid SenderId,
    string SenderName,
    string SenderRole,
    string? Body,
    string? ImageUrl,
    string? AudioUrl,
    DateTime SentAtUtc
);
