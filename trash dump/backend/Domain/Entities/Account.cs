namespace Amuse.Api.Domain.Entities;

public sealed class Account
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AccountStatus Status { get; set; } = AccountStatus.Active;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Organization> CreatedOrganizations { get; set; } = [];
    public ICollection<OrganizationMember> OrganizationMemberships { get; set; } = [];
    public ICollection<RefreshSession> RefreshSessions { get; set; } = [];
}

public enum AccountStatus
{
    Active,
    Disabled
}
