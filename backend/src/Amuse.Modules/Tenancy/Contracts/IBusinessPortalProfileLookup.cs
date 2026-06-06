namespace Amuse.Modules.Tenancy.Contracts;

public sealed record BusinessPortalProfileSnapshot(
    Guid AccountId,
    string? DisplayName,
    int? AvatarAccentSeed,
    string? AvatarObjectKey);

public interface IBusinessPortalProfileLookup
{
    Task<IReadOnlyDictionary<Guid, BusinessPortalProfileSnapshot>> GetByAccountIdsAsync(
        IReadOnlyCollection<Guid> accountIds,
        CancellationToken cancellationToken);
}

public interface IBusinessPortalProfileOnboardingReadModel
{
    Task<bool> IsCompleteAsync(Amuse.Domain.Identity.AccountId accountId, CancellationToken cancellationToken);
}
