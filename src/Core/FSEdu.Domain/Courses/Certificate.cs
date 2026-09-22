using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Courses;

public sealed class Certificate : AggregateRoot<Guid>
{
    public Guid StudentId { get; private set; }
    public Student Student { get; private set; } = default!;
    public Guid CourseId { get; private set; }
    public Course Course { get; private set; } = default!;
    public string CertificateNumber { get; private set; } = default!;
    public DateTime IssuedAtUtc { get; private set; }
    public decimal FinalGrade { get; private set; }

    private Certificate() { }

    public Certificate(Guid id, Guid studentId, Guid courseId, decimal finalGrade) : base(id)
    {
        StudentId = studentId;
        CourseId = courseId;
        IssuedAtUtc = DateTime.UtcNow;
        FinalGrade = Math.Clamp(finalGrade, 0, 100);
        CertificateNumber = $"FSE-{IssuedAtUtc:yyyyMM}-{Convert.ToString(id.GetHashCode() & 0x7FFFFFFF, 16).ToUpperInvariant().PadLeft(6, '0')[..6]}";
    }
}
