using FSEdu.Domain.Assessments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSEdu.Persistence.Configurations;

public sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> b)
    {
        b.ToTable("Questions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.Difficulty).HasConversion<int>();
        b.Property(x => x.QuestionJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.CorrectAnswerJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.Explanation).HasMaxLength(4000);
        b.Property(x => x.TagsCsv).HasMaxLength(500);

        b.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.SubjectId, x.StageId, x.Difficulty });
    }
}

public sealed class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> b)
    {
        b.ToTable("Assessments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.TotalMarks).HasColumnType("decimal(6,2)");
        b.Property(x => x.PassingMarks).HasColumnType("decimal(6,2)");
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.ShowResults).HasMaxLength(20);

        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AssessmentQuestionConfiguration : IEntityTypeConfiguration<AssessmentQuestion>
{
    public void Configure(EntityTypeBuilder<AssessmentQuestion> b)
    {
        b.ToTable("AssessmentQuestions");
        b.HasKey(x => new { x.AssessmentId, x.QuestionId });
        b.Property(x => x.Marks).HasColumnType("decimal(6,2)");
        b.HasOne<Assessment>().WithMany(a => a.Questions).HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Question).WithMany().HasForeignKey(x => x.QuestionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AssessmentAttemptConfiguration : IEntityTypeConfiguration<AssessmentAttempt>
{
    public void Configure(EntityTypeBuilder<AssessmentAttempt> b)
    {
        b.ToTable("AssessmentAttempts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Score).HasColumnType("decimal(6,2)");
        b.Property(x => x.AnswersJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.Status).HasConversion<int>();

        b.HasOne(x => x.Assessment).WithMany().HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.StudentId, x.SubmittedAtUtc });
    }
}

public sealed class PastPaperConfiguration : IEntityTypeConfiguration<PastPaper>
{
    public void Configure(EntityTypeBuilder<PastPaper> b)
    {
        b.ToTable("PastPapers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).HasMaxLength(255).IsRequired();
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.EducationalAdministration).HasMaxLength(255);
        b.Property(x => x.AnswerKeyUrl).HasMaxLength(1000);
        b.Property(x => x.Term).HasConversion<int>();
        b.Property(x => x.ExamType).HasConversion<int>();
        b.Property(x => x.TotalMarks).HasColumnType("decimal(10,2)");

        b.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);

        b.HasQueryFilter(x => x.DeletedAtUtc == null);
        b.HasIndex(x => new { x.StageId, x.SubjectId, x.Year, x.Term });
        b.HasIndex(x => new { x.Published, x.Year });
    }
}

public sealed class PastPaperQuestionConfiguration : IEntityTypeConfiguration<PastPaperQuestion>
{
    public void Configure(EntityTypeBuilder<PastPaperQuestion> b)
    {
        b.ToTable("PastPaperQuestions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.Type).HasConversion<int>();
        b.Property(x => x.OptionsJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.CorrectAnswerJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.Marks).HasColumnType("decimal(10,2)");
        b.Property(x => x.Explanation).HasColumnType("nvarchar(max)");

        b.HasOne<PastPaper>().WithMany(p => p.Questions).HasForeignKey(x => x.PastPaperId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.PastPaperId, x.OrderNum });
    }
}

public sealed class PastPaperAttemptConfiguration : IEntityTypeConfiguration<PastPaperAttempt>
{
    public void Configure(EntityTypeBuilder<PastPaperAttempt> b)
    {
        b.ToTable("PastPaperAttempts");
        b.HasKey(x => x.Id);
        b.Property(x => x.Score).HasColumnType("decimal(10,2)");
        b.Property(x => x.MaxScore).HasColumnType("decimal(10,2)");
        b.Property(x => x.AnswersJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.Status).HasConversion<int>();

        b.HasOne(x => x.PastPaper).WithMany().HasForeignKey(x => x.PastPaperId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.StudentId, x.SubmittedAtUtc });
        b.HasIndex(x => new { x.PastPaperId, x.StudentId });
    }
}
