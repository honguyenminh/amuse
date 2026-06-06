import { authFetch } from "@/lib/auth/authFetch";
import { getApiBaseUrl, WEB_CLIENT_HEADER } from "./config";
import type {
  CompleteListenerAvatarUploadResponse,
  EnsureListenerProfileResponse,
  ListenerProfileResponse,
  PresignListenerAvatarUploadResponse,
  UpdateListenerProfileRequest,
} from "./types";
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

export function getListenerProfile(): Promise<ListenerProfileResponse> {
  return authFetch<ListenerProfileResponse>("/api/v1/listener/profile");
}

export function updateListenerProfile(
  body: UpdateListenerProfileRequest,
): Promise<ListenerProfileResponse> {
  return authFetch<ListenerProfileResponse>("/api/v1/listener/profile", {
    method: "PATCH",
    body: JSON.stringify(body),
  });
}

export function presignListenerAvatarUpload(body: {
  fileName: string;
  contentType: string;
}): Promise<PresignListenerAvatarUploadResponse> {
  return authFetch<PresignListenerAvatarUploadResponse>(
    "/api/v1/listener/profile/avatar/presign-upload",
    {
      method: "POST",
      body: JSON.stringify(body),
    },
  );
}

export function completeListenerAvatarUpload(body: {
  key: string;
}): Promise<CompleteListenerAvatarUploadResponse> {
  return authFetch<CompleteListenerAvatarUploadResponse>(
    "/api/v1/listener/profile/avatar/complete",
    {
      method: "POST",
      body: JSON.stringify(body),
    },
  );
}
