using Amuse.Architecture.Tests.EntityTracking;

namespace Amuse.Architecture.Tests;

public sealed class EntityTrackingArchitectureTests
{
    [Fact]
    public void Production_modules_do_not_mutate_AsNoTracking_entities_before_SaveChanges()
    {
        var backendRoot = FindBackendRoot();
        var modulesPath = Path.Combine(backendRoot, "src", "Amuse.Modules");
        var workerPath = Path.Combine(backendRoot, "src", "Amuse.Worker.Transcoder");

        var violations = EntityTrackingRules.AnalyzeDirectory(modulesPath)
            .Concat(EntityTrackingRules.AnalyzeDirectory(workerPath))
            .Where(v => v.RuleId == EntityTrackingRules.NoTrackingMutationRuleId)
            .ToList();

        Assert.True(
            violations.Count == 0,
            FormatViolations(
                "AsNoTracking entities must not be mutated in methods that call SaveChangesAsync.",
                violations));
    }

    [Fact]
    public void Profile_services_do_not_expose_unsafe_GetFor_domain_loaders()
    {
        var backendRoot = FindBackendRoot();
        var modulesPath = Path.Combine(backendRoot, "src", "Amuse.Modules");

        var violations = EntityTrackingRules.AnalyzeDirectory(modulesPath)
            .Where(v => v.RuleId == EntityTrackingRules.ProfileServiceUnsafeGetRuleId)
            .ToList();

        Assert.True(
            violations.Count == 0,
            FormatViolations(
                "Profile service types must not expose GetFor* methods that return domain entities without ForUpdate/ForRead.",
                violations));
    }

    [Fact]
    public void Analyzer_flags_AsNoTracking_mutation_before_SaveChanges()
    {
        const string source = """
            using System.Threading.Tasks;
            namespace Sample;

            internal sealed class BadHandler
            {
                public async Task HandleAsync(SampleDbContext db)
                {
                    var profile = await db.Profiles.AsNoTracking().FirstAsync();
                    profile.Update();
                    await db.SaveChangesAsync();
                }
            }
            """;

        var violations = EntityTrackingRules.AnalyzeFile("Sample/BadHandler.cs", source);

        Assert.Contains(
            violations,
            v => v.RuleId == EntityTrackingRules.NoTrackingMutationRuleId
                && v.Message.Contains("profile", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyzer_flags_unsafe_profile_service_GetFor_domain_return()
    {
        const string source = """
            using System.Threading.Tasks;
            namespace Amuse.Domain.Listener;

            internal sealed class ListenerProfile {}
            internal sealed class ListenerPreference {}

            namespace Amuse.Modules.Listener.Services;

            internal sealed class ListenerProfileService
            {
                public Task<(ListenerProfile Profile, ListenerPreference? Preference)> GetForAccountAsync()
                    => throw null!;
            }
            """;

        var violations = EntityTrackingRules.AnalyzeFile(
            "Amuse.Modules/Listener/Services/ListenerProfileService.cs",
            source);

        Assert.Contains(
            violations,
            v => v.RuleId == EntityTrackingRules.ProfileServiceUnsafeGetRuleId
                && v.Message.Contains("GetForAccountAsync", StringComparison.Ordinal));
    }

    private static string FindBackendRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate backend root (backend.slnx).");
    }

    private static string FormatViolations(string heading, IReadOnlyList<EntityTrackingViolation> violations)
    {
        if (violations.Count == 0)
            return heading;

        var lines = violations
            .Select(v => $"  [{v.RuleId}] {v.FilePath}:{v.Line} {v.Message}");

        return heading + Environment.NewLine + string.Join(Environment.NewLine, lines);
    }
}
