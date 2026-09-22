using FluentValidation;
using FSEdu.Application.Abstractions;
using FSEdu.Domain.Courses;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// ─── Get announcements for a course ───────────────────
public sealed record GetCourseAnnouncementsQuery(Guid CourseId, int Take = 30)
    : IQuery<CourseAnnouncementsResponse>;

public sealed class GetCourseAnnouncementsHandler
    : IQueryHandler<GetCourseAnnouncementsQuery, CourseAnnouncementsResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetCourseAnnouncementsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<CourseAnnouncementsResponse>> Handle(
        GetCourseAnnouncementsQuery request, CancellationToken ct)
    {
        var course = await _db.Courses
            .Where(c => c.Id == request.CourseId)
            .Select(c => new { c.Id, c.TeacherId })
            .FirstOrDefaultAsync(ct);
        if (course is null)
            return Error.NotFound("COURSE.NOT_FOUND", "الدورة غير موجودة", "Not found");

        var canPost = false;
        if (_currentUser.UserId is Guid uid)
        {
            canPost = _currentUser.IsInRole("Admin") || course.TeacherId == uid;
        }

        var list = await _db.CourseAnnouncements
            .Where(a => a.CourseId == request.CourseId)
            .OrderByDescending(a => a.Pinned)
            .ThenByDescending(a => a.CreatedAtUtc)
            .Take(Math.Clamp(request.Take, 1, 100))
            .Select(a => new CourseAnnouncementDto(
                a.Id, a.CourseId, a.AuthorUserId, a.AuthorName,
                a.Title, a.Body, a.Pinned, a.CreatedAtUtc, a.UpdatedAtUtc))
            .ToListAsync(ct);

        var total = await _db.CourseAnnouncements.CountAsync(a => a.CourseId == request.CourseId, ct);

        return Result.Success(new CourseAnnouncementsResponse(canPost, total, list));
    }
}

// ─── Post a new announcement ──────────────────────────
public sealed record PostAnnouncementCommand(Guid CourseId, string Title, string Body, bool Pinned)
    : ICommand<Guid>;

public sealed class PostAnnouncementValidator : AbstractValidator<PostAnnouncementCommand>
{
    public PostAnnouncementValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(255);
        RuleFor(x => x.Body).NotEmpty().MinimumLength(3).MaximumLength(4000);
    }
}

public sealed class PostAnnouncementHandler : ICommandHandler<PostAnnouncementCommand, Guid>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationService _notifications;

    public PostAnnouncementHandler(IApplicationDbContext db, ICurrentUser currentUser,
                                    INotificationService notifications)
    {
        _db = db; _currentUser = currentUser; _notifications = notifications;
    }

    public async Task<Result<Guid>> Handle(PostAnnouncementCommand request, CancellationToken ct)
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
            return Error.Forbidden("ANNOUNCE.FORBIDDEN", "فقط مدرّس الدورة يستطيع النشر", "Teacher only");

        var name = await _db.Users.Where(u => u.Id == userId)
            .Select(u => u.FullName).FirstOrDefaultAsync(ct) ?? "المدرّس";

        var ann = new CourseAnnouncement(Guid.NewGuid(), request.CourseId, userId, name,
            request.Title, request.Body, request.Pinned);
        _db.CourseAnnouncements.Add(ann);
        await _db.SaveChangesAsync(ct);

        // Notify all enrolled students
        var enrolledIds = await _db.Enrollments
            .Where(e => e.CourseId == request.CourseId)
            .Select(e => e.StudentId)
            .ToListAsync(ct);

        if (enrolledIds.Count > 0)
        {
            var url = $"/courses/{request.CourseId}";
            var notifTitle = $"📣 إعلان جديد · {course.Title}";
            var notifBody = ann.Title;
            await _notifications.SendBulkAsync(enrolledIds, "course.announcement",
                notifTitle, notifBody,
                new { announcementId = ann.Id, courseId = request.CourseId, url }, ct);
        }

        return Result.Success(ann.Id);
    }
}

// ─── Delete an announcement ───────────────────────────
public sealed record DeleteAnnouncementCommand(Guid AnnouncementId) : ICommand;

public sealed class DeleteAnnouncementHandler : ICommandHandler<DeleteAnnouncementCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public DeleteAnnouncementHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result> Handle(DeleteAnnouncementCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var ann = await _db.CourseAnnouncements
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == request.AnnouncementId, ct);
        if (ann is null)
            return Error.NotFound("ANNOUNCE.NOT_FOUND", "الإعلان غير موجود", "Not found");

        var isAdmin = _currentUser.IsInRole("Admin");
        var isAuthor = ann.AuthorUserId == userId;
        var isCourseTeacher = ann.Course.TeacherId == userId;
        if (!isAdmin && !isAuthor && !isCourseTeacher)
            return Error.Forbidden("ANNOUNCE.FORBIDDEN", "غير مسموح بالحذف", "Forbidden");

        _db.CourseAnnouncements.Remove(ann);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }
}
