using FSEdu.Application.Abstractions;
using FSEdu.Domain.Courses;
using FSEdu.Domain.Engagement;
using FSEdu.Shared.Kernel.Results;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Application.Features.Courses;

public sealed record TrackLessonViewCommand(
    Guid LessonId,
    int LastPositionSec,
    decimal WatchedPct
) : ICommand;

public sealed class TrackLessonViewHandler : ICommandHandler<TrackLessonViewCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessPolicy _access;
    private readonly IGamificationService _gamification;
    private readonly IDailyChallengeService _dailyChallenge;

    public TrackLessonViewHandler(IApplicationDbContext db, ICurrentUser currentUser,
                                    IAccessPolicy access, IGamificationService gamification,
                                    IDailyChallengeService dailyChallenge)
    {
        _db = db; _currentUser = currentUser; _access = access;
        _gamification = gamification; _dailyChallenge = dailyChallenge;
    }

    public async Task<Result> Handle(TrackLessonViewCommand request, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
            return Error.Unauthorized("AUTH.REQUIRED", "يجب تسجيل الدخول", "Auth required");

        var studentId = _currentUser.UserId.Value;
        var isStudent = await _db.Students.AnyAsync(s => s.Id == studentId, ct);
        if (!isStudent) return Result.Success(); // skip for teachers/admins

        var hasAccess = await _access.CanAccessLessonAsync(studentId, request.LessonId, ct);
        if (!hasAccess) return Result.Success(); // silently skip if no access

        var existing = await _db.LessonProgress
            .FirstOrDefaultAsync(lp => lp.StudentId == studentId && lp.LessonId == request.LessonId, ct);

        var isFirstView = existing is null;
        var wasIncomplete = existing is null || !existing.Completed;

        if (existing is null)
        {
            existing = new LessonProgress(studentId, request.LessonId);
            _db.LessonProgress.Add(existing);
        }

        existing.UpdatePosition(request.LastPositionSec, request.WatchedPct);
        await _db.SaveChangesAsync(ct);

        // Award XP & badges
        if (isFirstView)
        {
            await _gamification.AwardXpAsync(studentId, 5, "first_view_lesson", ct);

            // Award first-lesson badge if it's the very first lesson watched
            var totalProgress = await _db.LessonProgress.CountAsync(lp => lp.StudentId == studentId, ct);
            if (totalProgress == 1)
            {
                await _gamification.TryAwardBadgeAsync(studentId, BadgeCodes.FirstLesson, ct);
                await _gamification.AwardXpAsync(studentId, 20, "first_ever_lesson_bonus", ct);
            }
        }

        if (wasIncomplete && existing.Completed)
        {
            await _gamification.AwardXpAsync(studentId, 15, "completed_lesson", ct);
            await _dailyChallenge.RecordProgressAsync(studentId, DailyChallengeType.CompleteLessons, 1, ct);
        }

        // Watch minutes — credit each call by the delta watched (approx via WatchedPct).
        // Use a simple heuristic: if WatchedPct grew, credit the proportional minutes
        // (lesson durations live elsewhere; keep this lightweight — 1 min per call as a floor).
        if (!existing.Completed && request.WatchedPct > 0)
        {
            await _dailyChallenge.RecordProgressAsync(studentId, DailyChallengeType.WatchMinutes, 1, ct);
        }

        return Result.Success();
    }
}
