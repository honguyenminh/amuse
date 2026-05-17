using Microsoft.AspNetCore.Identity;

namespace Amuse.Modules.Identity.Persistence;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? AccountId { get; set; }
}
