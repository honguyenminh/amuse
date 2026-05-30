using System.Text.Json;
using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Common.Binding;

public static class CamelCaseEnumQuery
{
    private static readonly JsonNamingPolicy Naming = JsonNamingPolicy.CamelCase;

    public static bool TryParseOnboardingStatus(
        string? value,
        out OrganizationOnboardingStatus? status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = null;
            return true;
        }

        foreach (OrganizationOnboardingStatus candidate in Enum.GetValues<OrganizationOnboardingStatus>())
        {
            var name = candidate.ToString();
            if (string.Equals(name, value, StringComparison.OrdinalIgnoreCase)
                || string.Equals(Naming.ConvertName(name!), value, StringComparison.OrdinalIgnoreCase))
            {
                status = candidate;
                return true;
            }
        }

        status = null;
        return false;
    }
}
