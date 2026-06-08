using Amuse.Domain.Billing;
using Amuse.Modules.Billing.Features.PayoutProfile;
using FluentValidation;

namespace Amuse.Modules.Billing.Features.Common;

internal sealed class UpsertPayoutProfileRequestValidator : AbstractValidator<UpsertPayoutProfileRequest>
{
    public UpsertPayoutProfileRequestValidator()
    {
        RuleFor(x => x.LegalName)
            .NotEmpty()
            .MaximumLength(Amuse.Domain.Billing.PayoutProfile.MaxLegalNameLength);

        RuleFor(x => x.AddressLine1)
            .NotEmpty()
            .MaximumLength(Amuse.Domain.Billing.PayoutProfile.MaxAddressLineLength);

        RuleFor(x => x.AddressLine2)
            .MaximumLength(Amuse.Domain.Billing.PayoutProfile.MaxAddressLineLength)
            .When(x => !string.IsNullOrWhiteSpace(x.AddressLine2));

        RuleFor(x => x.City)
            .NotEmpty()
            .MaximumLength(Amuse.Domain.Billing.PayoutProfile.MaxCityLength);

        RuleFor(x => x.Region)
            .MaximumLength(Amuse.Domain.Billing.PayoutProfile.MaxRegionLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Region));

        RuleFor(x => x.PostalCode)
            .NotEmpty()
            .MaximumLength(Amuse.Domain.Billing.PayoutProfile.MaxPostalCodeLength);

        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .Length(Amuse.Domain.Billing.PayoutProfile.MaxCountryCodeLength);

        RuleFor(x => x.RepresentativeName)
            .NotEmpty()
            .MaximumLength(Amuse.Domain.Billing.PayoutProfile.MaxRepresentativeNameLength)
            .When(x => x.LegalEntityType == LegalEntityType.Company);

        RuleFor(x => x.BankName)
            .MaximumLength(Amuse.Domain.Billing.PayoutProfile.MaxBankNameLength)
            .When(x => !string.IsNullOrWhiteSpace(x.BankName));

        RuleForEach(x => x.DocumentObjectKeys)
            .MaximumLength(Amuse.Domain.Billing.PayoutProfile.MaxDocumentKeyLength);
    }
}

internal sealed class RejectPayoutProfileRequestValidator : AbstractValidator<RejectPayoutProfileRequest>
{
    public RejectPayoutProfileRequestValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty()
            .MaximumLength(Amuse.Domain.Billing.PayoutProfile.MaxRejectionReasonLength);
    }
}
