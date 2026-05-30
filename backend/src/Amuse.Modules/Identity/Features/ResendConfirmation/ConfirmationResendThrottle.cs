using System.Collections.Concurrent;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Identity.Options;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Identity.Features.ResendConfirmation;

internal sealed class ConfirmationResendThrottle(IOptions<IdentityEmailOptions> options)
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSent = new();

    public Result TryAcquire(string email, DateTimeOffset now)
    {
        var key = email.Trim().ToLowerInvariant();
        var cooldown = TimeSpan.FromSeconds(options.Value.ResendCooldownSeconds);

        if (_lastSent.TryGetValue(key, out var last) && now - last < cooldown)
            return Result.Failure(IdentityErrors.ResendConfirmationRateLimited);

        _lastSent[key] = now;
        return Result.Success();
    }
}
