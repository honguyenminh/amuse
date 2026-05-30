using Amuse.Modules.Identity.Features.RegisterPassword;
using Amuse.Modules.Identity.Options;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Identity.Email;

internal sealed class EmailConfirmationLinkBuilder(IOptions<IdentityEmailOptions> options)
{
    public string Build(RegistrationPortal portal, Guid userId, string token)
    {
        var baseUrl = portal switch
        {
            RegistrationPortal.Consumer => options.Value.ConsumerAppBaseUrl,
            RegistrationPortal.Business => options.Value.BusinessAppBaseUrl,
            _ => options.Value.ConsumerAppBaseUrl,
        };

        baseUrl = baseUrl.TrimEnd('/');
        var encodedToken = Uri.EscapeDataString(token);
        return $"{baseUrl}/confirm-email?userId={userId:D}&token={encodedToken}";
    }
}
