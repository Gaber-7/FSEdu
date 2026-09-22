namespace FSEdu.Shared.Contracts.LiveClassroom;

public sealed record ScheduleLiveSessionRequest(
    int SubjectId,
    int StageId,
    string Title,
    string? Description,
    DateTime ScheduledAtUtc,
    int DurationMinutes
);

public sealed record LiveSessionListItemDto(
    Guid Id,
    string Title,
    string? Description,
    Guid TeacherId,
    string TeacherName,
    int SubjectId,
    string SubjectName,
    string StageName,
    string Status,         // Scheduled | Live | Ended | Cancelled
    DateTime ScheduledAtUtc,
    int DurationMinutes,
    DateTime? StartedAtUtc,
    DateTime? EndedAtUtc,
    int CurrentParticipants,
    string RoomId,
    string? RecordingUrl = null,
    string? RecordingStatus = null,
    int StageId = 0
);

public sealed record EditLiveSessionRequest(
    string Title,
    string? Description,
    int SubjectId,
    int StageId,
    DateTime ScheduledAtUtc,
    int DurationMinutes
);

public sealed record JoinLiveResponse(
    Guid SessionId,
    string Title,
    string TeacherName,
    string RoomId,
    string Status,
    bool IsTeacher,
    string CurrentUserName,
    string? LiveKitUrl = null,
    string? LiveKitToken = null
);

public sealed record LiveChatMessageDto(
    Guid SenderId,
    string SenderName,
    string Body,
    bool IsTeacher,
    DateTime SentAtUtc
);
