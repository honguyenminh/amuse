import { getAccessToken } from "@/lib/auth/sessionStore";
import { getApiBaseUrl, WEB_CLIENT_HEADER } from "./config";
import { ApiError, type ApiProblem } from "./types";

/**
 * Anonymous-friendly fetch for public endpoints (catalog browse).
 *
 * - Sends `Authorization: Bearer <token>` only when the session already has one,
 *   so logged-in visitors still get personalised results if the backend chooses
 *   to vary on identity later.
 * - Never throws "auth.not_authenticated"; the caller can fall through to
 *   anonymous data. 401 from a server-side `AllowAnonymous` endpoint would still
 *   surface as an `ApiError`, but for the current catalog endpoints that just
 *   means a real problem, not a missing token.
 * - Does not attempt token refresh; the access token is treated as best-effort.
 *   The auth-only `authFetch` is responsible for the refresh dance on protected
 *   endpoints.
 */
export async function publicFetch<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("X-Amuse-Client", WEB_CLIENT_HEADER);
  const token = getAccessToken();
  if (token) headers.set("Authorization", `Bearer ${token}`);
  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    ...init,
    headers,
    credentials: "include",
  });

  if (!response.ok) {
    let code: string | undefined;
    let detail = response.statusText;
    try {
      const body = (await response.json()) as ApiProblem;
      code = body.code ?? body.title;
      detail = body.detail ?? detail;
    } catch {
      /* empty */
    }
    throw new ApiError(detail, response.status, code);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}
