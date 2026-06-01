"use client";

import {
  FeaturingArtistsDialog,
  FeaturingArtistsSummary,
} from "@/components/catalog/FeaturingArtistsDialog";
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
  listArtists,
  listArtistReleaseGroups,
  updateRelease,
  type ManageArtistSummaryResponse,
  type ManageReleaseDetailResponse,
  type ManageReleaseGroupResponse,
  type ReleaseType,
} from "@/lib/api/catalogClient";
import {
  defaultLocalDatetimeInput,
  RELEASE_DATE_TIME_HELPER,
  toLocalDatetimeInput,
  toReleaseDateIso,
} from "@/lib/catalog/releaseDateTime";
import { Users } from "lucide-react";
import { useEffect, useMemo, useState } from "react";

type EditReleaseMetadataDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  release: ManageReleaseDetailResponse;
  onUpdated: (release: ManageReleaseDetailResponse) => void;
};

const releaseTypeOptions: { value: ReleaseType; label: string }[] = [
  { value: "single", label: "Single" },
  { value: "ep", label: "EP" },
  { value: "album", label: "Album" },
  { value: "compilation", label: "Compilation" },
];

export function EditReleaseMetadataDialog({
  open,
  onOpenChange,
  release,
  onUpdated,
}: EditReleaseMetadataDialogProps) {
  const [title, setTitle] = useState(release.title);
  const [releaseType, setReleaseType] = useState<ReleaseType>(release.releaseType);
  const [releaseDate, setReleaseDate] = useState(toLocalDatetimeInput(release.releaseDate));
  const [releaseGroupId, setReleaseGroupId] = useState(release.releaseGroupId ?? "");
  const [description, setDescription] = useState(release.description ?? "");
  const [upc, setUpc] = useState(release.upc ?? "");
  const [primaryGenre, setPrimaryGenre] = useState(release.primaryGenre ?? "");
  const [tags, setTags] = useState(release.tags ?? "");
  const [languageCode, setLanguageCode] = useState(release.languageCode ?? "");
  const [labelName, setLabelName] = useState(release.labelName ?? "");
  const [pLine, setPLine] = useState(release.pLine ?? "");
  const [cLine, setCLine] = useState(release.cLine ?? "");
  const [originalReleaseDate, setOriginalReleaseDate] = useState(
    release.originalReleaseDate ? toLocalDatetimeInput(release.originalReleaseDate) : "",
  );
  const [metadataComplete, setMetadataComplete] = useState(release.metadataComplete);
  const [collaboratorArtistIds, setCollaboratorArtistIds] = useState<string[]>(
    release.collaborators.map((c) => c.artistId),
  );
  const [releaseGroups, setReleaseGroups] = useState<ManageReleaseGroupResponse[]>([]);
  const [rosterArtists, setRosterArtists] = useState<ManageArtistSummaryResponse[]>([]);
  const [featuringDialogOpen, setFeaturingDialogOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setTitle(release.title);
      setReleaseType(release.releaseType);
      setReleaseDate(toLocalDatetimeInput(release.releaseDate));
      setReleaseGroupId(release.releaseGroupId ?? "");
      setDescription(release.description ?? "");
      setUpc(release.upc ?? "");
      setPrimaryGenre(release.primaryGenre ?? "");
      setTags(release.tags ?? "");
      setLanguageCode(release.languageCode ?? "");
      setLabelName(release.labelName ?? "");
      setPLine(release.pLine ?? "");
      setCLine(release.cLine ?? "");
      setOriginalReleaseDate(
        release.originalReleaseDate ? toLocalDatetimeInput(release.originalReleaseDate) : "",
      );
      setMetadataComplete(release.metadataComplete);
      setCollaboratorArtistIds(release.collaborators.map((c) => c.artistId));
      setError(null);

      listArtistReleaseGroups(release.artistId)
        .then((response) => setReleaseGroups(response.items))
        .catch(() => undefined);
      listArtists()
        .then((response) => setRosterArtists(response.items))
        .catch(() => undefined);
    }
  }, [open, release]);

  const selectedCollaborators = useMemo(
    () =>
      rosterArtists.filter((artist) => collaboratorArtistIds.includes(artist.id)),
    [rosterArtists, collaboratorArtistIds],
  );

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    const trimmedTitle = title.trim();
    if (!trimmedTitle) {
      setError("Title is required.");
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      const updated = await updateRelease(release.id, {
        title: trimmedTitle,
        releaseType,
        releaseDate: toReleaseDateIso(releaseDate),
        releaseGroupId: releaseGroupId || null,
        description: description.trim() || null,
        upc: upc.trim() || null,
        primaryGenre: primaryGenre.trim() || null,
        tags: tags.trim() || null,
        languageCode: languageCode.trim() || null,
        labelName: labelName.trim() || null,
        pLine: pLine.trim() || null,
        cLine: cLine.trim() || null,
        originalReleaseDate: originalReleaseDate
          ? toReleaseDateIso(originalReleaseDate)
          : null,
        metadataComplete,
        collaboratorArtistIds,
      });
      onUpdated(updated);
      onOpenChange(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update release.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <>
      <Dialog open={open} onOpenChange={onOpenChange}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>Edit release metadata</DialogTitle>
            <DialogDescription>
              Slug /{release.slug} · {release.lifecycleStatus}
            </DialogDescription>
          </DialogHeader>
          <form className="flex flex-col" onSubmit={onSubmit}>
            <DialogBody>
            <div className="grid gap-2">
              <Label htmlFor="release-title">Title</Label>
              <Input
                id="release-title"
                value={title}
                onChange={(event) => setTitle(event.target.value)}
                disabled={submitting}
                required
              />
            </div>
            <div className="grid gap-2 sm:grid-cols-2 sm:items-start">
              <div className="grid gap-2">
                <Label htmlFor="release-type">Type</Label>
                <select
                  id="release-type"
                  className="h-9 w-full rounded-md border border-input bg-transparent px-3 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
                  value={releaseType}
                  onChange={(event) => setReleaseType(event.target.value as ReleaseType)}
                  disabled={submitting}
                >
                  {releaseTypeOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="grid gap-2">
                <Label htmlFor="release-date">Release date</Label>
                <Input
                  id="release-date"
                  type="datetime-local"
                  className="h-9"
                  value={releaseDate}
                  min={defaultLocalDatetimeInput()}
                  onChange={(event) => setReleaseDate(event.target.value)}
                  disabled={submitting}
                  required
                />
                <p className="text-xs text-muted-foreground">{RELEASE_DATE_TIME_HELPER}</p>
              </div>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="release-group">Release group</Label>
              <select
                id="release-group"
                className="h-9 w-full rounded-md border border-input bg-transparent px-3 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
                value={releaseGroupId}
                onChange={(event) => setReleaseGroupId(event.target.value)}
                disabled={submitting}
              >
                <option value="">None</option>
                {releaseGroups.map((group) => (
                  <option key={group.id} value={group.id}>
                    {group.title}
                  </option>
                ))}
              </select>
            </div>
            <div className="grid gap-2">
              <Label>Featuring artists</Label>
              <FeaturingArtistsSummary
                artists={selectedCollaborators}
                disabled={submitting}
                onRemove={(id) =>
                  setCollaboratorArtistIds((current) =>
                    current.filter((artistId) => artistId !== id),
                  )
                }
              />
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="w-fit"
                onClick={() => setFeaturingDialogOpen(true)}
                disabled={submitting}
              >
                <Users />
                {selectedCollaborators.length > 0
                  ? "Change featuring artists"
                  : "Add featuring artists"}
              </Button>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="release-description">Description</Label>
              <textarea
                id="release-description"
                className="min-h-20 w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
                value={description}
                onChange={(event) => setDescription(event.target.value)}
                disabled={submitting}
              />
            </div>
            <div className="grid gap-2 sm:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="release-upc">UPC</Label>
                <Input
                  id="release-upc"
                  value={upc}
                  onChange={(event) => setUpc(event.target.value)}
                  disabled={submitting}
                />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="release-genre">Primary genre</Label>
                <Input
                  id="release-genre"
                  value={primaryGenre}
                  onChange={(event) => setPrimaryGenre(event.target.value)}
                  disabled={submitting}
                />
              </div>
            </div>
            <div className="grid gap-2 sm:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="release-language">Language code</Label>
                <Input
                  id="release-language"
                  value={languageCode}
                  onChange={(event) => setLanguageCode(event.target.value)}
                  disabled={submitting}
                />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="release-label">Label</Label>
                <Input
                  id="release-label"
                  value={labelName}
                  onChange={(event) => setLabelName(event.target.value)}
                  disabled={submitting}
                />
              </div>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="release-tags">Tags</Label>
              <Input
                id="release-tags"
                value={tags}
                onChange={(event) => setTags(event.target.value)}
                disabled={submitting}
                placeholder="Comma-separated"
              />
            </div>
            <div className="grid gap-2 sm:grid-cols-2">
              <div className="grid gap-2">
                <Label htmlFor="release-pline">P-line</Label>
                <Input
                  id="release-pline"
                  value={pLine}
                  onChange={(event) => setPLine(event.target.value)}
                  disabled={submitting}
                />
              </div>
              <div className="grid gap-2">
                <Label htmlFor="release-cline">C-line</Label>
                <Input
                  id="release-cline"
                  value={cLine}
                  onChange={(event) => setCLine(event.target.value)}
                  disabled={submitting}
                />
              </div>
            </div>
            <div className="grid gap-2">
              <Label htmlFor="release-original-date">Original release date</Label>
              <Input
                id="release-original-date"
                type="datetime-local"
                value={originalReleaseDate}
                onChange={(event) => setOriginalReleaseDate(event.target.value)}
                disabled={submitting}
              />
              <p className="text-xs text-muted-foreground">{RELEASE_DATE_TIME_HELPER}</p>
            </div>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={metadataComplete}
                onChange={(event) => setMetadataComplete(event.target.checked)}
                disabled={submitting}
              />
              Metadata complete
            </label>
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

      <FeaturingArtistsDialog
        open={featuringDialogOpen}
        onOpenChange={setFeaturingDialogOpen}
        artists={rosterArtists.filter((artist) => artist.id !== release.artistId)}
        primaryArtistName={release.artistName}
        selectedIds={collaboratorArtistIds}
        onSelectedIdsChange={setCollaboratorArtistIds}
      />
    </>
  );
}
