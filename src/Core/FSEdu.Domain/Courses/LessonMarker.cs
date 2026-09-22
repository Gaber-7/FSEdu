using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Courses;

// A timestamp the student saved while watching a lesson — like a bookmark
// at a specific moment in the video. Used as a private study tool.
public sealed class LessonMarker : Entity<Guid>
{
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;
    public Guid LessonId { get; private set; }
    public Lesson Lesson { get; private set; } = default!;
    public int PositionSec { get; private set; }
    public string? Label { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private LessonMarker() { }

    public LessonMarker(Guid id, Guid studentId, Guid lessonId, int positionSec, string? label) : base(id)
    {
        StudentId = studentId;
        LessonId = lessonId;
        PositionSec = Math.Max(0, positionSec);
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateLabel(string? label)
    {
        Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
    }
}
