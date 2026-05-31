"use client";

import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { AvailablePersona } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/AuthProvider";
import { personaKey } from "@/lib/auth/personaKeys";
import {
  recordRecentPersona,
  resolveRecentPersonas,
} from "@/lib/auth/recentPersonas";
import {
  contextLabel,
  getPersonaLabel,
  isOrgScopedPortalPath,
  isPlatformPersonaActive,
  personaMatchesContext,
} from "@/lib/auth/resolveBusinessPersonas";
import { ChevronsUpDown, LayoutGrid, Loader2, Plus } from "lucide-react";
import { usePathname, useRouter } from "next/navigation";
import { useMemo, useState } from "react";

type PersonaSwitcherProps = {
  compact?: boolean;
};

export function PersonaSwitcher({ compact = false }: PersonaSwitcherProps) {
  const auth = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const [loading, setLoading] = useState(false);

  const recentPersonas = useMemo(
    () => resolveRecentPersonas(auth.businessPersonas, 4),
    [auth.businessPersonas],
  );

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
    : "Select workspace";

  const switchHref = `/select-persona?switch=1&returnTo=${encodeURIComponent(pathname)}`;
  const createHref = `/create-organization?returnTo=${encodeURIComponent(pathname)}`;

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
      recordRecentPersona(persona);
      if (persona.type === "platform" && isOrgScopedPortalPath(pathname)) {
        router.replace("/platform/applications");
      } else if (
        persona.type === "org"
        && (pathname.startsWith("/platform") || isPlatformPersonaActive(auth.activePersona))
      ) {
        router.replace("/dashboard");
      }
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
        <DropdownMenuGroup>
          {recentPersonas.length > 0 ? (
            <>
              <DropdownMenuLabel>Recent</DropdownMenuLabel>
              {recentPersonas.map((persona) => {
                const label = getPersonaLabel(persona);
                const key = personaKey(persona);
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
              <DropdownMenuSeparator />
            </>
          ) : null}
          <DropdownMenuItem onClick={() => router.push(switchHref)}>
            <LayoutGrid className="size-4" />
            <span>All workspaces…</span>
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => router.push(createHref)}>
            <Plus className="size-4" />
            <span>Create organization</span>
          </DropdownMenuItem>
        </DropdownMenuGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
