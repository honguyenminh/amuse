import "server-only";

import { getServerApiBaseUrl, WEB_CLIENT_HEADER } from "./config";
import { ApiError, type ApiProblem } from "./types";

export type ServerFetchCacheOptions = {
  revalidate?: number;
  tags?: string[];
};

export async function serverPublicFetch<T>(
  path: string,
  init: RequestInit = {},
  cacheOptions: ServerFetchCacheOptions = {},
): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("X-Amuse-Client", WEB_CLIENT_HEADER);
  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${getServerApiBaseUrl()}${path}`, {
    ...init,
    headers,
    next: {
      revalidate: cacheOptions.revalidate,
      tags: cacheOptions.tags,
    },
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
