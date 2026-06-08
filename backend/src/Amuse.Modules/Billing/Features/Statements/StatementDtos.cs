namespace Amuse.Modules.Billing.Features.Statements;

public sealed record StatementLineRow(
    Guid Id,
    Guid PurchaseId,
    Guid TrackId,
    int ShareBps,
    long AmountMinor,
    string Currency,
    DateTimeOffset CreditedAt);

public sealed record PagedStatementsResponse(
    IReadOnlyList<StatementLineRow> Items,
    int Page,
    int PageSize,
    int TotalCount);
