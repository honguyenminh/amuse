using Amuse.Domain.Billing;

namespace Amuse.Modules.Billing.Features.PayoutProfile;

internal static class PayoutProfileMapper
{
    public static PayoutProfileResponse ToResponse(Amuse.Domain.Billing.PayoutProfile profile) =>
        new(
            profile.Id.Value,
            profile.OrganizationId.Value,
            profile.LegalEntityType,
            profile.LegalName,
            profile.AddressLine1,
            profile.AddressLine2,
            profile.City,
            profile.Region,
            profile.PostalCode,
            profile.CountryCode,
            !string.IsNullOrWhiteSpace(profile.TaxIdProtected),
            profile.RepresentativeName,
            profile.PayoutRail,
            MaskBankAccount(profile.BankAccountLast4),
            profile.BankName,
            profile.VerificationStatus,
            profile.DocumentObjectKeys,
            profile.CreatedAt,
            profile.UpdatedAt,
            profile.VerifiedAt,
            profile.RejectionReason,
            profile.BlocksWithdrawals);

    public static PlatformPayoutProfileRow ToPlatformRow(Amuse.Domain.Billing.PayoutProfile profile) =>
        new(
            profile.Id.Value,
            profile.OrganizationId.Value,
            profile.LegalEntityType,
            profile.LegalName,
            profile.CountryCode,
            profile.PayoutRail,
            profile.VerificationStatus,
            MaskBankAccount(profile.BankAccountLast4),
            profile.BankName,
            profile.DocumentObjectKeys,
            profile.UpdatedAt);

    public static string? MaskBankAccount(string? last4) =>
        last4 is { Length: 4 } ? $"****{last4}" : null;
}
