using FSEdu.Domain.Users;

namespace FSEdu.Domain.Courses;

// One review per (Student, Course). Updates overwrite. Course.RatingAvg
// is recomputed from this table whenever a review is written.
public sealed class CourseReview
{
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;
    public Guid CourseId { get; private set; }
    public Course Course { get; private set; } = default!;
    public int Rating { get; private set; }      // 1..5
    public string? Comment { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private CourseReview() { }

    public CourseReview(Guid studentId, Guid courseId, int rating, string? comment)
    {
        StudentId = studentId;
        CourseId = courseId;
        Rating = ClampRating(rating);
        Comment = comment?.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Update(int rating, string? comment)
    {
        Rating = ClampRating(rating);
        Comment = comment?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static int ClampRating(int r) => Math.Clamp(r, 1, 5);
}
