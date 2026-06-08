using Amuse.Domain.Billing.PayoutProfiles;
using Amuse.Domain.Identity;
using Amuse.Domain.SharedKernel;
using Amuse.Domain.Tenancy;

namespace Amuse.Domain.Billing;

public sealed class PayoutProfile
{
    public const int MaxLegalNameLength = 256;
    public const int MaxAddressLineLength = 256;
    public const int MaxCityLength = 128;
    public const int MaxRegionLength = 128;
    public const int MaxPostalCodeLength = 32;
    public const int MaxRepresentativeNameLength = 256;
    public const int MaxBankNameLength = 256;
    public const int MaxDocumentKeyLength = 512;
    public const int MaxRejectionReasonLength = 512;
    public const int MaxCountryCodeLength = 2;
    public const int MaxExternalRecipientIdLength = 256;
    public const int MaxBankAccountLength = 64;

    public PayoutProfileId Id { get; private set; }
    public OrganizationId OrganizationId { get; private set; }
    public LegalEntityType LegalEntityType { get; private set; }
    public string LegalName { get; private set; } = null!;
    public string AddressLine1 { get; private set; } = null!;
    public string? AddressLine2 { get; private set; }
    public string City { get; private set; } = null!;
    public string? Region { get; private set; }
    public string PostalCode { get; private set; } = null!;
    public string CountryCode { get; private set; } = null!;
    public string? TaxIdProtected { get; private set; }
    public string? RepresentativeName { get; private set; }
    public PayoutRail PayoutRail { get; private set; }
    public string? BankAccountProtected { get; private set; }
    public string? BankAccountLast4 { get; private set; }
    public string? BankName { get; private set; }
    public PayoutVerificationStatus VerificationStatus { get; private set; }
    public string? ExternalRecipientId { get; private set; }
    public IReadOnlyList<string> DocumentObjectKeys => _documentObjectKeys;

    private List<string> _documentObjectKeys = [];
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public AccountId? VerifiedBy { get; private set; }
    public string? RejectionReason { get; private set; }

    private PayoutProfileState? _state;

    private PayoutProfileState State => _state ??= PayoutProfileStates.From(VerificationStatus);

    private PayoutProfile()
    {
    }

    public bool IsVerified => VerificationStatus == PayoutVerificationStatus.Verified;

    public bool BlocksWithdrawals =>
        VerificationStatus is not PayoutVerificationStatus.Verified;

    public static Result<PayoutProfile> CreateDraft(
        OrganizationId organizationId,
        LegalEntityType legalEntityType,
        string legalName,
        DateTimeOffset now)
    {
        var normalizedName = legalName.Trim();
        if (normalizedName.Length is 0 or > MaxLegalNameLength)
            return Result<PayoutProfile>.Failure(BillingErrors.PayoutProfileInvalidLegalName);

        return Result<PayoutProfile>.Success(new PayoutProfile
        {
            Id = PayoutProfileId.New(),
            OrganizationId = organizationId,
            LegalEntityType = legalEntityType,
            LegalName = normalizedName,
            AddressLine1 = string.Empty,
            City = string.Empty,
            PostalCode = string.Empty,
            CountryCode = string.Empty,
            PayoutRail = PayoutRail.ManualBank,
            VerificationStatus = PayoutVerificationStatus.NotStarted,
            _state = NotStartedPayoutProfileState.Instance,
            CreatedAt = now,
            UpdatedAt = now,
        });
    }

    public Result ApplyDetails(PayoutProfileDetailsUpdate update, DateTimeOffset now)
    {
        var normalized = NormalizeDetails(update);
        if (!normalized.IsSuccess)
            return Result.Failure(normalized.Error!);

        var details = normalized.Value!;
        var materialChange = VerificationStatus == PayoutVerificationStatus.Verified
                             && HasMaterialChange(details);

        var transitionResult = State.ApplyDetails(this, materialChange, now);
        if (!transitionResult.IsSuccess)
            return transitionResult;

        ApplyNormalizedDetails(details);
        return Result.Success();
    }

    public Result Submit(DateTimeOffset now) => State.Submit(this, now);

    public Result EnterReview(DateTimeOffset now) => State.EnterReview(this, now);

    public Result Approve(AccountId verifiedBy, DateTimeOffset now) =>
        State.Approve(this, verifiedBy, now);

