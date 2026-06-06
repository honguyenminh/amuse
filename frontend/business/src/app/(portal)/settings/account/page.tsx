"use client";

import { AvatarField } from "@/components/account/AvatarField";
import { UserAvatar } from "@/components/account/UserAvatar";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/auth/AuthProvider";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

export default function BusinessAccountSettingsPage() {
  const auth = useAuth();
  const router = useRouter();
  const [displayName, setDisplayName] = useState("");
  const [accentSeed, setAccentSeed] = useState(0);
  const [avatarUrl, setAvatarUrl] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!auth.portalProfile) {
      return;
    }
    setDisplayName(auth.portalProfile.displayName ?? "");
    setAccentSeed(auth.portalProfile.avatarAccentSeed ?? 0);
    setAvatarUrl(auth.portalProfile.avatarUrl ?? null);
  }, [auth.portalProfile]);

  if (!auth.isReady) {
    return null;
  }

  if (!auth.isAuthenticated) {
    router.replace("/login?next=/settings/account");
    return null;
  }

  const save = async () => {
    setSaving(true);
    setError(null);
    try {
      await auth.completePortalProfile({
        displayName: displayName.trim(),
        avatarAccentSeed: accentSeed,
      });
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : "Could not save.");
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="mx-auto flex w-full max-w-xl flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Account</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Your business portal profile is separate from listener and organization profiles.
        </p>
      </div>

      <section className="flex flex-col gap-4 rounded-xl border bg-card p-4">
        <div className="flex items-center gap-3">
          <UserAvatar
            displayName={displayName}
            email={auth.account?.email}
            accentSeed={accentSeed}
            avatarUrl={avatarUrl}
            size="lg"
          />
          <div>
            <p className="font-medium">{displayName.trim() || "Account"}</p>
            {auth.account?.email ? (
              <p className="text-sm text-muted-foreground">{auth.account.email}</p>
            ) : null}
          </div>
        </div>

        <label className="flex flex-col gap-2 text-sm">
          <span className="font-medium">Display name</span>
          <input
            value={displayName}
            onChange={(event) => setDisplayName(event.target.value)}
            className="rounded-md border bg-background px-3 py-2"
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
            void auth.refreshPortalProfile();
          }}
          onClearUploadedAvatar={async () => {
            await auth.completePortalProfile({ clearAvatar: true });
          }}
        />

        {error ? <p className="text-sm text-destructive">{error}</p> : null}

        <Button type="button" disabled={saving} onClick={() => void save()}>
          {saving ? "Saving…" : "Save changes"}
        </Button>
      </section>
    </div>
  );
}
