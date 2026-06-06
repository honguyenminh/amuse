using Amuse.Domain.Identity;
using Amuse.Domain.Listener;

namespace Amuse.Domain.Tests.Listener;

public sealed class ListenerProfileTests
{
    private static readonly AccountId Account = AccountId.From(Guid.CreateVersion7());
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-06T00:00:00+00:00");

    [Fact]
    public void UpdatePresentation_sets_display_name_and_accent()
    {
        var profile = ListenerProfile.Create(Account, Now);

        var result = profile.UpdatePresentation("Listener One", 3, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal("Listener One", profile.DisplayName);
        Assert.Equal(3, profile.AvatarAccentSeed);
        Assert.True(profile.IsPresentationComplete);
    }

    [Fact]
    public void UpdatePresentation_rejects_invalid_display_name()
    {
        var profile = ListenerProfile.Create(Account, Now);

        var result = profile.UpdatePresentation("   ", null, Now);

        Assert.False(result.IsSuccess);
        Assert.Equal(ListenerErrors.InvalidDisplayName.Code, result.Error!.Code);
    }

    [Fact]
    public void Onboarding_is_complete_when_presentation_and_preference_are_set()
    {
        var profile = ListenerProfile.Create(Account, Now);
        Assert.True(profile.UpdatePresentation("Listener One", null, Now).IsSuccess);

        var preference = ListenerPreference.Create(Account, Now);
        Assert.True(preference.SetUnverifiedPreference(true, Now).IsSuccess);

        Assert.True(ListenerOnboarding.IsComplete(profile, preference));
    }

    [Fact]
    public void Onboarding_is_incomplete_without_preference_choice()
    {
        var profile = ListenerProfile.Create(Account, Now);
        Assert.True(profile.UpdatePresentation("Listener One", null, Now).IsSuccess);

        Assert.False(ListenerOnboarding.IsComplete(profile, null));
    }
}
