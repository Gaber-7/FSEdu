using FSEdu.Application.Abstractions;
using FSEdu.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace FSEdu.Infrastructure.Access;

public sealed class AccessPolicy : IAccessPolicy
{
    private readonly IApplicationDbContext _db;

    public AccessPolicy(IApplicationDbContext db) => _db = db;

    public async Task<bool> CanAccessSubjectAsync(Guid userId, int subjectId, CancellationToken ct = default)
    {
        var subject = await _db.Subjects
            .Where(s => s.Id == subjectId)
            .Select(s => new { s.Id, s.StageId })
            .FirstOrDefaultAsync(ct);
        if (subject is null) return false;

        var now = DateTime.UtcNow;

        return await _db.Subscriptions.AnyAsync(sub =>
            sub.UserId == userId &&
            sub.Status == SubscriptionStatus.Active &&
            sub.StartsAtUtc <= now && sub.EndsAtUtc >= now &&
            (
                (sub.Type != SubscriptionType.StageFullTerm && sub.SubjectId == subject.Id) ||
                (sub.Type == SubscriptionType.StageFullTerm && sub.StageId == subject.StageId)
            ), ct);
    }

    public async Task<bool> CanAccessCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default)
    {
        var subjectId = await _db.Courses
            .Where(c => c.Id == courseId)
            .Select(c => c.SubjectId)
            .FirstOrDefaultAsync(ct);
        if (subjectId == 0) return false;

        return await CanAccessSubjectAsync(userId, subjectId, ct);
    }

    public async Task<bool> CanAccessLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default)
    {
        var lesson = await _db.Lessons
            .Where(l => l.Id == lessonId)
            .Select(l => new { l.IsFreePreview, CourseId = l.Chapter.CourseId, SubjectId = l.Chapter.Course.SubjectId })
            .FirstOrDefaultAsync(ct);
        if (lesson is null) return false;
        if (lesson.IsFreePreview) return true;

        return await CanAccessSubjectAsync(userId, lesson.SubjectId, ct);
    }

    public async Task<AccessSummary> GetAccessSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var activeSubs = await _db.Subscriptions
            .Where(s => s.UserId == userId
                    && s.Status == SubscriptionStatus.Active
                    && s.StartsAtUtc <= now && s.EndsAtUtc >= now)
            .ToListAsync(ct);

        var stageIds = activeSubs
            .Where(s => s.Type == SubscriptionType.StageFullTerm && s.StageId.HasValue)
            .Select(s => s.StageId!.Value)
            .ToHashSet();

        // Subjects from individual subs
        var directSubjectIds = activeSubs
            .Where(s => s.Type != SubscriptionType.StageFullTerm && s.SubjectId.HasValue)
            .Select(s => s.SubjectId!.Value)
            .ToHashSet();

        // Subjects from stage bundles (all subjects of those stages)
        var stageBundleSubjects = await _db.Subjects
            .Where(s => stageIds.Contains(s.StageId))
            .Select(s => s.Id)
            .ToListAsync(ct);

        directSubjectIds.UnionWith(stageBundleSubjects);

        return new AccessSummary(directSubjectIds, stageIds, activeSubs.Count > 0);
    }
}
