"use client";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { useAuth } from "@/lib/auth/AuthProvider";
import { isPlatformPersonaActive } from "@/lib/auth/resolveBusinessPersonas";
import Link from "next/link";

export default function SettingsPage() {
  const auth = useAuth();
  const isPlatform = isPlatformPersonaActive(auth.activePersona);

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-4">
      <Card>
        <CardHeader>
          <CardTitle>Settings</CardTitle>
          <CardDescription>
            Account and workspace preferences for the business portal.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-6">
          {!isPlatform ? (
            <section className="flex flex-col gap-2">
              <h2 className="text-sm font-medium">Organizations</h2>
              <p className="text-sm text-muted-foreground">
                Create another indie group or backing organization you own.
              </p>
              <Button render={<Link href="/create-organization?returnTo=/settings" />}>
                Add organization
              </Button>
            </section>
          ) : (
            <p className="text-sm text-muted-foreground">
              Switch to an organization workspace to manage tenant settings, or
              use Platform → Applications to review backing organization
              requests.
            </p>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
