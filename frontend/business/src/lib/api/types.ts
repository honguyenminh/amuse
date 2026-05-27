export type PersonaContextType = "org" | "listener" | "platform";

export type PersonaContextRequest = {
  type: PersonaContextType;
  orgId: string | null;
  listenerId: string | null;
};

export type AuthTokenResponse = {
  accessToken: string;
  accessExpiresAt: string;
  refreshToken: string | null;
  refreshExpiresAt: string;
};

export type AvailablePersona = {
  type: string;
  orgId: string | null;
  listenerId: string | null;
  label: string | null;
};

export type CurrentAccountResponse = {
  accountId: string;
  idpIssuer: string;
  idpSubject: string;
  status: string;
};

export type ApiProblem = {
  title?: string;
  detail?: string;
  code?: string;
  status?: number;
};

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly code?: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}
