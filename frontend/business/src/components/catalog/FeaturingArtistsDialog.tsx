"use client";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import type { ManageArtistSummaryResponse } from "@/lib/api/catalogClient";
import { Check, Search } from "lucide-react";
import { useEffect, useMemo, useState } from "react";

type FeaturingArtistsDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  artists: ManageArtistSummaryResponse[];
  primaryArtistName: string;
  selectedIds: string[];
  onSelectedIdsChange: (ids: string[]) => void;
};

function formatVisibilityTier(tier: ManageArtistSummaryResponse["visibilityTier"]): string {
  return tier === "platformVerified" ? "Platform verified" : "Unverified";
}

function formatAddedDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

function shortArtistId(id: string): string {
  return id.slice(0, 8);
}

export function FeaturingArtistsDialog({
  open,
  onOpenChange,
  artists,
  primaryArtistName,
  selectedIds,
  onSelectedIdsChange,
}: FeaturingArtistsDialogProps) {
  const [search, setSearch] = useState("");
  const [draftIds, setDraftIds] = useState<string[]>(selectedIds);

  useEffect(() => {
    if (open) {
      setDraftIds(selectedIds);
      setSearch("");
    }
  }, [open, selectedIds]);

  const filteredArtists = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) {
      return artists;
    }

    return artists.filter(
      (artist) =>
        artist.name.toLowerCase().includes(query) ||
        artist.slug.toLowerCase().includes(query) ||
        artist.id.toLowerCase().includes(query),
    );
  }, [artists, search]);

  function toggleArtist(artistId: string) {
    setDraftIds((current) =>
      current.includes(artistId)
        ? current.filter((id) => id !== artistId)
        : [...current, artistId],
    );
  }

  function onConfirm() {
    onSelectedIdsChange(draftIds);
    onOpenChange(false);
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>Featuring artists</DialogTitle>
          <DialogDescription>
            Choose roster artists to feature on this release by {primaryArtistName}. Search
            by name, slug, or ID to avoid picking the wrong profile.
          </DialogDescription>
        </DialogHeader>

        <DialogBody className="gap-3">
          <div className="relative">
            <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search by name, slug, or ID…"
              className="pl-9"
              autoFocus
            />
          </div>

          <div className="max-h-80 overflow-y-auto rounded-md border">
            {artists.length === 0 ? (
              <p className="p-4 text-sm text-muted-foreground">
                No other artists on your roster yet.
              </p>
            ) : filteredArtists.length === 0 ? (
              <p className="p-4 text-sm text-muted-foreground">
                No artists match &ldquo;{search.trim()}&rdquo;.
              </p>
            ) : (
              <ul className="divide-y">
                {filteredArtists.map((artist) => {
                  const selected = draftIds.includes(artist.id);
                  return (
                    <li key={artist.id}>
                      <button
                        type="button"
                        onClick={() => toggleArtist(artist.id)}
                        className="flex w-full items-start gap-3 px-4 py-3 text-left transition-colors hover:bg-muted/50"
                      >
                        <span
                          className={`mt-0.5 flex size-5 shrink-0 items-center justify-center rounded border ${
                            selected
                              ? "border-primary bg-primary text-primary-foreground"
                              : "border-input bg-background"
                          }`}
                          aria-hidden
                        >
                          {selected ? <Check className="size-3.5" /> : null}
                        </span>

                        <span className="min-w-0 flex-1">
                          <span className="flex flex-wrap items-center gap-2">
                            <span className="font-medium">{artist.name}</span>
                            <span className="rounded-full border px-2 py-0.5 text-xs text-muted-foreground">
                              {formatVisibilityTier(artist.visibilityTier)}
                            </span>
                          </span>
                          <span className="mt-1 block text-xs text-muted-foreground">
                            Slug: /artists/{artist.slug}
                          </span>
                          <span className="mt-0.5 block text-xs text-muted-foreground">
                            Added {formatAddedDate(artist.createdAt)} · ID{" "}
                            {shortArtistId(artist.id)}
                          </span>
                        </span>
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>

          <p className="text-xs text-muted-foreground">
            {draftIds.length} artist{draftIds.length === 1 ? "" : "s"} selected
          </p>
        </DialogBody>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button type="button" onClick={onConfirm}>
            {draftIds.length > 0
              ? `Use ${draftIds.length} featuring artist${draftIds.length === 1 ? "" : "s"}`
              : "Clear featuring artists"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function FeaturingArtistsSummary({
  artists,
  onRemove,
  disabled,
}: {
  artists: ManageArtistSummaryResponse[];
  onRemove: (artistId: string) => void;
  disabled?: boolean;
}) {
  if (artists.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">No featuring artists selected.</p>
    );
  }

  return (
    <ul className="flex flex-col gap-2">
      {artists.map((artist) => (
        <li
          key={artist.id}
          className="flex items-start justify-between gap-3 rounded-md border px-3 py-2"
        >
          <div className="min-w-0">
            <p className="font-medium">{artist.name}</p>
            <p className="text-xs text-muted-foreground">
              /artists/{artist.slug} · {formatVisibilityTier(artist.visibilityTier)}
            </p>
          </div>
          {!disabled ? (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => onRemove(artist.id)}
            >
              Remove
            </Button>
          ) : null}
        </li>
      ))}
    </ul>
  );
}
