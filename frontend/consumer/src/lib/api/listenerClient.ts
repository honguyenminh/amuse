import { getApiBaseUrl, WEB_CLIENT_HEADER } from "./config";
import type { EnsureListenerProfileResponse } from "./types";
import { ApiError } from "./types";

export async function ensureListenerProfile(
  accessToken: string,
): Promise<EnsureListenerProfileResponse> {
  const response = await fetch(
    `${getApiBaseUrl()}/api/v1/listener/profile/ensure`,
    {
      method: "POST",
      headers: {
        "X-Amuse-Client": WEB_CLIENT_HEADER,
        Authorization: `Bearer ${accessToken}`,
      },
      credentials: "include",
    },
  );

  if (!response.ok) {
    let detail = response.statusText;
    try {
      const body = await response.json();
      detail = body.detail ?? detail;
    } catch {
      /* empty */
    }
    throw new ApiError(detail, response.status);
  }

  return (await response.json()) as EnsureListenerProfileResponse;
}
