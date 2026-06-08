using Amuse.Domain.SharedKernel;

namespace Amuse.Domain.Billing;

public static class BillingErrors
{
    public static readonly DomainError PurchaseDuplicate =
        new("billing.purchase.duplicate", "This track has already been purchased by the account.");

    public static readonly DomainError PurchaseNotFound =
        new("billing.purchase.not_found", "Purchase was not found.");

    public static readonly DomainError WithdrawalBelowMinimum =
        new("billing.withdrawal.below_minimum", "Withdrawal amount is below the minimum threshold.");

    public static readonly DomainError WithdrawalCooldownActive =
        new("billing.withdrawal.cooldown_active", "A withdrawal was completed recently; cooldown is still active.");

    public static readonly DomainError WithdrawalInsufficientBalance =
        new("billing.withdrawal.insufficient_balance", "Withdrawal amount exceeds available balance.");

    public static readonly DomainError WithdrawalReceivableOutstanding =
        new("billing.withdrawal.receivable_outstanding", "Withdrawals are blocked while seller receivable is outstanding.");

    public static readonly DomainError WithdrawalAlreadyInProgress =
        new("billing.withdrawal.already_in_progress", "An active withdrawal request is already in progress.");

    public static readonly DomainError WithdrawalInvalidTransferReference =
        new("billing.withdrawal.invalid_transfer_reference", "Transfer reference is required and must be at most 256 characters.");

    public static readonly DomainError WithdrawalInvalidProofObjectKey =
        new("billing.withdrawal.invalid_proof_object_key", "Proof object key must be at most 512 characters.");

    public static readonly DomainError FxRateNotFound =
        new("billing.fx_rate.not_found", "No FX rate is available for the requested currency.");

    public static readonly DomainError FxRateInvalid =
        new("billing.fx_rate.invalid", "FX rate must be positive and currencies must be valid ISO codes.");

    public static readonly DomainError FxRateQuoteMustNotBeUsd =
        new("billing.fx_rate.quote_must_not_be_usd", "Ops FX overrides apply to non-USD quote currencies only.");

    public static readonly DomainError PayoutProfileNotVerified =
        new("billing.payout_profile.not_verified", "Payout profile is not verified.");

    public static readonly DomainError PayoutProfileNotFound =
        new("billing.payout_profile.not_found", "Payout profile was not found.");

    public static readonly DomainError PayoutProfileInvalidStatusTransition =
        new("billing.payout_profile.invalid_status_transition", "Payout profile status transition is invalid.");

    public static readonly DomainError PayoutProfileIncomplete =
        new("billing.payout_profile.incomplete", "Payout profile is missing required fields for submission.");

    public static readonly DomainError PayoutProfileUpdateLocked =
        new("billing.payout_profile.update_locked", "Payout profile cannot be edited while under review.");

    public static readonly DomainError PayoutProfileInvalidLegalName =
        new("billing.payout_profile.invalid_legal_name", "Legal name is required and must be at most 256 characters.");

    public static readonly DomainError PayoutProfileInvalidAddress =
        new("billing.payout_profile.invalid_address", "Address fields are invalid.");

    public static readonly DomainError PayoutProfileInvalidCountry =
        new("billing.payout_profile.invalid_country", "Country must be a two-letter ISO code.");

    public static readonly DomainError PayoutProfileCompanyRepresentativeRequired =
        new("billing.payout_profile.representative_required", "Company payout profiles require a representative name.");

    public static readonly DomainError PayoutProfileInvalidBankAccount =
        new("billing.payout_profile.invalid_bank_account", "Bank account details are invalid.");

    public static readonly DomainError PayoutProfileInvalidDocumentKey =
        new("billing.payout_profile.invalid_document_key", "Document object key is invalid.");

    public static readonly DomainError PayoutProfileInvalidRejectionReason =
        new("billing.payout_profile.invalid_rejection_reason", "Rejection reason is required and must be at most 512 characters.");

    public static readonly DomainError InvalidPayoutVerificationStatusFilter =
        new("billing.payout_profile.invalid_status_filter", "Payout verification status filter is invalid.");

    public static readonly DomainError PayoutProfileInvalidTaxId =
        new("billing.payout_profile.invalid_tax_id", "Tax identifier is required.");

    public static readonly DomainError WithdrawalNotFound =
        new("billing.withdrawal.not_found", "Withdrawal request was not found.");

    public static readonly DomainError InvalidWithdrawalStatusFilter =
        new("billing.withdrawal.invalid_status_filter", "Withdrawal status filter is invalid.");

