"use client";

import { Button } from "@/components/ui/Button";
import { CrossfadeSwapText } from "@/components/ui/CrossfadeSwapText";
import { listLibraryReleases, saveRelease, unsaveRelease } from "@/lib/api/discoveryClient";
import { ApiError } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/AuthProvider";
import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

type SaveToLibraryButtonProps = {
  releaseId: string;
};

export function SaveToLibraryButton({ releaseId }: SaveToLibraryButtonProps) {
  const auth = useAuth();
  const [saved, setSaved] = useState<boolean | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!auth.isReady) return;

    if (!auth.isAuthenticated) {
      setSaved(false);
      return;
    }

    let cancelled = false;
    setSaved(null);

    void listLibraryReleases()
      .then((response) => {
        if (!cancelled) {
          setSaved(response.releases.some((release) => release.releaseId === releaseId));
        }
      })
      .catch(() => {
        if (!cancelled) setSaved(false);
      });

    return () => {
      cancelled = true;
    };
  }, [auth.isReady, auth.isAuthenticated, releaseId]);

  const toggle = useCallback(async () => {
    if (saved === null) return;
    setBusy(true);
    setError(null);
    try {
      if (saved) {
        await unsaveRelease(releaseId);
        setSaved(false);
      } else {
        await saveRelease(releaseId);
        setSaved(true);
      }
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Could not update library");
    } finally {
      setBusy(false);
    }
  }, [releaseId, saved]);

  if (!auth.isAuthenticated) {
    return (
      <Link href="/login">
        <Button type="button" variant="outlined">
          Save to library
        </Button>
      </Link>
    );
  }

  const statusLoading = saved === null;

  return (
    <div className="flex flex-col gap-1">
      <Button
        type="button"
        variant={saved ? "tertiary-tonal" : "outlined"}
        disabled={busy || statusLoading}
        aria-busy={busy || statusLoading}
        aria-pressed={saved === true}
        onClick={() => void toggle()}
      >
        <CrossfadeSwapText
          showSecondary={saved === true}
          primary="Save to library"
          secondary="Saved"
        />
      </Button>
      {error ? <span className="text-label-medium text-error">{error}</span> : null}
    </div>
  );
}