    public Result Reject(string reason, DateTimeOffset now) => State.Reject(this, reason, now);

    public Result CompleteStripeVerification(DateTimeOffset now) =>
        State.CompleteStripeVerification(this, now);

    public Result SetExternalRecipientId(string recipientId, DateTimeOffset now)
    {
        var normalized = recipientId.Trim();
        if (normalized.Length is 0 or > MaxExternalRecipientIdLength)
            return Result.Failure(BillingErrors.PayoutRecipientMissing);

        ExternalRecipientId = normalized;
        Touch(now);
        return Result.Success();
    }

    public bool IsCompleteForSubmission() =>
        !string.IsNullOrWhiteSpace(LegalName)
        && !string.IsNullOrWhiteSpace(AddressLine1)
        && !string.IsNullOrWhiteSpace(City)
        && !string.IsNullOrWhiteSpace(PostalCode)
        && CountryCode.Length == MaxCountryCodeLength
        && !string.IsNullOrWhiteSpace(TaxIdProtected)
        && !string.IsNullOrWhiteSpace(BankAccountProtected)
        && BankAccountLast4 is { Length: 4 }
        && !string.IsNullOrWhiteSpace(BankName)
        && DocumentObjectKeys.Count > 0
        && (LegalEntityType != LegalEntityType.Company
            || !string.IsNullOrWhiteSpace(RepresentativeName));

    internal void TransitionTo(PayoutProfileState next)
    {
        _state = next;
        VerificationStatus = next.StatusValue;
    }

    internal void Touch(DateTimeOffset now) => UpdatedAt = now;

    internal void ClearRejection() => RejectionReason = null;

    internal void ClearVerification()
    {
        VerifiedAt = null;
        VerifiedBy = null;
        RejectionReason = null;
    }

    internal void ApplyOperatorVerification(AccountId verifiedBy, DateTimeOffset now)
    {
        VerifiedAt = now;
        VerifiedBy = verifiedBy;
        RejectionReason = null;
        Touch(now);
    }

    internal void ApplyStripeVerification(DateTimeOffset now)
    {
        VerifiedAt = now;
        VerifiedBy = null;
        RejectionReason = null;
        Touch(now);
    }

    internal void ApplyRejection(string reason, DateTimeOffset now)
    {
        VerifiedAt = null;
        VerifiedBy = null;
        RejectionReason = reason;
        Touch(now);
    }

    private void ApplyNormalizedDetails(PayoutProfileDetailsUpdate details)
    {
        LegalEntityType = details.LegalEntityType;
        LegalName = details.LegalName;
        AddressLine1 = details.AddressLine1;
        AddressLine2 = details.AddressLine2;
        City = details.City;
        Region = details.Region;
        PostalCode = details.PostalCode;
        CountryCode = details.CountryCode;
        RepresentativeName = details.RepresentativeName;
        PayoutRail = details.PayoutRail;
        BankName = details.BankName;
        _documentObjectKeys = details.DocumentObjectKeys.ToList();

        if (details.TaxIdProtected is not null)
            TaxIdProtected = details.TaxIdProtected;

        if (details.BankAccountProtected is not null)
        {
            BankAccountProtected = details.BankAccountProtected;
            BankAccountLast4 = details.BankAccountLast4;
        }
    }

    private bool HasMaterialChange(PayoutProfileDetailsUpdate details) =>
        LegalEntityType != details.LegalEntityType
        || !string.Equals(LegalName, details.LegalName, StringComparison.Ordinal)
        || !string.Equals(AddressLine1, details.AddressLine1, StringComparison.Ordinal)
        || !string.Equals(AddressLine2, details.AddressLine2, StringComparison.Ordinal)
        || !string.Equals(City, details.City, StringComparison.Ordinal)
        || !string.Equals(Region, details.Region, StringComparison.Ordinal)
        || !string.Equals(PostalCode, details.PostalCode, StringComparison.Ordinal)
        || !string.Equals(CountryCode, details.CountryCode, StringComparison.Ordinal)
        || (details.TaxIdProtected is not null
            && !string.Equals(TaxIdProtected, details.TaxIdProtected, StringComparison.Ordinal))
        || !string.Equals(RepresentativeName, details.RepresentativeName, StringComparison.Ordinal)
        || PayoutRail != details.PayoutRail
        || (details.BankAccountProtected is not null
            && !string.Equals(BankAccountProtected, details.BankAccountProtected, StringComparison.Ordinal))
        || !string.Equals(BankName, details.BankName, StringComparison.Ordinal)
        || !DocumentObjectKeys.SequenceEqual(details.DocumentObjectKeys);

