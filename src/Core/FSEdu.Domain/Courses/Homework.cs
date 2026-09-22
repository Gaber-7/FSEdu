using FSEdu.Domain.Common;
using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Courses;

// A homework assignment created by a teacher for one of their courses.
// Auto-targets every student enrolled in the course; submissions are tracked
// individually via HomeworkSubmission.
public sealed class Homework : AggregateRoot<Guid>, IAuditable, ISoftDelete
{
    public Guid CourseId { get; private set; }
    public Course Course { get; private set; } = default!;
    public Guid TeacherId { get; private set; }
    public Teacher Teacher { get; private set; } = default!;

    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? AttachmentUrl { get; private set; }
    public DateTime DueDateUtc { get; private set; }
    public decimal MaxScore { get; private set; }
    public bool Published { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    private Homework() { }

    public Homework(Guid id, Guid courseId, Guid teacherId, string title,
                    DateTime dueDateUtc, decimal maxScore) : base(id)
    {
        CourseId = courseId;
        TeacherId = teacherId;
        Title = title;
        DueDateUtc = dueDateUtc;
        MaxScore = maxScore;
    }

    public void UpdateDetails(string title, string? description, string? attachmentUrl,
                              DateTime dueDateUtc, decimal maxScore)
    {
        Title = title;
        Description = description;
        AttachmentUrl = attachmentUrl;
        DueDateUtc = dueDateUtc;
        MaxScore = maxScore;
    }

    public void Publish() => Published = true;
    public void Unpublish() => Published = false;

    public bool IsOverdue(DateTime nowUtc) => nowUtc > DueDateUtc;
}

public sealed class HomeworkSubmission : AggregateRoot<Guid>, IAuditable
{
    public Guid HomeworkId { get; private set; }
    public Homework Homework { get; private set; } = default!;
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;

    public DateTime SubmittedAtUtc { get; private set; }
    public string? Body { get; private set; }                  // student's text answer
    public string? AttachmentUrl { get; private set; }         // uploaded file (image of work, pdf, etc.)
    public bool Late { get; private set; }

    public DateTime? GradedAtUtc { get; private set; }
    public Guid? GradedByUserId { get; private set; }
    public decimal? Score { get; private set; }
    public string? FeedbackBody { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    private HomeworkSubmission() { }

    public HomeworkSubmission(Guid id, Guid homeworkId, Guid studentId,
                               string? body, string? attachmentUrl, bool late) : base(id)
    {
        HomeworkId = homeworkId;
        StudentId = studentId;
        SubmittedAtUtc = DateTime.UtcNow;
        Body = body;
        AttachmentUrl = attachmentUrl;
        Late = late;
    }

    public void UpdateContent(string? body, string? attachmentUrl)
    {
        // Student can revise their submission until graded
        if (GradedAtUtc is not null)
            throw new InvalidOperationException("Cannot edit a graded submission");
        Body = body;
        AttachmentUrl = attachmentUrl;
        SubmittedAtUtc = DateTime.UtcNow;
    }

    public void Grade(Guid graderUserId, decimal score, string? feedback)
    {
        GradedByUserId = graderUserId;
        Score = score;
        FeedbackBody = feedback;
        GradedAtUtc = DateTime.UtcNow;
    }
}
