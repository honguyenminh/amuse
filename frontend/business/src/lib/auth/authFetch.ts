import { refreshTokens } from "@/lib/api/identityClient";
import { ApiError } from "@/lib/api/types";
import {
  isOrganizationUnavailableError,
  notifyOrgUnavailable,
} from "@/lib/auth/orgSessionEvents";
import {
  getAccessToken,
  getActivePersonaContext,
  setAccessToken,
} from "./sessionStore";
import { withRefreshLock } from "./refreshLock";

export async function authFetch<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const token = getAccessToken();
  if (!token) {
    throw new ApiError("Not authenticated.", 401, "auth.not_authenticated");
  }

  try {
    return await fetchJson<T>(path, token, init);
  } catch (error) {
    if (!(error instanceof ApiError) || error.status !== 401) {
      throw error;
    }

    const newToken = await withRefreshLock(async () => {
      const refreshed = await refreshTokens(getActivePersonaContext());
      setAccessToken(refreshed.accessToken);
      return refreshed.accessToken;
    });

    return fetchJson<T>(path, newToken, init);
  }
}

async function fetchJson<T>(
  path: string,
  accessToken: string,
  init: RequestInit,
): Promise<T> {
  const { getApiBaseUrl, WEB_CLIENT_HEADER } = await import("@/lib/api/config");
  const headers = new Headers(init.headers);
  headers.set("X-Amuse-Client", WEB_CLIENT_HEADER);
  headers.set("Authorization", `Bearer ${accessToken}`);
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
      const body = await response.json();
      code = body.code ?? body.title;
      detail = body.detail ?? detail;
    } catch {
      /* empty */
    }
    const apiError = new ApiError(detail, response.status, code);
    const persona = getActivePersonaContext();
    if (
      persona?.type === "org" &&
      response.status === 404 &&
      isOrganizationUnavailableError(code)
    ) {
      notifyOrgUnavailable(
        detail || "This organization is no longer available in your workspace.",
      );
    }
    throw apiError;
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
