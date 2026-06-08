using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Billing;

public static class CheckoutPricingGuard
{
    public static Result ValidateAmount(long amountMinor, long floorMinor, long? ceilingMinor)
    {
        if (amountMinor <= 0)
            return Result.Failure(BillingErrors.InvalidCheckoutAmount);

        if (amountMinor < floorMinor)
            return Result.Failure(BillingErrors.InvalidCheckoutAmount);

        if (ceilingMinor is { } ceiling && amountMinor > ceiling)
            return Result.Failure(BillingErrors.InvalidCheckoutAmount);

        return Result.Success();
    }
}
