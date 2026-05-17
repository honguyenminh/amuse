using Amuse.Domain.Identity;
using Amuse.Modules.Identity.Contracts;
using Amuse.Modules.Identity.Options;
using Amuse.Modules.Identity.Auth;
using Microsoft.Extensions.Options;

namespace Amuse.Modules.Identity.Tests;

public sealed class TokenIssuerTests
{
    [Fact]
    public void AccessToken_contains_ctx_and_sub()
    {
        var issuer = new TokenIssuer(Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "amuse",
            Audience = "amuse-api",
            SigningKey = "DEV_ONLY_CHANGE_ME_32_CHARS_MINIMUM_KEY",
            AccessTokenMinutes = 15,
        }));

        var token = issuer.CreateAccessToken(
            AccountId.New(),
            new PersonaAccessContext("listener", null, Guid.CreateVersion7(), null, ["listener:access"]),
            DateTimeOffset.UtcNow);

        Assert.Contains('.', token);
    }
}
