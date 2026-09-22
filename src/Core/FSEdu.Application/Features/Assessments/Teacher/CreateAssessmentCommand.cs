using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Assessments;
using FSEdu.Domain.Common;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Assessments.Teacher;

public sealed record CreateAssessmentCommand(
    Guid CourseId,
    string Type,
    string Title,
    string? Description,
    int? TimeLimitMinutes,
    decimal PassingMarks,
    int AttemptsAllowed,
    DateTime? AvailableFromUtc,
    DateTime? AvailableToUtc
) : ICommand<Guid>;

public sealed class CreateAssessmentValidator : AbstractValidator<CreateAssessmentCommand>
{
    public CreateAssessmentValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(255);
        RuleFor(x => x.Type).Must(t => t is "Quiz" or "Assignment" or "Exam")
            .WithMessage("نوع الاختبار غير صالح");
        RuleFor(x => x.AttemptsAllowed).GreaterThan(0);
        RuleFor(x => x.PassingMarks).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateAssessmentHandler : ICommandHandler<CreateAssessmentCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public CreateAssessmentHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(CreateAssessmentCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var teacherId = _currentUser.UserId.Value;
        var course = await _db.Courses.FirstOrDefaultAsync(
            c => c.Id == request.CourseId && c.TeacherId == teacherId, ct);
        if (course is null)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Course not found");

        var type = Enum.Parse<AssessmentType>(request.Type);
        var assessment = new Assessment(Guid.NewGuid(), teacherId, type,
            request.Title, 0m, request.PassingMarks);

        assessment.Configure(request.CourseId, request.Description,
            request.TimeLimitMinutes, request.AttemptsAllowed);
        assessment.SetAvailability(request.AvailableFromUtc, request.AvailableToUtc);

        _db.Assessments.Add(assessment);
        await _db.SaveChangesAsync(ct);

        return Result.Success(assessment.Id);
    }
}
