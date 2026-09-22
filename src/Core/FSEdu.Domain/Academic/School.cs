using FSEdu.Domain.Common;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Academic;

public sealed class School : AggregateRoot<int>
{
    public string Name { get; private set; } = default!;
    public int RegionId { get; private set; }
    public Region Region { get; private set; } = default!;
    public SchoolType Type { get; private set; }
    public bool IsActive { get; private set; } = true;

    private School() { }

    public School(string name, int regionId, SchoolType type)
    {
        Name = name;
        RegionId = regionId;
        Type = type;
    }
}
