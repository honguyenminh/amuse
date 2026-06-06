"use client";

import { AvatarField } from "@/components/account/AvatarField";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/auth/AuthProvider";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useEffect, useState } from "react";

function sanitizeReturnTo(value: string | null): string {
  if (!value || !value.startsWith("/") || value.startsWith("//")) {
    return "/dashboard";
  }
  return value;
}

function BusinessOnboardingInner() {
  const auth = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const returnTo = sanitizeReturnTo(searchParams.get("returnTo"));

  const [displayName, setDisplayName] = useState("");
  const [accentSeed, setAccentSeed] = useState(0);
  const [avatarUrl, setAvatarUrl] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => {
    setAvatarUrl(auth.portalProfile?.avatarUrl ?? null);
    if (auth.portalProfile?.avatarAccentSeed != null) {
      setAccentSeed(auth.portalProfile.avatarAccentSeed);
    }
  }, [auth.portalProfile]);

  if (!auth.isReady) {
    return (
      <div className="flex min-h-dvh items-center justify-center p-8 text-sm text-muted-foreground">
        Loading…
      </div>
    );
  }

  if (!auth.isAuthenticated) {
    router.replace(`/login?next=${encodeURIComponent("/onboarding")}`);
    return null;
  }

  if (auth.needsPersonaSelection) {
    router.replace("/select-persona");
    return null;
  }

  if (!auth.needsPortalProfileOnboarding) {
    router.replace(returnTo);
    return null;
  }

  const submit = async () => {
    setSubmitting(true);
    setError(null);
    try {
      await auth.completePortalProfile({
        displayName: displayName.trim(),
        avatarAccentSeed: accentSeed,
      });
      router.replace(returnTo);
    } catch (submitError) {
      setError(
        submitError instanceof Error
          ? submitError.message
          : "Could not save your profile.",
      );
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="mx-auto flex min-h-dvh w-full max-w-lg flex-col justify-center gap-6 p-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Set up your profile</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Choose how you appear in the business portal and member lists.
        </p>
      </div>

      <section className="flex flex-col gap-4 rounded-xl border bg-card p-4">
        <label className="flex flex-col gap-2 text-sm">
          <span className="font-medium">Display name</span>
          <input
            value={displayName}
            onChange={(event) => setDisplayName(event.target.value)}
            className="rounded-md border bg-background px-3 py-2"
            maxLength={80}
            autoFocus
          />
        </label>

        <AvatarField
          displayName={displayName}
          email={auth.account?.email}
          accentSeed={accentSeed}
          avatarUrl={avatarUrl}
          onAccentSeedChange={setAccentSeed}
          onAvatarUrlChange={(url) => {
            setAvatarUrl(url);
            void auth.refreshPortalProfile();
          }}
          onClearUploadedAvatar={async () => {
            await auth.completePortalProfile({ clearAvatar: true });
          }}
        />

        {error ? <p className="text-sm text-destructive">{error}</p> : null}

        <Button
          type="button"
          disabled={submitting || displayName.trim().length === 0}
          onClick={() => void submit()}
        >
          {submitting ? "Saving…" : "Continue to portal"}
        </Button>
      </section>
    </div>
  );
}

export default function BusinessOnboardingPage() {
  return (
    <Suspense
      fallback={
        <div className="flex min-h-dvh items-center justify-center p-8 text-sm text-muted-foreground">
          Loading…
        </div>
      }
    >
      <BusinessOnboardingInner />
    </Suspense>
  );
}
