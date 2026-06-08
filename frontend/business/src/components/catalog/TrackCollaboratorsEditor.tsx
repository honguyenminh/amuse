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
import { Label } from "@/components/ui/label";
import {
  searchCollaboratorArtists,
  type ManageArtistSummaryResponse,
  type ManageTrackCollaboratorResponse,
  type TrackCollaboratorEntryRequest,
} from "@/lib/api/catalogClient";
import { Check, Search } from "lucide-react";
import { useEffect, useMemo, useState } from "react";

export type TrackCollaboratorDraft =
  | { kind: "linked"; artistId: string; displayName: string }
  | { kind: "placeholder"; displayName: string };

export function collaboratorsToDrafts(
  collaborators: ManageTrackCollaboratorResponse[],
): TrackCollaboratorDraft[] {
  return collaborators.map((collaborator) =>
    collaborator.isPlaceholder
      ? { kind: "placeholder", displayName: collaborator.displayName }
      : {
          kind: "linked",
          artistId: collaborator.artistId!,
          displayName: collaborator.displayName,
        },
  );
}

export function draftsToRequest(collaborators: TrackCollaboratorDraft[]): TrackCollaboratorEntryRequest[] {
  return collaborators.map((collaborator) =>
    collaborator.kind === "linked"
      ? { artistId: collaborator.artistId }
      : { displayName: collaborator.displayName },
  );
}

function formatVisibilityTier(tier: ManageArtistSummaryResponse["visibilityTier"]): string {
  return tier === "platformVerified" ? "Platform verified" : "Unverified";
}

type TrackCollaboratorsDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  primaryArtistId: string;
  collaborators: TrackCollaboratorDraft[];
  onCollaboratorsChange: (collaborators: TrackCollaboratorDraft[]) => void;
};

