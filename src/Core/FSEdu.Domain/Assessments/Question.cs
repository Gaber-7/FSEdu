using FSEdu.Domain.Academic;
using FSEdu.Domain.Common;
using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Assessments;

public sealed class Question : AggregateRoot<Guid>, IAuditable
{
    public Guid? TeacherId { get; private set; }
    public Teacher? Teacher { get; private set; }
    public int SubjectId { get; private set; }
    public Subject Subject { get; private set; } = default!;
    public int StageId { get; private set; }
    public Stage Stage { get; private set; } = default!;

    public QuestionType Type { get; private set; }
    public Difficulty Difficulty { get; private set; }
    public string QuestionJson { get; private set; } = default!;
    public string CorrectAnswerJson { get; private set; } = default!;
    public string? Explanation { get; private set; }
    public string? TagsCsv { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }

    private Question() { }

    public Question(Guid id, int subjectId, int stageId, QuestionType type, Difficulty difficulty,
                    string questionJson, string correctAnswerJson, Guid? teacherId = null) : base(id)
    {
        SubjectId = subjectId;
        StageId = stageId;
        Type = type;
        Difficulty = difficulty;
        QuestionJson = questionJson;
        CorrectAnswerJson = correctAnswerJson;
        TeacherId = teacherId;
    }

    public void SetExplanation(string? text) => Explanation = text;
    public void SetTags(IEnumerable<string> tags) => TagsCsv = string.Join(",", tags);
}
