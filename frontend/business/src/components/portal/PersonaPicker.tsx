"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import type { AvailablePersona, PersonaContextRequest } from "@/lib/api/types";
import { personaKey } from "@/lib/auth/personaKeys";
import { resolveRecentPersonas } from "@/lib/auth/recentPersonas";
import {
  formatOnboardingStatus,
  formatPersonaType,
  getPersonaLabel,
  personaMatchesContext,
} from "@/lib/auth/resolveBusinessPersonas";
import { Building2, Check, Search, Shield } from "lucide-react";
import { useMemo, useState } from "react";

type PersonaPickerProps = {
  personas: AvailablePersona[];
  activePersona: PersonaContextRequest | null;
  onSelect: (persona: AvailablePersona) => Promise<void>;
  loadingKey: string | null;
  error: string | null;
  showRecent?: boolean;
};

function PersonaRow({
  persona,
  isActive,
  loading,
  onSelect,
}: {
  persona: AvailablePersona;
  isActive: boolean;
  loading: boolean;
  onSelect: () => void;
}) {
  const Icon = persona.type === "platform" ? Shield : Building2;
  const status = formatOnboardingStatus(persona.onboardingStatus);

  return (
    <Button
      variant="outline"
      className="h-auto w-full justify-start gap-3 px-4 py-3"
      disabled={loading}
      onClick={onSelect}
    >
      <Icon className="size-4 shrink-0" />
      <span className="flex min-w-0 flex-1 flex-col items-start gap-0.5 text-left">
        <span className="flex w-full items-center gap-2">
          <span className="truncate font-medium">{getPersonaLabel(persona)}</span>
          {isActive ? (
            <Check className="size-4 shrink-0 text-primary" aria-hidden />
          ) : null}
        </span>
        <span className="text-xs text-muted-foreground">
          {formatPersonaType(persona)}
          {status ? ` · ${status}` : ""}
        </span>
      </span>
    </Button>
  );
}

export function PersonaPicker({
  personas,
  activePersona,
  onSelect,
  loadingKey,
  error,
  showRecent = true,
}: PersonaPickerProps) {
  const [query, setQuery] = useState("");

  const platformPersona = personas.find((persona) => persona.type === "platform");
  const orgPersonas = personas.filter((persona) => persona.type === "org");

  const normalizedQuery = query.trim().toLowerCase();

  const filteredOrgs = useMemo(() => {
    if (!normalizedQuery) {
      return orgPersonas;
    }
    return orgPersonas.filter((persona) => {
      const label = getPersonaLabel(persona).toLowerCase();
      const status =
        formatOnboardingStatus(persona.onboardingStatus)?.toLowerCase() ?? "";
      return label.includes(normalizedQuery) || status.includes(normalizedQuery);
    });
  }, [normalizedQuery, orgPersonas]);

  const recentPersonas = useMemo(() => {
    if (!showRecent || normalizedQuery) {
      return [];
    }
    return resolveRecentPersonas(personas, 5);
  }, [normalizedQuery, personas, showRecent]);

  const recentKeys = new Set(recentPersonas.map(personaKey));

  const orgListExcludingRecent = filteredOrgs.filter(
    (persona) => !recentKeys.has(personaKey(persona)),
  );

  function renderPersona(persona: AvailablePersona) {
    const key = personaKey(persona);
    const isActive =
      activePersona !== null && personaMatchesContext(persona, activePersona);

    return (
      <PersonaRow
        key={key}
        persona={persona}
        isActive={isActive}
        loading={loadingKey === key}
        onSelect={() => void onSelect(persona)}
      />
    );
  }

  const showPlatformInMain =
    platformPersona &&
    (!normalizedQuery ||
      getPersonaLabel(platformPersona).toLowerCase().includes(normalizedQuery) ||
      "platform".includes(normalizedQuery));

  return (
    <div className="flex flex-col gap-4">
      {personas.length > 6 ? (
        <div className="relative">
          <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search organizations…"
            className="pl-9"
            aria-label="Search workspaces"
          />
        </div>
      ) : null}

      {recentPersonas.length > 0 ? (
        <section className="flex flex-col gap-2">
          <h3 className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
            Recent
          </h3>
          <div className="flex flex-col gap-2">
            {recentPersonas.map(renderPersona)}
          </div>
        </section>
      ) : null}

      {showPlatformInMain && platformPersona ? (
        <section className="flex flex-col gap-2">
          <h3 className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
            Platform
          </h3>
          {renderPersona(platformPersona)}
        </section>
      ) : null}

      {orgListExcludingRecent.length > 0 ? (
        <section className="flex flex-col gap-2">
          <h3 className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
            Organizations
            {normalizedQuery ? ` (${orgListExcludingRecent.length})` : ""}
          </h3>
          <div className="flex max-h-80 flex-col gap-2 overflow-y-auto pr-1">
            {orgListExcludingRecent.map(renderPersona)}
          </div>
        </section>
      ) : null}

      {filteredOrgs.length === 0 &&
      !showPlatformInMain &&
      recentPersonas.length === 0 ? (
        <p className="text-sm text-muted-foreground">
          No workspaces match your search.
        </p>
      ) : null}

      {error ? <p className="text-sm text-destructive">{error}</p> : null}
    </div>
  );
}

export function PersonaPickerSkeleton() {
  return (
    <div className="flex flex-col gap-3">
      <Skeleton className="h-10 w-full" />
      <Skeleton className="h-14 w-full" />
      <Skeleton className="h-14 w-full" />
      <Skeleton className="h-14 w-full" />
    </div>
  );
}
