using System.Text.Json;
using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Assessments;
using FSEdu.Domain.Common;
using FSEdu.Domain.Engagement;
using FSEdu.Shared.Contracts.Assessments;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.PastPapers;

// ─── Start attempt ─────────────────────────────
public sealed record StartPastPaperAttemptCommand(Guid PastPaperId) : ICommand<StartPastPaperAttemptResponse>;

public sealed class StartPastPaperAttemptHandler
    : ICommandHandler<StartPastPaperAttemptCommand, StartPastPaperAttemptResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public StartPastPaperAttemptHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result<StartPastPaperAttemptResponse>> Handle(StartPastPaperAttemptCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var paper = await _db.PastPapers
            .Include(p => p.Questions)
            .FirstOrDefaultAsync(p => p.Id == request.PastPaperId && p.Published, ct);
        if (paper is null)
            return Error.NotFound("PAPER.NOT_FOUND", "الامتحان غير موجود", "Past paper not found");
        if (paper.Questions.Count == 0)
            return Error.Validation("PAPER.EMPTY", "هذا الامتحان لا يحتوى على أسئلة بعد", "Empty paper");

        var attempt = new PastPaperAttempt(Guid.NewGuid(), paper.Id, studentId, paper.TotalMarks);
        _db.PastPaperAttempts.Add(attempt);
        await _db.SaveChangesAsync(ct);

        // Shuffle MCQ option order so students can't memorise positions across attempts
        var rnd = new Random(attempt.Id.GetHashCode());
        var questions = paper.Questions.OrderBy(q => q.OrderNum).Select(q =>
        {
            string[]? options = null;
            if (!string.IsNullOrEmpty(q.OptionsJson))
            {
                try
                {
                    options = JsonSerializer.Deserialize<string[]>(q.OptionsJson);
                    if (options is not null && options.Length > 1)
                        options = options.OrderBy(_ => rnd.Next()).ToArray();
                }
                catch { options = null; }
            }
            return new PastPaperAttemptQuestionDto(
                q.Id, q.Body, q.Type.ToString(), options, q.Marks, q.OrderNum);
        }).ToArray();

        return Result.Success(new StartPastPaperAttemptResponse(
            attempt.Id, paper.Id, paper.Title, paper.DurationMinutes,
            attempt.StartedAtUtc, questions));
    }
}

// ─── Submit attempt ────────────────────────────
public sealed record SubmitPastPaperAttemptCommand(
    Guid AttemptId,
    List<PastPaperAnswerDto> Answers
) : ICommand<PastPaperAttemptResultDto>;

public sealed class SubmitPastPaperAttemptValidator : AbstractValidator<SubmitPastPaperAttemptCommand>
{
    public SubmitPastPaperAttemptValidator()
    {
        RuleFor(x => x.AttemptId).NotEmpty();
        RuleFor(x => x.Answers).NotNull();
    }
}

public sealed class SubmitPastPaperAttemptHandler
    : ICommandHandler<SubmitPastPaperAttemptCommand, PastPaperAttemptResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IDailyChallengeService _dailyChallenge;

    public SubmitPastPaperAttemptHandler(IApplicationDbContext db, ICurrentUser currentUser,
        IDailyChallengeService dailyChallenge)
    { _db = db; _currentUser = currentUser; _dailyChallenge = dailyChallenge; }

    public async Task<Result<PastPaperAttemptResultDto>> Handle(SubmitPastPaperAttemptCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var attempt = await _db.PastPaperAttempts
            .Include(a => a.PastPaper).ThenInclude(p => p.Questions)
            .FirstOrDefaultAsync(a => a.Id == request.AttemptId, ct);
        if (attempt is null)
            return Error.NotFound("ATTEMPT.NOT_FOUND", "المحاولة غير موجودة", "Attempt not found");
        if (attempt.StudentId != _currentUser.UserId.Value)
            return Error.Forbidden("ATTEMPT.NOT_OWNER", "ليست محاولتك", "Not your attempt");
        if (attempt.Status != AttemptStatus.InProgress)
            return Error.Validation("ATTEMPT.ALREADY_SUBMITTED",
                "تم تسليم هذه المحاولة بالفعل", "Already submitted");

        // Auto-grade objective questions (MCQ, TrueFalse, ShortAnswer with exact match, FillBlank).
        // Essay and other manual types are not part of past papers since past papers are objective.
        var answersByQ = request.Answers.ToDictionary(a => a.QuestionId, a => a.AnswerJson);
        decimal totalScore = 0m;
        var reviewQuestions = new List<PastPaperReviewQuestionDto>();

        foreach (var q in attempt.PastPaper.Questions.OrderBy(x => x.OrderNum))
        {
            answersByQ.TryGetValue(q.Id, out var given);
            bool isCorrect = IsCorrect(q, given);
            decimal awarded = isCorrect ? q.Marks : 0m;
            totalScore += awarded;

            string[]? options = null;
            if (!string.IsNullOrEmpty(q.OptionsJson))
            {
                try { options = JsonSerializer.Deserialize<string[]>(q.OptionsJson); }
                catch { options = null; }
            }

            reviewQuestions.Add(new PastPaperReviewQuestionDto(
                q.Id, q.Body, q.Type.ToString(), options,
                q.CorrectAnswerJson, given,
                isCorrect, q.Marks, awarded, q.Explanation, q.OrderNum));
        }

        attempt.Submit(JsonSerializer.Serialize(request.Answers), totalScore);
        await _db.SaveChangesAsync(ct);

        await _dailyChallenge.RecordProgressAsync(attempt.StudentId, DailyChallengeType.SolvePastPaper, 1, ct);

        var percentage = attempt.MaxScore == 0 ? 0 : Math.Round(totalScore / attempt.MaxScore * 100m, 1);

        return Result.Success(new PastPaperAttemptResultDto(
            attempt.Id, attempt.PastPaperId, attempt.PastPaper.Title,
            totalScore, attempt.MaxScore, percentage,
            attempt.StartedAtUtc, attempt.SubmittedAtUtc!.Value,
            reviewQuestions.ToArray()));
    }

    private static bool IsCorrect(PastPaperQuestion q, string? given)
    {
        if (string.IsNullOrWhiteSpace(given)) return false;
        var correct = q.CorrectAnswerJson?.Trim() ?? "";
        var supplied = given.Trim();

        // Normalize JSON-encoded strings (both could be "\"a\"" or just "a")
        var normCorrect = TryUnquote(correct);
        var normSupplied = TryUnquote(supplied);

        return string.Equals(normCorrect, normSupplied, StringComparison.OrdinalIgnoreCase);
    }

    private static string TryUnquote(string s)
    {
        if (s.Length >= 2 && s[0] == '"' && s[^1] == '"')
        {
            try { return JsonSerializer.Deserialize<string>(s) ?? s; }
            catch { return s.Trim('"'); }
        }
        return s;
    }
}

