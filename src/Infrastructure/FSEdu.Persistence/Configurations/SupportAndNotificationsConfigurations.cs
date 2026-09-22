using FSEdu.Domain.Notifications;
using FSEdu.Domain.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSEdu.Persistence.Configurations;

public sealed class AskTicketConfiguration : IEntityTypeConfiguration<AskTicket>
{
    public void Configure(EntityTypeBuilder<AskTicket> b)
    {
        b.ToTable("AskTickets");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Priority).HasConversion<int>();

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssignedTo).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => new { x.Status, x.Priority });
    }
}

public sealed class TicketMessageConfiguration : IEntityTypeConfiguration<TicketMessage>
{
    public void Configure(EntityTypeBuilder<TicketMessage> b)
    {
        b.ToTable("TicketMessages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Body).HasColumnType("nvarchar(max)");
        b.Property(x => x.AudioUrl).HasMaxLength(1000);
        b.Property(x => x.ImageUrlsCsv).HasMaxLength(4000);
        b.HasOne<AskTicket>().WithMany(t => t.Messages).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasMaxLength(50).IsRequired();
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        b.Property(x => x.DataJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.Channel).HasConversion<int>();

        b.HasIndex(x => new { x.UserId, x.ReadAtUtc, x.CreatedAtUtc });
    }
}

public sealed class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> b)
    {
        b.ToTable("PushSubscriptions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Endpoint).HasMaxLength(2000).IsRequired();
        b.Property(x => x.P256dh).HasMaxLength(255).IsRequired();
        b.Property(x => x.Auth).HasMaxLength(255).IsRequired();
        b.Property(x => x.UserAgent).HasMaxLength(500);

        b.HasIndex(x => x.UserId);
        // Endpoint is unique-ish (one device subscription per browser).
        // Some endpoints exceed SQL Server's 900-byte index limit, so we use a hash column instead.
        // For simplicity here we rely on application-level dedup (delete-then-insert).
    }
}

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> b)
    {
        b.ToTable("NotificationPreferences");
        b.HasKey(x => new { x.UserId, x.Category });
        b.Property(x => x.Category).HasMaxLength(50).IsRequired();

        b.HasIndex(x => x.UserId);
    }
}
