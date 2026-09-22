namespace FSEdu.Application.Abstractions;

public interface IWeeklyDigestService
{
    // Build + send for one parent. Returns false if no email, no children, or send failed.
    Task<bool> SendForParentAsync(Guid parentId, CancellationToken ct = default);

    // Iterates all parents whose last digest is >7 days old (or null) and sends.
    Task<int> SendDueDigestsAsync(CancellationToken ct = default);
}
