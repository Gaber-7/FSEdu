using FSEdu.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSEdu.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> b)
    {
        b.ToTable("AuditEntries");
        b.HasKey(x => x.Id);
        b.Property(x => x.ActorName).HasMaxLength(255).IsRequired();
        b.Property(x => x.ActorRole).HasMaxLength(50).IsRequired();
        b.Property(x => x.Action).HasMaxLength(100).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(50);
        b.Property(x => x.EntityId).HasMaxLength(50);
        b.Property(x => x.IpAddress).HasMaxLength(64);

        b.HasIndex(x => x.AtUtc);
        b.HasIndex(x => new { x.ActorUserId, x.AtUtc });
        b.HasIndex(x => new { x.Action, x.AtUtc });
    }
}
