using Amuse.Domain.Identity;
using Amuse.Domain.Listener;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Listener.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Listener.Services;

internal sealed class ListenerPersonaReadModel(
    ListenerDbContext dbContext,
    EnsureListenerProfileService ensureService) : IListenerPersonaReadModel
{
    public async Task<Result<PersonaAccessContext>> GetListenerContextAsync(
        AccountId accountId,
        ListenerProfileId listenerId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.ListenerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == listenerId && p.AccountId == accountId, cancellationToken);

        if (profile is null)
            return Result<PersonaAccessContext>.Failure(IdentityErrors.InvalidPersonaContext);

        return Result<PersonaAccessContext>.Success(new PersonaAccessContext(
            "listener",
            null,
            profile.Id.Value,
            null,
            ["listener:access"]));
    }

    public async Task<ListenerProfileId?> GetProfileIdForAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.ListenerProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.AccountId == accountId, cancellationToken);

        return profile?.Id;
    }

    public async Task<Result<ListenerProfileId>> EnsureProfileForAccountAsync(
        AccountId accountId,
        CancellationToken cancellationToken)
    {
        var profile = await ensureService.EnsureAsync(accountId, cancellationToken);
        return Result<ListenerProfileId>.Success(profile.Id);
    }
}
