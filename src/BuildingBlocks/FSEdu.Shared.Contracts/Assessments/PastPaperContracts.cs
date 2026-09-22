namespace FSEdu.Shared.Contracts.Assessments;

public sealed record PastPaperListItemDto(
    Guid Id,
    string Title,
    int Year,
    string Term,
    string ExamType,
    string SubjectName,
    string? SubjectColor,
    string StageName,
    string? EducationalAdministration,
    int DurationMinutes,
    decimal TotalMarks,
    int QuestionCount,
    decimal? MyBestScore        // null if never attempted
);

public sealed record PastPaperBrowseFiltersDto(
    PastPaperFilterOption[] Subjects,
    PastPaperFilterOption[] Stages,
    int[] Years,
    string[] Terms,
    string[] ExamTypes
);

public sealed record PastPaperFilterOption(int Id, string Name);

public sealed record PastPaperBrowseQuery(
    int? SubjectId,
    int? StageId,
    int? Year,
    string? Term,
    string? ExamType
);

public sealed record StartPastPaperAttemptResponse(
    Guid AttemptId,
    Guid PastPaperId,
    string Title,
    int DurationMinutes,
    DateTime StartedAtUtc,
    PastPaperAttemptQuestionDto[] Questions
);

public sealed record PastPaperAttemptQuestionDto(
    Guid Id,
    string Body,
    string Type,
    string[]? Options,        // shuffled for the student
    decimal Marks,
    int OrderNum
);

public sealed record SubmitPastPaperAttemptRequest(
    Guid AttemptId,
    PastPaperAnswerDto[] Answers
);

public sealed record PastPaperAnswerDto(Guid QuestionId, string AnswerJson);

public sealed record PastPaperAttemptResultDto(
    Guid AttemptId,
    Guid PastPaperId,
    string Title,
    decimal Score,
    decimal MaxScore,
    decimal PercentageScore,
    DateTime StartedAtUtc,
    DateTime SubmittedAtUtc,
    PastPaperReviewQuestionDto[] Questions
);

public sealed record PastPaperReviewQuestionDto(
    Guid Id,
    string Body,
    string Type,
    string[]? Options,
    string CorrectAnswerJson,
    string? GivenAnswerJson,
    bool IsCorrect,
    decimal Marks,
    decimal AwardedMarks,
    string? Explanation,
    int OrderNum
);
