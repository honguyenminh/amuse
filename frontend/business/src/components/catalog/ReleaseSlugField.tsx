"use client";

import { checkReleaseSlugAvailability } from "@/lib/api/catalogClient";
import {
  isValidArtistSlug,
  normalizeSlugInput,
  slugValidationMessage,
  suggestArtistSlugFromName,
} from "@/lib/catalog/slug";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useEffect, useState } from "react";

export type ReleaseSlugStatus = "idle" | "checking" | "available" | "taken" | "invalid";

type ReleaseSlugFieldProps = {
  artistId: string;
  artistSlug: string;
  title: string;
  slug: string;
  onSlugChange: (slug: string) => void;
  slugManuallyEdited: boolean;
  onSlugManuallyEditedChange: (edited: boolean) => void;
  excludingReleaseId?: string;
  disabled?: boolean;
  optional?: boolean;
  onSlugStatusChange?: (status: ReleaseSlugStatus) => void;
};

export function ReleaseSlugField({
  artistId,
  artistSlug,
  title,
  slug,
  onSlugChange,
  slugManuallyEdited,
  onSlugManuallyEditedChange,
  excludingReleaseId,
  disabled = false,
  optional = false,
  onSlugStatusChange,
}: ReleaseSlugFieldProps) {
  const [slugStatus, setSlugStatus] = useState<ReleaseSlugStatus>("idle");

  const updateSlugStatus = (status: ReleaseSlugStatus) => {
    setSlugStatus(status);
    onSlugStatusChange?.(status);
  };

  useEffect(() => {
    if (!slugManuallyEdited) {
      onSlugChange(suggestArtistSlugFromName(title));
    }
  }, [title, slugManuallyEdited, onSlugChange]);

  useEffect(() => {
    const normalized = normalizeSlugInput(slug);
    if (!normalized) {
      updateSlugStatus(optional ? "idle" : "invalid");
      return;
    }

    const validationError = slugValidationMessage(slug);
    if (validationError) {
      updateSlugStatus("invalid");
      return;
    }

    updateSlugStatus("checking");
    const timer = window.setTimeout(() => {
      void checkReleaseSlugAvailability(artistId, normalized, excludingReleaseId)
        .then((response) => {
          if (!response.isValid) {
            updateSlugStatus("invalid");
            return;
          }
          updateSlugStatus(response.isAvailable ? "available" : "taken");
        })
        .catch(() => updateSlugStatus("idle"));
    }, 300);

    return () => window.clearTimeout(timer);
  }, [slug, artistId, excludingReleaseId, optional]);

  const normalizedSlug = normalizeSlugInput(slug);
  const slugValidationError = slug && !optional ? slugValidationMessage(slug) : slug ? slugValidationMessage(slug) : null;

  return (
    <div className="grid gap-2">
      <Label htmlFor="release-slug">URL slug {optional ? "(optional)" : ""}</Label>
      <Input
        id="release-slug"
        value={slug}
        onChange={(event) => {
          onSlugManuallyEditedChange(true);
          onSlugChange(event.target.value);
        }}
        placeholder="release-slug"
        disabled={disabled}
      />
      <p className="text-xs text-muted-foreground">
        Public path: /artist/{artistSlug}/release/{normalizedSlug || "your-slug"}
      </p>
      {slugValidationError ? (
        <p className="text-xs text-destructive">{slugValidationError}</p>
      ) : slugStatus === "checking" ? (
        <p className="text-xs text-muted-foreground">Checking availability…</p>
      ) : slugStatus === "available" ? (
        <p className="text-xs text-green-600 dark:text-green-400">Slug is available.</p>
      ) : slugStatus === "taken" ? (
        <p className="text-xs text-destructive">This slug is already used for this artist.</p>
      ) : optional && !normalizedSlug ? (
        <p className="text-xs text-muted-foreground">Leave empty to generate from the title.</p>
      ) : null}
    </div>
  );
}

export function releaseSlugReadyForSubmit(
  slug: string,
  slugStatus: ReleaseSlugStatus,
  optional: boolean,
): boolean {
  const normalized = normalizeSlugInput(slug);
  if (!normalized) return optional;
  if (!isValidArtistSlug(normalized)) return false;
  return slugStatus === "available";
}
