using FSEdu.Domain.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSEdu.Persistence.Configurations;

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> b)
    {
        b.ToTable("Courses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.ThumbnailUrl).HasMaxLength(500);
        b.Property(x => x.PreviewVideoUrl).HasMaxLength(1000);
        b.Property(x => x.Price).HasColumnType("decimal(10,2)");
        b.Property(x => x.Currency).HasMaxLength(3);
        b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Term).HasConversion<int>();
        b.Property(x => x.RatingAvg).HasColumnType("decimal(3,2)");

        b.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);

        b.HasQueryFilter(x => x.DeletedAtUtc == null);
        b.HasIndex(x => new { x.StageId, x.SubjectId, x.Status });
        b.HasIndex(x => x.TeacherId);
    }
}

public sealed class ChapterConfiguration : IEntityTypeConfiguration<Chapter>
{
    public void Configure(EntityTypeBuilder<Chapter> b)
    {
        b.ToTable("Chapters");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.HasOne(x => x.Course).WithMany(c => c.Chapters).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> b)
    {
        b.ToTable("Lessons");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.VideoUrl).HasMaxLength(1000);
        b.Property(x => x.VideoDrmKeyId).HasMaxLength(100);
        b.Property(x => x.Type).HasConversion<int>();

        b.HasOne(x => x.Chapter).WithMany(c => c.Lessons).HasForeignKey(x => x.ChapterId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.ChapterId, x.OrderNum });
        b.HasIndex(x => x.ScheduledAtUtc).HasFilter("[Type] = 2");
    }
}

public sealed class LessonAttachmentConfiguration : IEntityTypeConfiguration<LessonAttachment>
{
    public void Configure(EntityTypeBuilder<LessonAttachment> b)
    {
        b.ToTable("LessonAttachments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.FileUrl).HasMaxLength(1000).IsRequired();
        b.Property(x => x.FileType).HasMaxLength(20).IsRequired();
        b.HasOne<Lesson>().WithMany(l => l.Attachments).HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> b)
    {
        b.ToTable("Enrollments");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProgressPct).HasColumnType("decimal(5,2)");

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
    }
}

public sealed class LessonProgressConfiguration : IEntityTypeConfiguration<LessonProgress>
{
    public void Configure(EntityTypeBuilder<LessonProgress> b)
    {
        b.ToTable("LessonProgress");
        b.HasKey(x => new { x.StudentId, x.LessonId });
        b.Property(x => x.WatchedPct).HasColumnType("decimal(5,2)");
    }
}

public sealed class CourseReviewConfiguration : IEntityTypeConfiguration<CourseReview>
{
    public void Configure(EntityTypeBuilder<CourseReview> b)
    {
        b.ToTable("CourseReviews");
        b.HasKey(x => new { x.StudentId, x.CourseId });
        b.Property(x => x.Comment).HasMaxLength(1500);

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.CourseId, x.CreatedAtUtc });
    }
}

public sealed class LessonQuestionConfiguration : IEntityTypeConfiguration<LessonQuestion>
{
    public void Configure(EntityTypeBuilder<LessonQuestion> b)
    {
        b.ToTable("LessonQuestions");
        b.HasKey(x => x.Id);
        b.Property(x => x.AskedByName).HasMaxLength(255).IsRequired();
        b.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        b.Property(x => x.AnswerBody).HasMaxLength(4000);
        b.Property(x => x.AnsweredByName).HasMaxLength(255);

        b.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.LessonId, x.CreatedAtUtc });
    }
}

public sealed class CourseDiscussionConfiguration : IEntityTypeConfiguration<CourseDiscussion>
{
    public void Configure(EntityTypeBuilder<CourseDiscussion> b)
    {
        b.ToTable("CourseDiscussions");
        b.HasKey(x => x.Id);
        b.Property(x => x.AuthorName).HasMaxLength(255).IsRequired();
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();

        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.CourseId, x.Pinned, x.LastActivityAtUtc });
    }
}

public sealed class CourseDiscussionReplyConfiguration : IEntityTypeConfiguration<CourseDiscussionReply>
{
    public void Configure(EntityTypeBuilder<CourseDiscussionReply> b)
    {
        b.ToTable("CourseDiscussionReplies");
        b.HasKey(x => x.Id);
        b.Property(x => x.AuthorName).HasMaxLength(255).IsRequired();
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();

        b.HasOne(x => x.Discussion).WithMany().HasForeignKey(x => x.DiscussionId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.DiscussionId, x.CreatedAtUtc });
    }
}

