using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Amuse.Modules.Billing.Services;

internal sealed class StripeCheckoutProvider(
    IOptions<StripeConfig> stripeOptions,
    ILogger<StripeCheckoutProvider> logger) : ICheckoutProvider
{
    public async Task<Result<CheckoutSessionResult>> CreateSessionAsync(
        CheckoutSessionRequest request,
        CancellationToken cancellationToken)
    {
        var config = stripeOptions.Value;
        if (string.IsNullOrWhiteSpace(config.SecretKey))
            return Result<CheckoutSessionResult>.Failure(BillingErrors.CheckoutNotConfigured);

        StripeConfiguration.ApiKey = config.SecretKey;

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            ClientReferenceId = request.PurchaseId.ToString(),
            Metadata = new Dictionary<string, string>
            {
                ["purchase_id"] = request.PurchaseId.ToString(),
                ["payment_transaction_id"] = request.PaymentTransactionId.ToString(),
                ["account_id"] = request.AccountId.ToString(),
            },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency.ToLowerInvariant(),
                        UnitAmount = request.AmountMinor,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.ProductName,
                        },
                    },
                },
            ],
        };

        try
        {
            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(session.Url) || string.IsNullOrWhiteSpace(session.Id))
                return Result<CheckoutSessionResult>.Failure(BillingErrors.CheckoutNotConfigured);

            return Result<CheckoutSessionResult>.Success(new CheckoutSessionResult(session.Id, session.Url));
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe checkout session creation failed for purchase {PurchaseId}", request.PurchaseId);
            return Result<CheckoutSessionResult>.Failure(BillingErrors.CheckoutNotConfigured);
        }
    }

    public async Task<Result<CompletedCheckoutPayment>> GetCompletedPaymentAsync(
        string checkoutSessionId,
        CancellationToken cancellationToken)
    {
        var config = stripeOptions.Value;
        if (string.IsNullOrWhiteSpace(config.SecretKey))
            return Result<CompletedCheckoutPayment>.Failure(BillingErrors.CheckoutNotConfigured);

        StripeConfiguration.ApiKey = config.SecretKey;

        try
        {
            var sessionService = new SessionService();
            var session = await sessionService.GetAsync(
                checkoutSessionId,
                new SessionGetOptions { Expand = ["payment_intent.latest_charge.balance_transaction"] },
                cancellationToken: cancellationToken);

            if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
                return Result<CompletedCheckoutPayment>.Failure(BillingErrors.InvalidPaymentStatusTransition);

            var paymentIntent = session.PaymentIntent;
            var charge = paymentIntent?.LatestCharge;
            var balanceTransaction = charge?.BalanceTransaction;

            var grossMinor = session.AmountTotal ?? 0;
            var currency = (session.Currency ?? "usd").ToUpperInvariant();
            var pspFeeMinor = balanceTransaction?.Fee ?? 0;
            var providerReference = charge?.Id ?? paymentIntent?.Id ?? checkoutSessionId;
            var fingerprint = charge?.PaymentMethodDetails?.Card?.Fingerprint;

            return Result<CompletedCheckoutPayment>.Success(new CompletedCheckoutPayment(
                checkoutSessionId,
                providerReference,
                fingerprint,
                pspFeeMinor,
                grossMinor,
                currency));
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe checkout session lookup failed for session {SessionId}", checkoutSessionId);
            return Result<CompletedCheckoutPayment>.Failure(BillingErrors.CheckoutSessionNotFound);
        }
    }

    public async Task<Result<RefundChargeResult>> RefundChargeAsync(
        string chargeId,
        CancellationToken cancellationToken)
    {
        var config = stripeOptions.Value;
        if (string.IsNullOrWhiteSpace(config.SecretKey))
            return Result<RefundChargeResult>.Failure(BillingErrors.CheckoutNotConfigured);

        StripeConfiguration.ApiKey = config.SecretKey;

        try
        {
            var service = new RefundService();
            var refund = await service.CreateAsync(
                new RefundCreateOptions
                {
                    Charge = chargeId,
                    Expand = ["balance_transaction"],
                },
                cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(refund.Id))
                return Result<RefundChargeResult>.Failure(BillingErrors.RefundProviderFailed);

            var refundFeeMinor = refund.BalanceTransaction?.Fee ?? 0;
            return Result<RefundChargeResult>.Success(new RefundChargeResult(refund.Id, refundFeeMinor));
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Stripe refund failed for charge {ChargeId}", chargeId);
            return Result<RefundChargeResult>.Failure(BillingErrors.RefundProviderFailed);
        }
    }
}
