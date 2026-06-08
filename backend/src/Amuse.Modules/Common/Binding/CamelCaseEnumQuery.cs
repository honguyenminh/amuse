using System.Text.Json;
using Amuse.Domain.Billing;
using Amuse.Domain.Tenancy;

namespace Amuse.Modules.Common.Binding;

public static class CamelCaseEnumQuery
{
    private static readonly JsonNamingPolicy Naming = JsonNamingPolicy.CamelCase;

    public static bool TryParseOnboardingStatus(
        string? value,
        out OrganizationOnboardingStatus? status) =>
        TryParse(value, out status);

    public static bool TryParsePayoutVerificationStatus(
        string? value,
        out PayoutVerificationStatus? status) =>
        TryParse(value, out status);

    public static bool TryParseWithdrawalStatus(
        string? value,
        out WithdrawalStatus? status) =>
        TryParse(value, out status);

    public static bool TryParse<TEnum>(string? value, out TEnum? status)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = null;
            return true;
        }

        foreach (TEnum candidate in Enum.GetValues<TEnum>())
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
