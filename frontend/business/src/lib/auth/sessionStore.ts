import type { PersonaContextRequest } from "@/lib/api/types";

const ACTIVE_PERSONA_KEY = "amuse_active_persona";

let accessToken: string | null = null;
let activePersona: PersonaContextRequest | null = null;

export function getAccessToken(): string | null {
  return accessToken;
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

export function getActivePersona(): PersonaContextRequest | null {
  return activePersona;
}

export function getActivePersonaContext(): PersonaContextRequest {
  if (!activePersona) {
    throw new Error("No active business persona.");
  }
  return activePersona;
}

export function setActivePersona(persona: PersonaContextRequest | null): void {
  activePersona = persona;
  if (typeof window === "undefined") {
    return;
  }
  if (persona) {
    sessionStorage.setItem(ACTIVE_PERSONA_KEY, JSON.stringify(persona));
  } else {
    sessionStorage.removeItem(ACTIVE_PERSONA_KEY);
  }
}

export function readStoredPersona(): PersonaContextRequest | null {
  if (typeof window === "undefined") {
    return null;
  }
  const raw = sessionStorage.getItem(ACTIVE_PERSONA_KEY);
  if (!raw) {
    return null;
  }
  try {
    return JSON.parse(raw) as PersonaContextRequest;
  } catch {
    sessionStorage.removeItem(ACTIVE_PERSONA_KEY);
    return null;
  }
}

export function clearSession(): void {
  accessToken = null;
  activePersona = null;
  if (typeof window !== "undefined") {
    sessionStorage.removeItem(ACTIVE_PERSONA_KEY);
  }
}
