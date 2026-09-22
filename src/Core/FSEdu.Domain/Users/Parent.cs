using FSEdu.Domain.Common;

namespace FSEdu.Domain.Users;

public sealed class Parent : AppUser
{
    public string? NationalId { get; private set; }
    public string? Occupation { get; private set; }
    public DateTime? LastWeeklyDigestAtUtc { get; private set; }
    public bool WeeklyDigestEnabled { get; private set; } = true;

    private readonly List<ParentStudentLink> _children = new();
    public IReadOnlyCollection<ParentStudentLink> Children => _children.AsReadOnly();

    private Parent() { }

    public Parent(Guid id, string fullName, PhoneNumber phone, Email? email = null,
                  string? nationalId = null, string? occupation = null)
        : base(id, fullName, phone, email)
    {
        NationalId = nationalId;
        Occupation = occupation;
    }

    public void RecordWeeklyDigestSent() => LastWeeklyDigestAtUtc = DateTime.UtcNow;
    public void SetWeeklyDigestEnabled(bool enabled) => WeeklyDigestEnabled = enabled;

    public void LinkChild(Guid studentId, Relation relation, bool isPrimary = false)
    {
        if (_children.Any(c => c.StudentId == studentId)) return;
        _children.Add(new ParentStudentLink(Id, studentId, relation, isPrimary));
    }

    public void UnlinkChild(Guid studentId)
    {
        var link = _children.FirstOrDefault(c => c.StudentId == studentId);
        if (link is not null) _children.Remove(link);
    }
}

public sealed class ParentStudentLink
{
    public Guid ParentId { get; private set; }
    public Parent Parent { get; private set; } = default!;
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;
    public Relation Relation { get; private set; }
    public bool IsPrimary { get; private set; }

    private ParentStudentLink() { }

    public ParentStudentLink(Guid parentId, Guid studentId, Relation relation, bool isPrimary)
    {
        ParentId = parentId;
        StudentId = studentId;
        Relation = relation;
        IsPrimary = isPrimary;
    }
}
