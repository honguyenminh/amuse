using FluentValidation;

namespace Amuse.Modules.Billing.Features.RefundPurchase;

internal sealed class RefundPurchaseRequestValidator : AbstractValidator<RefundPurchaseRequest>
{
    public const int MaxReasonLength = 512;

    public RefundPurchaseRequestValidator()
    {
        RuleFor(request => request.Reason)
            .NotEmpty()
            .WithMessage("Refund reason is required.")
            .MaximumLength(MaxReasonLength);

        RuleFor(request => request.RefundFeeBearer)
            .Must(value => value is null or "platform" or "seller")
            .WithMessage("refundFeeBearer must be platform or seller when provided.");
    }
}