    private static Result<PayoutProfileDetailsUpdate> NormalizeDetails(PayoutProfileDetailsUpdate update)
    {
        var legalName = update.LegalName.Trim();
        if (legalName.Length is 0 or > MaxLegalNameLength)
            return Result<PayoutProfileDetailsUpdate>.Failure(BillingErrors.PayoutProfileInvalidLegalName);

        var addressLine1 = update.AddressLine1.Trim();
        if (addressLine1.Length is 0 or > MaxAddressLineLength)
            return Result<PayoutProfileDetailsUpdate>.Failure(BillingErrors.PayoutProfileInvalidAddress);

        var addressLine2 = string.IsNullOrWhiteSpace(update.AddressLine2)
            ? null
            : update.AddressLine2.Trim();
        if (addressLine2 is { Length: > MaxAddressLineLength })
            return Result<PayoutProfileDetailsUpdate>.Failure(BillingErrors.PayoutProfileInvalidAddress);

        var city = update.City.Trim();
        if (city.Length is 0 or > MaxCityLength)
            return Result<PayoutProfileDetailsUpdate>.Failure(BillingErrors.PayoutProfileInvalidAddress);

        var region = string.IsNullOrWhiteSpace(update.Region) ? null : update.Region.Trim();
        if (region is { Length: > MaxRegionLength })
            return Result<PayoutProfileDetailsUpdate>.Failure(BillingErrors.PayoutProfileInvalidAddress);

        var postalCode = update.PostalCode.Trim();
        if (postalCode.Length is 0 or > MaxPostalCodeLength)
            return Result<PayoutProfileDetailsUpdate>.Failure(BillingErrors.PayoutProfileInvalidAddress);

        var countryCode = update.CountryCode.Trim().ToUpperInvariant();
        if (countryCode.Length != MaxCountryCodeLength)
            return Result<PayoutProfileDetailsUpdate>.Failure(BillingErrors.PayoutProfileInvalidCountry);

        string? representativeName = null;
        if (update.LegalEntityType == LegalEntityType.Company)
        {
            representativeName = update.RepresentativeName?.Trim();
            if (string.IsNullOrWhiteSpace(representativeName)
                || representativeName.Length > MaxRepresentativeNameLength)
            {
                return Result<PayoutProfileDetailsUpdate>.Failure(
                    BillingErrors.PayoutProfileCompanyRepresentativeRequired);
            }
        }

        var bankName = update.BankName?.Trim();
        if (bankName is { Length: > MaxBankNameLength })
            return Result<PayoutProfileDetailsUpdate>.Failure(BillingErrors.PayoutProfileInvalidBankAccount);

        if (update.BankAccountLast4 is { Length: not 4 })
            return Result<PayoutProfileDetailsUpdate>.Failure(BillingErrors.PayoutProfileInvalidBankAccount);

        var documentKeys = update.DocumentObjectKeys
            .Select(key => key.Trim())
            .Where(key => key.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (documentKeys.Any(key => key.Length > MaxDocumentKeyLength))
            return Result<PayoutProfileDetailsUpdate>.Failure(BillingErrors.PayoutProfileInvalidDocumentKey);

        return Result<PayoutProfileDetailsUpdate>.Success(new PayoutProfileDetailsUpdate(
            update.LegalEntityType,
            legalName,
            addressLine1,
            addressLine2,
            city,
            region,
            postalCode,
            countryCode,
            update.TaxIdProtected,
            representativeName,
            update.PayoutRail,
            update.BankAccountProtected,
            update.BankAccountLast4,
            bankName,
            documentKeys));
    }
}

public sealed record PayoutProfileDetailsUpdate(
    LegalEntityType LegalEntityType,
    string LegalName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string PostalCode,
    string CountryCode,
    string? TaxIdProtected,
    string? RepresentativeName,
    PayoutRail PayoutRail,
    string? BankAccountProtected,
    string? BankAccountLast4,
    string? BankName,
    IReadOnlyList<string> DocumentObjectKeys);
