using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Academic;

public sealed class Stage : AggregateRoot<int>
{
    public string NameAr { get; private set; } = default!;
    public string? NameEn { get; private set; }
    public int LevelOrder { get; private set; }
    public int? ParentStageId { get; private set; }
    public Stage? ParentStage { get; private set; }

    // Full-term bundle price (only for leaf/grade stages)
    public decimal? FullTermPriceEgp { get; private set; }

    private readonly List<Stage> _children = new();
    public IReadOnlyCollection<Stage> Children => _children.AsReadOnly();

    private readonly List<Subject> _subjects = new();
    public IReadOnlyCollection<Subject> Subjects => _subjects.AsReadOnly();

    private Stage() { }

    public Stage(string nameAr, int levelOrder, string? nameEn = null, int? parentStageId = null)
    {
        NameAr = nameAr;
        NameEn = nameEn;
        LevelOrder = levelOrder;
        ParentStageId = parentStageId;
    }

    public void Rename(string nameAr, string? nameEn) { NameAr = nameAr; NameEn = nameEn; }
    public void SetFullTermPrice(decimal price) => FullTermPriceEgp = price;
}
