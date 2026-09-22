using FSEdu.Domain.Engagement;
using FSEdu.Shared.Contracts.Gamification;

namespace FSEdu.Application.Abstractions;

public interface IDailyChallengeService
{
    // Ensures the student has a challenge for today; creates one if missing.
    Task<DailyChallenge> EnsureTodayChallengeAsync(Guid studentId, CancellationToken ct = default);

    // Records progress against today's challenge for the given type and amount.
    // Awards XP + shield if the threshold is crossed. Safe to call on any action.
    Task RecordProgressAsync(Guid studentId, DailyChallengeType type, int amount = 1, CancellationToken ct = default);

    // Returns the student-facing view of today's challenge + shield count.
    Task<DailyChallengeStatusDto> GetTodayStatusAsync(Guid studentId, CancellationToken ct = default);
}
