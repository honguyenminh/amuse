using Amuse.Domain.Billing;

namespace Amuse.Modules.Billing.Features.PayoutProfile;

public sealed record PayoutProfileResponse(
    Guid Id,
    Guid OrganizationId,
    LegalEntityType LegalEntityType,
    string LegalName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string PostalCode,
    string CountryCode,
    bool HasTaxId,
    string? RepresentativeName,
    PayoutRail PayoutRail,
    string? BankAccountMasked,
    string? BankName,
    PayoutVerificationStatus VerificationStatus,
    IReadOnlyList<string> DocumentObjectKeys,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? VerifiedAt,
    string? RejectionReason,
    bool BlocksWithdrawals);

public sealed record UpsertPayoutProfileRequest(
    LegalEntityType LegalEntityType,
    string LegalName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string PostalCode,
    string CountryCode,
    string? TaxId,
    string? RepresentativeName,
    PayoutRail PayoutRail,
    string? BankAccountNumber,
    string? BankName,
    IReadOnlyList<string> DocumentObjectKeys);

public sealed record RejectPayoutProfileRequest(string Reason);

public sealed record PlatformPayoutProfileRow(
    Guid Id,
    Guid OrganizationId,
    LegalEntityType LegalEntityType,
    string LegalName,
    string CountryCode,
    PayoutRail PayoutRail,
    PayoutVerificationStatus VerificationStatus,
    string? BankAccountMasked,
    string? BankName,
    IReadOnlyList<string> DocumentObjectKeys,
    DateTimeOffset UpdatedAt);
