"use client";

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
import { getPersonaLabel } from "@/lib/auth/resolveBusinessPersonas";
import { Building2, Shield } from "lucide-react";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

export default function SelectPersonaPage() {
  const auth = useAuth();
  const router = useRouter();
  const [loadingKey, setLoadingKey] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!auth.isReady) {
      return;
    }
    if (!auth.isAuthenticated) {
      router.replace("/login");
      return;
    }
    if (auth.activePersona && !auth.needsPersonaSelection) {
      router.replace("/dashboard");
    }
  }, [
    auth.isReady,
    auth.isAuthenticated,
    auth.activePersona,
    auth.needsPersonaSelection,
    router,
  ]);

  async function onSelect(personaKey: string) {
    const persona = auth.businessPersonas.find((item) => {
      const key =
        item.type === "org" ? `org:${item.orgId}` : item.type;
      return key === personaKey;
    });
    if (!persona) {
      return;
    }
    setLoadingKey(personaKey);
    setError(null);
    try {
      await auth.selectPersona(persona);
      router.replace("/dashboard");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not switch persona.");
    } finally {
      setLoadingKey(null);
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
          <CardTitle>Choose a workspace</CardTitle>
          <CardDescription>
            Select which organization or platform context to use for this
            session.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-3">
          {auth.businessPersonas.map((persona) => {
            const key =
              persona.type === "org" ? `org:${persona.orgId}` : persona.type;
            const Icon = persona.type === "platform" ? Shield : Building2;
            const loading = loadingKey === key;

            return (
              <Button
                key={key}
                variant="outline"
                className="h-auto justify-start gap-3 px-4 py-3"
                disabled={loading}
                onClick={() => void onSelect(key)}
              >
                <Icon className="size-4 shrink-0" />
                <span className="flex flex-col items-start gap-0.5 text-left">
                  <span className="font-medium">{getPersonaLabel(persona)}</span>
                  <span className="text-xs text-muted-foreground capitalize">
                    {persona.type}
                  </span>
                </span>
              </Button>
            );
          })}
          {error ? (
            <p className="text-sm text-destructive">{error}</p>
          ) : null}
        </CardContent>
      </Card>
    </div>
  );
}
