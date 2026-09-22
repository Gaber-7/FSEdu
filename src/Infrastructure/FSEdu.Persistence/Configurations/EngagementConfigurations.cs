using FSEdu.Domain.Engagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSEdu.Persistence.Configurations;

public sealed class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> b)
    {
        b.ToTable("Badges");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(100).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(100);
        b.Property(x => x.IconUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.CriteriaJson).HasColumnType("nvarchar(max)");
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class StudentBadgeConfiguration : IEntityTypeConfiguration<StudentBadge>
{
    public void Configure(EntityTypeBuilder<StudentBadge> b)
    {
        b.ToTable("StudentBadges");
        b.HasKey(x => new { x.StudentId, x.BadgeId });
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Badge).WithMany().HasForeignKey(x => x.BadgeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DailyChallengeConfiguration : IEntityTypeConfiguration<DailyChallenge>
{
    public void Configure(EntityTypeBuilder<DailyChallenge> b)
    {
        b.ToTable("DailyChallenges");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<int>();

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        // One challenge per (student, day)
        b.HasIndex(x => new { x.StudentId, x.Day }).IsUnique();
    }
}

public sealed class WeeklyChallengeConfiguration : IEntityTypeConfiguration<WeeklyChallenge>
{
    public void Configure(EntityTypeBuilder<WeeklyChallenge> b)
    {
        b.ToTable("WeeklyChallenges");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.TargetMetric).HasMaxLength(50).IsRequired();
    }
}

public sealed class CommunityConfiguration : IEntityTypeConfiguration<Community>
{
    public void Configure(EntityTypeBuilder<Community> b)
    {
        b.ToTable("Communities");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CommunityMemberConfiguration : IEntityTypeConfiguration<CommunityMember>
{
    public void Configure(EntityTypeBuilder<CommunityMember> b)
    {
        b.ToTable("CommunityMembers");
        b.HasKey(x => new { x.CommunityId, x.UserId });
        b.Property(x => x.Role).HasConversion<int>();
        b.HasOne<Community>().WithMany(c => c.Members).HasForeignKey(x => x.CommunityId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
