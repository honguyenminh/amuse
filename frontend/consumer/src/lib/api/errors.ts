import { ApiError } from "@/lib/api/types";

/** Catalog not-found responses use HTTP 400; discovery uses 404. */
export function isNotFoundError(error: unknown): boolean {
  return (
    error instanceof ApiError && (error.status === 400 || error.status === 404)
  );
}

export function isForbiddenError(error: unknown): boolean {
  return error instanceof ApiError && error.status === 403;
}

/**
 * SSR cannot forward browser cookies today. A 403 usually means the playlist is
 * private — fall back to client fetch so the owner can still load it when logged in.
 */
export function shouldFallbackToClientFetch(error: unknown): boolean {
  return isForbiddenError(error);
}
