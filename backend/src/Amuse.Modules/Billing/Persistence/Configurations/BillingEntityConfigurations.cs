using Amuse.Domain.Billing;
using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amuse.Modules.Billing.Persistence.Configurations;

internal sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("purchase");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => PurchaseId.From(value));

        builder.Property(p => p.AccountId)
            .HasColumnName("account_id")
            .HasConversion(id => id.Value, value => AccountId.From(value));

        builder.Property(p => p.OrganizationId)
            .HasColumnName("organization_id")
            .HasConversion(id => id.Value, value => OrganizationId.From(value));

        builder.Property(p => p.PurchasedUnit)
            .HasColumnName("purchased_unit")
            .HasColumnType("billing.purchased_unit");

        builder.Property(p => p.TrackId).HasColumnName("track_id");
        builder.Property(p => p.ReleaseId).HasColumnName("release_id");

        builder.Property(p => p.PriceSnapshotMinor).HasColumnName("price_snapshot_minor");
        builder.Property(p => p.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();

        builder.Property(p => p.PaymentStatus)
            .HasColumnName("payment_status")
            .HasColumnType("billing.payment_status");

        builder.Property(p => p.EntitlementStatus)
            .HasColumnName("entitlement_status")
            .HasColumnType("billing.entitlement_status");

        builder.Property(p => p.PurchasedAt).HasColumnName("purchased_at").HasColumnType("timestamptz");
        builder.Property(p => p.PaidAt).HasColumnName("paid_at").HasColumnType("timestamptz");

        builder.Property(p => p.PaymentTransactionId)
            .HasColumnName("payment_transaction_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? PaymentTransactionId.From(value.Value) : null);

        builder.Property(p => p.RefundInitiatorRole)
            .HasColumnName("refund_initiator_role")
            .HasColumnType("billing.refund_initiator_role");

        builder.Property(p => p.RefundInitiatedByAccountId)
            .HasColumnName("refund_initiated_by_account_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? AccountId.From(value.Value) : null);

        builder.Property(p => p.RefundReason).HasColumnName("refund_reason").HasMaxLength(512);
        builder.Property(p => p.RefundFeeBearer)
            .HasColumnName("refund_fee_bearer")
            .HasColumnType("billing.refund_fee_bearer");

        builder.Property(p => p.RefundRequestedAt).HasColumnName("refund_requested_at").HasColumnType("timestamptz");
        builder.Property(p => p.RefundedAt).HasColumnName("refunded_at").HasColumnType("timestamptz");

        builder.HasIndex(p => new { p.AccountId, p.TrackId })
            .IsUnique()
            .HasFilter("track_id IS NOT NULL AND entitlement_status = 'active'");

        builder.HasIndex(p => new { p.AccountId, p.ReleaseId })
            .IsUnique()
            .HasFilter(
                "release_id IS NOT NULL AND purchased_unit = 'release' AND entitlement_status = 'active'");
    }
}

internal sealed class PurchaseAllocationSnapshotConfiguration : IEntityTypeConfiguration<PurchaseAllocationSnapshot>
{
    public void Configure(EntityTypeBuilder<PurchaseAllocationSnapshot> builder)
    {
        builder.ToTable("purchase_allocation_snapshot");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => PurchaseAllocationSnapshotId.From(value));

        builder.Property(s => s.PurchaseId)
            .HasColumnName("purchase_id")
            .HasConversion(id => id.Value, value => PurchaseId.From(value));

        builder.Property(s => s.TrackId).HasColumnName("track_id");

        builder.Property(s => s.PayeeOrganizationId)
            .HasColumnName("payee_organization_id")
            .HasConversion(id => id.Value, value => OrganizationId.From(value));

        builder.Property(s => s.ShareBps).HasColumnName("share_bps");
        builder.Property(s => s.AmountMinor).HasColumnName("amount_minor");
        builder.Property(s => s.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
    }
}

internal sealed class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transaction");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => PaymentTransactionId.From(value));

        builder.Property(t => t.PurchaseId)
            .HasColumnName("purchase_id")
            .HasConversion(id => id.Value, value => PurchaseId.From(value));

        builder.Property(t => t.AccountId)
            .HasColumnName("account_id")
            .HasConversion(id => id.Value, value => AccountId.From(value));

        builder.Property(t => t.GrossMinor).HasColumnName("gross_minor");
        builder.Property(t => t.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(t => t.ProviderReference).HasColumnName("provider_reference").HasMaxLength(256);
        builder.Property(t => t.CheckoutSessionId).HasColumnName("checkout_session_id").HasMaxLength(256);
        builder.Property(t => t.PaymentMethodFingerprint).HasColumnName("payment_method_fingerprint").HasMaxLength(256);
        builder.Property(t => t.PspFeeMinor).HasColumnName("psp_fee_minor");

        builder.HasIndex(t => t.CheckoutSessionId)
            .IsUnique()
            .HasFilter("checkout_session_id IS NOT NULL");

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasColumnType("billing.payment_status");

        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(t => t.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
    }
}

internal sealed class LedgerJournalConfiguration : IEntityTypeConfiguration<LedgerJournal>
{
    public void Configure(EntityTypeBuilder<LedgerJournal> builder)
    {
        builder.ToTable("ledger_journal");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => LedgerJournalId.From(value));

        builder.Property(j => j.JournalType)
            .HasColumnName("journal_type")
            .HasColumnType("billing.journal_type");

        builder.Property(j => j.ReferenceType)
            .HasColumnName("reference_type")
            .HasColumnType("billing.reference_type");

        builder.Property(j => j.ReferenceId).HasColumnName("reference_id");
        builder.Property(j => j.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(j => j.PostedAt).HasColumnName("posted_at").HasColumnType("timestamptz");
        builder.Property(j => j.AvailableAt).HasColumnName("available_at").HasColumnType("timestamptz");

        builder.HasMany(j => j.Entries)
            .WithOne()
            .HasForeignKey(e => e.JournalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(j => j.Entries).HasField("_entries");
    }
}

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entry");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => LedgerEntryId.From(value));

        builder.Property(e => e.JournalId)
            .HasColumnName("journal_id")
            .HasConversion(id => id.Value, value => LedgerJournalId.From(value));

        builder.Property(e => e.AccountType)
            .HasColumnName("account_type")
            .HasColumnType("billing.ledger_account_type");

        builder.Property(e => e.OrganizationId).HasColumnName("organization_id");
        builder.Property(e => e.Direction)
            .HasColumnName("direction")
            .HasColumnType("billing.entry_direction");

        builder.Property(e => e.AmountMinor).HasColumnName("amount_minor");
        builder.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
    }
}

