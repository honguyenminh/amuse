using Amuse.Domain.Billing;
using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Billing.Contracts;

public sealed record OrgBalanceSnapshot(IReadOnlyList<CurrencyBalance> Balances);

public interface ILedgerBalanceReadModel
{
    Task<OrgBalanceSnapshot> GetBalanceAsync(
        OrganizationId organizationId,
        CancellationToken cancellationToken);
}
