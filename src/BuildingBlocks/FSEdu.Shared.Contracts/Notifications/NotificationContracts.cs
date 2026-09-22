namespace FSEdu.Shared.Contracts.Notifications;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Body,
    string? DataJson,
    bool IsRead,
    DateTime CreatedAtUtc
);

public sealed record UnreadCountDto(int Count);
