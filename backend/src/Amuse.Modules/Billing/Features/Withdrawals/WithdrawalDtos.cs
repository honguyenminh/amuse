using Amuse.Domain.Billing;

namespace Amuse.Modules.Billing.Features.Withdrawals;

public sealed record CreateWithdrawalRequest(
    long AmountMinor,
    string Currency);

public sealed record WithdrawalRow(
    Guid Id,
    long AmountMinor,
    string Currency,
    WithdrawalStatus Status,
    string? TransferReference,
    string? ProofObjectKey,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? FailedAt);

public sealed record CompleteWithdrawalRequest(
    string TransferReference,
    string? ProofObjectKey);

public sealed record PlatformWithdrawalRow(
    Guid Id,
    Guid OrganizationId,
    long AmountMinor,
    string Currency,
    WithdrawalStatus Status,
    DateTimeOffset RequestedAt,
    string? TransferReference);
