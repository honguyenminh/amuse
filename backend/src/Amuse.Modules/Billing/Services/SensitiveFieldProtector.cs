using Amuse.Modules.Billing.Contracts;
using Microsoft.AspNetCore.DataProtection;

namespace Amuse.Modules.Billing.Services;

internal sealed class SensitiveFieldProtector(IDataProtectionProvider dataProtectionProvider)
    : ISensitiveFieldProtector
{
    private readonly IDataProtector _protector =
        dataProtectionProvider.CreateProtector("Amuse.Billing.PayoutProfile.SensitiveFields");

    public string Protect(string plaintext) => _protector.Protect(plaintext);

    public string Unprotect(string protectedPayload) => _protector.Unprotect(protectedPayload);
}
