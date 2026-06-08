namespace Amuse.Modules.Billing.Features.Common;

public sealed record FreeAcquisitionRequest(
    Guid? TrackId,
    Guid? ReleaseId);

public sealed record FreeAcquisitionResponse(
    Guid PurchaseId,
    string PurchasedUnit,
    Guid? TrackId,
    Guid? ReleaseId,
    bool ReleaseEntitlementGranted);

public sealed record PurchasedTrackRow(
    Guid PurchaseId,
    Guid TrackId,
    Guid ReleaseId,
    string ReleaseTitle,
    string TrackTitle,
    string ArtistName,
    string ArtistSlug,
    string ReleaseSlug,
    string? CoverArtUrl,
    long PriceSnapshotMinor,
    string Currency,
    string PaymentStatus,
    DateTimeOffset PurchasedAt);

public sealed record PurchasedReleaseRow(
    Guid PurchaseId,
    Guid ReleaseId,
    string ReleaseTitle,
    string ArtistName,
    string ArtistSlug,
    string ReleaseSlug,
    string? CoverArtUrl,
    long PriceSnapshotMinor,
    string Currency,
    string PaymentStatus,
    DateTimeOffset PurchasedAt);

public sealed record MyPurchasesResponse(
    IReadOnlyList<PurchasedTrackRow> Tracks,
    IReadOnlyList<PurchasedReleaseRow> Releases);

public sealed record OwnershipCheckResponse(
    bool OwnsTrack,
    bool OwnsRelease);

public sealed record CreateCheckoutSessionRequest(
    Guid? TrackId,
    Guid? ReleaseId,
    long AmountMinor);

public sealed record CheckoutSessionResponse(
    Guid PurchaseId,
    string SessionId,
    string CheckoutUrl);
