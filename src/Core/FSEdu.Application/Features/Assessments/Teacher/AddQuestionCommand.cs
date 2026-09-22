using System.Text.Json;
using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Assessments;
using FSEdu.Domain.Common;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Assessments.Teacher;

public sealed record AddQuestionCommand(
    Guid AssessmentId,
    string Type,                  // MultipleChoice | TrueFalse
    string Difficulty,
    string QuestionText,
    string[]? Options,
    int? CorrectOptionIndex,
    bool? CorrectBoolean,
    string? Explanation,
    decimal Marks
) : ICommand<Guid>;

public sealed class AddQuestionValidator : AbstractValidator<AddQuestionCommand>
{
    public AddQuestionValidator()
    {
        RuleFor(x => x.QuestionText).NotEmpty().MinimumLength(3);
        RuleFor(x => x.Type).Must(t => t is "MultipleChoice" or "TrueFalse");
        RuleFor(x => x.Marks).GreaterThan(0);

        When(x => x.Type == "MultipleChoice", () =>
        {
            RuleFor(x => x.Options).NotNull().Must(o => o != null && o.Length >= 2)
                .WithMessage("يجب تقديم خيارين على الأقل");
            RuleFor(x => x.CorrectOptionIndex).NotNull().GreaterThanOrEqualTo(0)
                .WithMessage("يجب تحديد الإجابة الصحيحة");
        });

        When(x => x.Type == "TrueFalse", () =>
        {
            RuleFor(x => x.CorrectBoolean).NotNull()
                .WithMessage("يجب تحديد ما إذا كانت العبارة صحيحة أم خاطئة");
        });
    }
}

public sealed class AddQuestionHandler : ICommandHandler<AddQuestionCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AddQuestionHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(AddQuestionCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var teacherId = _currentUser.UserId.Value;
        var assessment = await _db.Assessments
            .Include(a => a.Questions)
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId && a.TeacherId == teacherId, ct);

        if (assessment is null)
            return Error.NotFound("ASSESSMENT.NOT_FOUND", "الاختبار غير موجود", "Assessment not found");

        // Get course stage/subject for question bank categorization
        var course = assessment.CourseId.HasValue
            ? await _db.Courses.FirstAsync(c => c.Id == assessment.CourseId, ct)
            : null;

        var qType = Enum.Parse<QuestionType>(request.Type);
        var qDifficulty = Enum.Parse<Difficulty>(request.Difficulty);

        // Build question JSON
        var questionJson = qType == QuestionType.MultipleChoice
            ? JsonSerializer.Serialize(new { text = request.QuestionText, options = request.Options })
            : JsonSerializer.Serialize(new { text = request.QuestionText });

        var correctAnswerJson = qType == QuestionType.MultipleChoice
            ? JsonSerializer.Serialize(new { correct = request.CorrectOptionIndex })
            : JsonSerializer.Serialize(new { correct = request.CorrectBoolean });

        var question = new Question(
            Guid.NewGuid(),
            course?.SubjectId ?? 0,
            course?.StageId ?? 0,
            qType, qDifficulty,
            questionJson, correctAnswerJson,
            teacherId);

        if (!string.IsNullOrWhiteSpace(request.Explanation))
            question.SetExplanation(request.Explanation);

        _db.Questions.Add(question);

        var orderNum = assessment.Questions.Count + 1;
        assessment.AddQuestion(question.Id, request.Marks, orderNum);

        await _db.SaveChangesAsync(ct);
        return Result.Success(question.Id);
    }
}

// ─── Delete Question from Assessment ─────────────────
public sealed record RemoveQuestionCommand(Guid AssessmentId, Guid QuestionId) : ICommand;

public sealed class RemoveQuestionHandler : ICommandHandler<RemoveQuestionCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public RemoveQuestionHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(RemoveQuestionCommand request, CancellationToken ct)
    {
        var teacherId = _currentUser.UserId ?? Guid.Empty;
        var assessment = await _db.Assessments
            .Include(a => a.Questions)
            .FirstOrDefaultAsync(a => a.Id == request.AssessmentId && a.TeacherId == teacherId, ct);

        if (assessment is null)
            return Error.NotFound("ASSESSMENT.NOT_FOUND", "الاختبار غير موجود", "Not found");

        assessment.RemoveQuestion(request.QuestionId);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
