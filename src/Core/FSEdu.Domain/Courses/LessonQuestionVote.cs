using FSEdu.Domain.Users;

namespace FSEdu.Domain.Courses;

// One row per (Student, Question). Presence == upvote. No downvote.
// Used to surface the most-relevant questions in the lesson Q&A view.
public sealed class LessonQuestionVote
{
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;
    public Guid QuestionId { get; private set; }
    public LessonQuestion Question { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    private LessonQuestionVote() { }

    public LessonQuestionVote(Guid studentId, Guid questionId)
    {
        StudentId = studentId;
        QuestionId = questionId;
        CreatedAtUtc = DateTime.UtcNow;
    }
}
