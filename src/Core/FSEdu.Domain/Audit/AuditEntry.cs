using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Audit;

// Append-only audit log of privileged actions (approvals, broadcasts,
// campaign/coupon admin, etc.). Read by admins for compliance + forensics.
// Never mutated after insertion — there are no setters or update methods.
public sealed class AuditEntry : Entity<long>
{
    public Guid ActorUserId { get; private set; }
    public string ActorName { get; private set; } = default!;
    public string ActorRole { get; private set; } = default!;    // "Admin" / "Teacher" / etc.
    public string Action { get; private set; } = default!;       // "teacher.approve", "payment.approve", ...
    public string? EntityType { get; private set; }              // optional FK label
    public string? EntityId { get; private set; }                // string for Guid/int compatibility
    public string? DetailsJson { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTime AtUtc { get; private set; }

    private AuditEntry() { }

    public AuditEntry(Guid actorUserId, string actorName, string actorRole,
                       string action, string? entityType, string? entityId,
                       string? detailsJson, string? ipAddress)
    {
        ActorUserId = actorUserId;
        ActorName = actorName;
        ActorRole = actorRole;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        DetailsJson = detailsJson;
        IpAddress = ipAddress;
        AtUtc = DateTime.UtcNow;
    }
}
