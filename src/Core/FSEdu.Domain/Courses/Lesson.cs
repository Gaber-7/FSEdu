using FSEdu.Domain.Common;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Courses;

public sealed class Lesson : Entity<Guid>
{
    public Guid ChapterId { get; private set; }
    public Chapter Chapter { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public LessonType Type { get; private set; }
    public int DurationSeconds { get; private set; }
    public string? VideoUrl { get; private set; }
    public string? VideoDrmKeyId { get; private set; }
    public DateTime? ScheduledAtUtc { get; private set; }
    public bool IsFreePreview { get; private set; }
    public int OrderNum { get; private set; }

    private readonly List<LessonAttachment> _attachments = new();
    public IReadOnlyCollection<LessonAttachment> Attachments => _attachments.AsReadOnly();

    private Lesson() { }

    public Lesson(Guid id, Guid chapterId, string title, LessonType type, int order) : base(id)
    {
        ChapterId = chapterId;
        Title = title;
        Type = type;
        OrderNum = order;
    }

    public void SetVideo(string url, string? drmKeyId, int durationSeconds)
    {
        VideoUrl = url;
        VideoDrmKeyId = drmKeyId;
        DurationSeconds = durationSeconds;
    }

    public void Schedule(DateTime utcTime) => ScheduledAtUtc = utcTime;
    public void MarkFreePreview() => IsFreePreview = true;
    public void AddAttachment(LessonAttachment attachment) => _attachments.Add(attachment);
}

public sealed class LessonAttachment : Entity<Guid>
{
    public Guid LessonId { get; private set; }
    public string Title { get; private set; } = default!;
    public string FileUrl { get; private set; } = default!;
    public string FileType { get; private set; } = default!;
    public long SizeBytes { get; private set; }
    public bool DownloadAllowed { get; private set; } = true;

    private LessonAttachment() { }

    public LessonAttachment(Guid id, Guid lessonId, string title, string fileUrl, string fileType,
                             long sizeBytes, bool downloadAllowed = true) : base(id)
    {
        LessonId = lessonId;
        Title = title;
        FileUrl = fileUrl;
        FileType = fileType;
        SizeBytes = sizeBytes;
        DownloadAllowed = downloadAllowed;
    }
}