internal sealed class PayoutProfileConfiguration : IEntityTypeConfiguration<PayoutProfile>
{
    public void Configure(EntityTypeBuilder<PayoutProfile> builder)
    {
        builder.ToTable("payout_profile");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => PayoutProfileId.From(value));

        builder.Property(p => p.OrganizationId)
            .HasColumnName("organization_id")
            .HasConversion(id => id.Value, value => OrganizationId.From(value));

        builder.Property(p => p.LegalEntityType)
            .HasColumnName("legal_entity_type")
            .HasColumnType("billing.legal_entity_type");

        builder.Property(p => p.LegalName).HasColumnName("legal_name").HasMaxLength(PayoutProfile.MaxLegalNameLength).IsRequired();
        builder.Property(p => p.AddressLine1).HasColumnName("address_line1").HasMaxLength(PayoutProfile.MaxAddressLineLength).IsRequired();
        builder.Property(p => p.AddressLine2).HasColumnName("address_line2").HasMaxLength(PayoutProfile.MaxAddressLineLength);
        builder.Property(p => p.City).HasColumnName("city").HasMaxLength(PayoutProfile.MaxCityLength).IsRequired();
        builder.Property(p => p.Region).HasColumnName("region").HasMaxLength(PayoutProfile.MaxRegionLength);
        builder.Property(p => p.PostalCode).HasColumnName("postal_code").HasMaxLength(PayoutProfile.MaxPostalCodeLength).IsRequired();
        builder.Property(p => p.CountryCode).HasColumnName("country_code").HasMaxLength(PayoutProfile.MaxCountryCodeLength).IsRequired();
        builder.Property(p => p.TaxIdProtected).HasColumnName("tax_id_protected");
        builder.Property(p => p.RepresentativeName).HasColumnName("representative_name").HasMaxLength(PayoutProfile.MaxRepresentativeNameLength);
        builder.Property(p => p.BankAccountProtected).HasColumnName("bank_account_protected");
        builder.Property(p => p.BankAccountLast4).HasColumnName("bank_account_last4").HasMaxLength(4);
        builder.Property(p => p.BankName).HasColumnName("bank_name").HasMaxLength(PayoutProfile.MaxBankNameLength);

