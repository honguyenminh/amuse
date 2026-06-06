"use client";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { uploadArtistCover } from "@/lib/catalog/artistCover";
import { ImageIcon, Upload } from "lucide-react";
import { useEffect, useRef, useState } from "react";

type CoverUploadStatus = "idle" | "uploading" | "success" | "error";

type ArtistCoverPanelProps = {
  artistId: string;
  artistName: string;
  coverUrl: string | null;
  canUpload: boolean;
  onCoverUpdated: (coverUrl: string | null) => void;
};

export function ArtistCoverPanel({
  artistId,
  artistName,
  coverUrl,
  canUpload,
  onCoverUpdated,
}: ArtistCoverPanelProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [status, setStatus] = useState<CoverUploadStatus>("idle");
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(coverUrl);

  useEffect(() => {
    setPreviewUrl(coverUrl);
  }, [coverUrl]);

  useEffect(() => {
    if (status !== "success") {
      return;
    }
    const timer = window.setTimeout(() => {
      setStatus("idle");
      setStatusMessage(null);
    }, 4000);
    return () => window.clearTimeout(timer);
  }, [status]);

  async function onFileSelected(file: File) {
    setStatus("uploading");
    setStatusMessage("Uploading cover image…");
    setPreviewUrl(URL.createObjectURL(file));

    try {
      const result = await uploadArtistCover(artistId, file);
      onCoverUpdated(result.coverUrl);
      setPreviewUrl(result.coverUrl ?? URL.createObjectURL(file));
      setStatus("success");
      setStatusMessage("Cover image saved.");
    } catch (err) {
      setPreviewUrl(coverUrl);
      setStatus("error");
      setStatusMessage(
        err instanceof Error ? err.message : "Failed to upload cover image.",
      );
    }
  }

  const displayUrl = previewUrl;

  return (
    <Card>
      <CardHeader>
        <CardTitle>Cover image</CardTitle>
        <CardDescription>
          {canUpload
            ? "Choosing a file saves immediately — there is no separate Save button."
            : `${artistName}'s cover image.`}
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-4 sm:flex-row sm:items-start">
        <div className="flex w-full shrink-0 items-center justify-center sm:w-auto">
          {displayUrl ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={displayUrl}
              alt={`${artistName} cover`}
              className="aspect-[3/1] w-full max-w-md rounded-md border object-cover shadow-sm sm:w-80"
            />
          ) : (
            <div className="flex aspect-[3/1] w-full max-w-md flex-col items-center justify-center gap-2 rounded-md border border-dashed bg-muted/40 text-muted-foreground sm:w-80">
              <ImageIcon className="size-8 opacity-60" />
              <span className="text-xs">No cover yet</span>
            </div>
          )}
        </div>

        <div className="flex min-w-0 flex-1 flex-col gap-3 text-sm">
          {statusMessage ? (
            <p
              className={
                status === "error"
                  ? "text-destructive"
                  : status === "success"
                    ? "text-green-600 dark:text-green-500"
                    : "text-muted-foreground"
              }
            >
              {statusMessage}
            </p>
          ) : displayUrl ? (
            <p className="text-muted-foreground">
              Cover image is set for this artist.
            </p>
          ) : (
            <p className="text-muted-foreground">
              Upload a wide banner image for the artist page header in the consumer
              app.
            </p>
          )}

          {canUpload ? (
            <>
              <input
                ref={inputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                className="hidden"
                onChange={(event) => {
                  const file = event.target.files?.[0];
                  if (file) {
                    void onFileSelected(file);
                  }
                  event.target.value = "";
                }}
              />
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="w-fit"
                disabled={status === "uploading"}
                onClick={() => inputRef.current?.click()}
              >
                <Upload />
                {status === "uploading"
                  ? "Uploading…"
                  : displayUrl
                    ? "Replace cover image"
                    : "Upload cover image"}
              </Button>
              <p className="text-xs text-muted-foreground">
                JPEG, PNG, or WebP. A wide landscape image works best.
              </p>
            </>
          ) : null}
        </div>
      </CardContent>
    </Card>
  );
}
