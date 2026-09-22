namespace FSEdu.Shared.Contracts.Assessments;

// ─── Teacher: Create / Edit ──────────────────────────
public sealed record CreateAssessmentRequest(
    Guid CourseId,
    string Type,                 // "Quiz" | "Assignment" | "Exam"
    string Title,
    string? Description,
    int? TimeLimitMinutes,
    decimal PassingMarks,
    int AttemptsAllowed,
    DateTime? AvailableFromUtc,
    DateTime? AvailableToUtc
);

public sealed record AddQuestionRequest(
    string Type,                 // "MultipleChoice" | "TrueFalse"
    string Difficulty,           // "Easy" | "Medium" | "Hard"
    string QuestionText,
    string[]? Options,           // MCQ only
    int? CorrectOptionIndex,     // MCQ only
    bool? CorrectBoolean,        // True/False only
    string? Explanation,
    decimal Marks
);

public sealed record AssessmentListItemDto(
    Guid Id,
    string Title,
    string Type,
    int QuestionsCount,
    decimal TotalMarks,
    int? TimeLimitMinutes,
    DateTime? AvailableFromUtc,
    DateTime? AvailableToUtc,
    bool IsAvailable
);

public sealed record AssessmentDetailDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string Title,
    string? Description,
    string Type,
    decimal TotalMarks,
    decimal PassingMarks,
    int? TimeLimitMinutes,
    int AttemptsAllowed,
    DateTime? AvailableFromUtc,
    DateTime? AvailableToUtc,
    QuestionForEditDto[] Questions
);

public sealed record QuestionForEditDto(
    Guid Id,
    string Type,
    string Difficulty,
    string QuestionText,
    string[]? Options,
    int? CorrectOptionIndex,
    bool? CorrectBoolean,
    string? Explanation,
    decimal Marks,
    int OrderNum
);

// ─── Student: Take Assessment ─────────────────────────
public sealed record StartAttemptResponse(
    Guid AttemptId,
    Guid AssessmentId,
    string AssessmentTitle,
    string Type,
    DateTime StartedAtUtc,
    int? TimeLimitMinutes,
    DateTime? DeadlineAtUtc,
    decimal TotalMarks,
    QuestionForTakingDto[] Questions
);

public sealed record QuestionForTakingDto(
    Guid Id,
    string Type,
    string QuestionText,
    string[]? Options,           // empty/null for True/False
    int OrderNum,
    decimal Marks
);

public sealed record SubmitAttemptRequest(AnswerDto[] Answers);

public sealed record AnswerDto(
    Guid QuestionId,
    int? SelectedOptionIndex,    // MCQ
    bool? SelectedBoolean,       // True/False
    string? TextAnswer           // Short answer (future)
);

public sealed record AttemptResultDto(
    Guid Id,
    Guid AssessmentId,
    string AssessmentTitle,
    decimal Score,
    decimal TotalMarks,
    decimal Percentage,
    bool Passed,
    string Status,
    DateTime SubmittedAtUtc,
    QuestionResultDto[] Questions
);

public sealed record QuestionResultDto(
    Guid QuestionId,
    string QuestionText,
    string Type,
    bool IsCorrect,
    decimal MarksEarned,
    decimal MaxMarks,
    string[]? Options,
    int? UserSelectedOption,
    int? CorrectOption,
    bool? UserSelectedBoolean,
    bool? CorrectBoolean,
    string? Explanation
);
