using Microsoft.EntityFrameworkCore;

namespace Amuse.Modules.Common.Persistence;

public static class DbContextConfigurationExtensions
{
    public static void ApplyConfigurationsFromNamespace(
        this ModelBuilder modelBuilder,
        Type contextType,
        string configurationNamespace) =>
        modelBuilder.ApplyConfigurationsFromAssembly(
            contextType.Assembly,
            type => type.Namespace == configurationNamespace);
}
