"use client";

import { PersonaPicker } from "@/components/portal/PersonaPicker";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { useAuth } from "@/lib/auth/AuthProvider";
import { personaKey } from "@/lib/auth/personaKeys";
import { defaultPortalPathForPersona } from "@/lib/auth/resolveBusinessPersonas";
import { recordRecentPersona } from "@/lib/auth/recentPersonas";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";

function SelectPersonaContent() {
  const auth = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const switchMode = searchParams.get("switch") === "1";
  const returnTo = searchParams.get("returnTo") ?? "/dashboard";

  const [loadingKey, setLoadingKey] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [signingOut, setSigningOut] = useState(false);
  const hasPersonas = auth.businessPersonas.length > 0;

  useEffect(() => {
    if (!auth.isReady) {
      return;
    }
    if (!auth.isAuthenticated) {
      router.replace("/login");
      return;
    }
    if (!switchMode && auth.activePersona && !auth.needsPersonaSelection) {
      router.replace("/dashboard");
    }
  }, [
    auth.isReady,
    auth.isAuthenticated,
    auth.activePersona,
    auth.needsPersonaSelection,
    router,
    switchMode,
  ]);

  async function handleSelect(persona: Parameters<typeof auth.selectPersona>[0]) {
    const key = personaKey(persona);
    setLoadingKey(key);
    setError(null);
    try {
      await auth.selectPersona(persona);
      recordRecentPersona(persona);
      const safeReturn =
        returnTo.startsWith("/") && !returnTo.startsWith("//")
          ? returnTo
          : "/dashboard";
      router.replace(defaultPortalPathForPersona(persona, safeReturn));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not switch workspace.");
    } finally {
      setLoadingKey(null);
    }
  }

  async function handleSignOut() {
    setSigningOut(true);
    try {
      await auth.logout();
      router.replace("/login");
    } finally {
      setSigningOut(false);
    }
  }

  if (!auth.isReady) {
    return (
      <div className="flex min-h-dvh items-center justify-center p-6">
        <Skeleton className="h-64 w-full max-w-lg" />
      </div>
    );
  }

  return (
    <div className="flex min-h-dvh items-center justify-center bg-muted/40 p-6">
      <Card className="w-full max-w-lg">
        <CardHeader>
          <CardTitle>
            {switchMode
              ? "Switch workspace"
              : hasPersonas
                ? "Choose a workspace"
                : "No workspaces yet"}
          </CardTitle>
          <CardDescription>
            {switchMode
              ? "Pick an organization or platform context. Use search when you belong to many organizations."
              : hasPersonas
                ? "Select which organization or platform context to use for this session."
                : "Create an organization or accept an invite to get started."}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <PersonaPicker
            personas={auth.businessPersonas}
            activePersona={auth.activePersona}
            onSelect={handleSelect}
            loadingKey={loadingKey}
            error={error}
            showRecent={switchMode}
          />
          <div className="mt-6 flex flex-col items-center gap-3">
            <p className="text-center text-sm text-muted-foreground">
              Logged in as{" "}
              <span className="font-medium text-foreground">
                {auth.account?.email ?? "your account"}
              </span>
            </p>
            <p className="text-center text-sm text-muted-foreground">
              <Link
                href={`/create-organization?returnTo=${encodeURIComponent(
                  returnTo.startsWith("/") && !returnTo.startsWith("//")
                    ? returnTo
                    : "/select-persona",
                )}`}
                className="underline-offset-4 hover:underline"
              >
                Create new organization
              </Link>
            </p>
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={signingOut}
              onClick={() => void handleSignOut()}
            >
              {signingOut ? "Signing out…" : "Sign out"}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

export default function SelectPersonaPage() {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-dvh items-center justify-center p-6">
          <Skeleton className="h-64 w-full max-w-lg" />
        </div>
      }
    >
      <SelectPersonaContent />
    </Suspense>
  );
}
