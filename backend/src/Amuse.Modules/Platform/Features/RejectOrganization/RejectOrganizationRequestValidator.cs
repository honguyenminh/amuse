using Amuse.Domain.Tenancy;
using FluentValidation;

namespace Amuse.Modules.Platform.Features.RejectOrganization;

public sealed class RejectOrganizationRequestValidator : AbstractValidator<RejectOrganizationRequest>
{
    public RejectOrganizationRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(Organization.MaxRejectionReasonLength);
    }
}
