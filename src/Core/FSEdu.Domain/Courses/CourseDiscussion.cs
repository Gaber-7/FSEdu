using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Courses;

// Student-to-student discussion thread for a course (different from
// Lesson Q&A which is student-to-teacher). Anyone with access to the
// course can start a thread; teacher/admin can pin or lock threads.
public sealed class CourseDiscussion : AggregateRoot<Guid>
{
    public Guid CourseId { get; private set; }
    public Course Course { get; private set; } = default!;
    public Guid AuthorUserId { get; private set; }
    public string AuthorName { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public bool Pinned { get; private set; }
    public bool Locked { get; private set; }
    public int RepliesCount { get; private set; }
    public DateTime LastActivityAtUtc { get; private set; }

    private CourseDiscussion() { }

    public CourseDiscussion(Guid id, Guid courseId, Guid authorUserId, string authorName,
                             string title, string body) : base(id)
    {
        CourseId = courseId;
        AuthorUserId = authorUserId;
        AuthorName = authorName;
        Title = title.Trim();
        Body = body.Trim();
        CreatedAtUtc = DateTime.UtcNow;
        LastActivityAtUtc = CreatedAtUtc;
    }

    public void Edit(string title, string body)
    {
        Title = title.Trim();
        Body = body.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Pin() => Pinned = true;
    public void Unpin() => Pinned = false;
    public void Lock() => Locked = true;
    public void Unlock() => Locked = false;

    public void RecordReplyAdded()
    {
        RepliesCount++;
        LastActivityAtUtc = DateTime.UtcNow;
    }

    public void RecordReplyRemoved()
    {
        if (RepliesCount > 0) RepliesCount--;
    }
}

public sealed class CourseDiscussionReply : Entity<Guid>
{
    public Guid DiscussionId { get; private set; }
    public CourseDiscussion Discussion { get; private set; } = default!;
    public Guid AuthorUserId { get; private set; }
    public string AuthorName { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    private CourseDiscussionReply() { }

    public CourseDiscussionReply(Guid id, Guid discussionId, Guid authorUserId, string authorName, string body)
        : base(id)
    {
        DiscussionId = discussionId;
        AuthorUserId = authorUserId;
        AuthorName = authorName;
        Body = body.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }
}
