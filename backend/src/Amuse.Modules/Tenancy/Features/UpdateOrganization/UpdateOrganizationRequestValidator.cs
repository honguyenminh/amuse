using Amuse.Domain.Tenancy;
using FluentValidation;

namespace Amuse.Modules.Tenancy.Features.UpdateOrganization;

public sealed class UpdateOrganizationProfileRequestValidator
    : AbstractValidator<UpdateOrganizationProfileRequest>
{
    public UpdateOrganizationProfileRequestValidator()
    {
        RuleFor(x => x.Description)
            .MaximumLength(Organization.MaxDescriptionLength);

        RuleFor(x => x.WebsiteUrl)
            .MaximumLength(Organization.MaxWebsiteUrlLength);

        RuleFor(x => x.CountryCode)
            .MaximumLength(Organization.MaxCountryCodeLength);

        RuleFor(x => x.ImprintName)
            .MaximumLength(Organization.MaxImprintNameLength);
    }
}