        builder.Property(p => p.PayoutRail)
            .HasColumnName("payout_rail")
            .HasColumnType("billing.payout_rail");

        builder.Property(p => p.VerificationStatus)
            .HasColumnName("verification_status")
            .HasColumnType("billing.payout_verification_status");

        builder.Property(p => p.ExternalRecipientId).HasColumnName("external_recipient_id").HasMaxLength(PayoutProfile.MaxExternalRecipientIdLength);

        builder.Property<List<string>>("_documentObjectKeys")
            .HasColumnName("document_object_keys")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb");

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(p => p.VerifiedAt).HasColumnName("verified_at").HasColumnType("timestamptz");

        builder.Property(p => p.VerifiedBy)
            .HasColumnName("verified_by")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? AccountId.From(value.Value) : null);

        builder.Property(p => p.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(PayoutProfile.MaxRejectionReasonLength);

        builder.HasIndex(p => p.OrganizationId).IsUnique();
        builder.HasIndex(p => p.VerificationStatus);
    }
}

internal sealed class WithdrawalRequestConfiguration : IEntityTypeConfiguration<WithdrawalRequest>
{
    public void Configure(EntityTypeBuilder<WithdrawalRequest> builder)
    {
        builder.ToTable("withdrawal_request");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => WithdrawalRequestId.From(value));

        builder.Property(w => w.OrganizationId)
            .HasColumnName("organization_id")
            .HasConversion(id => id.Value, value => OrganizationId.From(value));

        builder.Property(w => w.AmountMinor).HasColumnName("amount_minor");
        builder.Property(w => w.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();

        builder.Property(w => w.Status)
            .HasColumnName("status")
            .HasColumnType("billing.withdrawal_status");

        builder.Property(w => w.FxRateId)
            .HasColumnName("fx_rate_id")
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? FxRateId.From(value.Value) : null);

        builder.Property(w => w.TransferReference).HasColumnName("transfer_reference").HasMaxLength(256);
        builder.Property(w => w.ProofObjectKey).HasColumnName("proof_object_key").HasMaxLength(WithdrawalRequest.MaxProofObjectKeyLength);
        builder.Property(w => w.RequestedAt).HasColumnName("requested_at").HasColumnType("timestamptz");
        builder.Property(w => w.CompletedAt).HasColumnName("completed_at").HasColumnType("timestamptz");
        builder.Property(w => w.FailedAt).HasColumnName("failed_at").HasColumnType("timestamptz");

        builder.HasIndex(w => w.OrganizationId);
        builder.HasIndex(w => new { w.OrganizationId, w.Status });
    }
}

internal sealed class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
{
    public void Configure(EntityTypeBuilder<CreditNote> builder)
    {
        builder.ToTable("credit_note");

        builder.HasKey(note => note.Id);

        builder.Property(note => note.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => CreditNoteId.From(value));

        builder.Property(note => note.CreditNoteNumber).HasColumnName("credit_note_number").HasMaxLength(64).IsRequired();
        builder.Property(note => note.TaxInvoiceId)
            .HasColumnName("tax_invoice_id")
            .HasConversion(id => id.Value, value => TaxInvoiceId.From(value));

        builder.Property(note => note.PurchaseId)
            .HasColumnName("purchase_id")
            .HasConversion(id => id.Value, value => PurchaseId.From(value));

        builder.Property(note => note.IssuedAt).HasColumnName("issued_at").HasColumnType("timestamptz");
        builder.Property(note => note.GrossMinor).HasColumnName("gross_minor");
        builder.Property(note => note.VatMinor).HasColumnName("vat_minor");
        builder.Property(note => note.NetExVatMinor).HasColumnName("net_ex_vat_minor");
        builder.Property(note => note.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(note => note.VatRateBps).HasColumnName("vat_rate_bps");
        builder.Property(note => note.RefundFeeBearer)
            .HasColumnName("refund_fee_bearer")
            .HasColumnType("billing.refund_fee_bearer");

        builder.Property(note => note.RefundFeeMinor).HasColumnName("refund_fee_minor");

        builder.HasIndex(note => note.CreditNoteNumber).IsUnique();
        builder.HasIndex(note => note.PurchaseId).IsUnique();
    }
}

internal sealed class TaxInvoiceConfiguration : IEntityTypeConfiguration<TaxInvoice>
{
    public void Configure(EntityTypeBuilder<TaxInvoice> builder)
    {
        builder.ToTable("tax_invoice");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => TaxInvoiceId.From(value));

