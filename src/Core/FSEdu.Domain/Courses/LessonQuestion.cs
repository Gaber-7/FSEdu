using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Courses;

// Per-lesson Q&A. Any user with access to the lesson can ask. Course teacher
// or an admin can answer. One answer per question (no threading).
public sealed class LessonQuestion : AggregateRoot<Guid>
{
    public Guid LessonId { get; private set; }
    public Lesson Lesson { get; private set; } = default!;
    public Guid AskedByUserId { get; private set; }
    public string AskedByName { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    public string? AnswerBody { get; private set; }
    public Guid? AnsweredByUserId { get; private set; }
    public string? AnsweredByName { get; private set; }
    public DateTime? AnsweredAtUtc { get; private set; }

    private LessonQuestion() { }

    public LessonQuestion(Guid id, Guid lessonId, Guid askedByUserId, string askedByName, string body)
        : base(id)
    {
        LessonId = lessonId;
        AskedByUserId = askedByUserId;
        AskedByName = askedByName;
        Body = body.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Answer(Guid byUserId, string byName, string body)
    {
        AnswerBody = body.Trim();
        AnsweredByUserId = byUserId;
        AnsweredByName = byName;
        AnsweredAtUtc = DateTime.UtcNow;
    }

    public bool IsAnswered => !string.IsNullOrEmpty(AnswerBody);
}
