let accessToken: string | null = null;
let listenerId: string | null = null;

export function getAccessToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

export function getListenerId(): string | null {
  return listenerId;
}

export function setListenerId(id: string | null): void {
  listenerId = id;
}

export function clearSession(): void {
  accessToken = null;
  listenerId = null;
}
