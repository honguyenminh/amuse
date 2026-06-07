using System.Text.Json.Serialization;

namespace Amuse.Modules.Identity.Auth;

internal sealed record JwtBlacklistRevocationMessage(
    [property: JsonPropertyName("jti")] string Jti,
    [property: JsonPropertyName("expUnix")] long ExpUnix);
