using System.Text.Json;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Assessments;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Assessments.Queries;

// ─── List assessments for a course ────────────────
public sealed record GetCourseAssessmentsQuery(Guid CourseId) : IQuery<List<AssessmentListItemDto>>;

public sealed class GetCourseAssessmentsHandler : IQueryHandler<GetCourseAssessmentsQuery, List<AssessmentListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetCourseAssessmentsHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<List<AssessmentListItemDto>>> Handle(GetCourseAssessmentsQuery request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var list = await _db.Assessments
            .Where(a => a.CourseId == request.CourseId)
            .Include(a => a.Questions)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Select(a => new AssessmentListItemDto(
                a.Id, a.Title, a.Type.ToString(),
                a.Questions.Count, a.TotalMarks,
                a.TimeLimitMinutes,
                a.AvailableFromUtc, a.AvailableToUtc,
                (a.AvailableFromUtc == null || a.AvailableFromUtc <= now)
                    && (a.AvailableToUtc == null || a.AvailableToUtc >= now)
            ))
            .ToListAsync(ct);

        return Result.Success(list);
    }
}

// ─── Assessment details (for teacher editing) ───────
public sealed record GetAssessmentDetailsQuery(Guid AssessmentId) : IQuery<AssessmentDetailDto>;

public sealed class GetAssessmentDetailsHandler : IQueryHandler<GetAssessmentDetailsQuery, AssessmentDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetAssessmentDetailsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<AssessmentDetailDto>> Handle(GetAssessmentDetailsQuery request, CancellationToken ct)
    {
        var assessment = await _db.Assessments
            .Include(a => a.Course)
            .Include(a => a.Questions).ThenInclude(aq => aq.Question)
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId, ct);

        if (assessment is null)
            return Error.NotFound("ASSESSMENT.NOT_FOUND", "الاختبار غير موجود", "Not found");

        var isOwner = _currentUser.UserId == assessment.TeacherId;
        var isAdmin = _currentUser.IsInRole("Admin");

        var questions = assessment.Questions.OrderBy(q => q.OrderNum).Select(aq =>
        {
            var q = aq.Question;
            using var doc = JsonDocument.Parse(q.QuestionJson);
            using var aDoc = JsonDocument.Parse(q.CorrectAnswerJson);
            var root = doc.RootElement;
            var aRoot = aDoc.RootElement;

            var text = root.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
            string[]? options = null;
            int? correctOpt = null;
            bool? correctBool = null;

            if (q.Type == Domain.Common.QuestionType.MultipleChoice)
            {
                if (root.TryGetProperty("options", out var o))
                    options = o.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
                if (isOwner || isAdmin)
                    if (aRoot.TryGetProperty("correct", out var c) && c.ValueKind == JsonValueKind.Number)
                        correctOpt = c.GetInt32();
            }
            else if (q.Type == Domain.Common.QuestionType.TrueFalse)
            {
                if (isOwner || isAdmin)
                    if (aRoot.TryGetProperty("correct", out var c))
                        correctBool = c.ValueKind == JsonValueKind.True || c.ValueKind == JsonValueKind.False
                            ? c.GetBoolean() : null;
            }

            return new QuestionForEditDto(
                q.Id, q.Type.ToString(), q.Difficulty.ToString(),
                text, options, correctOpt, correctBool,
                isOwner || isAdmin ? q.Explanation : null,
                aq.Marks, aq.OrderNum);
        }).ToArray();

        return Result.Success(new AssessmentDetailDto(
            assessment.Id,
            assessment.CourseId ?? Guid.Empty,
            assessment.Course?.Title ?? "",
            assessment.Title,
            assessment.Description,
            assessment.Type.ToString(),
            assessment.TotalMarks,
            assessment.PassingMarks,
            assessment.TimeLimitMinutes,
            assessment.AttemptsAllowed,
            assessment.AvailableFromUtc,
            assessment.AvailableToUtc,
            questions));
    }
}

// ─── Get attempt result ─────────────────────────────
public sealed record GetAttemptResultQuery(Guid AttemptId) : IQuery<AttemptResultDto>;

