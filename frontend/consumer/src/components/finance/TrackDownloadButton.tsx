"use client";

import { Button } from "@/components/ui/Button";
import { getTrackDownload } from "@/lib/api/financeClient";
import { useCallback, useState } from "react";

type TrackDownloadButtonProps = {
  trackId: string;
  variant?: "primary" | "outlined" | "text";
  className?: string;
};

export function TrackDownloadButton({
  trackId,
  variant = "outlined",
  className,
}: TrackDownloadButtonProps) {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleDownload = useCallback(async () => {
    setError(null);
    setLoading(true);
    try {
      const response = await getTrackDownload(trackId);
      const anchor = document.createElement("a");
      anchor.href = response.url;
      anchor.download = response.fileName;
      anchor.rel = "noopener noreferrer";
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Download failed.");
    } finally {
      setLoading(false);
    }
  }, [trackId]);

  return (
    <div className={className}>
      <Button
        type="button"
        variant={variant}
        disabled={loading}
        onClick={() => void handleDownload()}
      >
        {loading ? "Preparing…" : "Download"}
      </Button>
      {error ? (
        <p className="mt-1 text-label-small text-error" role="alert">
          {error}
        </p>
      ) : null}
    </div>
  );
}
