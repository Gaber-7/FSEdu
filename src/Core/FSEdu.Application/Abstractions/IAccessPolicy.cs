namespace FSEdu.Application.Abstractions;

public interface IAccessPolicy
{
    Task<bool> CanAccessSubjectAsync(Guid userId, int subjectId, CancellationToken ct = default);
    Task<bool> CanAccessCourseAsync(Guid userId, Guid courseId, CancellationToken ct = default);
    Task<bool> CanAccessLessonAsync(Guid userId, Guid lessonId, CancellationToken ct = default);
    Task<AccessSummary> GetAccessSummaryAsync(Guid userId, CancellationToken ct = default);
}

public sealed record AccessSummary(
    HashSet<int> AccessibleSubjectIds,
    HashSet<int> AccessibleStageIds,
    bool HasAnyActiveSubscription
);
