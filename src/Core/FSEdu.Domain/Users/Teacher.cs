using FSEdu.Domain.Academic;
using FSEdu.Domain.Common;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Users;

public sealed class Teacher : AppUser
{
    public string? Bio { get; private set; }
    public int YearsOfExperience { get; private set; }
    public decimal RatingAvg { get; private set; }
    public int RatingsCount { get; private set; }
    public bool Verified { get; private set; }
    public DateTime? VerifiedAtUtc { get; private set; }
    public Guid? VerifiedBy { get; private set; }
    public decimal CommissionRate { get; private set; } = 20m;

    private readonly List<TeacherSubject> _subjects = new();
    public IReadOnlyCollection<TeacherSubject> Subjects => _subjects.AsReadOnly();

    private readonly List<TeacherRegion> _regions = new();
    public IReadOnlyCollection<TeacherRegion> Regions => _regions.AsReadOnly();

    private readonly List<TeacherQualification> _qualifications = new();
    public IReadOnlyCollection<TeacherQualification> Qualifications => _qualifications.AsReadOnly();

    private Teacher() { }

    public Teacher(Guid id, string fullName, PhoneNumber phone, int yearsOfExperience,
                   string? bio = null, Email? email = null)
        : base(id, fullName, phone, email)
    {
        YearsOfExperience = yearsOfExperience;
        Bio = bio;
    }

    public void Verify(Guid verifiedBy)
    {
        Verified = true;
        VerifiedAtUtc = DateTime.UtcNow;
        VerifiedBy = verifiedBy;
        Activate();
    }

    public void UpdateBio(string? bio, int years) { Bio = bio; YearsOfExperience = years; }

    public void AddSubject(int subjectId)
    {
        if (_subjects.Any(s => s.SubjectId == subjectId)) return;
        _subjects.Add(new TeacherSubject(Id, subjectId));
    }

    public void AddRegion(int regionId)
    {
        if (_regions.Any(r => r.RegionId == regionId)) return;
        _regions.Add(new TeacherRegion(Id, regionId));
    }

    public void AddQualification(string title, string institution, int year, string? documentUrl)
    {
        _qualifications.Add(new TeacherQualification(Id, title, institution, year, documentUrl));
    }

    public void AddRating(decimal rating)
    {
        var total = RatingAvg * RatingsCount + rating;
        RatingsCount++;
        RatingAvg = Math.Round(total / RatingsCount, 2);
    }
}

public sealed class TeacherSubject
{
    public Guid TeacherId { get; private set; }
    public int SubjectId { get; private set; }
    public Subject Subject { get; private set; } = default!;

    private TeacherSubject() { }
    public TeacherSubject(Guid teacherId, int subjectId) { TeacherId = teacherId; SubjectId = subjectId; }
}

public sealed class TeacherRegion
{
    public Guid TeacherId { get; private set; }
    public int RegionId { get; private set; }
    public Region Region { get; private set; } = default!;

    private TeacherRegion() { }
    public TeacherRegion(Guid teacherId, int regionId) { TeacherId = teacherId; RegionId = regionId; }
}

public sealed class TeacherQualification : Entity<long>
{
    public Guid TeacherId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Institution { get; private set; } = default!;
    public int Year { get; private set; }
    public string? DocumentUrl { get; private set; }

    private TeacherQualification() { }

    public TeacherQualification(Guid teacherId, string title, string institution, int year, string? documentUrl)
    {
        TeacherId = teacherId;
        Title = title;
        Institution = institution;
        Year = year;
        DocumentUrl = documentUrl;
    }
}
