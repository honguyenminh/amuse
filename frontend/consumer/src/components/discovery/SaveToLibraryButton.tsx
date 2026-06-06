"use client";

import { Button } from "@/components/ui/Button";
import { saveRelease, unsaveRelease } from "@/lib/api/discoveryClient";
import { ApiError } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/AuthProvider";
import Link from "next/link";
import { useCallback, useState } from "react";

type SaveToLibraryButtonProps = {
  releaseId: string;
};

export function SaveToLibraryButton({ releaseId }: SaveToLibraryButtonProps) {
  const auth = useAuth();
  const [saved, setSaved] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const toggle = useCallback(async () => {
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

  return (
    <div className="flex flex-col gap-1">
      <Button type="button" variant="outlined" disabled={busy} onClick={() => void toggle()}>
        {saved ? "Saved" : "Save to library"}
      </Button>
      {error ? <span className="text-label-medium text-error">{error}</span> : null}
    </div>
  );
}
