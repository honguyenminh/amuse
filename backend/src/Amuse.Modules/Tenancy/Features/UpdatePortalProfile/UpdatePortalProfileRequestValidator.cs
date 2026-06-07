using Amuse.Domain.Tenancy;
using Amuse.Modules.Tenancy.Features.Common;
using FluentValidation;

namespace Amuse.Modules.Tenancy.Features.UpdatePortalProfile;

public sealed class UpdateBusinessPortalProfileRequestValidator
    : AbstractValidator<UpdateBusinessPortalProfileRequest>
{
    public UpdateBusinessPortalProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .MaximumLength(BusinessPortalProfile.MaxDisplayNameLength)
            .When(x => x.DisplayName is not null);

        RuleFor(x => x.AvatarAccentSeed)
            .InclusiveBetween(BusinessPortalProfile.MinAvatarAccentSeed, BusinessPortalProfile.MaxAvatarAccentSeed)
            .When(x => x.AvatarAccentSeed is not null);

        RuleFor(x => x)
            .Must(x => x.DisplayName is not null
                       || x.AvatarAccentSeed is not null
                       || x.ClearAvatar == true)
            .WithMessage("At least one profile field must be provided.");
    }
}
