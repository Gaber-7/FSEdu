using FSEdu.Domain.Academic;
using FSEdu.Domain.Common;
using FSEdu.Domain.Users;
using FSEdu.Shared.Kernel.Primitives;

namespace FSEdu.Domain.Engagement;

public sealed class Community : AggregateRoot<Guid>
{
    public int StageId { get; private set; }
    public Stage Stage { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public int MaxMembers { get; private set; } = 150;
    public Guid? SupervisorId { get; private set; }

    private readonly List<CommunityMember> _members = new();
    public IReadOnlyCollection<CommunityMember> Members => _members.AsReadOnly();

    private Community() { }

    public Community(Guid id, int stageId, string name, int maxMembers = 150, Guid? supervisorId = null)
        : base(id)
    {
        StageId = stageId;
        Name = name;
        MaxMembers = maxMembers;
        SupervisorId = supervisorId;
    }

    public bool TryAddMember(Guid userId, CommunityRole role)
    {
        if (_members.Count >= MaxMembers) return false;
        if (_members.Any(m => m.UserId == userId)) return false;
        _members.Add(new CommunityMember(Id, userId, role));
        return true;
    }
}

public sealed class CommunityMember
{
    public Guid CommunityId { get; private set; }
    public Guid UserId { get; private set; }
    public AppUser User { get; private set; } = default!;
    public CommunityRole Role { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }

    private CommunityMember() { }

    public CommunityMember(Guid communityId, Guid userId, CommunityRole role)
    {
        CommunityId = communityId;
        UserId = userId;
        Role = role;
        JoinedAtUtc = DateTime.UtcNow;
    }
}
