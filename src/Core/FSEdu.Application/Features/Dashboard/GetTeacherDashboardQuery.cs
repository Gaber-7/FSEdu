using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Domain.Support;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Contracts.Dashboard;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Dashboard;

public sealed record GetTeacherDashboardQuery() : IQuery<TeacherDashboardDto>;

public sealed class GetTeacherDashboardHandler : IQueryHandler<GetTeacherDashboardQuery, TeacherDashboardDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetTeacherDashboardHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<TeacherDashboardDto>> Handle(GetTeacherDashboardQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var teacherId = _currentUser.UserId.Value;
        var teacher = await _db.Teachers
            .FirstOrDefaultAsync(t => t.Id == teacherId, ct);

        if (teacher is null)
            return Error.NotFound("TEACHER.NOT_FOUND", "المدرس غير موجود", "Teacher not found");

        var courses = await _db.Courses
            .Where(c => c.TeacherId == teacherId)
            .Include(c => c.Subject)
            .Include(c => c.Stage)
            .Include(c => c.Chapters).ThenInclude(ch => ch.Lessons)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(ct);

        var published = courses.Count(c => c.Status == CourseStatus.Published);
        var pending = courses.Count(c => c.Status == CourseStatus.PendingReview);
        var draft = courses.Count(c => c.Status == CourseStatus.Draft);
        var lessonsCount = courses.Sum(c => c.Chapters.Sum(ch => ch.Lessons.Count));
        var totalEnrollments = courses.Sum(c => c.EnrollmentCount);

        var recent = courses.Take(6).Select(c => new CourseListItemDto(
            c.Id, c.Title, c.ThumbnailUrl,
            c.SubjectId, c.Subject.NameAr,
            c.StageId, c.Stage.NameAr,
            c.Status.ToString(),
            c.EnrollmentCount, c.RatingAvg,
            c.Chapters.Sum(ch => ch.Lessons.Count)
        )).ToArray();

        // ─── Live session analytics ───────────────────────────
        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);

        var sessions = await _db.LiveSessions
            .Where(s => s.TeacherId == teacherId)
            .Include(s => s.Attendance)
            .ToListAsync(ct);

        var totalSessions = sessions.Count;
        var sessionsThisWeek = sessions.Count(s =>
            (s.EndedAtUtc != null && s.EndedAtUtc >= weekAgo)
            || (s.StartedAtUtc != null && s.StartedAtUtc >= weekAgo)
            || s.ScheduledAtUtc >= weekAgo);
        var liveNow = sessions.Count(s => s.Status == LiveSessionStatus.Live);
        var scheduledUpcoming = sessions.Count(s => s.Status == LiveSessionStatus.Scheduled && s.ScheduledAtUtc >= now);
        var recordingsAvailable = sessions.Count(s => !string.IsNullOrEmpty(s.RecordingUrl));

        var endedSessions = sessions.Where(s => s.Status == LiveSessionStatus.Ended).ToList();
        var uniqueAttendees = sessions.SelectMany(s => s.Attendance.Select(a => a.StudentId)).Distinct().Count();
        var totalSeconds = sessions.Sum(s => s.Attendance.Sum(a => (long)a.TotalDurationSec));
        var totalMinutes = (int)(totalSeconds / 60);
        var avgAttendance = endedSessions.Count == 0
            ? 0
            : (int)Math.Round(endedSessions.Average(s => (double)s.Attendance.Count));

        // ─── Qualified attendance (watched > 50% of session duration) ───
        // This is what's counted toward teacher compensation — passing-glance
        // attendees don't count.
        int QualifiedCount(Domain.LiveClassroom.LiveSession s)
        {
            var halfSec = s.DurationMinutes * 60 / 2;
            return s.Attendance.Count(a => a.TotalDurationSec >= halfSec);
        }
        var qualifiedTotal = sessions.Sum(s => QualifiedCount(s));
        var avgQualified = endedSessions.Count == 0
            ? 0
            : (int)Math.Round(endedSessions.Average(s => (double)QualifiedCount(s)));

        var openTickets = await _db.AskTickets
            .CountAsync(t => t.AssignedTo == teacherId && t.Status == TicketStatus.Open, ct);

        var recentSessions = sessions
            .OrderByDescending(s => s.ScheduledAtUtc)
            .Take(6)
            .Select(s => new TeacherSessionRowDto(
                s.Id, s.Title, s.Status.ToString(),
                s.ScheduledAtUtc, s.EndedAtUtc, s.DurationMinutes,
                s.Attendance.Count, QualifiedCount(s),
                !string.IsNullOrEmpty(s.RecordingUrl)))
            .ToArray();

        var liveAnalytics = new TeacherLiveAnalyticsDto(
            totalSessions, sessionsThisWeek, liveNow, scheduledUpcoming,
            recordingsAvailable, uniqueAttendees, totalMinutes, avgAttendance,
            openTickets, qualifiedTotal, avgQualified, recentSessions);

        var dto = new TeacherDashboardDto(
            teacher.FullName,
            teacher.Verified,
            teacher.YearsOfExperience,
            teacher.RatingAvg,
            teacher.RatingsCount,
            courses.Count,
            published, pending, draft,
            lessonsCount,
            totalEnrollments,
            recent,
            liveAnalytics
        );

        return Result.Success(dto);
    }
}
