using FSEdu.Domain.Common;
using FSEdu.Domain.Courses;
using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Assessments;

public sealed class Assessment : AggregateRoot<Guid>, IAuditable
{
    public Guid? CourseId { get; private set; }
    public Course? Course { get; private set; }
    public Guid TeacherId { get; private set; }
    public Teacher Teacher { get; private set; } = default!;
    public AssessmentType Type { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public int? TimeLimitMinutes { get; private set; }
    public decimal TotalMarks { get; private set; }
    public decimal PassingMarks { get; private set; }
    public DateTime? AvailableFromUtc { get; private set; }
    public DateTime? AvailableToUtc { get; private set; }
    public int AttemptsAllowed { get; private set; } = 1;
    public bool ShuffleQuestions { get; private set; } = true;
    public string ShowResults { get; private set; } = "Immediately";

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    private readonly List<AssessmentQuestion> _questions = new();
    public IReadOnlyCollection<AssessmentQuestion> Questions => _questions.AsReadOnly();

    private Assessment() { }

    public Assessment(Guid id, Guid teacherId, AssessmentType type, string title,
                      decimal totalMarks, decimal passingMarks) : base(id)
    {
        TeacherId = teacherId;
        Type = type;
        Title = title;
        TotalMarks = totalMarks;
        PassingMarks = passingMarks;
    }

    public void AddQuestion(Guid questionId, decimal marks, int order)
    {
        _questions.Add(new AssessmentQuestion(Id, questionId, marks, order));
        TotalMarks += marks;
    }

    public void RemoveQuestion(Guid questionId)
    {
        var q = _questions.FirstOrDefault(x => x.QuestionId == questionId);
        if (q is not null)
        {
            TotalMarks -= q.Marks;
            _questions.Remove(q);
        }
    }

    public void SetAvailability(DateTime? from, DateTime? to)
    {
        AvailableFromUtc = from;
        AvailableToUtc = to;
    }

    public void Configure(Guid? courseId, string? description, int? timeLimitMinutes, int attemptsAllowed)
    {
        CourseId = courseId;
        Description = description;
        TimeLimitMinutes = timeLimitMinutes;
        AttemptsAllowed = attemptsAllowed;
    }

    public bool IsAvailableNow(DateTime nowUtc) =>
        (AvailableFromUtc is null || AvailableFromUtc <= nowUtc) &&
        (AvailableToUtc is null || AvailableToUtc >= nowUtc);
}

public sealed class AssessmentQuestion
{
    public Guid AssessmentId { get; private set; }
    public Guid QuestionId { get; private set; }
    public Question Question { get; private set; } = default!;
    public decimal Marks { get; private set; }
    public int OrderNum { get; private set; }

    private AssessmentQuestion() { }

    public AssessmentQuestion(Guid assessmentId, Guid questionId, decimal marks, int order)
    {
        AssessmentId = assessmentId;
        QuestionId = questionId;
        Marks = marks;
        OrderNum = order;
    }
}

public sealed class AssessmentAttempt : AggregateRoot<Guid>
{
    public Guid AssessmentId { get; private set; }
    public Assessment Assessment { get; private set; } = default!;
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public decimal Score { get; private set; }
    public bool AutoGraded { get; private set; }
    public string? AnswersJson { get; private set; }
    public AttemptStatus Status { get; private set; } = AttemptStatus.InProgress;

    private AssessmentAttempt() { }

    public AssessmentAttempt(Guid id, Guid assessmentId, Guid studentId) : base(id)
    {
        AssessmentId = assessmentId;
        StudentId = studentId;
        StartedAtUtc = DateTime.UtcNow;
    }

    public void Submit(string answersJson, decimal score, bool autoGraded)
    {
        AnswersJson = answersJson;
        Score = score;
        AutoGraded = autoGraded;
        SubmittedAtUtc = DateTime.UtcNow;
        Status = autoGraded ? AttemptStatus.Graded : AttemptStatus.Submitted;
    }

    public void ManualGrade(decimal score)
    {
        Score = score;
        Status = AttemptStatus.Graded;
    }

    public void Expire() => Status = AttemptStatus.Expired;
}
