using FSEdu.Domain.Academic;
using FSEdu.Domain.Common;

namespace FSEdu.Domain.Users;

public sealed class Student : AppUser
{
    public int StageId { get; private set; }
    public Stage Stage { get; private set; } = default!;
    public int? SchoolId { get; private set; }
    public School? School { get; private set; }
    public int RegionId { get; private set; }
    public Region Region { get; private set; } = default!;

    public int XpPoints { get; private set; }
    public int CurrentStreakDays { get; private set; }
    public int LongestStreakDays { get; private set; }
    public int StreakShields { get; private set; }   // protects streak when student misses a day

    public string? ReferralCode { get; private set; }
    public string? ReferredByCode { get; private set; }
    public int ReferralCredits { get; private set; }  // EGP credit accumulated from successful referrals

    private readonly List<ParentStudentLink> _parents = new();
    public IReadOnlyCollection<ParentStudentLink> Parents => _parents.AsReadOnly();

    private Student() { }

    public Student(Guid id, string fullName, PhoneNumber phone, int stageId, int regionId,
                   int? schoolId = null, Email? email = null)
        : base(id, fullName, phone, email)
    {
        StageId = stageId;
        RegionId = regionId;
        SchoolId = schoolId;
    }

    public void AddXp(int amount)
    {
        if (amount <= 0) return;
        XpPoints += amount;
    }

    public void IncrementStreak()
    {
        CurrentStreakDays++;
        if (CurrentStreakDays > LongestStreakDays) LongestStreakDays = CurrentStreakDays;
    }

    public void ResetStreak() => CurrentStreakDays = 0;

    public void AddStreakShield(int count = 1)
    {
        if (count <= 0) return;
        StreakShields = Math.Min(StreakShields + count, 7); // cap at 7
    }

    // Returns true if a shield was consumed (and streak preserved), false otherwise.
    public bool TryConsumeStreakShield()
    {
        if (StreakShields <= 0) return false;
        StreakShields--;
        return true;
    }

    public void ChangeStage(int newStageId) => StageId = newStageId;
    public void ChangeSchool(int? schoolId) => SchoolId = schoolId;

    public void EnsureReferralCode()
    {
        if (!string.IsNullOrEmpty(ReferralCode)) return;
        // Deterministic short code from user Id, 8 chars [A-Z0-9]
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/1/I
        var bytes = Id.ToByteArray();
        Span<char> code = stackalloc char[8];
        for (int i = 0; i < 8; i++)
            code[i] = alphabet[bytes[i] % alphabet.Length];
        ReferralCode = new string(code);
    }

    public void SetReferredBy(string? code)
    {
        if (ReferredByCode is not null) return; // immutable once set
        ReferredByCode = string.IsNullOrWhiteSpace(code) ? null : code.Trim().ToUpperInvariant();
    }

    public void AddReferralCredit(int amountEgp)
    {
        if (amountEgp <= 0) return;
        ReferralCredits += amountEgp;
    }

    public void SpendReferralCredit(int amountEgp)
    {
        if (amountEgp <= 0) return;
        ReferralCredits = Math.Max(0, ReferralCredits - amountEgp);
    }
}
