using FSEdu.Domain.Academic;
using FSEdu.Domain.Common;
using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Courses;

public sealed class Course : AggregateRoot<Guid>, IAuditable, ISoftDelete
{
    public Guid TeacherId { get; private set; }
    public Teacher Teacher { get; private set; } = default!;
    public int SubjectId { get; private set; }
    public Subject Subject { get; private set; } = default!;
    public int StageId { get; private set; }
    public Stage Stage { get; private set; } = default!;

    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public string? PreviewVideoUrl { get; private set; }   // short promo / introduction video (e.g., YouTube)
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "EGP";
    public CourseStatus Status { get; private set; } = CourseStatus.Draft;
    public AcademicTerm Term { get; private set; } = AcademicTerm.Annual;
    public int EnrollmentCount { get; private set; }
    public decimal RatingAvg { get; private set; }
    public int RatingsCount { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    private readonly List<Chapter> _chapters = new();
    public IReadOnlyCollection<Chapter> Chapters => _chapters.AsReadOnly();

    private Course() { }

    public Course(Guid id, Guid teacherId, int subjectId, int stageId, string title,
                  decimal price, AcademicTerm term = AcademicTerm.Annual) : base(id)
    {
        TeacherId = teacherId;
        SubjectId = subjectId;
        StageId = stageId;
        Title = title;
        Price = price;
        Term = term;
    }

    public void UpdateDetails(string title, string? description, string? thumbnailUrl, decimal price)
    {
        Title = title;
        Description = description;
        ThumbnailUrl = thumbnailUrl;
        Price = price;
    }

    public void SetPreviewVideo(string? url)
    {
        PreviewVideoUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
    }

    public void SubmitForReview() => Status = CourseStatus.PendingReview;
    public void Publish() => Status = CourseStatus.Published;
    public void Reject() => Status = CourseStatus.Rejected;
    public void Archive() => Status = CourseStatus.Archived;

    public void AddChapter(string title, int order)
    {
        var chapter = new Chapter(Guid.NewGuid(), Id, title, order);
        _chapters.Add(chapter);
    }

    public void IncrementEnrollment() => EnrollmentCount++;

    public void AddRating(decimal rating)
    {
        var total = RatingAvg * RatingsCount + rating;
        RatingsCount++;
        RatingAvg = Math.Round(total / RatingsCount, 2);
    }

    public void SetRatingStats(int count, decimal avg)
    {
        RatingsCount = count;
        RatingAvg = Math.Round(avg, 2);
    }
}
