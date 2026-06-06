"use client";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { uploadReleaseCoverArt } from "@/lib/catalog/coverArt";
import { ImageIcon, Upload } from "lucide-react";
import { useEffect, useRef, useState } from "react";

type CoverUploadStatus = "idle" | "uploading" | "success" | "error";

type ReleaseCoverArtPanelProps = {
  releaseId: string;
  coverArtUrl: string | null;
  canUpload: boolean;
  onCoverUpdated: (coverArtUrl: string | null) => void;
};

export function ReleaseCoverArtPanel({
  releaseId,
  coverArtUrl,
  canUpload,
  onCoverUpdated,
}: ReleaseCoverArtPanelProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [status, setStatus] = useState<CoverUploadStatus>("idle");
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(coverArtUrl);

  useEffect(() => {
    setPreviewUrl(coverArtUrl);
  }, [coverArtUrl]);

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
    setStatusMessage("Uploading cover art…");
    setPreviewUrl(URL.createObjectURL(file));

    try {
      const result = await uploadReleaseCoverArt(releaseId, file);
      onCoverUpdated(result.coverArtUrl);
      setPreviewUrl(result.coverArtUrl ?? URL.createObjectURL(file));
      setStatus("success");
      setStatusMessage("Cover art saved.");
    } catch (err) {
      setPreviewUrl(coverArtUrl);
      setStatus("error");
      setStatusMessage(err instanceof Error ? err.message : "Failed to upload cover art.");
    }
  }

  const displayUrl = previewUrl;

  return (
    <Card>
      <CardHeader>
        <CardTitle>Cover art</CardTitle>
        <CardDescription>
          {canUpload
            ? "Choosing a file saves immediately — there is no separate Save button."
            : "Release cover art."}
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-4 sm:flex-row sm:items-start">
        <div className="flex shrink-0 items-center justify-center">
          {displayUrl ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={displayUrl}
              alt="Release cover art"
              className="aspect-square size-40 rounded-md border object-cover shadow-sm"
            />
          ) : (
            <div className="flex aspect-square size-40 flex-col items-center justify-center gap-2 rounded-md border border-dashed bg-muted/40 text-muted-foreground">
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
            <p className="text-muted-foreground">Cover art is set for this release.</p>
          ) : (
            <p className="text-muted-foreground">
              Upload an image, or add audio files with embedded artwork — cover art from
              the first track with embedded art can be applied when you upload audio on
              the tracks below.
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
                    ? "Replace cover art"
                    : "Upload cover art"}
              </Button>
              <p className="text-xs text-muted-foreground">
                JPEG, PNG, or WebP.
              </p>
            </>
          ) : null}
        </div>
      </CardContent>
    </Card>
  );
}

type PendingCoverArtPreviewProps = {
  previewUrl: string | null;
  sourceLabel: string | null;
  disabled?: boolean;
  onSelectFile: (file: File) => void;
  onClear?: () => void;
};

export function PendingCoverArtPreview({
  previewUrl,
  sourceLabel,
  disabled = false,
  onSelectFile,
  onClear,
}: PendingCoverArtPreviewProps) {
  const inputRef = useRef<HTMLInputElement>(null);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Cover art</CardTitle>
        <CardDescription>
          Optional. Embedded artwork from your audio files is detected automatically and
          uploaded when you create the release.
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-4 sm:flex-row sm:items-start">
        <div className="flex shrink-0 items-center justify-center">
          {previewUrl ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={previewUrl}
              alt="Pending release cover art"
              className="aspect-square size-40 rounded-md border object-cover shadow-sm"
            />
          ) : (
            <div className="flex aspect-square size-40 flex-col items-center justify-center gap-2 rounded-md border border-dashed bg-muted/40 text-muted-foreground">
              <ImageIcon className="size-8 opacity-60" />
              <span className="text-xs">No cover selected</span>
            </div>
          )}
        </div>

        <div className="flex min-w-0 flex-1 flex-col gap-3 text-sm">
          {sourceLabel ? (
            <p className="text-muted-foreground">{sourceLabel}</p>
          ) : (
            <p className="text-muted-foreground">
              Add audio with embedded artwork or choose an image file below.
            </p>
          )}

          <input
            ref={inputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            className="hidden"
            disabled={disabled}
            onChange={(event) => {
              const file = event.target.files?.[0];
              if (file) {
                onSelectFile(file);
              }
              event.target.value = "";
            }}
          />
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={disabled}
              onClick={() => inputRef.current?.click()}
            >
              <Upload />
              {previewUrl ? "Replace image" : "Choose image"}
            </Button>
            {previewUrl && onClear ? (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                disabled={disabled}
                onClick={onClear}
              >
                Remove
              </Button>
            ) : null}
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
