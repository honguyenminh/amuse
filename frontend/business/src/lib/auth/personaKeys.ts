import type { AvailablePersona } from "@/lib/api/types";

export function personaKey(persona: AvailablePersona): string {
  if (persona.type === "org" && persona.orgId) {
    return `org:${persona.orgId}`;
  }
  return persona.type;
}

export function findPersonaByKey(
  personas: AvailablePersona[],
  key: string,
): AvailablePersona | undefined {
  return personas.find((persona) => personaKey(persona) === key);
}
