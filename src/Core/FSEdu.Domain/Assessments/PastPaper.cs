using FSEdu.Domain.Academic;
using FSEdu.Domain.Common;
using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Assessments;

// Historical exam papers (last year's final, mid-term, etc.). Independent of
// any course or teacher — managed by admins / content team.
public sealed class PastPaper : AggregateRoot<Guid>, IAuditable, ISoftDelete
{
    public int SubjectId { get; private set; }
    public Subject Subject { get; private set; } = default!;
    public int StageId { get; private set; }
    public Stage Stage { get; private set; } = default!;

    public int Year { get; private set; }                       // e.g. 2024
    public PastPaperTerm Term { get; private set; }             // First | Second | EndOfYear | MidTerm
    public PastPaperExamType ExamType { get; private set; }     // School | Governorate | National | Ministry
    public string? EducationalAdministration { get; private set; } // e.g. "إدارة مصر الجديدة"
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public int DurationMinutes { get; private set; }
    public decimal TotalMarks { get; private set; }
    public string? AnswerKeyUrl { get; private set; }           // optional PDF with full model answer
    public bool Published { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    private readonly List<PastPaperQuestion> _questions = new();
    public IReadOnlyCollection<PastPaperQuestion> Questions => _questions.AsReadOnly();

    private PastPaper() { }

    public PastPaper(Guid id, int subjectId, int stageId, int year, PastPaperTerm term,
                     PastPaperExamType examType, string title, int durationMinutes) : base(id)
    {
        SubjectId = subjectId;
        StageId = stageId;
        Year = year;
        Term = term;
        ExamType = examType;
        Title = title;
        DurationMinutes = durationMinutes;
    }

    public void UpdateMeta(string title, string? description, string? administration,
                           int durationMinutes, string? answerKeyUrl)
    {
        Title = title;
        Description = description;
        EducationalAdministration = administration;
        DurationMinutes = durationMinutes;
        AnswerKeyUrl = answerKeyUrl;
    }

    public void AddQuestion(string body, QuestionType type, string? optionsJson,
                            string correctAnswerJson, decimal marks, string? explanation)
    {
        var order = _questions.Count + 1;
        _questions.Add(new PastPaperQuestion(Guid.NewGuid(), Id, body, type, optionsJson,
            correctAnswerJson, marks, order, explanation));
        TotalMarks += marks;
    }

    public void RemoveQuestion(Guid questionId)
    {
        var q = _questions.FirstOrDefault(x => x.Id == questionId);
        if (q is null) return;
        TotalMarks -= q.Marks;
        _questions.Remove(q);
    }

    public void Publish() => Published = true;
    public void Unpublish() => Published = false;
}

public sealed class PastPaperQuestion
{
    public Guid Id { get; private set; }
    public Guid PastPaperId { get; private set; }
    public string Body { get; private set; } = default!;
    public QuestionType Type { get; private set; }
    public string? OptionsJson { get; private set; }       // MCQ: ["a","b","c","d"]
    public string CorrectAnswerJson { get; private set; } = default!;  // varies per type
    public decimal Marks { get; private set; }
    public int OrderNum { get; private set; }
    public string? Explanation { get; private set; }       // shown after submission

    private PastPaperQuestion() { }

    public PastPaperQuestion(Guid id, Guid pastPaperId, string body, QuestionType type,
                             string? optionsJson, string correctAnswerJson,
                             decimal marks, int order, string? explanation)
    {
        Id = id;
        PastPaperId = pastPaperId;
        Body = body;
        Type = type;
        OptionsJson = optionsJson;
        CorrectAnswerJson = correctAnswerJson;
        Marks = marks;
        OrderNum = order;
        Explanation = explanation;
    }
}

public sealed class PastPaperAttempt : AggregateRoot<Guid>
{
    public Guid PastPaperId { get; private set; }
    public PastPaper PastPaper { get; private set; } = default!;
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public decimal Score { get; private set; }
    public decimal MaxScore { get; private set; }
    public string? AnswersJson { get; private set; }
    public AttemptStatus Status { get; private set; } = AttemptStatus.InProgress;

    private PastPaperAttempt() { }

    public PastPaperAttempt(Guid id, Guid pastPaperId, Guid studentId, decimal maxScore) : base(id)
    {
        PastPaperId = pastPaperId;
        StudentId = studentId;
        MaxScore = maxScore;
        StartedAtUtc = DateTime.UtcNow;
    }

    public void Submit(string answersJson, decimal score)
    {
        AnswersJson = answersJson;
        Score = score;
        SubmittedAtUtc = DateTime.UtcNow;
        Status = AttemptStatus.Graded;
    }

    public void Expire() => Status = AttemptStatus.Expired;
}

public enum PastPaperTerm
{
    FirstTerm = 1,
    SecondTerm = 2,
    EndOfYear = 3,
    MidTerm = 4,
}

public enum PastPaperExamType
{
    School = 1,         // امتحان مدرسة
    Administration = 2, // امتحان إدارة تعليمية
    Governorate = 3,    // امتحان محافظة
    National = 4,       // امتحان قومى/شهادة
    Ministry = 5,       // امتحان وزارة
}
