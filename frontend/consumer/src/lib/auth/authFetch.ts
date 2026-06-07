import { refreshTokens } from "@/lib/api/identityClient";
import type { PersonaContextRequest } from "@/lib/api/types";
import { ApiError } from "@/lib/api/types";
import { listenerBootstrapContext } from "./listenerBootstrapContext";
import { getAccessToken, getListenerId, setAccessToken } from "./sessionStore";
import { withRefreshLock } from "./refreshLock";

function refreshContext(): PersonaContextRequest {
  const listenerId = getListenerId();
  if (listenerId) {
    return { type: "listener", orgId: null, listenerId };
  }
  return listenerBootstrapContext;
}

/** Refresh the in-memory access token from the HTTP-only refresh cookie. */
export async function refreshAccessToken(): Promise<string | null> {
  try {
    return await withRefreshLock(async () => {
      const refreshed = await refreshTokens(refreshContext());
      setAccessToken(refreshed.accessToken);
      return refreshed.accessToken;
    });
  } catch {
    return null;
  }
}

export async function authFetch<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  let token = getAccessToken();
  if (!token) {
    token = await refreshAccessToken();
    if (!token) {
      throw new ApiError("Not authenticated.", 401, "auth.not_authenticated");
    }
  }

  try {
    return await fetchJson<T>(path, token, init);
  } catch (error) {
    if (!(error instanceof ApiError) || error.status !== 401) {
      throw error;
    }

    const refreshedToken = await refreshAccessToken();
    if (!refreshedToken) {
      throw new ApiError("Not authenticated.", 401, "auth.not_authenticated");
    }

    return fetchJson<T>(path, refreshedToken, init);
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
    throw new ApiError(detail, response.status, code);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
