"use client";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { AvailablePersona } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/AuthProvider";
import {
  contextLabel,
  getPersonaLabel,
  personaMatchesContext,
} from "@/lib/auth/resolveBusinessPersonas";
import { ChevronsUpDown, Loader2 } from "lucide-react";
import { useState } from "react";

type PersonaSwitcherProps = {
  compact?: boolean;
};

export function PersonaSwitcher({ compact = false }: PersonaSwitcherProps) {
  const auth = useAuth();
  const [loading, setLoading] = useState(false);

  if (auth.businessPersonas.length <= 1) {
    const persona = auth.businessPersonas[0];
    if (!persona) {
      return null;
    }
    return (
      <span className="text-sm text-muted-foreground">
        {getPersonaLabel(persona)}
      </span>
    );
  }

  const activeLabel = auth.activePersona
    ? contextLabel(auth.activePersona, auth.businessPersonas)
    : "Select persona";

  async function onSelect(persona: AvailablePersona) {
    if (
      auth.activePersona &&
      personaMatchesContext(persona, auth.activePersona)
    ) {
      return;
    }
    setLoading(true);
    try {
      await auth.selectPersona(persona);
    } finally {
      setLoading(false);
    }
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={
          <Button
            variant="outline"
            size={compact ? "sm" : "default"}
            className="gap-2"
            disabled={loading}
          />
        }
      >
        {loading ? (
          <Loader2 className="size-4 animate-spin" />
        ) : (
          <ChevronsUpDown className="size-4" />
        )}
        <span className="max-w-40 truncate">{activeLabel}</span>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel>Switch persona</DropdownMenuLabel>
        <DropdownMenuSeparator />
        {auth.businessPersonas.map((persona) => {
          const label = getPersonaLabel(persona);
          const key =
            persona.type === "org"
              ? `org:${persona.orgId}`
              : persona.type;
          const isActive =
            auth.activePersona !== null &&
            personaMatchesContext(persona, auth.activePersona);

          return (
            <DropdownMenuItem
              key={key}
              onClick={() => void onSelect(persona)}
              data-active={isActive}
            >
              <span className="truncate">{label}</span>
            </DropdownMenuItem>
          );
        })}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
