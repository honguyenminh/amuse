import { authFetch } from "@/lib/auth/authFetch";
import type {
  FreeAcquisitionRequest,
  FreeAcquisitionResponse,
  MyPurchasesResponse,
  OwnershipCheckResponse,
  TrackDownloadResponse,
  CreateCheckoutSessionRequest,
  CheckoutSessionResponse,
} from "./financeTypes";

const BASE = "/api/v1/billing";

export function acquireFree(request: FreeAcquisitionRequest): Promise<FreeAcquisitionResponse> {
  return authFetch<FreeAcquisitionResponse>(`${BASE}/acquisitions/free`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function listMyPurchases(): Promise<MyPurchasesResponse> {
  return authFetch<MyPurchasesResponse>(`${BASE}/purchases/me`, { method: "GET" });
}

export function checkOwnership(params: {
  trackId?: string;
  releaseId: string;
}): Promise<OwnershipCheckResponse> {
  const search = new URLSearchParams();
  if (params.trackId) search.set("trackId", params.trackId);
  search.set("releaseId", params.releaseId);
  return authFetch<OwnershipCheckResponse>(`${BASE}/entitlements/ownership?${search}`, {
    method: "GET",
  });
}

export function checkReleaseOwnership(releaseId: string): Promise<OwnershipCheckResponse> {
  const search = new URLSearchParams({ releaseId });
  return authFetch<OwnershipCheckResponse>(`${BASE}/entitlements/ownership?${search}`, {
    method: "GET",
  });
}

export function getTrackDownload(trackId: string): Promise<TrackDownloadResponse> {
  return authFetch<TrackDownloadResponse>(
    `${BASE}/downloads/tracks/${encodeURIComponent(trackId)}`,
    { method: "GET" },
  );
}

export function createCheckoutSession(
  request: CreateCheckoutSessionRequest,
): Promise<CheckoutSessionResponse> {
  return authFetch<CheckoutSessionResponse>(`${BASE}/checkout/sessions`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}
