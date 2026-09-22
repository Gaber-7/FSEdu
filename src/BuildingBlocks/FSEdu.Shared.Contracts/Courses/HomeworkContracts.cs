namespace FSEdu.Shared.Contracts.Courses;

// ─── Teacher ──────────────────────────────────
public sealed record CreateHomeworkRequest(
    Guid CourseId,
    string Title,
    string? Description,
    string? AttachmentUrl,
    DateTime DueDateUtc,
    decimal MaxScore,
    bool PublishNow
);

public sealed record UpdateHomeworkRequest(
    string Title,
    string? Description,
    string? AttachmentUrl,
    DateTime DueDateUtc,
    decimal MaxScore
);

public sealed record TeacherHomeworkRowDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string Title,
    DateTime DueDateUtc,
    decimal MaxScore,
    bool Published,
    int TotalEnrolled,
    int SubmittedCount,
    int GradedCount
);

public sealed record TeacherHomeworkDetailDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string Title,
    string? Description,
    string? AttachmentUrl,
    DateTime DueDateUtc,
    decimal MaxScore,
    bool Published,
    TeacherHomeworkSubmissionRowDto[] Submissions
);

public sealed record TeacherHomeworkSubmissionRowDto(
    Guid SubmissionId,
    Guid StudentId,
    string StudentName,
    DateTime SubmittedAtUtc,
    bool Late,
    string? AttachmentUrl,
    string? Body,
    bool Graded,
    decimal? Score,
    string? FeedbackBody
);

public sealed record GradeHomeworkSubmissionRequest(decimal Score, string? Feedback);

// ─── Student ──────────────────────────────────
public sealed record StudentHomeworkRowDto(
    Guid HomeworkId,
    Guid CourseId,
    string CourseTitle,
    string? SubjectColor,
    string Title,
    DateTime DueDateUtc,
    decimal MaxScore,
    string Status,         // "Pending" | "Submitted" | "Late" | "Graded" | "Overdue"
    DateTime? SubmittedAtUtc,
    decimal? Score
);

public sealed record StudentHomeworkDetailDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string Title,
    string? Description,
    string? AttachmentUrl,
    DateTime DueDateUtc,
    decimal MaxScore,
    bool Overdue,
    StudentHomeworkSubmissionDto? MySubmission
);

public sealed record StudentHomeworkSubmissionDto(
    Guid Id,
    DateTime SubmittedAtUtc,
    string? Body,
    string? AttachmentUrl,
    bool Late,
    DateTime? GradedAtUtc,
    decimal? Score,
    string? FeedbackBody
);

public sealed record SubmitHomeworkRequest(string? Body, string? AttachmentUrl);