export function TrackCollaboratorsDialog({
  open,
  onOpenChange,
  primaryArtistId,
  collaborators,
  onCollaboratorsChange,
}: TrackCollaboratorsDialogProps) {
  const [search, setSearch] = useState("");
  const [placeholderName, setPlaceholderName] = useState("");
  const [draftCollaborators, setDraftCollaborators] = useState<TrackCollaboratorDraft[]>(collaborators);
  const [searchResults, setSearchResults] = useState<ManageArtistSummaryResponse[]>([]);
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setDraftCollaborators(collaborators);
      setSearch("");
      setPlaceholderName("");
      setSearchResults([]);
      setSearchError(null);
    }
  }, [open, collaborators]);

  useEffect(() => {
    if (!open) {
      return;
    }

    const trimmed = search.trim();
    if (trimmed.length < 2) {
      setSearchResults([]);
      setSearching(false);
      setSearchError(null);
      return;
    }

    let cancelled = false;
    setSearching(true);
    setSearchError(null);

    const timeout = window.setTimeout(() => {
      void searchCollaboratorArtists(trimmed, {
        excludingArtistId: primaryArtistId,
        limit: 30,
      })
        .then((response) => {
          if (!cancelled) {
            setSearchResults(response.items);
          }
        })
        .catch((err) => {
          if (!cancelled) {
            setSearchError(err instanceof Error ? err.message : "Search failed.");
            setSearchResults([]);
          }
        })
        .finally(() => {
          if (!cancelled) {
            setSearching(false);
          }
        });
    }, 250);

    return () => {
      cancelled = true;
      window.clearTimeout(timeout);
    };
  }, [open, primaryArtistId, search]);

  const selectedLinkedIds = useMemo(
    () =>
      new Set(
        draftCollaborators
          .filter((entry): entry is Extract<TrackCollaboratorDraft, { kind: "linked" }> => entry.kind === "linked")
          .map((entry) => entry.artistId),
      ),
    [draftCollaborators],
  );

  function toggleArtist(artist: ManageArtistSummaryResponse) {
    setDraftCollaborators((current) => {
      if (current.some((entry) => entry.kind === "linked" && entry.artistId === artist.id)) {
        return current.filter((entry) => !(entry.kind === "linked" && entry.artistId === artist.id));
      }

      return [...current, { kind: "linked", artistId: artist.id, displayName: artist.name }];
    });
  }

  function addPlaceholder() {
    const trimmed = placeholderName.trim();
    if (!trimmed) {
      return;
    }

    const duplicate = draftCollaborators.some(
      (entry) => entry.displayName.localeCompare(trimmed, undefined, { sensitivity: "accent" }) === 0,
    );
    if (duplicate) {
      return;
    }

    setDraftCollaborators((current) => [...current, { kind: "placeholder", displayName: trimmed }]);
    setPlaceholderName("");
  }

  function onConfirm() {
    onCollaboratorsChange(draftCollaborators);
    onOpenChange(false);
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>Featuring artists</DialogTitle>
          <DialogDescription>
            Add platform artists from any organization, or placeholder names for collaborators
            who are not on Amuse yet.
          </DialogDescription>
        </DialogHeader>

        <DialogBody className="gap-4">
          <div className="space-y-2">
            <Label htmlFor="collaborator-search">Search platform artists</Label>
            <div className="relative">
              <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                id="collaborator-search"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder="Search by name, slug, or artist ID…"
                className="pl-9"
                autoFocus
              />
            </div>
            {searchError ? <p className="text-sm text-destructive">{searchError}</p> : null}
            <div className="max-h-56 overflow-y-auto rounded-md border">
              {search.trim().length < 2 ? (
                <p className="p-4 text-sm text-muted-foreground">
                  Type at least 2 characters to search artists across all organizations.
                </p>
              ) : searching ? (
                <p className="p-4 text-sm text-muted-foreground">Searching…</p>
              ) : searchResults.length === 0 ? (
                <p className="p-4 text-sm text-muted-foreground">
                  No artists match &ldquo;{search.trim()}&rdquo;.
                </p>
              ) : (
                <ul className="divide-y">
                  {searchResults.map((artist) => {
                    const selected = selectedLinkedIds.has(artist.id);
                    return (
                      <li key={artist.id}>
                        <button
                          type="button"
                          onClick={() => toggleArtist(artist)}
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
                              /artists/{artist.slug}
                            </span>
                          </span>
                        </button>
                      </li>
                    );
                  })}
                </ul>
              )}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="placeholder-name">Custom name (not on platform)</Label>
            <div className="flex gap-2">
              <Input
                id="placeholder-name"
                value={placeholderName}
                onChange={(event) => setPlaceholderName(event.target.value)}
                placeholder="e.g. Guest vocalist"
                onKeyDown={(event) => {
                  if (event.key === "Enter") {
                    event.preventDefault();
                    addPlaceholder();
                  }
                }}
              />
              <Button type="button" variant="outline" onClick={addPlaceholder}>
                Add
              </Button>
            </div>
          </div>

          <TrackCollaboratorsSummary
            collaborators={draftCollaborators}
            onRemove={(index) =>
              setDraftCollaborators((current) => current.filter((_, entryIndex) => entryIndex !== index))
            }
          />
        </DialogBody>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button type="button" onClick={onConfirm}>
            {draftCollaborators.length > 0
              ? `Use ${draftCollaborators.length} featuring artist${draftCollaborators.length === 1 ? "" : "s"}`
              : "Clear featuring artists"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

export function TrackCollaboratorsSummary({
  collaborators,
  onRemove,
  disabled,
}: {
  collaborators: TrackCollaboratorDraft[];
  onRemove?: (index: number) => void;
  disabled?: boolean;
}) {
  if (collaborators.length === 0) {
    return <p className="text-sm text-muted-foreground">No featuring artists selected.</p>;
  }

  return (
    <ul className="flex flex-col gap-2">
      {collaborators.map((collaborator, index) => (
        <li
          key={`${collaborator.kind}-${collaborator.displayName}-${index}`}
          className="flex items-start justify-between gap-3 rounded-md border px-3 py-2"
        >
          <div className="min-w-0">
            <p className="font-medium">{collaborator.displayName}</p>
            <p className="text-xs text-muted-foreground">
              {collaborator.kind === "placeholder"
                ? "Placeholder · not linked to a platform profile"
                : "Platform artist"}
            </p>
          </div>
          {!disabled && onRemove ? (
            <Button type="button" variant="ghost" size="sm" onClick={() => onRemove(index)}>
              Remove
            </Button>
          ) : null}
        </li>
      ))}
    </ul>
  );
}

export function formatTrackCollaborators(collaborators: ManageTrackCollaboratorResponse[]): string {
  if (collaborators.length === 0) {
    return "";
  }

  return collaborators.map((collaborator) => collaborator.displayName).join(", ");
}
