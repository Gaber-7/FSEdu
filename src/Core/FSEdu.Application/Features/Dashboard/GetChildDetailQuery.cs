using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Dashboard;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Dashboard;

// Deep child profile for the parent's child-detail page.
// Returns 403 unless the current user is linked as a parent of the student.
public sealed record GetChildDetailQuery(Guid StudentId) : IQuery<ChildDetailDto>;

public sealed class GetChildDetailHandler : IQueryHandler<GetChildDetailQuery, ChildDetailDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetChildDetailHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<ChildDetailDto>> Handle(GetChildDetailQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var parentId = _currentUser.UserId.Value;
        var isAdmin = _currentUser.IsInRole("Admin");

        // Verify link unless admin
        if (!isAdmin)
        {
            var linked = await _db.ParentStudentLinks
                .AnyAsync(l => l.ParentId == parentId && l.StudentId == request.StudentId, ct);
            if (!linked)
                return Error.Forbidden("PARENT.NOT_LINKED", "هذا الحساب ليس مرتبطًا بك", "Not linked");
        }

        var student = await _db.Students
            .Include(s => s.Stage)
            .Include(s => s.Region)
            .FirstOrDefaultAsync(s => s.Id == request.StudentId, ct);
        if (student is null)
            return Error.NotFound("STUDENT.NOT_FOUND", "الطالب غير موجود", "Not found");

        // ─── Subscriptions ────────────────────────────────
        var subs = await _db.Subscriptions
            .Where(sub => sub.UserId == student.Id)
            .OrderByDescending(sub => sub.EndsAtUtc)
            .Select(sub => new ChildSubscriptionDto(
                sub.Id,
                sub.Type.ToString(),
                sub.SubjectId == null ? null : sub.Subject!.NameAr,
                sub.StageId == null ? null : sub.Stage!.NameAr,
                sub.Status.ToString(),
                sub.StartsAtUtc, sub.EndsAtUtc,
                sub.AmountPaid, sub.Currency))
            .Take(10)
            .ToArrayAsync(ct);

        // ─── Progress stats ───────────────────────────────
        var nowUtc = DateTime.UtcNow;
        var todayStart = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc);
        var tomorrowStart = todayStart.AddDays(1);

        var enrolledCount = await _db.Enrollments.CountAsync(e => e.StudentId == student.Id, ct);
        var lessonsCompleted = await _db.LessonProgress.CountAsync(lp => lp.StudentId == student.Id && lp.Completed, ct);
        var viewedToday = await _db.LessonProgress.CountAsync(lp =>
            lp.StudentId == student.Id
            && lp.LastViewedAtUtc >= todayStart
            && lp.LastViewedAtUtc < tomorrowStart, ct);
        var submittedTotal = await _db.AssessmentAttempts.CountAsync(a =>
            a.StudentId == student.Id && a.SubmittedAtUtc != null, ct);
        var avgScore = await _db.AssessmentAttempts
            .Where(a => a.StudentId == student.Id && a.SubmittedAtUtc != null)
            .Select(a => (decimal?)a.Score)
            .AverageAsync(ct) ?? 0m;

        var badgesCount = await _db.StudentBadges.CountAsync(b => b.StudentId == student.Id, ct);
        var globalRank = await _db.Students.CountAsync(s => s.XpPoints > student.XpPoints, ct) + 1;

        // ─── Recent lessons ────────────────────────────────
        var recentLessons = await (
            from lp in _db.LessonProgress
            where lp.StudentId == student.Id
            join l in _db.Lessons on lp.LessonId equals l.Id
            orderby lp.LastViewedAtUtc descending
            select new ChildRecentLessonDto(
                l.Id,
                l.Title,
                l.Chapter.Course.Title,
                lp.WatchedPct,
                lp.Completed,
                lp.LastViewedAtUtc
            )).Take(6).ToArrayAsync(ct);

        // ─── Recent attempts ───────────────────────────────
        var recentAttempts = await (
            from a in _db.AssessmentAttempts
            where a.StudentId == student.Id && a.SubmittedAtUtc != null
            join asm in _db.Assessments on a.AssessmentId equals asm.Id
            orderby a.SubmittedAtUtc descending
            select new ChildRecentAttemptDto(
                a.Id, asm.Title,
                a.Score,
                asm.TotalMarks,
                a.SubmittedAtUtc!.Value
            )).Take(6).ToArrayAsync(ct);

        // ─── Upcoming live sessions for child's subjects ──
        // We need to know which subjects the student has access to.
        var subjectsAccessible = subs
            .Where(s => s.Status == "Active")
            .Select(s => s.SubjectName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .ToList();

        var upcoming = await (
            from sess in _db.LiveSessions
            where sess.SubjectId != null
                  && sess.Status != LiveSessionStatus.Cancelled
                  && sess.Status != LiveSessionStatus.Ended
                  && sess.ScheduledAtUtc >= nowUtc.AddHours(-1)
                  && sess.ScheduledAtUtc <= nowUtc.AddDays(14)
            join sub in _db.Subjects on sess.SubjectId equals sub.Id into sj
            from sub in sj.DefaultIfEmpty()
            where sub != null && subjectsAccessible.Contains(sub.NameAr)
            orderby sess.ScheduledAtUtc ascending
            select new ChildUpcomingSessionDto(
                sess.Id, sess.Title,
                sub != null ? sub.NameAr : null,
                sess.Teacher.FullName,
                sess.ScheduledAtUtc, sess.DurationMinutes,
                sess.Status.ToString()
            )).Take(6).ToArrayAsync(ct);

        var dto = new ChildDetailDto(
            student.Id, student.FullName, student.Phone.Value, student.Email?.Value,
            student.Stage.NameAr, student.Region.NameAr, student.AvatarUrl,
            student.XpPoints, student.CurrentStreakDays, student.LongestStreakDays,
            badgesCount, globalRank,
            subs,
            enrolledCount, lessonsCompleted, viewedToday, submittedTotal,
            Math.Round(avgScore, 2),
            recentLessons, recentAttempts, upcoming
        );

        return Result.Success(dto);
    }
}
