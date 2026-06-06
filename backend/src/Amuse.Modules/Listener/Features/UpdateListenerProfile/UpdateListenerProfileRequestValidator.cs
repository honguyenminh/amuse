using Amuse.Domain.Listener;
using FluentValidation;

namespace Amuse.Modules.Listener.Features.UpdateListenerProfile;

public sealed class UpdateListenerProfileRequestValidator
    : AbstractValidator<UpdateListenerProfileRequest>
{
    public UpdateListenerProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(ListenerProfile.MaxDisplayNameLength)
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.AvatarAccentSeed)
            .InclusiveBetween(ListenerProfile.MinAvatarAccentSeed, ListenerProfile.MaxAvatarAccentSeed)
            .When(x => x.AvatarAccentSeed is not null);

        RuleFor(x => x)
            .Must(x => x.DisplayName is not null
                       || x.AvatarAccentSeed is not null
                       || x.AllowUnverifiedArtists is not null
                       || x.ClearAvatar == true)
            .WithMessage("At least one profile field must be provided.");
    }
}
