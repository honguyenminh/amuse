"use client";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { useAuth } from "@/lib/auth/AuthProvider";
import { contextLabel } from "@/lib/auth/resolveBusinessPersonas";

export default function DashboardPage() {
  const auth = useAuth();
  const personaLabel =
    auth.activePersona && auth.businessPersonas.length > 0
      ? contextLabel(auth.activePersona, auth.businessPersonas)
      : "Unknown";

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-4">
      <Card>
        <CardHeader>
          <CardTitle>Welcome to Amuse Console</CardTitle>
          <CardDescription>
            Base portal shell for business and platform administration.
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-2 text-sm text-muted-foreground">
          <p>
            Active persona:{" "}
            <span className="font-medium text-foreground">{personaLabel}</span>
          </p>
          <p>
            Account:{" "}
            <span className="font-medium text-foreground">
              {auth.account?.accountId ?? "—"}
            </span>
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