        builder.Property(i => i.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(64).IsRequired();
        builder.Property(i => i.PurchaseId)
            .HasColumnName("purchase_id")
            .HasConversion(id => id.Value, value => PurchaseId.From(value));

        builder.Property(i => i.BuyerAccountId)
            .HasColumnName("buyer_account_id")
            .HasConversion(id => id.Value, value => AccountId.From(value));

        builder.Property(i => i.IssuedAt).HasColumnName("issued_at").HasColumnType("timestamptz");
        builder.Property(i => i.GrossMinor).HasColumnName("gross_minor");
        builder.Property(i => i.VatMinor).HasColumnName("vat_minor");
        builder.Property(i => i.NetExVatMinor).HasColumnName("net_ex_vat_minor");
        builder.Property(i => i.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(i => i.VatRateBps).HasColumnName("vat_rate_bps");

        builder.HasIndex(i => i.InvoiceNumber).IsUnique();
    }
}

internal sealed class BannedPaymentInstrumentConfiguration : IEntityTypeConfiguration<BannedPaymentInstrument>
{
    public void Configure(EntityTypeBuilder<BannedPaymentInstrument> builder)
    {
        builder.ToTable("banned_payment_instrument");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => BannedPaymentInstrumentId.From(value));

        builder.Property(b => b.PaymentMethodFingerprint)
            .HasColumnName("payment_method_fingerprint")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(b => b.Reason).HasColumnName("reason").HasMaxLength(512);
        builder.Property(b => b.BannedAt).HasColumnName("banned_at").HasColumnType("timestamptz");

        builder.HasIndex(b => b.PaymentMethodFingerprint).IsUnique();
    }
}

internal sealed class SellerReceivableConfiguration : IEntityTypeConfiguration<SellerReceivable>
{
    public void Configure(EntityTypeBuilder<SellerReceivable> builder)
    {
        builder.ToTable("seller_receivable");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => SellerReceivableId.From(value));

        builder.Property(r => r.OrganizationId)
            .HasColumnName("organization_id")
            .HasConversion(id => id.Value, value => OrganizationId.From(value));

        builder.Property(r => r.PurchaseId)
            .HasColumnName("purchase_id")
            .HasConversion(id => id.Value, value => PurchaseId.From(value));

        builder.Property(r => r.AmountMinor).HasColumnName("amount_minor");
        builder.Property(r => r.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        builder.Property(r => r.IsSettled).HasColumnName("is_settled");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(r => r.SettledAt).HasColumnName("settled_at").HasColumnType("timestamptz");
    }
}

internal sealed class FxRateConfiguration : IEntityTypeConfiguration<FxRate>
{
    public void Configure(EntityTypeBuilder<FxRate> builder)
    {
        builder.ToTable("fx_rate");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, value => FxRateId.From(value));

        builder.Property(f => f.BaseCurrency).HasColumnName("base_currency").HasMaxLength(3).IsRequired();
        builder.Property(f => f.QuoteCurrency).HasColumnName("quote_currency").HasMaxLength(3).IsRequired();
        builder.Property(f => f.Rate).HasColumnName("rate").HasPrecision(18, 8);

        builder.Property(f => f.Source)
            .HasColumnName("source")
            .HasColumnType("billing.fx_rate_source");

        builder.Property(f => f.EffectiveAt).HasColumnName("effective_at").HasColumnType("timestamptz");
        builder.Property(f => f.ImportedAt).HasColumnName("imported_at").HasColumnType("timestamptz");

        builder.HasIndex(f => new { f.BaseCurrency, f.QuoteCurrency, f.EffectiveAt });
    }
}
