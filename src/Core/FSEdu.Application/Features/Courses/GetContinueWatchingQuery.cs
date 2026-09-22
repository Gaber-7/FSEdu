using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using FSEdu.Shared.Contracts.Courses;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

// "Continue Watching" — last lessons the student opened but hasn't completed.
// Joined with Lesson + Chapter + Course + Subject for the widget.
public sealed record GetContinueWatchingQuery(int Take = 6)
    : IQuery<List<ContinueWatchingItemDto>>;

public sealed class GetContinueWatchingHandler
    : IQueryHandler<GetContinueWatchingQuery, List<ContinueWatchingItemDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetContinueWatchingHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db; _currentUser = currentUser;
    }

    public async Task<Result<List<ContinueWatchingItemDto>>> Handle(
        GetContinueWatchingQuery request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Result.Success(new List<ContinueWatchingItemDto>());

        var studentId = _currentUser.UserId.Value;

        var list = await _db.LessonProgress
            .Where(lp => lp.StudentId == studentId && !lp.Completed)
            .OrderByDescending(lp => lp.LastViewedAtUtc)
            .Take(Math.Clamp(request.Take, 1, 24))
            .Join(_db.Lessons, lp => lp.LessonId, l => l.Id, (lp, l) => new { lp, l })
            .Where(x => x.l.Type == LessonType.Recorded)
            .Select(x => new ContinueWatchingItemDto(
                x.l.Id,
                x.l.Title,
                x.l.Chapter.CourseId,
                x.l.Chapter.Course.Title,
                x.l.Chapter.Course.Subject.NameAr,
                x.l.Chapter.Course.ThumbnailUrl,
                x.lp.LastPositionSec,
                x.l.DurationSeconds,
                x.lp.WatchedPct,
                x.lp.LastViewedAtUtc
            ))
            .ToListAsync(ct);

        return Result.Success(list);
    }
}
