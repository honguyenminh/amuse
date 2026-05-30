namespace Amuse.Modules.Tenancy.Contracts;

/// <summary>
/// Contact snapshot for the account that created an organization application (B2B / platform review).
/// </summary>
public sealed record OrganizationApplicationOwner(
    Guid AccountId,
    string? Email,
    string IdpIssuer,
    string IdpSubject,
    string AccountStatus);
