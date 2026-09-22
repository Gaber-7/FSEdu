using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.LiveClassroom;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.LiveClassroom;

// ─── Teacher: My sessions ──────────────────────
public sealed record GetMyLiveSessionsQuery() : IQuery<List<LiveSessionListItemDto>>;

public sealed class GetMyLiveSessionsHandler : IQueryHandler<GetMyLiveSessionsQuery, List<LiveSessionListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetMyLiveSessionsHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<LiveSessionListItemDto>>> Handle(GetMyLiveSessionsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var teacherId = _currentUser.UserId.Value;

        var list = await (
            from s in _db.LiveSessions
            where s.TeacherId == teacherId
            join sub in _db.Subjects on s.SubjectId equals sub.Id into subj
            from sub in subj.DefaultIfEmpty()
            join st in _db.Stages on s.StageId equals st.Id into stg
            from st in stg.DefaultIfEmpty()
            orderby s.ScheduledAtUtc descending
            select new LiveSessionListItemDto(
                s.Id, s.Title, s.Description,
                s.TeacherId, s.Teacher.FullName,
                sub != null ? sub.Id : 0,
                sub != null ? sub.NameAr : "",
                st != null ? st.NameAr : "",
                s.Status.ToString(),
                s.ScheduledAtUtc, s.DurationMinutes,
                s.StartedAtUtc, s.EndedAtUtc,
                s.Attendance.Count(a => a.LeftAtUtc == null),
                s.RoomId,
                s.RecordingUrl,
                s.RecordingStatus,
                st != null ? st.Id : 0)
        ).ToListAsync(ct);

        return Result.Success(list);
    }
}

// ─── Student: Past recorded sessions ───────────
public sealed record GetRecordedLiveSessionsQuery() : IQuery<List<LiveSessionListItemDto>>;

public sealed class GetRecordedLiveSessionsHandler : IQueryHandler<GetRecordedLiveSessionsQuery, List<LiveSessionListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public GetRecordedLiveSessionsHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<List<LiveSessionListItemDto>>> Handle(GetRecordedLiveSessionsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var summary = await _access.GetAccessSummaryAsync(userId, ct);

        var sessions = await (
            from s in _db.LiveSessions
            where s.SubjectId != null
                  && summary.AccessibleSubjectIds.Contains(s.SubjectId.Value)
                  && s.Status == LiveSessionStatus.Ended
                  && s.RecordingUrl != null
            join sub in _db.Subjects on s.SubjectId equals sub.Id into subj
            from sub in subj.DefaultIfEmpty()
            join st in _db.Stages on s.StageId equals st.Id into stg
            from st in stg.DefaultIfEmpty()
            orderby s.EndedAtUtc descending
            select new LiveSessionListItemDto(
                s.Id, s.Title, s.Description,
                s.TeacherId, s.Teacher.FullName,
                sub != null ? sub.Id : 0,
                sub != null ? sub.NameAr : "",
                st != null ? st.NameAr : "",
                s.Status.ToString(),
                s.ScheduledAtUtc, s.DurationMinutes,
                s.StartedAtUtc, s.EndedAtUtc,
                0,
                s.RoomId,
                s.RecordingUrl,
                s.RecordingStatus,
                st != null ? st.Id : 0)
        ).ToListAsync(ct);

        return Result.Success(sessions);
    }
}

// ─── Student: Upcoming + Live sessions ─────────
public sealed record GetUpcomingLiveSessionsQuery() : IQuery<List<LiveSessionListItemDto>>;

public sealed class GetUpcomingLiveSessionsHandler : IQueryHandler<GetUpcomingLiveSessionsQuery, List<LiveSessionListItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;

    public GetUpcomingLiveSessionsHandler(IApplicationDbContext db, ICurrentUser currentUser, IAccessPolicy access)
    {
        _db = db; _currentUser = currentUser; _access = access;
    }

    public async Task<Result<List<LiveSessionListItemDto>>> Handle(GetUpcomingLiveSessionsQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var userId = _currentUser.UserId.Value;
        var summary = await _access.GetAccessSummaryAsync(userId, ct);

        var now = DateTime.UtcNow;
        var fromTime = now.AddHours(-2); // include sessions started recently

        var sessions = await (
            from s in _db.LiveSessions
            where s.SubjectId != null
                  && summary.AccessibleSubjectIds.Contains(s.SubjectId.Value)
                  && s.Status != LiveSessionStatus.Cancelled
                  && s.Status != LiveSessionStatus.Ended
                  && s.ScheduledAtUtc >= fromTime
            join sub in _db.Subjects on s.SubjectId equals sub.Id into subj
            from sub in subj.DefaultIfEmpty()
            join st in _db.Stages on s.StageId equals st.Id into stg
            from st in stg.DefaultIfEmpty()
            orderby s.ScheduledAtUtc ascending
            select new LiveSessionListItemDto(
                s.Id, s.Title, s.Description,
                s.TeacherId, s.Teacher.FullName,
                sub != null ? sub.Id : 0,
                sub != null ? sub.NameAr : "",
                st != null ? st.NameAr : "",
                s.Status.ToString(),
                s.ScheduledAtUtc, s.DurationMinutes,
                s.StartedAtUtc, s.EndedAtUtc,
                s.Attendance.Count(a => a.LeftAtUtc == null),
                s.RoomId,
                s.RecordingUrl,
                s.RecordingStatus,
                st != null ? st.Id : 0)
        ).ToListAsync(ct);

        return Result.Success(sessions);
    }
}
