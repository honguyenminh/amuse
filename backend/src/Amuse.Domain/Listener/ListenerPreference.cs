using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Listener;

public sealed class ListenerPreference
{
    public AccountId AccountId { get; private set; }
    public bool? AllowUnverifiedArtists { get; private set; }
    public bool SetDuringOnboarding { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ListenerPreference()
    {
    }

    private ListenerPreference(AccountId accountId, DateTimeOffset updatedAt)
    {
        AccountId = accountId;
        UpdatedAt = updatedAt;
    }

    public static ListenerPreference Create(AccountId accountId, DateTimeOffset now) =>
        new(accountId, now);

    public Result SetUnverifiedPreference(bool allow, DateTimeOffset now)
    {
        AllowUnverifiedArtists = allow;
        SetDuringOnboarding = true;
        UpdatedAt = now;
        return Result.Success();
    }
}
