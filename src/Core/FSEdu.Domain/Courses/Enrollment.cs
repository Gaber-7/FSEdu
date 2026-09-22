using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Courses;

public sealed class Enrollment : AggregateRoot<Guid>
{
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;
    public Guid CourseId { get; private set; }
    public Course Course { get; private set; } = default!;
    public DateTime EnrolledAtUtc { get; private set; }
    public decimal ProgressPct { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    private Enrollment() { }

    public Enrollment(Guid id, Guid studentId, Guid courseId) : base(id)
    {
        StudentId = studentId;
        CourseId = courseId;
        EnrolledAtUtc = DateTime.UtcNow;
    }

    public void UpdateProgress(decimal pct)
    {
        ProgressPct = Math.Clamp(pct, 0, 100);
        if (ProgressPct >= 100 && CompletedAtUtc is null) CompletedAtUtc = DateTime.UtcNow;
    }
}

public sealed class LessonProgress
{
    public Guid StudentId { get; private set; }
    public Guid LessonId { get; private set; }
    public int LastPositionSec { get; private set; }
    public bool Completed { get; private set; }
    public decimal WatchedPct { get; private set; }
    public DateTime FirstViewedAtUtc { get; private set; }
    public DateTime LastViewedAtUtc { get; private set; }

    private LessonProgress() { }

    public LessonProgress(Guid studentId, Guid lessonId)
    {
        StudentId = studentId;
        LessonId = lessonId;
        FirstViewedAtUtc = DateTime.UtcNow;
        LastViewedAtUtc = DateTime.UtcNow;
    }

    public void UpdatePosition(int seconds, decimal watchedPct)
    {
        LastPositionSec = seconds;
        WatchedPct = Math.Clamp(watchedPct, 0, 100);
        LastViewedAtUtc = DateTime.UtcNow;
        if (WatchedPct >= 90) Completed = true;
    }
}
