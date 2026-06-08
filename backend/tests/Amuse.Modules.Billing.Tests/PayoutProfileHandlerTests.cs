using Amuse.Domain.Billing;
using Amuse.Domain.Identity;
using Amuse.Domain.Tenancy;
using Amuse.Modules.Billing.Contracts;
using Amuse.Modules.Billing.Features.PayoutProfile;
using Amuse.Modules.Billing.Persistence;
using Amuse.Modules.Common.Time;
using Amuse.Modules.Platform.Features.ApprovePayoutProfile;
using Amuse.Modules.Platform.Features.RejectPayoutProfile;
using Amuse.Domain.Platform;
using Amuse.Modules.Platform.Contracts;
using Amuse.Modules.Audit;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Amuse.Modules.Billing.Tests;

public sealed class ApprovePayoutProfileHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

    [Fact]
    public async Task Approve_moves_under_review_profile_to_verified()
    {
        var orgId = OrganizationId.New();
        var operatorAccountId = AccountId.New();
        await using var billingDb = CreateBillingDb();
        var profile = SeedUnderReviewProfile(billingDb, orgId);

        var handler = new ApprovePayoutProfileHandler(
            billingDb,
            OperatorLookup(operatorAccountId),
            Substitute.For<IAuditWriter>(),
            new FixedClock(Now));

        var result = await handler.HandleAsync(
            orgId.Value,
            PlatformPrincipal(operatorAccountId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await billingDb.PayoutProfiles.SingleAsync();
        Assert.Equal(PayoutVerificationStatus.Verified, updated.VerificationStatus);
        Assert.Equal(operatorAccountId, updated.VerifiedBy);
    }

    [Fact]
    public async Task Reject_moves_under_review_profile_to_rejected()
    {
        var orgId = OrganizationId.New();
        var operatorAccountId = AccountId.New();
        await using var billingDb = CreateBillingDb();
        SeedUnderReviewProfile(billingDb, orgId);

        var handler = new RejectPayoutProfileHandler(
            billingDb,
            OperatorLookup(operatorAccountId),
            Substitute.For<IAuditWriter>(),
            new FixedClock(Now));

        var result = await handler.HandleAsync(
            orgId.Value,
            new RejectPayoutProfileRequest("Invalid bank statement"),
            PlatformPrincipal(operatorAccountId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = await billingDb.PayoutProfiles.SingleAsync();
        Assert.Equal(PayoutVerificationStatus.Rejected, updated.VerificationStatus);
        Assert.Equal("Invalid bank statement", updated.RejectionReason);
    }

    private static Amuse.Domain.Billing.PayoutProfile SeedUnderReviewProfile(
        BillingDbContext billingDb,
        OrganizationId orgId)
    {
        var profile = Amuse.Domain.Billing.PayoutProfile.CreateDraft(
            orgId,
            LegalEntityType.Individual,
            "Seller LLC",
            Now).Value!;

        profile.ApplyDetails(
            new PayoutProfileDetailsUpdate(
                LegalEntityType.Individual,
                "Seller LLC",
                "123 Main Street",
                null,
                "Ho Chi Minh City",
                null,
                "700000",
                "VN",
                "protected-tax-id",
                null,
                PayoutRail.ManualBank,
                "protected-bank-account",
                "1234",
                "Vietcombank",
                ["billing/payout-docs/id.pdf"]),
            Now);

        profile.Submit(Now);
        profile.EnterReview(Now);
        billingDb.PayoutProfiles.Add(profile);
        billingDb.SaveChanges();
        return profile;
    }

    private static BillingDbContext CreateBillingDb()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BillingDbContext(options);
    }

    private static IPlatformOperatorLookup OperatorLookup(AccountId accountId)
    {
        var lookup = Substitute.For<IPlatformOperatorLookup>();
        lookup.GetOperatorIdForAccountAsync(accountId, Arg.Any<CancellationToken>())
            .Returns(PlatformOperatorId.Root);
        return lookup;
    }

    private static System.Security.Claims.ClaimsPrincipal PlatformPrincipal(AccountId accountId)
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier,
                accountId.Value.ToString()),
            new System.Security.Claims.Claim("ctx", "platform"),
        };
        return new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(claims, "test"));
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}

public sealed class UpsertPayoutProfileHandlerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-08T12:00:00+00:00");

    [Fact]
    public async Task Upsert_encrypts_tax_id_and_bank_account()
    {
        var orgId = OrganizationId.New();
        await using var billingDb = CreateBillingDb();

        var protector = new TestSensitiveFieldProtector();
        var handler = new UpsertPayoutProfileHandler(
            billingDb,
            protector,
            new FixedClock(Now));

        var result = await handler.HandleAsync(
            new UpsertPayoutProfileRequest(
                LegalEntityType.Individual,
                "Seller LLC",
                "123 Main Street",
                null,
                "Ho Chi Minh City",
                null,
                "700000",
                "VN",
                "TAX-123",
                null,
                PayoutRail.ManualBank,
                "0123456789",
                "Vietcombank",
                ["billing/payout-docs/id.pdf"]),
            OrgPrincipal(orgId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var profile = await billingDb.PayoutProfiles.SingleAsync();
        Assert.Equal("protected:TAX-123", profile.TaxIdProtected);
        Assert.Equal("protected:0123456789", profile.BankAccountProtected);
        Assert.Equal("6789", profile.BankAccountLast4);
    }

    private static BillingDbContext CreateBillingDb()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BillingDbContext(options);
    }

    private static System.Security.Claims.ClaimsPrincipal OrgPrincipal(OrganizationId orgId)
    {
        var claims = new[]
        {
            new System.Security.Claims.Claim("ctx", "org"),
            new System.Security.Claims.Claim("org_id", orgId.Value.ToString()),
        };
        return new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(claims, "test"));
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class TestSensitiveFieldProtector : ISensitiveFieldProtector
    {
        public string Protect(string plaintext) => $"protected:{plaintext}";

        public string Unprotect(string protectedPayload) =>
            protectedPayload["protected:".Length..];
    }
}
