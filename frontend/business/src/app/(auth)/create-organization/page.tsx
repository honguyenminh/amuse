"use client";

import { CreateOrganizationForm } from "@/components/portal/CreateOrganizationForm";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
  organizationToPersona,
  type OrganizationResponse,
} from "@/lib/api/tenancyClient";
import { useAuth } from "@/lib/auth/AuthProvider";
import { recordRecentPersona } from "@/lib/auth/recentPersonas";
import { defaultPortalPathForPersona } from "@/lib/auth/resolveBusinessPersonas";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect } from "react";

function safeReturnPath(value: string | null): string {
  if (!value || !value.startsWith("/") || value.startsWith("//")) {
    return "/dashboard";
  }
  return value;
}

function CreateOrganizationContent() {
  const auth = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const returnTo = safeReturnPath(searchParams.get("returnTo"));

  useEffect(() => {
    if (!auth.isReady) {
      return;
    }
    if (!auth.isAuthenticated) {
      router.replace(`/login?next=${encodeURIComponent("/create-organization")}`);
    }
  }, [auth.isReady, auth.isAuthenticated, router]);

  async function handleCreated(organization: OrganizationResponse) {
    const personas = await auth.reloadBusinessPersonas();

    let persona = personas.find(
      (item) => item.type === "org" && item.orgId === organization.id,
    );
    if (!persona) {
      persona = organizationToPersona(organization);
    }

    await auth.selectPersona(persona);
    recordRecentPersona(persona);
    router.replace(defaultPortalPathForPersona(persona, returnTo));
  }

  if (!auth.isReady) {
    return (
      <div className="flex min-h-dvh items-center justify-center p-6">
        <Skeleton className="h-72 w-full max-w-lg" />
      </div>
    );
  }

  if (!auth.isAuthenticated) {
    return null;
  }

  return (
    <div className="flex min-h-dvh items-center justify-center bg-muted/40 p-6">
      <Card className="w-full max-w-lg">
        <CardHeader>
          <CardTitle>Create organization</CardTitle>
          <CardDescription>
            Add an indie group or backing organization to your signed-in
            account. You become the owner; member invites are not in this UI
            yet. Platform operators normally review backing orgs via Applications
            rather than creating tenants here.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <CreateOrganizationForm
            onCreated={handleCreated}
            cancelHref={returnTo}
          />
          <p className="mt-6 text-center text-sm text-muted-foreground">
            <Link
              href={`/select-persona?switch=1&returnTo=${encodeURIComponent(returnTo)}`}
              className="underline-offset-4 hover:underline"
            >
              Back to workspace list
            </Link>
          </p>
        </CardContent>
      </Card>
    </div>
  );
}

export default function CreateOrganizationPage() {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-dvh items-center justify-center p-6">
          <Skeleton className="h-72 w-full max-w-lg" />
        </div>
      }
    >
      <CreateOrganizationContent />
    </Suspense>
  );
}
