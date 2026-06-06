"use client";

import { AvatarField } from "@/components/account/AvatarField";
import { UserAvatar } from "@/components/account/UserAvatar";
import { AppShell } from "@/components/ui/AppShell";
import { Button } from "@/components/ui/Button";
import { PageContent } from "@/components/ui/PageContent";
import { Text } from "@/components/ui/Text";
import { useAuth } from "@/lib/auth/AuthProvider";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

export default function AccountSettingsPage() {
  const auth = useAuth();
  const router = useRouter();
  const [displayName, setDisplayName] = useState("");
  const [accentSeed, setAccentSeed] = useState(0);
  const [avatarUrl, setAvatarUrl] = useState<string | null>(null);
  const [allowUnverifiedArtists, setAllowUnverifiedArtists] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!auth.listenerProfile) {
      return;
    }
    setDisplayName(auth.listenerProfile.displayName ?? "");
    setAccentSeed(auth.listenerProfile.avatarAccentSeed ?? 0);
    setAvatarUrl(auth.listenerProfile.avatarUrl ?? null);
    setAllowUnverifiedArtists(auth.listenerProfile.allowUnverifiedArtists ?? false);
  }, [auth.listenerProfile]);

  useEffect(() => {
    if (!auth.isReady) {
      return;
    }
    if (!auth.isAuthenticated) {
      router.replace("/login?next=/settings/account");
    }
  }, [auth.isReady, auth.isAuthenticated, router]);

  if (!auth.isReady) {
    return null;
  }

  if (!auth.isAuthenticated) {
    return null;
  }

  const save = async () => {
    setSaving(true);
    setError(null);
    try {
      await auth.completeListenerOnboarding({
        displayName: displayName.trim(),
        avatarAccentSeed: accentSeed,
        allowUnverifiedArtists,
      });
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : "Could not save.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <AppShell title="Account" activePath="/settings">
      <PageContent width="account">
        <div>
          <Text variant="headline-medium">Account</Text>
          <Text variant="body-medium" className="mt-2 text-on-surface-variant">
            Your listener profile is separate from business portal identity.
          </Text>
        </div>

        <section className="flex flex-col gap-4 rounded-2xl border-2 border-outline bg-surface p-4">
          <div className="flex items-center gap-3">
            <UserAvatar
              displayName={displayName}
              email={auth.account?.email}
              accentSeed={accentSeed}
              avatarUrl={avatarUrl}
              size="lg"
            />
            <div>
              <Text variant="title-medium">{displayName.trim() || "Listener"}</Text>
              {auth.account?.email ? (
                <Text variant="label-medium" className="text-on-surface-variant">
                  {auth.account.email}
                </Text>
              ) : null}
            </div>
          </div>

          <label className="flex flex-col gap-2">
            <Text variant="label-medium">Display name</Text>
            <input
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              className="rounded-xl border-2 border-outline bg-background px-3 py-2"
              maxLength={80}
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
              void auth.refreshListenerProfile();
            }}
            onClearUploadedAvatar={async () => {
              await auth.completeListenerOnboarding({ clearAvatar: true });
            }}
          />

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
                Controls how unverified artists appear in search results.
              </Text>
            </span>
          </label>

          {error ? (
            <Text variant="body-medium" className="text-error">
              {error}
            </Text>
          ) : null}

          <Button type="button" disabled={saving} onClick={() => void save()}>
            {saving ? "Saving…" : "Save changes"}
          </Button>
        </section>
      </PageContent>
    </AppShell>
  );
}
