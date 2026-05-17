using Amuse.Domain.Identity;

namespace Amuse.Domain.Tenancy;

public sealed class OrganizationMember
{
    public Guid Id { get; private set; }
    public OrganizationId OrganizationId { get; private set; }
    public AccountId AccountId { get; private set; }
    public MembershipStatus Status { get; private set; }
    public string? PresetRoleLabel { get; private set; }
    public IReadOnlyList<string> Claims { get; private set; } = [];
    public bool IsOwner { get; private set; }

    private OrganizationMember()
    {
    }

    public bool IsActive => Status == MembershipStatus.Active;
}
