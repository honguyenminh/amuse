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
    }
}
