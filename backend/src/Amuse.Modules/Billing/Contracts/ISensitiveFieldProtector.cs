namespace Amuse.Modules.Billing.Contracts;

public interface ISensitiveFieldProtector
{
    string Protect(string plaintext);

    string Unprotect(string protectedPayload);
}
