"use client";

import { AvatarField } from "@/components/account/AvatarField";
import { Button } from "@/components/ui/Button";
import { Text } from "@/components/ui/Text";
import { useAuth } from "@/lib/auth/AuthProvider";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useMemo, useState } from "react";

function sanitizeNextPath(next: string | null): string {
  if (!next || !next.startsWith("/") || next.startsWith("//")) {
    return "/home";
  }
  return next;
}

export default function OnboardingPage() {
  const auth = useAuth();
  const router = useRouter();
  const searchParams = useSearchParams();
  const next = sanitizeNextPath(searchParams.get("next"));

  const [step, setStep] = useState<1 | 2>(1);
  const [displayName, setDisplayName] = useState("");
  const [accentSeed, setAccentSeed] = useState(0);
  const [avatarUrl, setAvatarUrl] = useState<string | null>(null);
  const [allowUnverifiedArtists, setAllowUnverifiedArtists] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const previewEmail = auth.account?.email ?? null;

  useEffect(() => {
    setAvatarUrl(auth.listenerProfile?.avatarUrl ?? null);
    if (auth.listenerProfile?.avatarAccentSeed != null) {
      setAccentSeed(auth.listenerProfile.avatarAccentSeed);
    }
  }, [auth.listenerProfile]);

  useEffect(() => {
    if (!auth.isReady) {
      return;
    }
    if (!auth.isAuthenticated) {
      router.replace(`/login?next=${encodeURIComponent("/onboarding")}`);
      return;
    }
    if (!auth.needsListenerOnboarding) {
      router.replace(next);
    }
  }, [
    auth.isReady,
    auth.isAuthenticated,
    auth.needsListenerOnboarding,
    next,
    router,
  ]);

  const canContinueStep1 = useMemo(
    () => displayName.trim().length > 0,
    [displayName],
  );

  if (!auth.isReady) {
    return (
      <div className="flex min-h-dvh items-center justify-center p-8">
        <Text variant="body-large">Loading…</Text>
      </div>
    );
  }

  if (!auth.isAuthenticated || !auth.needsListenerOnboarding) {
    return (
      <div className="flex min-h-dvh items-center justify-center p-8">
        <Text variant="body-large">Redirecting…</Text>
      </div>
    );
  }

  const submit = async () => {
    setSubmitting(true);
    setError(null);
    try {
      await auth.completeListenerOnboarding({
        displayName: displayName.trim(),
        avatarAccentSeed: accentSeed,
        allowUnverifiedArtists,
      });
      router.replace(next);
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
        <Text variant="headline-medium">Welcome to Amuse</Text>
        <Text variant="body-medium" className="mt-2 text-on-surface-variant">
          Set up your listener profile before you start exploring.
        </Text>
      </div>

      {step === 1 ? (
        <section className="flex flex-col gap-4 rounded-2xl border-2 border-outline bg-surface p-4">
          <label className="flex flex-col gap-2">
            <Text variant="label-medium">Display name</Text>
            <input
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              className="rounded-xl border-2 border-outline bg-background px-3 py-2"
              maxLength={80}
              autoFocus
            />
          </label>

          <AvatarField
            displayName={displayName}
            email={previewEmail}
            accentSeed={accentSeed}
            avatarUrl={avatarUrl}
            onAccentSeedChange={setAccentSeed}
            onAvatarUrlChange={(url) => {
              setAvatarUrl(url);
              void auth.refreshListenerProfile();
            }}
            onClearUploadedAvatar={async () => {
              await auth.completeListenerOnboarding({ clearAvatar: true });
            }}
          />

          <Button
            type="button"
            disabled={!canContinueStep1}
            onClick={() => setStep(2)}
          >
            Continue
          </Button>
        </section>
      ) : (
        <section className="flex flex-col gap-4 rounded-2xl border-2 border-outline bg-surface p-4">
          <Text variant="title-medium">Discovery preference</Text>
          <Text variant="body-medium" className="text-on-surface-variant">
            Choose whether unverified artists can appear in your search results
            without the usual ranking penalty applied to discover-mode content.
          </Text>
          <label className="flex items-start gap-3 rounded-xl border-2 border-outline p-3">
            <input
              type="checkbox"
              checked={allowUnverifiedArtists}
              onChange={(event) => setAllowUnverifiedArtists(event.target.checked)}
              className="mt-1"
            />
            <span>
              <Text variant="label-medium">Allow unverified artists</Text>
              <Text variant="body-medium" className="text-on-surface-variant">
                When enabled, unverified results are shown without the default
                discover penalty.
              </Text>
            </span>
          </label>
          {error ? (
            <Text variant="body-medium" className="text-error">
              {error}
            </Text>
          ) : null}
          <div className="flex gap-2">
            <Button type="button" variant="outlined" onClick={() => setStep(1)}>
              Back
            </Button>
            <Button type="button" disabled={submitting} onClick={() => void submit()}>
              {submitting ? "Saving…" : "Finish setup"}
            </Button>
          </div>
        </section>
      )}
    </div>
  );
}
