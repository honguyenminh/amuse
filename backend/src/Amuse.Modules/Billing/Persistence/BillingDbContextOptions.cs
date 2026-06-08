using Amuse.Domain.Billing;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Persistence;

public static class BillingDbContextOptions
{
    public static void Configure(DbContextOptionsBuilder options, string connectionString) =>
        options.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_billing", "billing");
                npgsql.MapEnum<PaymentStatus>("payment_status", "billing");
                npgsql.MapEnum<EntitlementStatus>("entitlement_status", "billing");
                npgsql.MapEnum<PurchasedUnit>("purchased_unit", "billing");
                npgsql.MapEnum<PayoutVerificationStatus>("payout_verification_status", "billing");
                npgsql.MapEnum<PayoutRail>("payout_rail", "billing");
                npgsql.MapEnum<LegalEntityType>("legal_entity_type", "billing");
                npgsql.MapEnum<WithdrawalStatus>("withdrawal_status", "billing");
                npgsql.MapEnum<JournalType>("journal_type", "billing");
                npgsql.MapEnum<EntryDirection>("entry_direction", "billing");
                npgsql.MapEnum<ReferenceType>("reference_type", "billing");
                npgsql.MapEnum<RefundFeeBearer>("refund_fee_bearer", "billing");
                npgsql.MapEnum<RefundInitiatorRole>("refund_initiator_role", "billing");
                npgsql.MapEnum<FxRateSource>("fx_rate_source", "billing");
                npgsql.MapEnum<LedgerAccountType>("ledger_account_type", "billing");
            });
}