public sealed class LessonQuestionVoteConfiguration : IEntityTypeConfiguration<LessonQuestionVote>
{
    public void Configure(EntityTypeBuilder<LessonQuestionVote> b)
    {
        b.ToTable("LessonQuestionVotes");
        b.HasKey(x => new { x.StudentId, x.QuestionId });

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Question).WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.QuestionId);
    }
}

public sealed class LessonReactionConfiguration : IEntityTypeConfiguration<LessonReaction>
{
    public void Configure(EntityTypeBuilder<LessonReaction> b)
    {
        b.ToTable("LessonReactions");
        b.HasKey(x => new { x.StudentId, x.LessonId, x.Type });
        b.Property(x => x.Type).HasConversion<int>();

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.LessonId, x.Type });
    }
}

public sealed class LessonMarkerConfiguration : IEntityTypeConfiguration<LessonMarker>
{
    public void Configure(EntityTypeBuilder<LessonMarker> b)
    {
        b.ToTable("LessonMarkers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Label).HasMaxLength(255);

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.StudentId, x.LessonId, x.PositionSec });
    }
}

public sealed class CourseBookmarkConfiguration : IEntityTypeConfiguration<CourseBookmark>
{
    public void Configure(EntityTypeBuilder<CourseBookmark> b)
    {
        b.ToTable("CourseBookmarks");
        b.HasKey(x => new { x.StudentId, x.CourseId });

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.StudentId, x.CreatedAtUtc });
    }
}

public sealed class CourseAnnouncementConfiguration : IEntityTypeConfiguration<CourseAnnouncement>
{
    public void Configure(EntityTypeBuilder<CourseAnnouncement> b)
    {
        b.ToTable("CourseAnnouncements");
        b.HasKey(x => x.Id);
        b.Property(x => x.AuthorName).HasMaxLength(255).IsRequired();
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();

        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.CourseId, x.Pinned, x.CreatedAtUtc });
    }
}

public sealed class LessonNoteConfiguration : IEntityTypeConfiguration<LessonNote>
{
    public void Configure(EntityTypeBuilder<LessonNote> b)
    {
        b.ToTable("LessonNotes");
        b.HasKey(x => new { x.StudentId, x.LessonId });
        // nvarchar caps at 4000; use nvarchar(max) for long-form text.
        b.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Lesson).WithMany().HasForeignKey(x => x.LessonId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.StudentId, x.UpdatedAtUtc });
    }
}

public sealed class HomeworkConfiguration : IEntityTypeConfiguration<Homework>
{
    public void Configure(EntityTypeBuilder<Homework> b)
    {
        b.ToTable("Homeworks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.AttachmentUrl).HasMaxLength(1000);
        b.Property(x => x.MaxScore).HasColumnType("decimal(10,2)");

        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);

        b.HasQueryFilter(x => x.DeletedAtUtc == null);
        b.HasIndex(x => new { x.CourseId, x.DueDateUtc });
        b.HasIndex(x => new { x.TeacherId, x.DueDateUtc });
    }
}

public sealed class HomeworkSubmissionConfiguration : IEntityTypeConfiguration<HomeworkSubmission>
{
    public void Configure(EntityTypeBuilder<HomeworkSubmission> b)
    {
        b.ToTable("HomeworkSubmissions");
        b.HasKey(x => x.Id);
        // nvarchar caps at 4000; use nvarchar(max) for long-form text.
        b.Property(x => x.Body).HasColumnType("nvarchar(max)");
        b.Property(x => x.AttachmentUrl).HasMaxLength(1000);
        b.Property(x => x.Score).HasColumnType("decimal(10,2)");
        b.Property(x => x.FeedbackBody).HasMaxLength(4000);

        b.HasOne(x => x.Homework).WithMany().HasForeignKey(x => x.HomeworkId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        // One submission per (homework, student) — student edits in place
        b.HasIndex(x => new { x.HomeworkId, x.StudentId }).IsUnique();
        b.HasIndex(x => new { x.StudentId, x.SubmittedAtUtc });
    }
}

public sealed class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> b)
    {
        b.ToTable("Certificates");
        b.HasKey(x => x.Id);
        b.Property(x => x.CertificateNumber).HasMaxLength(40).IsRequired();
        b.Property(x => x.FinalGrade).HasColumnType("decimal(5,2)");

        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();
        b.HasIndex(x => x.CertificateNumber).IsUnique();
    }
}
