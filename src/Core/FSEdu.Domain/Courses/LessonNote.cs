using FSEdu.Domain.Users;

namespace FSEdu.Domain.Courses;

// Private per-student note for a lesson. One row per (Student, Lesson).
// Auto-saved as the student types — the panel sends the latest body and
// we overwrite. Empty body deletes the row.
public sealed class LessonNote
{
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;
    public Guid LessonId { get; private set; }
    public Lesson Lesson { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private LessonNote() { }

    public LessonNote(Guid studentId, Guid lessonId, string body)
    {
        StudentId = studentId;
        LessonId = lessonId;
        Body = body;
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public void Update(string body)
    {
        Body = body;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
