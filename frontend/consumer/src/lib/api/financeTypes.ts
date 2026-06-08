export type FreeAcquisitionRequest = {
  trackId?: string;
  releaseId?: string;
};

export type FreeAcquisitionResponse = {
  purchaseId: string;
  purchasedUnit: "track" | "release";
  trackId: string | null;
  releaseId: string | null;
  releaseEntitlementGranted: boolean;
};

export type PurchasedTrackRow = {
  purchaseId: string;
  trackId: string;
  releaseId: string;
  releaseTitle: string;
  trackTitle: string;
  artistName: string;
  artistSlug: string;
  releaseSlug: string;
  coverArtUrl: string | null;
  priceSnapshotMinor: number;
  currency: string;
  paymentStatus: string;
  purchasedAt: string;
};

export type PurchasedReleaseRow = {
  purchaseId: string;
  releaseId: string;
  releaseTitle: string;
  artistName: string;
  artistSlug: string;
  releaseSlug: string;
  coverArtUrl: string | null;
  priceSnapshotMinor: number;
  currency: string;
  paymentStatus: string;
  purchasedAt: string;
};

export type MyPurchasesResponse = {
  tracks: PurchasedTrackRow[];
  releases: PurchasedReleaseRow[];
};

export type OwnershipCheckResponse = {
  ownsTrack: boolean;
  ownsRelease: boolean;
};

export type TrackDownloadResponse = {
  trackId: string;
  url: string;
  contentType: string;
  expiresAt: string;
  fileName: string;
};

export type CreateCheckoutSessionRequest = {
  trackId?: string;
  releaseId?: string;
  amountMinor: number;
};

export type CheckoutSessionResponse = {
  purchaseId: string;
  sessionId: string;
  checkoutUrl: string;
};
