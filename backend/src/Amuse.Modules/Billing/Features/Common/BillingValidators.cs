using Amuse.Modules.Billing.Features.AcquireFree;
using Amuse.Modules.Billing.Features.CreateCheckoutSession;
using FluentValidation;

namespace Amuse.Modules.Billing.Features.Common;

internal sealed class FreeAcquisitionRequestValidator : AbstractValidator<FreeAcquisitionRequest>
{
    public FreeAcquisitionRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.TrackId.HasValue ^ x.ReleaseId.HasValue)
            .WithMessage("Specify exactly one of trackId or releaseId.");

        RuleFor(x => x.TrackId)
            .Must(id => id != Guid.Empty)
            .When(x => x.TrackId.HasValue)
            .WithMessage("trackId must be a non-empty GUID.");

        RuleFor(x => x.ReleaseId)
            .Must(id => id != Guid.Empty)
            .When(x => x.ReleaseId.HasValue)
            .WithMessage("releaseId must be a non-empty GUID.");
    }
}

internal sealed class CreateCheckoutSessionRequestValidator : AbstractValidator<CreateCheckoutSessionRequest>
{
    public CreateCheckoutSessionRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => x.TrackId.HasValue ^ x.ReleaseId.HasValue)
            .WithMessage("Specify exactly one of trackId or releaseId.");

        RuleFor(x => x.TrackId)
            .Must(id => id != Guid.Empty)
            .When(x => x.TrackId.HasValue)
            .WithMessage("trackId must be a non-empty GUID.");

        RuleFor(x => x.ReleaseId)
            .Must(id => id != Guid.Empty)
            .When(x => x.ReleaseId.HasValue)
            .WithMessage("releaseId must be a non-empty GUID.");

        RuleFor(x => x.AmountMinor)
            .GreaterThan(0)
            .WithMessage("amountMinor must be greater than zero.");
    }
}
