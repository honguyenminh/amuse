using Amuse.Domain.SharedKernel;
using FluentValidation;

namespace Amuse.Modules.Billing.Features.Withdrawals;

internal sealed class CreateWithdrawalRequestValidator : AbstractValidator<CreateWithdrawalRequest>
{
    public CreateWithdrawalRequestValidator()
    {
        RuleFor(request => request.AmountMinor).GreaterThan(0);
        RuleFor(request => request.Currency)
            .NotEmpty()
            .Length(Money.Iso4217CurrencyLength)
            .Must(Money.IsValidCurrency)
            .WithMessage("Currency must be a valid ISO 4217 code.");
    }
}

internal sealed class CompleteWithdrawalRequestValidator : AbstractValidator<CompleteWithdrawalRequest>
{
    public CompleteWithdrawalRequestValidator()
    {
        RuleFor(request => request.TransferReference).NotEmpty().MaximumLength(256);
        RuleFor(request => request.ProofObjectKey).MaximumLength(512).When(request => request.ProofObjectKey is not null);
    }
}
