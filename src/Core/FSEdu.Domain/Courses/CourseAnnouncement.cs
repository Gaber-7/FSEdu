using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Courses;

// Per-course announcement posted by the course teacher (or admin).
// Visible to anyone who can access the course; pushed to all enrollees.
public sealed class CourseAnnouncement : AggregateRoot<Guid>
{
    public Guid CourseId { get; private set; }
    public Course Course { get; private set; } = default!;
    public Guid AuthorUserId { get; private set; }
    public string AuthorName { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public bool Pinned { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private CourseAnnouncement() { }

    public CourseAnnouncement(Guid id, Guid courseId, Guid authorUserId, string authorName,
                              string title, string body, bool pinned)
        : base(id)
    {
        CourseId = courseId;
        AuthorUserId = authorUserId;
        AuthorName = authorName;
        Title = title.Trim();
        Body = body.Trim();
        Pinned = pinned;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Update(string title, string body, bool pinned)
    {
        Title = title.Trim();
        Body = body.Trim();
        Pinned = pinned;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
