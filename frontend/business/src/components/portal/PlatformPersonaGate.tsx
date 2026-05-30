"use client";

import { useAuth } from "@/lib/auth/AuthProvider";
import { isPlatformPersonaActive } from "@/lib/auth/resolveBusinessPersonas";
import { useRouter } from "next/navigation";
import { useEffect, type ReactNode } from "react";

type PlatformPersonaGateProps = {
  children: ReactNode;
};

export function PlatformPersonaGate({ children }: PlatformPersonaGateProps) {
  const auth = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!auth.isReady || !auth.isAuthenticated) {
      return;
    }
    if (!isPlatformPersonaActive(auth.activePersona)) {
      router.replace("/dashboard");
    }
  }, [auth.isReady, auth.isAuthenticated, auth.activePersona, router]);

  if (!auth.isReady || !isPlatformPersonaActive(auth.activePersona)) {
    return null;
  }

  return children;
}
