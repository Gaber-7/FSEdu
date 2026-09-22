using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Academic;

public sealed class Subject : AggregateRoot<int>
{
    public int StageId { get; private set; }
    public Stage Stage { get; private set; } = default!;
    public string NameAr { get; private set; } = default!;
    public string? NameEn { get; private set; }
    public string? IconUrl { get; private set; }
    public string? ColorHex { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Pricing (per stage's subject)
    public decimal MonthlyPriceEgp { get; private set; }
    public decimal TermPriceEgp { get; private set; }

    private Subject() { }

    public Subject(int stageId, string nameAr, string? nameEn = null, string? iconUrl = null, string? colorHex = null)
    {
        StageId = stageId;
        NameAr = nameAr;
        NameEn = nameEn;
        IconUrl = iconUrl;
        ColorHex = colorHex;
    }

    public void Update(string nameAr, string? nameEn, string? iconUrl, string? colorHex)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        IconUrl = iconUrl;
        ColorHex = colorHex;
    }

    public void SetPricing(decimal monthly, decimal term)
    {
        MonthlyPriceEgp = monthly;
        TermPriceEgp = term;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
