using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// Teacher sends a targeted notification to a subset of enrolled students.
// Validates ownership; intersects the provided IDs with actual enrollees
// so a teacher can't send to students who never enrolled in the course.
// If StudentIds is empty, it broadcasts to all enrollees.
public sealed record SendCourseMessageCommand(
    Guid CourseId,
    Guid[] StudentIds,
    string Title,
    string Body,
    string? Url
) : ICommand<SendCourseMessageResponse>;

public sealed class SendCourseMessageValidator : AbstractValidator<SendCourseMessageCommand>
{
    public SendCourseMessageValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(2).MaximumLength(255);
        RuleFor(x => x.Body).NotEmpty().MinimumLength(2).MaximumLength(2000);
        RuleFor(x => x.Url).MaximumLength(500);
    }
}

public sealed class SendCourseMessageHandler
    : ICommandHandler<SendCourseMessageCommand, SendCourseMessageResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public SendCourseMessageHandler(IApplicationDbContext db, ICurrentUser currentUser, INotificationService notifications)
    {
        _db = db; _currentUser = currentUser; _notifications = notifications;
    }

    public async Task<Result<SendCourseMessageResponse>> Handle(
        SendCourseMessageCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var course = await _db.Courses
            .Where(c => c.Id == request.CourseId)
            .Select(c => new { c.Id, c.Title, c.TeacherId })
            .FirstOrDefaultAsync(ct);
        if (course is null)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Not found");

        var isAdmin = _currentUser.IsInRole("Admin");
        if (!isAdmin && course.TeacherId != userId)
            return Error.Forbidden("COURSE.NOT_YOURS", "هذه الدورة ليست لك", "Not yours");

        // Enrolled student IDs are the only valid targets.
        var enrolledIds = await _db.Enrollments
            .Where(e => e.CourseId == request.CourseId)
            .Select(e => e.StudentId)
            .ToListAsync(ct);

        List<Guid> targets;
        if (request.StudentIds is null || request.StudentIds.Length == 0)
        {
            targets = enrolledIds;
        }
        else
        {
            var requested = request.StudentIds.Distinct().ToHashSet();
            targets = enrolledIds.Where(id => requested.Contains(id)).ToList();
        }

        if (targets.Count == 0)
            return Result.Success(new SendCourseMessageResponse(0));

        var url = string.IsNullOrWhiteSpace(request.Url) ? $"/courses/{course.Id}" : request.Url.Trim();
        var title = $"📣 {request.Title.Trim()} — {course.Title}";

        await _notifications.SendBulkAsync(targets,
            "course.announcement",
            title, request.Body.Trim(),
            new { courseId = course.Id, url }, ct);

        return Result.Success(new SendCourseMessageResponse(targets.Count));
    }
}