// ─── Get attempt result (for review later) ─────
public sealed record GetPastPaperAttemptResultQuery(Guid AttemptId) : IQuery<PastPaperAttemptResultDto>;

public sealed class GetPastPaperAttemptResultHandler
    : IQueryHandler<GetPastPaperAttemptResultQuery, PastPaperAttemptResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetPastPaperAttemptResultHandler(IApplicationDbContext db, ICurrentUser currentUser)
    { _db = db; _currentUser = currentUser; }

    public async Task<Result<PastPaperAttemptResultDto>> Handle(GetPastPaperAttemptResultQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var attempt = await _db.PastPaperAttempts
            .Include(a => a.PastPaper).ThenInclude(p => p.Questions)
            .FirstOrDefaultAsync(a => a.Id == request.AttemptId, ct);
        if (attempt is null || attempt.StudentId != _currentUser.UserId.Value)
            return Error.NotFound("ATTEMPT.NOT_FOUND", "المحاولة غير موجودة", "Not found");
        if (attempt.SubmittedAtUtc is null)
            return Error.Validation("ATTEMPT.NOT_SUBMITTED", "لم يتم تسليم هذه المحاولة بعد", "Not submitted");

        Dictionary<Guid, string>? givenAnswers = null;
        if (!string.IsNullOrEmpty(attempt.AnswersJson))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<PastPaperAnswerDto>>(attempt.AnswersJson);
                givenAnswers = list?.ToDictionary(x => x.QuestionId, x => x.AnswerJson);
            }
            catch { givenAnswers = null; }
        }

        var reviewQuestions = attempt.PastPaper.Questions.OrderBy(q => q.OrderNum).Select(q =>
        {
            givenAnswers?.TryGetValue(q.Id, out var given);
            string? givenAns = givenAnswers is not null && givenAnswers.TryGetValue(q.Id, out var g) ? g : null;
            bool isCorrect = IsCorrect(q.CorrectAnswerJson, givenAns);
            decimal awarded = isCorrect ? q.Marks : 0m;
            string[]? options = null;
            if (!string.IsNullOrEmpty(q.OptionsJson))
            {
                try { options = JsonSerializer.Deserialize<string[]>(q.OptionsJson); }
                catch { options = null; }
            }
            return new PastPaperReviewQuestionDto(
                q.Id, q.Body, q.Type.ToString(), options,
                q.CorrectAnswerJson, givenAns, isCorrect,
                q.Marks, awarded, q.Explanation, q.OrderNum);
        }).ToArray();

        var percentage = attempt.MaxScore == 0 ? 0 : Math.Round(attempt.Score / attempt.MaxScore * 100m, 1);
        return Result.Success(new PastPaperAttemptResultDto(
            attempt.Id, attempt.PastPaperId, attempt.PastPaper.Title,
            attempt.Score, attempt.MaxScore, percentage,
            attempt.StartedAtUtc, attempt.SubmittedAtUtc.Value, reviewQuestions));
    }

    private static bool IsCorrect(string correct, string? given)
    {
        if (string.IsNullOrWhiteSpace(given)) return false;
        return string.Equals(correct.Trim().Trim('"'), given.Trim().Trim('"'), StringComparison.OrdinalIgnoreCase);
    }
}