    public static readonly DomainError InvalidPaymentStatusTransition =
        new("billing.payment.invalid_status_transition", "Payment status transition is invalid.");

    public static readonly DomainError InvalidEntitlementStatusTransition =
        new("billing.entitlement.invalid_status_transition", "Entitlement status transition is invalid.");

    public static readonly DomainError InvalidWithdrawalStatusTransition =
        new("billing.withdrawal.invalid_status_transition", "Withdrawal status transition is invalid.");

    public static readonly DomainError InvalidLedgerJournal =
        new("billing.ledger.invalid_journal", "Ledger journal entries are not balanced.");

    public static readonly DomainError PaymentInstrumentBanned =
        new("billing.payment_instrument.banned", "This payment instrument is banned.");

    public static readonly DomainError AccountBanned =
        new("billing.account.banned", "This account is banned from purchases.");

    public static readonly DomainError NotForSale =
        new("billing.purchase.not_for_sale", "This item is not available for purchase.");

    public static readonly DomainError NotFreeEligible =
        new("billing.purchase.not_free_eligible", "Free acquisition requires a price floor of zero.");

    public static readonly DomainError PurchaseReleaseDuplicate =
        new("billing.purchase.release_duplicate", "This release has already been purchased by the account.");

    public static readonly DomainError InvalidAcquisitionTarget =
        new(
            "billing.purchase.invalid_acquisition_target",
            "Specify exactly one of trackId or releaseId for a free acquisition.");

    public static readonly DomainError TrackNotFound =
        new("billing.purchase.track_not_found", "Track was not found or is not purchasable.");

    public static readonly DomainError ReleaseNotFound =
        new("billing.purchase.release_not_found", "Release was not found or is not purchasable.");

    public static readonly DomainError DownloadForbidden =
        new("billing.download.forbidden", "Download requires ownership of the track or its release.");

    public static readonly DomainError DownloadNotReady =
        new("billing.download.not_ready", "Download is not available because the track master is missing.");

    public static readonly DomainError InvalidCheckoutAmount =
        new("billing.checkout.invalid_amount", "Checkout amount is outside the allowed price range.");

    public static readonly DomainError CheckoutNotConfigured =
        new("billing.checkout.not_configured", "Checkout is not configured.");

    public static readonly DomainError OrgSalesBlocked =
        new("billing.checkout.org_sales_blocked", "This seller is not accepting new purchases.");

    public static readonly DomainError CheckoutSessionNotFound =
        new("billing.checkout.session_not_found", "Checkout session was not found.");

    public static readonly DomainError WebhookInvalid =
        new("billing.webhook.invalid", "Payment webhook signature or payload is invalid.");

    public static readonly DomainError RefundNotAllowed =
        new("billing.refund.not_allowed", "You are not allowed to refund this purchase.");

    public static readonly DomainError RefundAlreadyProcessed =
        new("billing.refund.already_processed", "This purchase has already been refunded.");

    public static readonly DomainError RefundNotEligible =
        new("billing.refund.not_eligible", "Only paid purchases can be refunded.");

    public static readonly DomainError RefundReasonRequired =
        new("billing.refund.reason_required", "Refund reason is required.");

    public static readonly DomainError RefundFeeBearerRequired =
        new("billing.refund.fee_bearer_required", "Platform-initiated refunds require refundFeeBearer.");

    public static readonly DomainError RefundProviderFailed =
        new("billing.refund.provider_failed", "Payment provider refund failed.");

    public static readonly DomainError TaxInvoiceNotFound =
        new("billing.tax_invoice.not_found", "Tax invoice was not found for this purchase.");

    public static readonly DomainError RefundInProgress =
        new("billing.refund.in_progress", "A refund is already in progress for this purchase.");

    public static readonly DomainError ChargebackNotEligible =
        new("billing.chargeback.not_eligible", "Only paid purchases can be charged back.");

    public static readonly DomainError ChargebackAlreadyProcessed =
        new("billing.chargeback.already_processed", "This purchase has already been charged back.");

    public static readonly DomainError PayoutNotConfigured =
        new("billing.payout.not_configured", "Stripe global payout is not configured.");

    public static readonly DomainError PayoutRecipientMissing =
        new("billing.payout.recipient_missing", "Stripe payout recipient has not been created yet.");

    public static readonly DomainError PayoutOutboundFailed =
        new("billing.payout.outbound_failed", "Stripe outbound payout submission failed.");
}
