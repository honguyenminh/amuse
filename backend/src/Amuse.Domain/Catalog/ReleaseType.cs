using System.Diagnostics.CodeAnalysis;

namespace Amuse.Domain.Catalog;

public enum ReleaseType
{
    [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "Music industry term for a single-track release.")]
    Single = 1,
    Ep = 2,
    Album = 3,
    Compilation = 4,
}
