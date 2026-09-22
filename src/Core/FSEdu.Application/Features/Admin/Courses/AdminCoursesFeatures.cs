using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Admin.Courses;

public sealed record GetPendingCoursesQuery() : IQuery<List<CourseListItemDto>>;

public sealed class GetPendingCoursesHandler : IQueryHandler<GetPendingCoursesQuery, List<CourseListItemDto>>
{
    private readonly IApplicationDbContext _db;
    public GetPendingCoursesHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<List<CourseListItemDto>>> Handle(GetPendingCoursesQuery request, CancellationToken ct)
    {
        var courses = await _db.Courses
            .Where(c => c.Status == CourseStatus.PendingReview)
            .Include(c => c.Subject)
            .Include(c => c.Stage)
            .Include(c => c.Chapters).ThenInclude(ch => ch.Lessons)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(ct);

        return Result.Success(courses.Select(c => new CourseListItemDto(
            c.Id, c.Title, c.ThumbnailUrl,
            c.SubjectId, c.Subject.NameAr,
            c.StageId, c.Stage.NameAr,
            c.Status.ToString(),
            c.EnrollmentCount, c.RatingAvg,
            c.Chapters.Sum(ch => ch.Lessons.Count)
        )).ToList());
    }
}

public sealed record ApproveCourseCommand(Guid CourseId) : ICommand;

public sealed class ApproveCourseHandler : ICommandHandler<ApproveCourseCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notifications;
    public ApproveCourseHandler(IApplicationDbContext db, INotificationService notifications)
    { _db = db; _notifications = notifications; }

    public async Task<Result> Handle(ApproveCourseCommand request, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);
        if (course is null) return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Not found");
        course.Publish();
        await _db.SaveChangesAsync(ct);

        await _notifications.SendAsync(course.TeacherId,
            NotificationTypes.CourseApproved,
            "تم نشر دورتك! 🚀",
            $"تمت الموافقة على دورة \"{course.Title}\" وأصبحت متاحة للطلاب الآن.",
            new { courseId = course.Id, url = $"/teacher/courses/{course.Id}/edit" },
            ct);

        return Result.Success();
    }
}

public sealed record RejectCourseCommand(Guid CourseId, string? Reason) : ICommand;

public sealed class RejectCourseHandler : ICommandHandler<RejectCourseCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notifications;
    public RejectCourseHandler(IApplicationDbContext db, INotificationService notifications)
    { _db = db; _notifications = notifications; }

    public async Task<Result> Handle(RejectCourseCommand request, CancellationToken ct)
    {
        var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == request.CourseId, ct);
        if (course is null) return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Not found");
        course.Reject();
        await _db.SaveChangesAsync(ct);

        var reason = string.IsNullOrEmpty(request.Reason)
            ? "يرجى مراجعة المحتوى والمحاولة مرة أخرى."
            : request.Reason;

        await _notifications.SendAsync(course.TeacherId,
            NotificationTypes.CourseRejected,
            "لم تتم الموافقة على دورتك",
            $"دورة \"{course.Title}\": {reason}",
            new { courseId = course.Id, url = $"/teacher/courses/{course.Id}/edit" },
            ct);

        return Result.Success();
    }
}
