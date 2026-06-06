namespace Amuse.Domain.Listener;

public static class ListenerOnboarding
{
    public static bool IsComplete(ListenerProfile profile, ListenerPreference? preference) =>
        profile.IsPresentationComplete
        && preference is { SetDuringOnboarding: true, AllowUnverifiedArtists: not null };
}
