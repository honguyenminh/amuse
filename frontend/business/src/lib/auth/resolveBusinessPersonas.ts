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

export function formatPersonaType(persona: AvailablePersona): string {
  if (persona.type === "platform") {
    return "Platform operator";
  }
  if (persona.type === "org") {
    if (persona.orgClass === "backingOrg") {
      return "Backing organization";
    }
    if (persona.orgClass === "indieGroup") {
      return "Indie group";
    }
    return "Organization";
  }
  return persona.type;
}

export function formatOnboardingStatus(
  status: string | null | undefined,
): string | null {
  if (!status) {
    return null;
  }
  switch (status) {
    case "pendingReview":
      return "Pending approval";
    case "approved":
      return "Approved";
    case "rejected":
      return "Rejected";
    case "notRequired":
      return null;
    default:
      return status;
  }
}

export function isPlatformPersonaActive(
  activePersona: PersonaContextRequest | null,
): boolean {
  return activePersona?.type === "platform";
}

/** Routes that require an active org workspace persona. */
export function isOrgScopedPortalPath(pathname: string): boolean {
  return (
    pathname === "/dashboard"
    || pathname.startsWith("/dashboard/")
    || pathname.startsWith("/members")
  );
}

/** Routes platform operators may use without switching to an org persona. */
export function isPlatformPersonaAllowedPath(pathname: string): boolean {
  return (
    pathname.startsWith("/platform")
    || pathname.startsWith("/settings")
  );
}

export function defaultPortalPathForPersona(
  persona: AvailablePersona,
  fallback = "/dashboard",
): string {
  if (persona.type === "platform") {
    if (isPlatformPersonaAllowedPath(fallback)) {
      return fallback;
    }
    return "/platform/applications";
  }
  if (persona.type === "org") {
    return fallback.startsWith("/platform") ? "/dashboard" : fallback;
  }
  return fallback;
}
