namespace Amuse.Architecture.Tests.EntityTracking;

internal sealed record EntityTrackingViolation(
    string RuleId,
    string FilePath,
    int Line,
    string Message);