public sealed class GetAttemptResultHandler : IQueryHandler<GetAttemptResultQuery, AttemptResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetAttemptResultHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<AttemptResultDto>> Handle(GetAttemptResultQuery request, CancellationToken ct)
    {
        var attempt = await _db.AssessmentAttempts
            .Include(a => a.Assessment).ThenInclude(a => a.Questions).ThenInclude(aq => aq.Question)
            .FirstOrDefaultAsync(a => a.Id == request.AttemptId, ct);

        if (attempt is null)
            return Error.NotFound("ATTEMPT.NOT_FOUND", "المحاولة غير موجودة", "Not found");

        var userId = _currentUser.UserId ?? Guid.Empty;
        if (attempt.StudentId != userId && !_currentUser.IsInRole("Admin")
            && userId != attempt.Assessment.TeacherId)
            return Error.Forbidden("ACCESS.DENIED", "ليس لديك صلاحية عرض هذه النتيجة", "Access denied");

        if (attempt.Status == AttemptStatus.InProgress)
            return Error.Validation("ATTEMPT.IN_PROGRESS", "المحاولة لم تُقدَّم بعد", "Not submitted");

        // Reconstruct answers from saved JSON
        Dictionary<Guid, AnswerDto> answersByQ = new();
        if (!string.IsNullOrEmpty(attempt.AnswersJson))
        {
            var answers = JsonSerializer.Deserialize<AnswerDto[]>(attempt.AnswersJson) ?? Array.Empty<AnswerDto>();
            answersByQ = answers.ToDictionary(a => a.QuestionId, a => a);
        }

        var questionResults = new List<QuestionResultDto>();

        foreach (var aq in attempt.Assessment.Questions.OrderBy(q => q.OrderNum))
        {
            var q = aq.Question;
            using var qDoc = JsonDocument.Parse(q.QuestionJson);
            using var aDoc = JsonDocument.Parse(q.CorrectAnswerJson);
            var qRoot = qDoc.RootElement;
            var aRoot = aDoc.RootElement;

            var text = qRoot.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
            string[]? options = null;
            int? correctOpt = null;
            bool? correctBool = null;
            bool isCorrect = false;
            decimal earned = 0;

            var userAnswer = answersByQ.GetValueOrDefault(q.Id);

            if (q.Type == Domain.Common.QuestionType.MultipleChoice)
            {
                if (qRoot.TryGetProperty("options", out var o))
                    options = o.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
                if (aRoot.TryGetProperty("correct", out var c) && c.ValueKind == JsonValueKind.Number)
                    correctOpt = c.GetInt32();
                if (userAnswer?.SelectedOptionIndex is int p && p == correctOpt)
                { isCorrect = true; earned = aq.Marks; }
            }
            else if (q.Type == Domain.Common.QuestionType.TrueFalse)
            {
                if (aRoot.TryGetProperty("correct", out var c))
                    correctBool = c.ValueKind == JsonValueKind.True || c.ValueKind == JsonValueKind.False
                        ? c.GetBoolean() : null;
                if (userAnswer?.SelectedBoolean is bool p && p == correctBool)
                { isCorrect = true; earned = aq.Marks; }
            }

            questionResults.Add(new QuestionResultDto(
                q.Id, text, q.Type.ToString(), isCorrect, earned, aq.Marks, options,
                userAnswer?.SelectedOptionIndex, correctOpt,
                userAnswer?.SelectedBoolean, correctBool,
                q.Explanation));
        }

        var percentage = attempt.Assessment.TotalMarks > 0
            ? Math.Round(attempt.Score / attempt.Assessment.TotalMarks * 100, 2)
            : 0;
        var passed = attempt.Score >= attempt.Assessment.PassingMarks;

        return Result.Success(new AttemptResultDto(
            attempt.Id, attempt.AssessmentId, attempt.Assessment.Title,
            attempt.Score, attempt.Assessment.TotalMarks, percentage, passed,
            attempt.Status.ToString(),
            attempt.SubmittedAtUtc ?? DateTime.UtcNow,
            questionResults.ToArray()));
    }
}
