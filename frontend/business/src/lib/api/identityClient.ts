import { getApiBaseUrl, WEB_CLIENT_HEADER } from "./config";
import type {
  ApiProblem,
  AuthTokenResponse,
  AvailablePersona,
  CurrentAccountResponse,
  PersonaContextRequest,
} from "./types";
import { ApiError } from "./types";

async function readProblem(response: Response): Promise<ApiError> {
  let code: string | undefined;
  let detail = response.statusText;
  try {
    const body = (await response.json()) as ApiProblem;
    code = body.code ?? body.title;
    detail = body.detail ?? detail;
  } catch {
    /* empty */
  }
  return new ApiError(detail, response.status, code);
}

async function identityFetch<T>(
  path: string,
  init: RequestInit & { accessToken?: string | null } = {},
): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("X-Amuse-Client", WEB_CLIENT_HEADER);
  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }
  if (init.accessToken) {
    headers.set("Authorization", `Bearer ${init.accessToken}`);
  }

  const response = await fetch(`${getApiBaseUrl()}${path}`, {
    ...init,
    headers,
    credentials: "include",
  });

  if (!response.ok) {
    throw await readProblem(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function loginPassword(
  email: string,
  password: string,
  context: PersonaContextRequest,
): Promise<AuthTokenResponse> {
  return identityFetch<AuthTokenResponse>("/api/v1/identity/login/password", {
    method: "POST",
    body: JSON.stringify({ email, password, context }),
  });
}

export function refreshTokens(
  context: PersonaContextRequest,
  refreshToken?: string | null,
): Promise<AuthTokenResponse> {
  return identityFetch<AuthTokenResponse>("/api/v1/identity/refresh", {
    method: "POST",
    body: JSON.stringify({ refreshToken: refreshToken ?? null, context }),
  });
}

export function revokeSession(accessToken: string | null): Promise<void> {
  return identityFetch<void>("/api/v1/identity/revoke", {
    method: "POST",
    accessToken,
    body: JSON.stringify({ refreshToken: null }),
  });
}

export function getCurrentAccount(
  accessToken: string,
): Promise<CurrentAccountResponse> {
  return identityFetch<CurrentAccountResponse>("/api/v1/identity/me", {
    method: "GET",
    accessToken,
  });
}

export function listPersonas(
  accessToken: string,
): Promise<AvailablePersona[]> {
  return identityFetch<AvailablePersona[]>("/api/v1/identity/personas", {
    method: "GET",
    accessToken,
  });
}

export type RegistrationPortal = "consumer" | "business";

export type RegisterPasswordResponse = {
  message: string;
  email: string;
};

export function registerPassword(
  email: string,
  password: string,
  portal: RegistrationPortal,
): Promise<RegisterPasswordResponse> {
  return identityFetch<RegisterPasswordResponse>(
    "/api/v1/identity/register/password",
    {
      method: "POST",
      body: JSON.stringify({ email, password, portal }),
    },
  );
}

export function confirmEmail(userId: string, token: string): Promise<void> {
  return identityFetch<void>("/api/v1/identity/confirm-email", {
    method: "POST",
    body: JSON.stringify({ userId, token }),
  });
}

export function resendConfirmation(
  email: string,
  portal: RegistrationPortal,
): Promise<{ message: string }> {
  return identityFetch<{ message: string }>(
    "/api/v1/identity/resend-confirmation",
    {
      method: "POST",
      body: JSON.stringify({ email, portal }),
    },
  );
}
