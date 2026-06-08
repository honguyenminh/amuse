using System.Security.Claims;
using Amuse.Domain.Billing;
using Amuse.Domain.SharedKernel;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Common.Time;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Billing.Features.PayoutProfile;

internal sealed class UpsertPayoutProfileHandler(
    BillingDbContext billingDb,
    ISensitiveFieldProtector sensitiveFieldProtector,
    IClock clock)
{
    public async Task<Result<PayoutProfileResponse>> HandleAsync(
        UpsertPayoutProfileRequest request,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken)
    {
        var orgResult = BillingPersonaAccessor.GetOrganizationId(principal);
        if (!orgResult.IsSuccess)
            return Result<PayoutProfileResponse>.Failure(orgResult.Error!);

        var organizationId = orgResult.Value!;
        var now = clock.UtcNow;

        var profile = await billingDb.PayoutProfiles
            .SingleOrDefaultAsync(p => p.OrganizationId == organizationId, cancellationToken);

        if (profile is null)
        {
            var draft = Amuse.Domain.Billing.PayoutProfile.CreateDraft(
                organizationId,
                request.LegalEntityType,
                request.LegalName,
                now);
            if (!draft.IsSuccess)
                return Result<PayoutProfileResponse>.Failure(draft.Error!);

            profile = draft.Value!;
            billingDb.PayoutProfiles.Add(profile);
        }

        var taxIdProtected = ResolveProtectedField(
            request.TaxId,
            profile.TaxIdProtected,
            sensitiveFieldProtector,
            BillingErrors.PayoutProfileInvalidTaxId);

        if (!taxIdProtected.IsSuccess)
            return Result<PayoutProfileResponse>.Failure(taxIdProtected.Error!);

        var bankProtected = ResolveBankAccount(
            request.BankAccountNumber,
            profile.BankAccountProtected,
            profile.BankAccountLast4,
            sensitiveFieldProtector);

        if (!bankProtected.IsSuccess)
            return Result<PayoutProfileResponse>.Failure(bankProtected.Error!);

        var update = new PayoutProfileDetailsUpdate(
            request.LegalEntityType,
            request.LegalName,
            request.AddressLine1,
            request.AddressLine2,
            request.City,
            request.Region,
            request.PostalCode,
            request.CountryCode,
            taxIdProtected.Value!.Protected,
            request.RepresentativeName,
            request.PayoutRail,
            bankProtected.Value!.Protected,
            bankProtected.Value!.Last4,
            request.BankName,
            request.DocumentObjectKeys);

        var applyResult = profile.ApplyDetails(update, now);
        if (!applyResult.IsSuccess)
            return Result<PayoutProfileResponse>.Failure(applyResult.Error!);

        await billingDb.SaveChangesAsync(cancellationToken);
        return Result<PayoutProfileResponse>.Success(PayoutProfileMapper.ToResponse(profile));
    }

    private static Result<(string? Protected, string? Last4)> ResolveBankAccount(
        string? bankAccountNumber,
        string? existingProtected,
        string? existingLast4,
        ISensitiveFieldProtector protector)
    {
        if (string.IsNullOrWhiteSpace(bankAccountNumber))
            return Result<(string?, string?)>.Success((null, existingLast4));

        var normalized = bankAccountNumber.Trim();
        if (normalized.Length is < 4 or > Amuse.Domain.Billing.PayoutProfile.MaxBankAccountLength)
            return Result<(string?, string?)>.Failure(BillingErrors.PayoutProfileInvalidBankAccount);

        var last4 = normalized[^4..];
        return Result<(string?, string?)>.Success((protector.Protect(normalized), last4));
    }

    private static Result<(string? Protected, string _)> ResolveProtectedField(
        string? plaintext,
        string? existingProtected,
        ISensitiveFieldProtector protector,
        DomainError requiredError)
    {
        if (string.IsNullOrWhiteSpace(plaintext))
        {
            if (string.IsNullOrWhiteSpace(existingProtected))
                return Result<(string?, string)>.Failure(requiredError);

            return Result<(string?, string)>.Success((null, string.Empty));
        }

        var normalized = plaintext.Trim();
        if (normalized.Length == 0)
            return Result<(string?, string)>.Failure(requiredError);

        return Result<(string?, string)>.Success((protector.Protect(normalized), string.Empty));
    }
}
