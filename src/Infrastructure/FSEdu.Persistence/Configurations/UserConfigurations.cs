using FSEdu.Domain.Common;
using FSEdu.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSEdu.Persistence.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        b.Property(x => x.AvatarUrl).HasMaxLength(500);
        b.Property(x => x.Locale).HasMaxLength(5);
        b.Property(x => x.Timezone).HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Gender).HasConversion<int?>();

        // Email value object
        b.OwnsOne(x => x.Email, email =>
        {
            email.Property(p => p.Value).HasColumnName("Email").HasMaxLength(256);
            email.HasIndex(p => p.Value).IsUnique().HasFilter("[Email] IS NOT NULL");
        });

        // Phone value object
        b.OwnsOne(x => x.Phone, phone =>
        {
            phone.Property(p => p.Value).HasColumnName("Phone").HasMaxLength(20).IsRequired();
            phone.HasIndex(p => p.Value).IsUnique();
        });

        b.Property(x => x.WhatsAppNumber).HasMaxLength(20);

        b.Property(x => x.TotpSecret).HasMaxLength(64);
        b.Property(x => x.TotpRecoveryCodesHashed).HasMaxLength(2000);

        b.HasQueryFilter(x => x.DeletedAtUtc == null);

        b.HasDiscriminator<string>("UserType")
            .HasValue<Student>("Student")
            .HasValue<Parent>("Parent")
            .HasValue<Teacher>("Teacher")
            .HasValue<AppUser>("Generic");
    }
}

public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> b)
    {
        b.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Region).WithMany().HasForeignKey(x => x.RegionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.SetNull);

        b.Property(x => x.ReferralCode).HasMaxLength(16);
        b.Property(x => x.ReferredByCode).HasMaxLength(16);
        b.HasIndex(x => x.ReferralCode).IsUnique().HasFilter("[ReferralCode] IS NOT NULL");
        b.HasIndex(x => x.ReferredByCode).HasFilter("[ReferredByCode] IS NOT NULL");
    }
}

public sealed class ReferralConfiguration : IEntityTypeConfiguration<Referral>
{
    public void Configure(EntityTypeBuilder<Referral> b)
    {
        b.ToTable("Referrals");
        b.HasKey(x => x.Id);

        b.HasOne(x => x.ReferrerStudent).WithMany().HasForeignKey(x => x.ReferrerStudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReferredStudent).WithMany().HasForeignKey(x => x.ReferredStudentId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.ReferredStudentId).IsUnique();   // a student can only be referred once
        b.HasIndex(x => x.ReferrerStudentId);
    }
}

public sealed class TeacherConfiguration : IEntityTypeConfiguration<Teacher>
{
    public void Configure(EntityTypeBuilder<Teacher> b)
    {
        b.Property(x => x.Bio).HasMaxLength(2000);
        b.Property(x => x.RatingAvg).HasColumnType("decimal(3,2)");
        b.Property(x => x.CommissionRate).HasColumnType("decimal(5,2)");
    }
}

public sealed class ParentConfiguration : IEntityTypeConfiguration<Parent>
{
    public void Configure(EntityTypeBuilder<Parent> b)
    {
        b.Property(x => x.NationalId).HasMaxLength(20);
        b.Property(x => x.Occupation).HasMaxLength(100);
    }
}

public sealed class ParentStudentLinkConfiguration : IEntityTypeConfiguration<ParentStudentLink>
{
    public void Configure(EntityTypeBuilder<ParentStudentLink> b)
    {
        b.ToTable("ParentStudentLinks");
        b.HasKey(x => new { x.ParentId, x.StudentId });
        b.Property(x => x.Relation).HasConversion<int>();

        b.HasOne(x => x.Parent)
         .WithMany(p => p.Children)
         .HasForeignKey(x => x.ParentId)
         .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.Student)
         .WithMany(s => s.Parents)
         .HasForeignKey(x => x.StudentId)
         .OnDelete(DeleteBehavior.NoAction);
    }
}

public sealed class TeacherSubjectConfiguration : IEntityTypeConfiguration<TeacherSubject>
{
    public void Configure(EntityTypeBuilder<TeacherSubject> b)
    {
        b.ToTable("TeacherSubjects");
        b.HasKey(x => new { x.TeacherId, x.SubjectId });
        b.HasOne<Teacher>().WithMany(t => t.Subjects).HasForeignKey(x => x.TeacherId);
        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId);
    }
}

public sealed class TeacherRegionConfiguration : IEntityTypeConfiguration<TeacherRegion>
{
    public void Configure(EntityTypeBuilder<TeacherRegion> b)
    {
        b.ToTable("TeacherRegions");
        b.HasKey(x => new { x.TeacherId, x.RegionId });
        b.HasOne<Teacher>().WithMany(t => t.Regions).HasForeignKey(x => x.TeacherId);
        b.HasOne(x => x.Region).WithMany().HasForeignKey(x => x.RegionId);
    }
}

public sealed class TeacherQualificationConfiguration : IEntityTypeConfiguration<TeacherQualification>
{
    public void Configure(EntityTypeBuilder<TeacherQualification> b)
    {
        b.ToTable("TeacherQualifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Institution).HasMaxLength(200).IsRequired();
        b.Property(x => x.DocumentUrl).HasMaxLength(500);
        b.HasOne<Teacher>().WithMany(t => t.Qualifications).HasForeignKey(x => x.TeacherId);
    }
}
