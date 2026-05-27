import type {
  AvailablePersona,
  PersonaContextRequest,
} from "@/lib/api/types";

export function filterBusinessPersonas(
  personas: AvailablePersona[],
): AvailablePersona[] {
  return personas.filter(
    (persona) => persona.type === "org" || persona.type === "platform",
  );
}

export function personaToContext(
  persona: AvailablePersona,
): PersonaContextRequest {
  if (persona.type === "org") {
    return { type: "org", orgId: persona.orgId, listenerId: null };
  }
  if (persona.type === "platform") {
    return { type: "platform", orgId: null, listenerId: null };
  }
  throw new Error(`Unsupported business persona type: ${persona.type}`);
}

export function personaMatchesContext(
  persona: AvailablePersona,
  context: PersonaContextRequest,
): boolean {
  if (persona.type !== context.type) {
    return false;
  }
  if (context.type === "org") {
    return persona.orgId === context.orgId;
  }
  return true;
}

export function getPersonaLabel(persona: AvailablePersona): string {
  if (persona.label) {
    return persona.label;
  }
  if (persona.type === "platform") {
    return "Platform";
  }
  if (persona.type === "org") {
    return "Organization";
  }
  return persona.type;
}

export function contextLabel(
  context: PersonaContextRequest,
  personas: AvailablePersona[],
): string {
  const match = personas.find((persona) =>
    personaMatchesContext(persona, context),
  );
  return match ? getPersonaLabel(match) : context.type;
}
