"use client";

import { PortalShell } from "@/components/portal/PortalShell";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useAuth } from "@/lib/auth/AuthProvider";
import { isPlatformPersonaActive, isOrgScopedPortalPath } from "@/lib/auth/resolveBusinessPersonas";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";

type PortalGateProps = {
  children: ReactNode;
};

export function PortalGate({ children }: PortalGateProps) {
  const auth = useAuth();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (!auth.isReady) {
      return;
    }
    if (!auth.isAuthenticated) {
      router.replace(`/login?next=${encodeURIComponent(pathname)}`);
      return;
    }
    if (auth.needsPersonaSelection) {
      router.replace("/select-persona");
      return;
    }

    const isPlatform = isPlatformPersonaActive(auth.activePersona);
    if (isPlatform && isOrgScopedPortalPath(pathname)) {
      router.replace("/platform/applications");
      return;
    }
    if (!isPlatform && pathname.startsWith("/platform")) {
      router.replace("/dashboard");
      return;
    }

    if (auth.activePersona?.type === "org" && auth.activePersona.orgId) {
      const orgStillAvailable = auth.businessPersonas.some(
        (persona) => persona.type === "org" && persona.orgId === auth.activePersona?.orgId,
      );
      if (!orgStillAvailable) {
        void auth.reloadBusinessPersonas().then((personas) => {
          const fallback = personas[0];
          if (fallback) {
            void auth.selectPersona(fallback).then(() => router.replace("/dashboard"));
          } else {
            router.replace("/select-persona?switch=1&returnTo=/dashboard");
          }
        });
      }
    }
  }, [
    auth.isReady,
    auth.isAuthenticated,
    auth.needsPersonaSelection,
    auth.activePersona,
    auth.businessPersonas,
    pathname,
    router,
    auth,
  ]);

  if (!auth.isReady) {
    return (
      <div className="flex min-h-dvh flex-col gap-4 p-6">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-32 w-full max-w-xl" />
        <Skeleton className="h-32 w-full max-w-xl" />
      </div>
    );
  }

  if (!auth.isAuthenticated || auth.needsPersonaSelection) {
    return null;
  }

  if (auth.bootstrapError) {
    return (
      <div className="flex min-h-dvh flex-col items-center justify-center gap-4 p-6">
        <p className="max-w-md text-center text-sm text-muted-foreground">
          {auth.bootstrapError}
        </p>
        <div className="flex gap-2">
          <Button onClick={() => void auth.retryBootstrap()}>Retry</Button>
          <Button variant="outline" onClick={() => void auth.logout()}>
            Sign out
          </Button>
        </div>
      </div>
    );
  }

  return <PortalShell>{children}</PortalShell>;
}
