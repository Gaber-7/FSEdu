using System.Text.Json;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Assessments;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Assessments;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Assessments.Student;

public sealed record SubmitAttemptCommand(Guid AttemptId, AnswerDto[] Answers) : ICommand<AttemptResultDto>;

public sealed class SubmitAttemptHandler : ICommandHandler<SubmitAttemptCommand, AttemptResultDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IGamificationService _gamification;

    public SubmitAttemptHandler(IApplicationDbContext db, ICurrentUser currentUser, IGamificationService gamification)
    {
        _db = db; _currentUser = currentUser; _gamification = gamification;
    }

    public async Task<Result<AttemptResultDto>> Handle(SubmitAttemptCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;

        var attempt = await _db.AssessmentAttempts
            .Include(a => a.Assessment).ThenInclude(a => a.Questions).ThenInclude(aq => aq.Question)
            .FirstOrDefaultAsync(a => a.Id == request.AttemptId && a.StudentId == studentId, ct);

        if (attempt is null)
            return Error.NotFound("ATTEMPT.NOT_FOUND", "المحاولة غير موجودة", "Attempt not found");

        if (attempt.Status != AttemptStatus.InProgress)
            return Error.Validation("ATTEMPT.ALREADY_SUBMITTED", "تم تقديم الاختبار بالفعل", "Already submitted");

        // Build answers dictionary
        var answersByQ = request.Answers.ToDictionary(a => a.QuestionId, a => a);

        decimal totalScore = 0;
        var questionResults = new List<QuestionResultDto>();

        foreach (var aq in attempt.Assessment.Questions.OrderBy(q => q.OrderNum))
        {
            var question = aq.Question;
            var userAnswer = answersByQ.GetValueOrDefault(question.Id);

            decimal earned = 0;
            bool isCorrect = false;

            using var qDoc = JsonDocument.Parse(question.QuestionJson);
            using var aDoc = JsonDocument.Parse(question.CorrectAnswerJson);
            var qRoot = qDoc.RootElement;
            var aRoot = aDoc.RootElement;

            var qText = qRoot.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
            string[]? options = null;
            int? correctOptIdx = null;
            bool? correctBool = null;

            if (question.Type == QuestionType.MultipleChoice)
            {
                if (qRoot.TryGetProperty("options", out var o))
                    options = o.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();
                if (aRoot.TryGetProperty("correct", out var c))
                    correctOptIdx = c.ValueKind == JsonValueKind.Number ? c.GetInt32() : null;

                if (userAnswer?.SelectedOptionIndex is int picked && picked == correctOptIdx)
                {
                    isCorrect = true;
                    earned = aq.Marks;
                }
            }
            else if (question.Type == QuestionType.TrueFalse)
            {
                if (aRoot.TryGetProperty("correct", out var c))
                    correctBool = c.ValueKind == JsonValueKind.True
                                  || c.ValueKind == JsonValueKind.False
                                  ? c.GetBoolean()
                                  : (bool?)null;

                if (userAnswer?.SelectedBoolean is bool picked && picked == correctBool)
                {
                    isCorrect = true;
                    earned = aq.Marks;
                }
            }

            totalScore += earned;

            questionResults.Add(new QuestionResultDto(
                question.Id, qText, question.Type.ToString(),
                isCorrect, earned, aq.Marks,
                options,
                userAnswer?.SelectedOptionIndex, correctOptIdx,
                userAnswer?.SelectedBoolean, correctBool,
                question.Explanation
            ));
        }

        var answersJson = JsonSerializer.Serialize(request.Answers);
        attempt.Submit(answersJson, totalScore, autoGraded: true);
        await _db.SaveChangesAsync(ct);

        // Award XP for passing + Perfect Score badge
        var passed = totalScore >= attempt.Assessment.PassingMarks;
        var isPerfect = attempt.Assessment.TotalMarks > 0 && totalScore == attempt.Assessment.TotalMarks;

        if (passed)
        {
            var xpReward = 20 + (int)(totalScore / attempt.Assessment.TotalMarks * 30);
            await _gamification.AwardXpAsync(studentId, xpReward, "passed_assessment", ct);
        }
        if (isPerfect)
        {
            await _gamification.AwardXpAsync(studentId, 50, "perfect_score_bonus", ct);
            await _gamification.TryAwardBadgeAsync(studentId, BadgeCodes.PerfectScore, ct);
        }

        var percentage = attempt.Assessment.TotalMarks > 0
            ? Math.Round(totalScore / attempt.Assessment.TotalMarks * 100, 2)
            : 0;

        return Result.Success(new AttemptResultDto(
            attempt.Id,
            attempt.AssessmentId,
            attempt.Assessment.Title,
            totalScore,
            attempt.Assessment.TotalMarks,
            percentage,
            passed,
            attempt.Status.ToString(),
            attempt.SubmittedAtUtc ?? DateTime.UtcNow,
            questionResults.ToArray()));
    }
}
