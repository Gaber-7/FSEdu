using System.Text.Json;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Assessments;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Assessments;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Assessments.Student;

public sealed record StartAttemptCommand(Guid AssessmentId) : ICommand<StartAttemptResponse>;

public sealed class StartAttemptHandler : ICommandHandler<StartAttemptCommand, StartAttemptResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public StartAttemptHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<StartAttemptResponse>> Handle(StartAttemptCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;

        var assessment = await _db.Assessments
            .Include(a => a.Questions).ThenInclude(aq => aq.Question)
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId, ct);

        if (assessment is null)
            return Error.NotFound("ASSESSMENT.NOT_FOUND", "الاختبار غير موجود", "Not found");

        var now = DateTime.UtcNow;
        if (!assessment.IsAvailableNow(now))
            return Error.Validation("ASSESSMENT.UNAVAILABLE", "هذا الاختبار غير متاح حاليًا", "Not available");

        if (assessment.Questions.Count == 0)
            return Error.Validation("ASSESSMENT.EMPTY", "الاختبار لا يحتوي على أسئلة بعد", "No questions");

        // Access check: student must have access to the course's subject
        if (assessment.CourseId.HasValue && assessment.Course is not null)
        {
            var hasAccess = await _access.CanAccessSubjectAsync(studentId, assessment.Course.SubjectId, ct);
            if (!hasAccess)
                return Error.Forbidden("ACCESS.DENIED",
                    "يجب أن تكون مشتركًا في المادة لدخول الاختبار", "Subscription required");
        }

        // Attempts limit check
        var priorAttempts = await _db.AssessmentAttempts
            .CountAsync(x => x.AssessmentId == assessment.Id && x.StudentId == studentId, ct);
        if (priorAttempts >= assessment.AttemptsAllowed)
            return Error.Validation("ASSESSMENT.MAX_ATTEMPTS",
                "لقد استنفدت عدد المحاولات المسموح بها", "Max attempts reached");

        // Create attempt
        var attempt = new AssessmentAttempt(Guid.NewGuid(), assessment.Id, studentId);
        _db.AssessmentAttempts.Add(attempt);
        await _db.SaveChangesAsync(ct);

        // Build response WITHOUT correct answers
        var questions = assessment.Questions
            .OrderBy(q => q.OrderNum)
            .Select(aq =>
            {
                var q = aq.Question;
                using var doc = JsonDocument.Parse(q.QuestionJson);
                var root = doc.RootElement;
                var text = root.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
                string[]? options = null;
                if (q.Type == QuestionType.MultipleChoice && root.TryGetProperty("options", out var o))
                    options = o.EnumerateArray().Select(e => e.GetString() ?? "").ToArray();

                return new QuestionForTakingDto(
                    q.Id, q.Type.ToString(), text, options, aq.OrderNum, aq.Marks);
            })
            .ToArray();

        DateTime? deadline = assessment.TimeLimitMinutes.HasValue
            ? attempt.StartedAtUtc?.AddMinutes(assessment.TimeLimitMinutes.Value)
            : null;

        return Result.Success(new StartAttemptResponse(
            attempt.Id,
            assessment.Id,
            assessment.Title,
            assessment.Type.ToString(),
            attempt.StartedAtUtc ?? DateTime.UtcNow,
            assessment.TimeLimitMinutes,
            deadline,
            assessment.TotalMarks,
            questions));
    }
}
