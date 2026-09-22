using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Academic;

public sealed class Region : AggregateRoot<int>
{
    public string NameAr { get; private set; } = default!;
    public string? NameEn { get; private set; }
    public string Code { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;

    private Region() { }

    public Region(string nameAr, string code, string? nameEn = null)
    {
        NameAr = nameAr;
        Code = code;
        NameEn = nameEn;
    }
}
