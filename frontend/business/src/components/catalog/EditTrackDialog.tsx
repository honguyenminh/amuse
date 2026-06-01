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
  updateTrack,
  type ManageTrackResponse,
} from "@/lib/api/catalogClient";
import { formatDurationMs } from "@/lib/catalog/audioUpload";
import { useEffect, useState } from "react";

type EditTrackDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  track: ManageTrackResponse;
  onUpdated: (track: ManageTrackResponse) => void;
};

export function EditTrackDialog({
  open,
  onOpenChange,
  track,
  onUpdated,
}: EditTrackDialogProps) {
  const [title, setTitle] = useState(track.title);
  const [trackNumber, setTrackNumber] = useState(String(track.trackNumber));
  const [explicitFlag, setExplicitFlag] = useState(track.explicitFlag);
  const [isrc, setIsrc] = useState(track.isrc ?? "");
  const [lyrics, setLyrics] = useState(track.lyrics ?? "");
  const [languageCode, setLanguageCode] = useState(track.languageCode ?? "");
  const [versionTitle, setVersionTitle] = useState(track.versionTitle ?? "");
  const [composerCredits, setComposerCredits] = useState(track.composerCredits ?? "");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setTitle(track.title);
      setTrackNumber(String(track.trackNumber));
      setExplicitFlag(track.explicitFlag);
      setIsrc(track.isrc ?? "");
      setLyrics(track.lyrics ?? "");
      setLanguageCode(track.languageCode ?? "");
      setVersionTitle(track.versionTitle ?? "");
      setComposerCredits(track.composerCredits ?? "");
      setError(null);
    }
  }, [open, track]);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    const trimmedTitle = title.trim();
    const parsedTrackNumber = Number.parseInt(trackNumber, 10);

    if (!trimmedTitle) {
      setError("Title is required.");
      return;
    }
    if (!Number.isFinite(parsedTrackNumber) || parsedTrackNumber < 1) {
      setError("Track number must be at least 1.");
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      const updated = await updateTrack(track.id, {
        title: trimmedTitle,
        trackNumber: parsedTrackNumber,
        explicitFlag,
        isrc: isrc.trim() || null,
        lyrics: lyrics.trim() || null,
        languageCode: languageCode.trim() || null,
        versionTitle: versionTitle.trim() || null,
        composerCredits: composerCredits.trim() || null,
      });
      onUpdated(updated);
      onOpenChange(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update track.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Edit track</DialogTitle>
          <DialogDescription>
            Duration is set from the uploaded audio file and cannot be edited here.
          </DialogDescription>
        </DialogHeader>
        <form className="flex flex-col" onSubmit={onSubmit}>
          <DialogBody>
          <div className="grid gap-2">
            <Label htmlFor="track-title">Title</Label>
            <Input
              id="track-title"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              disabled={submitting}
              required
            />
          </div>
          <div className="grid gap-2 sm:grid-cols-2">
            <div className="grid gap-2">
              <Label htmlFor="track-number">Track number</Label>
              <Input
                id="track-number"
                type="number"
                min={1}
                value={trackNumber}
                onChange={(event) => setTrackNumber(event.target.value)}
                disabled={submitting}
                required
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="track-duration">Duration</Label>
              <p
                id="track-duration"
                className="flex h-9 items-center rounded-md border border-input bg-muted/40 px-3 text-sm text-muted-foreground"
              >
                {formatDurationMs(track.durationMs)}
              </p>
            </div>
          </div>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={explicitFlag}
              onChange={(event) => setExplicitFlag(event.target.checked)}
              disabled={submitting}
            />
            Explicit content
          </label>
          <div className="grid gap-2 sm:grid-cols-2">
            <div className="grid gap-2">
              <Label htmlFor="track-isrc">ISRC</Label>
              <Input
                id="track-isrc"
                value={isrc}
                onChange={(event) => setIsrc(event.target.value)}
                disabled={submitting}
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="track-language">Language code</Label>
              <Input
                id="track-language"
                value={languageCode}
                onChange={(event) => setLanguageCode(event.target.value)}
                disabled={submitting}
                placeholder="en"
              />
            </div>
          </div>
          <div className="grid gap-2">
            <Label htmlFor="track-version">Version title</Label>
            <Input
              id="track-version"
              value={versionTitle}
              onChange={(event) => setVersionTitle(event.target.value)}
              disabled={submitting}
              placeholder="Live, Remix, etc."
            />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="track-composers">Composer credits</Label>
            <Input
              id="track-composers"
              value={composerCredits}
              onChange={(event) => setComposerCredits(event.target.value)}
              disabled={submitting}
            />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="track-lyrics">Lyrics</Label>
            <textarea
              id="track-lyrics"
              className="min-h-24 w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
              value={lyrics}
              onChange={(event) => setLyrics(event.target.value)}
              disabled={submitting}
            />
          </div>
          {error ? <p className="text-sm text-destructive">{error}</p> : null}
          </DialogBody>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting ? "Saving…" : "Save changes"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
