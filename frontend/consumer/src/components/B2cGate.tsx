"use client";

import { Button } from "@/components/ui/Button";
import { Text } from "@/components/ui/Text";
import { useAuth } from "@/lib/auth/AuthProvider";
import { isPublicBrowsePath } from "@/lib/routing/publicBrowsePaths";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";

const ONBOARDING_ALLOWLIST = [
  "/onboarding",
  "/login",
  "/signup",
  "/confirm-email",
];

function isAllowlisted(pathname: string): boolean {
  return ONBOARDING_ALLOWLIST.some(
    (path) => pathname === path || pathname.startsWith(`${path}/`),
  );
}

export function B2cGate({ children }: { children: ReactNode }) {
  const auth = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const publicBrowse = isPublicBrowsePath(pathname);

  useEffect(() => {
    if (!auth.isReady || !auth.isAuthenticated || !auth.needsListenerOnboarding) {
      return;
    }
    if (isAllowlisted(pathname)) {
      return;
    }
    const next = encodeURIComponent(pathname);
    router.replace(`/onboarding?next=${next}`);
  }, [
    auth.isReady,
    auth.isAuthenticated,
    auth.needsListenerOnboarding,
    pathname,
    router,
  ]);

  if (!auth.isReady) {
    if (publicBrowse) {
      return children;
    }
    return (
      <div className="flex h-dvh items-center justify-center bg-background p-8">
        <Text variant="body-large">Loading…</Text>
      </div>
    );
  }

  if (auth.isAuthenticated && auth.bootstrapError) {
    return (
      <div className="flex h-dvh flex-col items-center justify-center gap-4 bg-background p-8">
        <Text variant="headline-medium">Could not prepare listener profile</Text>
        <Text variant="body-medium">{auth.bootstrapError}</Text>
        <Button type="button" onClick={() => void auth.retryBootstrap()}>
          Retry
        </Button>
      </div>
    );
  }

  if (
    auth.isAuthenticated &&
    auth.needsListenerOnboarding &&
    !isAllowlisted(pathname)
  ) {
    return (
      <div className="flex h-dvh items-center justify-center bg-background p-8">
        <Text variant="body-large">Redirecting to onboarding…</Text>
      </div>
    );
  }

  return children;
}
