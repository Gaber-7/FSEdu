using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Users;

public sealed class Referral : AggregateRoot<Guid>
{
    public Guid ReferrerStudentId { get; private set; }
    public Student ReferrerStudent { get; private set; } = default!;
    public Guid ReferredStudentId { get; private set; }
    public Student ReferredStudent { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? RewardedAtUtc { get; private set; }
    public int RewardEgp { get; private set; }
    public Guid? TriggerSubscriptionId { get; private set; }   // the first paid subscription that triggered the reward

    private Referral() { }

    public Referral(Guid id, Guid referrerStudentId, Guid referredStudentId) : base(id)
    {
        if (referrerStudentId == referredStudentId)
            throw new InvalidOperationException("Self-referral is not allowed");
        ReferrerStudentId = referrerStudentId;
        ReferredStudentId = referredStudentId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void MarkRewarded(int rewardEgp, Guid subscriptionId)
    {
        if (RewardedAtUtc is not null) return;
        RewardEgp = Math.Max(0, rewardEgp);
        TriggerSubscriptionId = subscriptionId;
        RewardedAtUtc = DateTime.UtcNow;
    }
}
