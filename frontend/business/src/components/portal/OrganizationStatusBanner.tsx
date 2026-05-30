"use client";

import { useAuth } from "@/lib/auth/AuthProvider";
import {
  formatOnboardingStatus,
  personaMatchesContext,
} from "@/lib/auth/resolveBusinessPersonas";
import { AlertCircle, Clock } from "lucide-react";
import Link from "next/link";

export function OrganizationStatusBanner() {
  const auth = useAuth();
  const active = auth.activePersona;

  if (!active || active.type !== "org") {
    return null;
  }

  const persona = auth.businessPersonas.find((item) =>
    personaMatchesContext(item, active),
  );
  const status = persona?.onboardingStatus;

  if (status === "pendingReview") {
    return (
      <div
        role="status"
        className="flex items-start gap-3 rounded-lg border border-amber-500/40 bg-amber-500/10 px-4 py-3 text-sm"
      >
        <Clock className="mt-0.5 size-4 shrink-0 text-amber-600 dark:text-amber-400" />
        <div className="flex flex-col gap-1">
          <p className="font-medium text-amber-950 dark:text-amber-50">
            Application pending platform approval
          </p>
          <p className="text-amber-900/80 dark:text-amber-100/80">
            {formatOnboardingStatus(status) ?? "Pending approval"} — upload and
            draft actions may be limited until a platform operator approves this
            backing organization.
          </p>
        </div>
      </div>
    );
  }

  if (status === "rejected") {
    return (
      <div
        role="alert"
        className="flex items-start gap-3 rounded-lg border border-destructive/40 bg-destructive/10 px-4 py-3 text-sm"
      >
        <AlertCircle className="mt-0.5 size-4 shrink-0 text-destructive" />
        <div className="flex flex-col gap-1">
          <p className="font-medium">Organization application rejected</p>
          <p className="text-muted-foreground">
            Contact support or{" "}
            <Link
              href="/create-organization?returnTo=/dashboard"
              className="font-medium text-foreground underline-offset-4 hover:underline"
            >
              create a new organization
            </Link>{" "}
            if you need to reapply.
          </p>
        </div>
      </div>
    );
  }

  return null;
}
