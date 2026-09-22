using FSEdu.Domain.Users;

namespace FSEdu.Domain.Courses;

// Per-student saved courses ("محفوظاتي"). One row per (Student, Course).
// Toggle pattern: save creates the row, unsave deletes it.
public sealed class CourseBookmark
{
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;
    public Guid CourseId { get; private set; }
    public Course Course { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    private CourseBookmark() { }

    public CourseBookmark(Guid studentId, Guid courseId)
    {
        StudentId = studentId;
        CourseId = courseId;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
