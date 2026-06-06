"use client";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { uploadArtistAvatar } from "@/lib/catalog/artistAvatar";
import { ImageIcon, Upload } from "lucide-react";
import { useEffect, useRef, useState } from "react";

type AvatarUploadStatus = "idle" | "uploading" | "success" | "error";

type ArtistAvatarPanelProps = {
  artistId: string;
  artistName: string;
  avatarUrl: string | null;
  canUpload: boolean;
  onAvatarUpdated: (avatarUrl: string | null) => void;
};

export function ArtistAvatarPanel({
  artistId,
  artistName,
  avatarUrl,
  canUpload,
  onAvatarUpdated,
}: ArtistAvatarPanelProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [status, setStatus] = useState<AvatarUploadStatus>("idle");
  const [statusMessage, setStatusMessage] = useState<string | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(avatarUrl);

  useEffect(() => {
    setPreviewUrl(avatarUrl);
  }, [avatarUrl]);

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
    setStatusMessage("Uploading profile picture…");
    setPreviewUrl(URL.createObjectURL(file));

    try {
      const result = await uploadArtistAvatar(artistId, file);
      onAvatarUpdated(result.avatarUrl);
      setPreviewUrl(result.avatarUrl ?? URL.createObjectURL(file));
      setStatus("success");
      setStatusMessage("Profile picture saved.");
    } catch (err) {
      setPreviewUrl(avatarUrl);
      setStatus("error");
      setStatusMessage(
        err instanceof Error ? err.message : "Failed to upload profile picture.",
      );
    }
  }

  const displayUrl = previewUrl;

  return (
    <Card>
      <CardHeader>
        <CardTitle>Profile picture</CardTitle>
        <CardDescription>
          {canUpload
            ? "Choosing a file saves immediately — there is no separate Save button."
            : `${artistName}'s profile picture.`}
        </CardDescription>
      </CardHeader>
      <CardContent className="flex flex-col gap-4 sm:flex-row sm:items-start">
        <div className="flex shrink-0 items-center justify-center">
          {displayUrl ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={displayUrl}
              alt={`${artistName} profile picture`}
              className="aspect-square size-40 rounded-full border object-cover shadow-sm"
            />
          ) : (
            <div className="flex aspect-square size-40 flex-col items-center justify-center gap-2 rounded-full border border-dashed bg-muted/40 text-muted-foreground">
              <ImageIcon className="size-8 opacity-60" />
              <span className="text-xs">No picture yet</span>
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
              Profile picture is set for this artist.
            </p>
          ) : (
            <p className="text-muted-foreground">
              Upload a square image to show on the artist page in the consumer app.
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
                    ? "Replace profile picture"
                    : "Upload profile picture"}
              </Button>
              <p className="text-xs text-muted-foreground">
                JPEG, PNG, or WebP. Saved as soon as the upload completes.
              </p>
            </>
          ) : null}
        </div>
      </CardContent>
    </Card>
  );
}
