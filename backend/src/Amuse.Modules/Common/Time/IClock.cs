namespace Amuse.Modules.Common.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
