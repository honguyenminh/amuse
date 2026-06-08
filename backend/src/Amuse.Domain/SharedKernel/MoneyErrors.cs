using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.SharedKernel;

public static class MoneyErrors
{
    public static readonly DomainError InvalidAmount =
        new("shared.money.invalid_amount", "Money amount must be zero or greater.");

    public static readonly DomainError InvalidCurrency =
        new("shared.money.invalid_currency", "Currency must be a valid ISO 4217 code.");
}
