using Amuse.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Tenancy.Persistence;

public static class TenancyDbContextOptions
{
    public static void Configure(DbContextOptionsBuilder options, string connectionString) =>
        options.UseNpgsql(
            connectionString,
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory_tenancy", "tenancy");
                npgsql.MapEnum<OrganizationClass>("org_class", "tenancy");
                npgsql.MapEnum<OrganizationLifecycleStatus>("organization_lifecycle_status", "tenancy");
                npgsql.MapEnum<OrganizationOnboardingStatus>("organization_onboarding_status", "tenancy");
                npgsql.MapEnum<OrganizationTrustTier>("organization_trust_tier", "tenancy");
            });
}
