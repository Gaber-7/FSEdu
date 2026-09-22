using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// Returns the student's upcoming live sessions and active assessments
// within a date window so the frontend can render an agenda / weekly view.
public sealed record GetStudentScheduleQuery(int Days = 14) : IQuery<StudentScheduleResponse>;

public sealed class GetStudentScheduleHandler : IQueryHandler<GetStudentScheduleQuery, StudentScheduleResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public GetStudentScheduleHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<StudentScheduleResponse>> Handle(GetStudentScheduleQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var now = DateTime.UtcNow;
        var fromUtc = now.AddHours(-2);
        var days = Math.Clamp(request.Days, 1, 60);
        var toUtc = now.AddDays(days);

        var summary = await _access.GetAccessSummaryAsync(userId, ct);

        // ─── Live sessions in window ─────────────────────────
        var liveItems = await (
            from s in _db.LiveSessions
            where s.SubjectId != null
                  && summary.AccessibleSubjectIds.Contains(s.SubjectId.Value)
                  && s.Status != LiveSessionStatus.Cancelled
                  && s.Status != LiveSessionStatus.Ended
                  && s.ScheduledAtUtc >= fromUtc
                  && s.ScheduledAtUtc <= toUtc
            join sub in _db.Subjects on s.SubjectId equals sub.Id into subj
            from sub in subj.DefaultIfEmpty()
            select new ScheduleItemDto(
                "live",
                s.Id,
                s.Title,
                sub != null ? sub.NameAr : null,
                null,
                s.Teacher.FullName,
                s.ScheduledAtUtc,
                s.DurationMinutes,
                s.Status.ToString(),
                s.RoomId
            )).ToListAsync(ct);

        // ─── Assessments in window ───────────────────────────
        // Show assessments whose AvailableFromUtc/AvailableToUtc intersects the window
        // and the student has access to the course's subject.
        var assItems = await (
            from a in _db.Assessments
            where a.CourseId != null
                  && summary.AccessibleSubjectIds.Contains(a.Course!.SubjectId)
                  && (
                       (a.AvailableToUtc != null && a.AvailableToUtc >= now && a.AvailableToUtc <= toUtc)
                       || (a.AvailableFromUtc != null && a.AvailableFromUtc >= fromUtc && a.AvailableFromUtc <= toUtc)
                       // currently open
                       || ((a.AvailableFromUtc == null || a.AvailableFromUtc <= now)
                           && (a.AvailableToUtc == null || a.AvailableToUtc >= now))
                     )
            select new ScheduleItemDto(
                "assessment",
                a.Id,
                a.Title,
                a.Course!.Subject.NameAr,
                a.Course.Title,
                a.Course.Teacher.FullName,
                a.AvailableToUtc != null && a.AvailableToUtc >= now ? a.AvailableToUtc.Value
                    : (a.AvailableFromUtc ?? a.CreatedAtUtc),
                (int?)null,
                a.AvailableFromUtc != null && a.AvailableFromUtc > now ? "upcoming" : "available",
                (string?)null
            )).ToListAsync(ct);

        var all = liveItems.Concat(assItems)
            .Where(i => i.AtUtc >= fromUtc && i.AtUtc <= toUtc)
            .OrderBy(i => i.AtUtc)
            .ToList();

        return Result.Success(new StudentScheduleResponse(fromUtc, toUtc, all));
    }
}
