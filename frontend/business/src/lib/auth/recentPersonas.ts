import { findPersonaByKey, personaKey } from "@/lib/auth/personaKeys";
import type { AvailablePersona } from "@/lib/api/types";

const STORAGE_KEY = "amuse_recent_personas";
const MAX_RECENT = 8;

function readKeys(): string[] {
  if (typeof window === "undefined") {
    return [];
  }
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return [];
    }
    const parsed = JSON.parse(raw) as unknown;
    if (!Array.isArray(parsed)) {
      return [];
    }
    return parsed.filter((item): item is string => typeof item === "string");
  } catch {
    return [];
  }
}

function writeKeys(keys: string[]): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(keys.slice(0, MAX_RECENT)));
}

export function recordRecentPersona(persona: AvailablePersona): void {
  const key = personaKey(persona);
  const next = [key, ...readKeys().filter((item) => item !== key)];
  writeKeys(next);
}

export function resolveRecentPersonas(
  personas: AvailablePersona[],
  limit = 5,
): AvailablePersona[] {
  const resolved: AvailablePersona[] = [];
  for (const key of readKeys()) {
    if (resolved.length >= limit) {
      break;
    }
    const persona = findPersonaByKey(personas, key);
    if (persona) {
      resolved.push(persona);
    }
  }
  return resolved;
}
