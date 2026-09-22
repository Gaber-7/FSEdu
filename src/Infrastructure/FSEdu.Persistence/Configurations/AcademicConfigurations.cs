using FSEdu.Domain.Academic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSEdu.Persistence.Configurations;

public sealed class StageConfiguration : IEntityTypeConfiguration<Stage>
{
    public void Configure(EntityTypeBuilder<Stage> b)
    {
        b.ToTable("Stages");
        b.HasKey(x => x.Id);
        b.Property(x => x.NameAr).HasMaxLength(100).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(100);
        b.Property(x => x.FullTermPriceEgp).HasColumnType("decimal(10,2)");
        b.HasOne(x => x.ParentStage)
         .WithMany(x => x.Children)
         .HasForeignKey(x => x.ParentStageId)
         .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.LevelOrder);
    }
}

public sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> b)
    {
        b.ToTable("Subjects");
        b.HasKey(x => x.Id);
        b.Property(x => x.NameAr).HasMaxLength(100).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(100);
        b.Property(x => x.IconUrl).HasMaxLength(500);
        b.Property(x => x.ColorHex).HasMaxLength(7);
        b.Property(x => x.MonthlyPriceEgp).HasColumnType("decimal(10,2)");
        b.Property(x => x.TermPriceEgp).HasColumnType("decimal(10,2)");
        b.HasOne(x => x.Stage)
         .WithMany(x => x.Subjects)
         .HasForeignKey(x => x.StageId)
         .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.StageId, x.NameAr });
    }
}

public sealed class RegionConfiguration : IEntityTypeConfiguration<Region>
{
    public void Configure(EntityTypeBuilder<Region> b)
    {
        b.ToTable("Regions");
        b.HasKey(x => x.Id);
        b.Property(x => x.NameAr).HasMaxLength(100).IsRequired();
        b.Property(x => x.NameEn).HasMaxLength(100);
        b.Property(x => x.Code).HasMaxLength(10).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> b)
    {
        b.ToTable("Schools");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Type).HasConversion<int>();
        b.HasOne(x => x.Region)
         .WithMany()
         .HasForeignKey(x => x.RegionId)
         .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.RegionId, x.Name });
    }
}
