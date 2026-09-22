using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Contracts.Dashboard;
using FSEdu.Shared.Contracts.Subscriptions;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Dashboard;

public sealed record GetStudentDashboardQuery() : IQuery<StudentDashboardDto>;

public sealed class GetStudentDashboardHandler : IQueryHandler<GetStudentDashboardQuery, StudentDashboardDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;
    private readonly IGamificationService _gamification;

    public GetStudentDashboardHandler(IApplicationDbContext db, ICurrentUser currentUser,
                                       IAccessPolicy access, IGamificationService gamification)
    {
        _db = db; _currentUser = currentUser; _access = access; _gamification = gamification;
    }

    public async Task<Result<StudentDashboardDto>> Handle(GetStudentDashboardQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;

        // Update streak on dashboard load (idempotent within 12h)
        await _gamification.UpdateStreakAsync(userId, ct);

        var student = await _db.Students
            .Include(s => s.Stage)
            .FirstOrDefaultAsync(s => s.Id == userId, ct);

        if (student is null)
            return Error.NotFound("STUDENT.NOT_FOUND", "الطالب غير موجود", "Student not found");

        var now = DateTime.UtcNow;

        var activeSubs = await _db.Subscriptions
            .Where(s => s.UserId == userId
                     && s.Status == SubscriptionStatus.Active
                     && s.StartsAtUtc <= now && s.EndsAtUtc >= now)
            .Include(s => s.Subject)
            .Include(s => s.Stage)
            .ToListAsync(ct);

        var bundleSub = activeSubs.FirstOrDefault(s => s.Type == SubscriptionType.StageFullTerm);
        var individualCount = activeSubs.Count(s => s.Type != SubscriptionType.StageFullTerm);
        var expiresAt = activeSubs.Any() ? activeSubs.Max(s => s.EndsAtUtc) : (DateTime?)null;

        var summary = await _access.GetAccessSummaryAsync(userId, ct);

        var accessibleCoursesQuery = _db.Courses
            .Where(c => c.Status == CourseStatus.Published && summary.AccessibleSubjectIds.Contains(c.SubjectId));

        var accessibleCoursesCount = await accessibleCoursesQuery.CountAsync(ct);

        var accessibleLessonsCount = await _db.Lessons
            .Where(l => summary.AccessibleSubjectIds.Contains(l.Chapter.Course.SubjectId)
                     && l.Chapter.Course.Status == CourseStatus.Published)
            .CountAsync(ct);

        var recentCourses = await accessibleCoursesQuery
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(6)
            .Include(c => c.Subject)
            .Include(c => c.Stage)
            .Include(c => c.Chapters).ThenInclude(ch => ch.Lessons)
            .Select(c => new CourseListItemDto(
                c.Id, c.Title, c.ThumbnailUrl,
                c.SubjectId, c.Subject.NameAr,
                c.StageId, c.Stage.NameAr,
                c.Status.ToString(),
                c.EnrollmentCount, c.RatingAvg,
                c.Chapters.SelectMany(ch => ch.Lessons).Count()
            ))
            .ToListAsync(ct);

        MySubscriptionDto? bundleDto = null;
        if (bundleSub is not null)
        {
            bundleDto = new MySubscriptionDto(
                bundleSub.Id, bundleSub.Type.ToString(),
                "جميع المواد / ترم",
                null, null,
                bundleSub.StageId, bundleSub.Stage?.NameAr,
                bundleSub.Term?.ToString(),
                bundleSub.StartsAtUtc, bundleSub.EndsAtUtc,
                bundleSub.AmountPaid, bundleSub.Status.ToString(),
                bundleSub.IsActiveNow(now));
        }

        var dto = new StudentDashboardDto(
            student.FullName,
            student.Stage.NameAr,
            student.XpPoints,
            student.CurrentStreakDays,
            student.LongestStreakDays,
            accessibleCoursesCount,
            accessibleLessonsCount,
            bundleDto,
            individualCount,
            expiresAt,
            recentCourses.ToArray()
        );

        return Result.Success(dto);
    }
}
