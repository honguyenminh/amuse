"use client";

import { UserAvatar } from "@/components/account/UserAvatar";
import { Button } from "@/components/ui/Button";
import { Text } from "@/components/ui/Text";
import {
  AVATAR_ACCENT_COUNT,
  avatarAccentClass,
  normalizeAvatarAccentSeed,
} from "@/lib/account/avatarAccent";
import { uploadListenerAvatar } from "@/lib/account/uploadAvatar";
import { cn } from "@/lib/cn";
import { useRef, useState } from "react";

type AvatarFieldProps = {
  displayName: string;
  email?: string | null;
  accentSeed: number;
  avatarUrl?: string | null;
  onAccentSeedChange: (seed: number) => void;
  onAvatarUrlChange: (url: string | null) => void;
  onClearUploadedAvatar?: () => Promise<void>;
};

export function AvatarField({
  displayName,
  email,
  accentSeed,
  avatarUrl,
  onAccentSeedChange,
  onAvatarUrlChange,
  onClearUploadedAvatar,
}: AvatarFieldProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onFileSelected = async (file: File | undefined) => {
    if (!file) {
      return;
    }
    setUploading(true);
    setError(null);
    try {
      const url = await uploadListenerAvatar(file);
      onAvatarUrlChange(url);
    } catch (uploadError) {
      setError(
        uploadError instanceof Error
          ? uploadError.message
          : "Could not upload avatar.",
      );
    } finally {
      setUploading(false);
      if (inputRef.current) {
        inputRef.current.value = "";
      }
    }
  };

  const removePhoto = async () => {
    setError(null);
    if (onClearUploadedAvatar) {
      try {
        await onClearUploadedAvatar();
      } catch (clearError) {
        setError(
          clearError instanceof Error
            ? clearError.message
            : "Could not remove avatar.",
        );
        return;
      }
    }
    onAvatarUrlChange(null);
  };

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center gap-3">
        <UserAvatar
          displayName={displayName}
          email={email}
          accentSeed={accentSeed}
          avatarUrl={avatarUrl}
          size="lg"
        />
        <div className="flex flex-col gap-2">
          <input
            ref={inputRef}
            type="file"
            accept="image/jpeg,image/png,image/webp"
            className="hidden"
            onChange={(event) => void onFileSelected(event.target.files?.[0])}
          />
          <Button
            type="button"
            variant="outlined"
            disabled={uploading}
            onClick={() => inputRef.current?.click()}
          >
            {uploading ? "Uploading…" : "Upload photo"}
          </Button>
          {avatarUrl ? (
            <Button type="button" variant="text" onClick={() => void removePhoto()}>
              Remove photo
            </Button>
          ) : null}
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <Text variant="label-medium">Or pick an accent color</Text>
        <div className="flex flex-wrap gap-2">
          {Array.from({ length: AVATAR_ACCENT_COUNT }, (_, index) => (
            <button
              key={index}
              type="button"
              aria-label={`Accent ${index + 1}`}
              className={cn(
                "size-8 rounded-full border-2",
                avatarAccentClass(index),
                normalizeAvatarAccentSeed(accentSeed) === index
                  ? "border-on-surface"
                  : "border-transparent",
              )}
              onClick={() => onAccentSeedChange(index)}
            />
          ))}
        </div>
      </div>

      {error ? (
        <Text variant="body-medium" className="text-error">
          {error}
        </Text>
      ) : null}
    </div>
  );
}
