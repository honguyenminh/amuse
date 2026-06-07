"use client";

import { FilterChip } from "@/components/ui/FilterChip";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Text } from "@/components/ui/Text";
import {
  DEFAULT_PLAYLIST_LIBRARY_FILTERS,
  type PlaylistLibraryFilters,
  type PlaylistOwnershipFilter,
  type PlaylistVisibilityFilter,
} from "@/lib/discovery/playlistLibraryFilters";
import { useEffect, useId, useState } from "react";

type PlaylistFiltersDialogProps = {
  open: boolean;
  filters: PlaylistLibraryFilters;
  onClose: () => void;
  onApply: (filters: PlaylistLibraryFilters) => void;
};

const OWNERSHIP_OPTIONS: { value: PlaylistOwnershipFilter; label: string }[] = [
  { value: "all", label: "All" },
  { value: "mine", label: "Mine" },
  { value: "following", label: "Following" },
];

const VISIBILITY_OPTIONS: { value: PlaylistVisibilityFilter; label: string }[] = [
  { value: "all", label: "All" },
  { value: "public", label: "Public" },
  { value: "private", label: "Private" },
];

export function PlaylistFiltersDialog({
  open,
  filters,
  onClose,
  onApply,
}: PlaylistFiltersDialogProps) {
  const titleId = useId();
  const [draft, setDraft] = useState(filters);

  useEffect(() => {
    if (open) setDraft(filters);
  }, [open, filters]);

  useEffect(() => {
    if (!open) return;
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    document.addEventListener("keydown", onKey);
    const prev = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", onKey);
      document.body.style.overflow = prev;
    };
  }, [open, onClose]);

  if (!open) return null;

  return (
    <div className="fixed inset-0 z-[60] flex items-end justify-center p-4 sm:items-center">
      <div
        className="absolute inset-0 bg-black/40 backdrop-blur-sm"
        onClick={onClose}
        aria-hidden
      />
      <Card
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className="relative z-10 w-full max-w-lg shadow-2xl"
        onClick={(event) => event.stopPropagation()}
      >
        <h2 id={titleId} className="text-title-large text-on-surface">
          Filter playlists
        </h2>
        <Text variant="body-medium" className="mt-1 text-on-surface-variant">
          Narrow your library by ownership and visibility.
        </Text>

        <div className="mt-5 flex flex-col gap-5">
          <section>
            <Text variant="label-large" className="mb-2 text-on-surface-variant">
              Show
            </Text>
            <div className="flex flex-wrap gap-2">
              {OWNERSHIP_OPTIONS.map((option) => (
                <FilterChip
                  key={option.value}
                  selected={draft.ownership === option.value}
                  onClick={() => setDraft((prev) => ({ ...prev, ownership: option.value }))}
                >
                  {option.label}
                </FilterChip>
              ))}
            </div>
          </section>

          <section>
            <Text variant="label-large" className="mb-2 text-on-surface-variant">
              Visibility
            </Text>
            <div className="flex flex-wrap gap-2">
              {VISIBILITY_OPTIONS.map((option) => (
                <FilterChip
                  key={option.value}
                  selected={draft.visibility === option.value}
                  onClick={() => setDraft((prev) => ({ ...prev, visibility: option.value }))}
                >
                  {option.label}
                </FilterChip>
              ))}
            </div>
          </section>
        </div>

        <div className="mt-6 flex flex-wrap justify-end gap-2">
          <Button
            type="button"
            variant="text"
            onClick={() => setDraft(DEFAULT_PLAYLIST_LIBRARY_FILTERS)}
          >
            Reset
          </Button>
          <Button type="button" variant="outlined" onClick={onClose}>
            Cancel
          </Button>
          <Button
            type="button"
            variant="filled"
            onClick={() => {
              onApply(draft);
              onClose();
            }}
          >
            Apply
          </Button>
        </div>
      </Card>
    </div>
  );
}
