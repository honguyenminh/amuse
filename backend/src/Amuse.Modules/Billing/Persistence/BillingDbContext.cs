using Amuse.Domain.Billing;
using Amuse.Modules.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Persistence;

public sealed class BillingDbContext : ModuleDbContextBase
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Purchase> Purchases => Set<Purchase>();
    public DbSet<PurchaseAllocationSnapshot> PurchaseAllocationSnapshots => Set<PurchaseAllocationSnapshot>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<LedgerJournal> LedgerJournals => Set<LedgerJournal>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<PayoutProfile> PayoutProfiles => Set<PayoutProfile>();
    public DbSet<WithdrawalRequest> WithdrawalRequests => Set<WithdrawalRequest>();
    public DbSet<TaxInvoice> TaxInvoices => Set<TaxInvoice>();
    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
    public DbSet<BannedPaymentInstrument> BannedPaymentInstruments => Set<BannedPaymentInstrument>();
    public DbSet<SellerReceivable> SellerReceivables => Set<SellerReceivable>();
    public DbSet<FxRate> FxRates => Set<FxRate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("billing");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
    }
}
