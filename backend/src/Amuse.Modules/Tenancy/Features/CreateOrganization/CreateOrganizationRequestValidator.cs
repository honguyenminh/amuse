using Amuse.Domain.Tenancy;
using FluentValidation;

namespace Amuse.Modules.Tenancy.Features.CreateOrganization;

public sealed class CreateOrganizationRequestValidator : AbstractValidator<CreateOrganizationRequest>
{
    public CreateOrganizationRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(Organization.MaxDisplayNameLength);

        RuleFor(x => x.OrgClass)
            .IsInEnum();

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
