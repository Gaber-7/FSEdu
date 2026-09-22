using FSEdu.Domain.LiveClassroom;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSEdu.Persistence.Configurations;

public sealed class LiveSessionConfiguration : IEntityTypeConfiguration<LiveSession>
{
    public void Configure(EntityTypeBuilder<LiveSession> b)
    {
        b.ToTable("LiveSessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.RoomId).HasMaxLength(100).IsRequired();
        b.Property(x => x.RecordingUrl).HasMaxLength(1000);
        b.Property(x => x.RecordingStatus).HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<int>();

        b.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.ScheduledAtUtc, x.Status });
        b.HasIndex(x => x.RoomId);
        b.HasIndex(x => new { x.SubjectId, x.Status });
    }
}

public sealed class LiveAttendanceConfiguration : IEntityTypeConfiguration<LiveAttendance>
{
    public void Configure(EntityTypeBuilder<LiveAttendance> b)
    {
        b.ToTable("LiveAttendance");
        b.HasKey(x => new { x.SessionId, x.StudentId });
        b.Property(x => x.AttentionScore).HasColumnType("decimal(5,2)");
        b.HasOne<LiveSession>().WithMany(s => s.Attendance).HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
