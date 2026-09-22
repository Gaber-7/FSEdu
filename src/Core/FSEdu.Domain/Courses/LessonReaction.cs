using FSEdu.Domain.Users;

namespace FSEdu.Domain.Courses;

// Per-student reaction to a lesson. Composite key (Student, Lesson, Type)
// allows a student to have multiple reaction types on the same lesson
// (e.g., both 👍 helpful AND 🎉 loved it).
public sealed class LessonReaction
{
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;
    public Guid LessonId { get; private set; }
    public Lesson Lesson { get; private set; } = default!;
    public ReactionType Type { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private LessonReaction() { }

    public LessonReaction(Guid studentId, Guid lessonId, ReactionType type)
    {
        StudentId = studentId;
        LessonId = lessonId;
        Type = type;
        CreatedAtUtc = DateTime.UtcNow;
    }
}

public enum ReactionType
{
    Helpful = 1,    // 👍
    Confusing = 2,  // 🤔
    Loved = 3       // 🎉
}
