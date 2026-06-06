"use client";

import {
  FeaturingArtistsDialog,
  FeaturingArtistsSummary,
} from "@/components/catalog/FeaturingArtistsDialog";
import {
  ReleaseSlugField,
  releaseSlugReadyForSubmit,
  type ReleaseSlugStatus,
} from "@/components/catalog/ReleaseSlugField";
import { PendingCoverArtPreview } from "@/components/catalog/ReleaseCoverArtPanel";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  createRelease,
  createTrack,
  getArtist,
  listArtists,
  listArtistReleaseGroups,
  type ManageArtistSummaryResponse,
  type ManageReleaseGroupResponse,
  type ReleaseType,
} from "@/lib/api/catalogClient";
import { normalizeSlugInput } from "@/lib/catalog/slug";
import { ApiError } from "@/lib/api/types";
import {
  extractEmbeddedCoverArtFromFiles,
  uploadReleaseCoverArt,
} from "@/lib/catalog/coverArt";
import {
  formatDurationMs,
  formatUploadError,
  formatUploadProgress,
  inferAudioDurationMs,
  inferTrackTitle,
  uploadTrackAudioMaster,
} from "@/lib/catalog/audioUpload";
import {
  defaultLocalDatetimeInput,
  RELEASE_DATE_TIME_HELPER,
  toReleaseDateIso,
} from "@/lib/catalog/releaseDateTime";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import { Trash2, Upload, Users } from "lucide-react";
import Link from "next/link";
import { useParams, useRouter, useSearchParams } from "next/navigation";
import { useEffect, useMemo, useRef, useState } from "react";

type TrackDraft = {
  key: string;
  file: File;
  title: string;
  trackNumber: number;
  durationMs: number | null;
  durationLoading: boolean;
  durationError: string | null;
  explicitFlag: boolean;
};

type TrackUploadState = {
  status: "pending" | "uploading" | "done" | "error";
  progressLabel: string | null;
  error: string | null;
};

const releaseTypeOptions: { value: ReleaseType; label: string }[] = [
  { value: "single", label: "Single" },
  { value: "ep", label: "EP" },
  { value: "album", label: "Album" },
  { value: "compilation", label: "Compilation" },
];

function inferReleaseType(trackCount: number): ReleaseType {
  if (trackCount <= 1) {
    return "single";
  }
  if (trackCount <= 6) {
    return "ep";
  }
  return "album";
}

export default function NewReleasePage() {
  const params = useParams<{ id: string }>();
  const searchParams = useSearchParams();
  const artistId = params.id;
  const router = useRouter();
  const auth = useAuth();
  const orgId = auth.activePersona?.type === "org" ? auth.activePersona.orgId : null;
  const token = getAccessToken();
  const canWrite = hasClaim(token, "write_draft:catalog:all");
  const canUpload = hasClaim(token, "upload:catalog:all");

  const fileInputRef = useRef<HTMLInputElement>(null);

  const [artistName, setArtistName] = useState<string | null>(null);
  const [artistSlug, setArtistSlug] = useState("");
  const [slug, setSlug] = useState("");
  const [slugManuallyEdited, setSlugManuallyEdited] = useState(false);
  const [slugStatus, setSlugStatus] = useState<ReleaseSlugStatus>("idle");
  const [rosterArtists, setRosterArtists] = useState<ManageArtistSummaryResponse[]>([]);
  const [title, setTitle] = useState("");
  const [releaseType, setReleaseType] = useState<ReleaseType>("single");
  const [releaseTypeManual, setReleaseTypeManual] = useState(false);
  const [releaseDate, setReleaseDate] = useState(() => defaultLocalDatetimeInput());
  const [tracks, setTracks] = useState<TrackDraft[]>([]);
  const [releaseGroups, setReleaseGroups] = useState<ManageReleaseGroupResponse[]>([]);
  const [linkToExistingGroup, setLinkToExistingGroup] = useState(
    () => Boolean(searchParams.get("releaseGroupId")),
  );
  const [releaseGroupId, setReleaseGroupId] = useState(
    () => searchParams.get("releaseGroupId") ?? "",
  );
  const [description, setDescription] = useState("");
  const [upc, setUpc] = useState("");
  const [primaryGenre, setPrimaryGenre] = useState("");
  const [languageCode, setLanguageCode] = useState("");
  const [labelName, setLabelName] = useState("");
  const [collaboratorArtistIds, setCollaboratorArtistIds] = useState<string[]>([]);
  const [featuringDialogOpen, setFeaturingDialogOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [uploadPhase, setUploadPhase] = useState(false);
  const [createdReleaseId, setCreatedReleaseId] = useState<string | null>(null);
  const [uploadByTrackKey, setUploadByTrackKey] = useState<
    Record<string, TrackUploadState>
  >({});
  const [trackIdByDraftKey, setTrackIdByDraftKey] = useState<Record<string, string>>({});
  const [pendingCoverFile, setPendingCoverFile] = useState<File | null>(null);
  const [pendingCoverPreviewUrl, setPendingCoverPreviewUrl] = useState<string | null>(null);
  const [pendingCoverSource, setPendingCoverSource] = useState<string | null>(null);
  const [coverSourceManual, setCoverSourceManual] = useState(false);

  useEffect(() => {
    return () => {
      if (pendingCoverPreviewUrl?.startsWith("blob:")) {
        URL.revokeObjectURL(pendingCoverPreviewUrl);
      }
    };
  }, [pendingCoverPreviewUrl]);

  function setPendingCover(file: File, source: string) {
    setPendingCoverFile(file);
    setPendingCoverPreviewUrl((current) => {
      if (current?.startsWith("blob:")) {
        URL.revokeObjectURL(current);
      }
      return URL.createObjectURL(file);
    });
    setPendingCoverSource(source);
  }

  function clearPendingCover() {
    setPendingCoverFile(null);
    setPendingCoverPreviewUrl((current) => {
      if (current?.startsWith("blob:")) {
        URL.revokeObjectURL(current);
      }
      return null;
    });
    setPendingCoverSource(null);
    setCoverSourceManual(false);
  }

  useEffect(() => {
    if (!artistId || !canWrite) {
      return;
    }

    getArtist(artistId)
      .then((artist) => {
        setArtistName(artist.name);
        setArtistSlug(artist.slug);
      })
      .catch(() => undefined);

    listArtists()
      .then((response) => setRosterArtists(response.items))
      .catch(() => undefined);

    listArtistReleaseGroups(artistId)
      .then((response) => setReleaseGroups(response.items))
      .catch(() => undefined);
  }, [artistId, canWrite]);

  useEffect(() => {
    if (!releaseTypeManual) {
      setReleaseType(inferReleaseType(tracks.length));
    }
  }, [tracks.length, releaseTypeManual]);

  function updateTrack(key: string, patch: Partial<TrackDraft>) {
    setTracks((current) =>
      current.map((track) => (track.key === key ? { ...track, ...patch } : track)),
    );
  }

  function removeTrack(key: string) {
    setTracks((current) =>
      current
        .filter((track) => track.key !== key)
        .map((track, index) => ({ ...track, trackNumber: index + 1 })),
    );
  }

  async function onFilesSelected(fileList: FileList | null) {
    if (!fileList || fileList.length === 0) {
      return;
    }

    const files = Array.from(fileList).filter((file) => file.type.startsWith("audio/") || file.name.match(/\.(flac|wav|mp3|aac|ogg|m4a)$/i));
    if (files.length === 0) {
      setError("Select at least one audio file.");
      return;
    }

    setError(null);
    const startNumber = tracks.length + 1;
    const placeholders: TrackDraft[] = files.map((file, index) => ({
      key: crypto.randomUUID(),
      file,
      title: inferTrackTitle(file),
      trackNumber: startNumber + index,
      durationMs: null,
      durationLoading: true,
      durationError: null,
      explicitFlag: false,
    }));

    setTracks((current) => [...current, ...placeholders]);

    for (const placeholder of placeholders) {
      try {
        const durationMs = await inferAudioDurationMs(placeholder.file);
        updateTrack(placeholder.key, { durationMs, durationLoading: false });
      } catch (err) {
        updateTrack(placeholder.key, {
          durationLoading: false,
          durationError:
            err instanceof Error ? err.message : "Could not read audio duration.",
        });
      }
    }

    if (!coverSourceManual) {
      const embedded = await extractEmbeddedCoverArtFromFiles(files);
      if (embedded) {
        setPendingCover(
          embedded.cover,
          `Embedded artwork detected in ${embedded.sourceFileName}.`,
        );
      }
    }
  }

  async function retryUpload(trackKey: string) {
    const trackId = trackIdByDraftKey[trackKey];
    const track = tracks.find((entry) => entry.key === trackKey);
    if (!trackId || !track) {
      return;
    }

    setUploadByTrackKey((current) => ({
      ...current,
      [trackKey]: { status: "uploading", progressLabel: "Retrying…", error: null },
    }));

    try {
      await uploadTrackAudioMaster(trackId, track.file, {
        onProgress: (progress) => {
          setUploadByTrackKey((current) => ({
            ...current,
            [trackKey]: {
              status: "uploading",
              progressLabel: formatUploadProgress(progress),
              error: null,
            },
          }));
        },
      });
      setUploadByTrackKey((current) => ({
        ...current,
        [trackKey]: { status: "done", progressLabel: "Uploaded", error: null },
      }));
    } catch (err) {
      setUploadByTrackKey((current) => ({
        ...current,
        [trackKey]: {
          status: "error",
          progressLabel: null,
          error: formatUploadError(err),
        },
      }));
    }
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    const trimmedTitle = title.trim();
    if (!trimmedTitle) {
      setError("Release title is required.");
      return;
    }

    if (tracks.length === 0) {
      setError("Add at least one audio file.");
      return;
    }

    for (const track of tracks) {
      if (track.durationLoading) {
        setError("Wait for audio duration analysis to finish.");
        return;
      }
      if (track.durationError || !track.durationMs || track.durationMs <= 0) {
        setError(`Track "${track.title}" needs a readable audio duration. Re-add the file.`);
        return;
      }
      if (!track.title.trim()) {
        setError("Every track needs a title.");
        return;
      }
    }

    if (!canUpload) {
      setError("Your workspace token does not include catalog upload permission.");
      return;
    }

    if (!releaseSlugReadyForSubmit(slug, slugStatus, true)) {
      const normalized = normalizeSlugInput(slug);
      if (normalized && slugStatus === "checking") {
        setError("Wait for slug availability check to finish.");
      } else if (normalized && slugStatus === "taken") {
        setError("Choose a different release slug.");
      } else if (normalized) {
        setError("Fix the release slug before saving.");
      }
      return;
    }

    setSubmitting(true);
    setError(null);

    try {
      const normalizedSlug = normalizeSlugInput(slug);
      const release = await createRelease(artistId, {
        title: trimmedTitle,
        slug: normalizedSlug || undefined,
        releaseType,
        releaseDate: toReleaseDateIso(releaseDate),
        releaseGroupId: linkToExistingGroup && releaseGroupId ? releaseGroupId : undefined,
        description: description.trim() || undefined,
        upc: upc.trim() || undefined,
        primaryGenre: primaryGenre.trim() || undefined,
        languageCode: languageCode.trim() || undefined,
        labelName: labelName.trim() || undefined,
        collaboratorArtistIds:
          collaboratorArtistIds.length > 0 ? collaboratorArtistIds : undefined,
      });

      if (pendingCoverFile) {
        await uploadReleaseCoverArt(release.id, pendingCoverFile);
      }

      const idMap: Record<string, string> = {};
      for (const track of tracks) {
        const created = await createTrack(release.id, {
          title: track.title.trim(),
          trackNumber: track.trackNumber,
          durationMs: track.durationMs!,
          explicitFlag: track.explicitFlag,
        });
        idMap[track.key] = created.id;
      }

      setCreatedReleaseId(release.id);
      setTrackIdByDraftKey(idMap);
      setUploadPhase(true);

      const initialUploadState: Record<string, TrackUploadState> = {};
      for (const track of tracks) {
        initialUploadState[track.key] = {
          status: "pending",
          progressLabel: "Waiting…",
          error: null,
        };
      }
      setUploadByTrackKey(initialUploadState);

      const uploadResults = await Promise.all(
        tracks.map(async (track) => {
          const trackId = idMap[track.key];
          setUploadByTrackKey((current) => ({
            ...current,
            [track.key]: {
              status: "uploading",
              progressLabel: "Starting…",
              error: null,
            },
          }));

          try {
            await uploadTrackAudioMaster(trackId, track.file, {
              onProgress: (progress) => {
                setUploadByTrackKey((current) => ({
                  ...current,
                  [track.key]: {
                    status: "uploading",
                    progressLabel: formatUploadProgress(progress),
                    error: null,
                  },
                }));
              },
            });
            setUploadByTrackKey((current) => ({
              ...current,
              [track.key]: { status: "done", progressLabel: "Uploaded", error: null },
            }));
            return true;
          } catch (err) {
            setUploadByTrackKey((current) => ({
              ...current,
              [track.key]: {
                status: "error",
                progressLabel: null,
                error: formatUploadError(err),
              },
            }));
            return false;
          }
        }),
      );

      if (uploadResults.every(Boolean)) {
        router.push(`/catalog/releases/${release.id}`);
      }
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError(err instanceof Error ? err.message : "Could not create release.");
      }
    } finally {
      setSubmitting(false);
    }
  }

  const allUploadsDone =
    uploadPhase &&
    tracks.length > 0 &&
    tracks.every((track) => uploadByTrackKey[track.key]?.status === "done");

  const hasUploadErrors =
    uploadPhase &&
    tracks.some((track) => uploadByTrackKey[track.key]?.status === "error");

  const collaboratorOptions = rosterArtists.filter((artist) => artist.id !== artistId);
  const selectedCollaborators = useMemo(
    () => rosterArtists.filter((artist) => collaboratorArtistIds.includes(artist.id)),
    [rosterArtists, collaboratorArtistIds],
  );

  if (!orgId) {
    return (
      <p className="text-sm text-muted-foreground">
        Select an organization workspace to create a release.
      </p>
    );
  }

  if (!canWrite) {
    return (
      <p className="text-sm text-muted-foreground">
        Your current workspace token does not include catalog write permission.
      </p>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-2xl flex-col gap-4">
      <div>
        <Link
          href={`/catalog/artists/${artistId}`}
          className="text-sm text-muted-foreground hover:text-foreground"
        >
          ← {artistName ?? "Artist"}
        </Link>
        <h1 className="mt-1 text-2xl font-semibold tracking-tight">New release</h1>
        <p className="text-sm text-muted-foreground">
          Add audio files up front. The release stays in draft until you publish it
          after all tracks are uploaded and processed.
        </p>
      </div>

      <Card className="border-dashed">
        <CardHeader>
          <CardTitle className="text-base">Visibility</CardTitle>
          <CardDescription>
            New releases start as drafts and are not public. You can publish once every
            track has finished processing.
          </CardDescription>
        </CardHeader>
      </Card>

      {!uploadPhase ? (
        <form className="flex flex-col gap-4" onSubmit={onSubmit}>
          <Card>
            <CardHeader>
              <CardTitle>Release details</CardTitle>
              <CardDescription>Metadata for the draft release.</CardDescription>
            </CardHeader>
            <CardContent className="flex flex-col gap-4">
              <div className="grid gap-2">
                <Label htmlFor="title">Title</Label>
                <Input
                  id="title"
                  value={title}
                  onChange={(event) => setTitle(event.target.value)}
                  placeholder="Release title"
                  required
                  disabled={submitting}
                />
              </div>

              {artistSlug ? (
                <ReleaseSlugField
                  artistId={artistId}
                  artistSlug={artistSlug}
                  title={title}
                  slug={slug}
                  onSlugChange={setSlug}
                  slugManuallyEdited={slugManuallyEdited}
                  onSlugManuallyEditedChange={setSlugManuallyEdited}
                  onSlugStatusChange={setSlugStatus}
                  disabled={submitting}
                  optional
                />
              ) : null}

              <div className="grid gap-2 sm:grid-cols-2 sm:items-start">
                <div className="grid gap-2">
                  <Label htmlFor="releaseType">Type</Label>
                  <select
                    id="releaseType"
                    value={releaseType}
                    onChange={(event) => {
                      setReleaseTypeManual(true);
                      setReleaseType(event.target.value as ReleaseType);
                    }}
                    className="h-9 w-full rounded-md border border-input bg-background px-3 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
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
                  <Label htmlFor="releaseDate">Release date</Label>
                  <Input
                    id="releaseDate"
                    type="datetime-local"
                    className="h-9"
                    value={releaseDate}
                    min={defaultLocalDatetimeInput()}
                    onChange={(event) => setReleaseDate(event.target.value)}
                    required
                    disabled={submitting}
                  />
                  <p className="text-xs text-muted-foreground">{RELEASE_DATE_TIME_HELPER}</p>
                  <p className="text-xs text-muted-foreground">
                    If you schedule publishing, the release will go public automatically at this
                    time. You can also publish manually once ready.
                  </p>
                </div>
              </div>

              <div className="grid gap-2">
                <Label>Release group</Label>
                <p className="text-xs text-muted-foreground">
                  A release group is created automatically from the release title unless you
                  link this release to an existing group (for remasters, reissues, or other
                  editions of the same album).
                </p>
                <label className="inline-flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={linkToExistingGroup}
                    onChange={(event) => {
                      setLinkToExistingGroup(event.target.checked);
                      if (!event.target.checked) {
                        setReleaseGroupId("");
                      }
                    }}
                    disabled={submitting}
                  />
                  Link to an existing release group
                </label>
                {linkToExistingGroup ? (
                  <select
                    id="releaseGroupId"
                    value={releaseGroupId}
                    onChange={(event) => setReleaseGroupId(event.target.value)}
                    className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                    disabled={submitting}
                    required
                  >
                    <option value="">Select a release group</option>
                    {releaseGroups.map((group) => (
                      <option key={group.id} value={group.id}>
                        {group.title}
                      </option>
                    ))}
                  </select>
                ) : null}
              </div>

              <div className="grid gap-2">
                <Label htmlFor="description">Description (optional)</Label>
                <textarea
                  id="description"
                  value={description}
                  onChange={(event) => setDescription(event.target.value)}
                  className="min-h-24 rounded-md border border-input bg-background px-3 py-2 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                  disabled={submitting}
                />
              </div>

              <div className="grid gap-2 sm:grid-cols-2">
                <div className="grid gap-2">
                  <Label htmlFor="upc">UPC/EAN (optional)</Label>
                  <Input
                    id="upc"
                    value={upc}
                    onChange={(event) => setUpc(event.target.value)}
                    placeholder="e.g. 123456789012"
                    disabled={submitting}
                  />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="primaryGenre">Primary genre (optional)</Label>
                  <Input
                    id="primaryGenre"
                    value={primaryGenre}
                    onChange={(event) => setPrimaryGenre(event.target.value)}
                    placeholder="e.g. Indie Pop"
                    disabled={submitting}
                  />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="languageCode">Language code (optional)</Label>
                  <Input
                    id="languageCode"
                    value={languageCode}
                    onChange={(event) => setLanguageCode(event.target.value)}
                    placeholder="e.g. en"
                    disabled={submitting}
                  />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="labelName">Label display name (optional)</Label>
                  <Input
                    id="labelName"
                    value={labelName}
                    onChange={(event) => setLabelName(event.target.value)}
                    placeholder="Label / imprint"
                    disabled={submitting}
                  />
                </div>
              </div>

              <div className="grid gap-2">
                <Label>Featuring artists (optional)</Label>
                <FeaturingArtistsSummary
                  artists={selectedCollaborators}
                  disabled={submitting}
                  onRemove={(id) =>
                    setCollaboratorArtistIds((current) =>
                      current.filter((artistId) => artistId !== id),
                    )
                  }
                />
                {collaboratorOptions.length > 0 ? (
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="w-fit"
                    disabled={submitting}
                    onClick={() => setFeaturingDialogOpen(true)}
                  >
                    <Users />
                    {selectedCollaborators.length > 0
                      ? "Edit featuring artists"
                      : "Add featuring artists"}
                  </Button>
                ) : (
                  <p className="text-xs text-muted-foreground">
                    Add more artists to your roster to feature collaborators on this release.
                  </p>
                )}
              </div>
            </CardContent>
          </Card>

          <PendingCoverArtPreview
            previewUrl={pendingCoverPreviewUrl}
            sourceLabel={pendingCoverSource}
            disabled={submitting}
            onSelectFile={(file) => {
              setCoverSourceManual(true);
              setPendingCover(file, "Image file selected.");
            }}
            onClear={clearPendingCover}
          />

          <Card>
            <CardHeader className="flex flex-row items-start justify-between gap-4">
              <div>
                <CardTitle>Tracks</CardTitle>
                <CardDescription>
                  Select audio files. Duration is read from each file automatically.
                </CardDescription>
              </div>
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={submitting}
                onClick={() => fileInputRef.current?.click()}
              >
                <Upload />
                Add audio
              </Button>
              <input
                ref={fileInputRef}
                type="file"
                accept="audio/*,.flac,.wav,.mp3,.aac,.ogg,.m4a"
                multiple
                className="hidden"
                onChange={(event) => {
                  void onFilesSelected(event.target.files);
                  event.target.value = "";
                }}
              />
            </CardHeader>
            <CardContent className="flex flex-col gap-4">
              {tracks.length === 0 ? (
                <p className="text-sm text-muted-foreground">
                  No tracks yet. Add one or more audio files to begin.
                </p>
              ) : (
                tracks.map((track) => (
                  <div
                    key={track.key}
                    className="flex flex-col gap-3 rounded-lg border p-4"
                  >
                    <div className="flex items-center justify-between gap-2">
                      <span className="text-sm font-medium">
                        Track {track.trackNumber}
                      </span>
                      {!submitting ? (
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => removeTrack(track.key)}
                        >
                          <Trash2 />
                          Remove
                        </Button>
                      ) : null}
                    </div>

                    <p className="truncate text-xs text-muted-foreground">
                      {track.file.name}
                    </p>

                    <div className="grid gap-2">
                      <Label htmlFor={`title-${track.key}`}>Title</Label>
                      <Input
                        id={`title-${track.key}`}
                        value={track.title}
                        onChange={(event) =>
                          updateTrack(track.key, { title: event.target.value })
                        }
                        placeholder="Track title"
                        disabled={submitting}
                      />
                    </div>

                    <p className="text-xs text-muted-foreground">
                      Duration:{" "}
                      {track.durationLoading
                        ? "Analyzing…"
                        : track.durationError
                          ? track.durationError
                          : track.durationMs
                            ? formatDurationMs(track.durationMs)
                            : "—"}
                    </p>

                    <div className="flex items-center gap-2">
                      <input
                        id={`explicit-${track.key}`}
                        type="checkbox"
                        checked={track.explicitFlag}
                        onChange={(event) =>
                          updateTrack(track.key, { explicitFlag: event.target.checked })
                        }
                        disabled={submitting}
                        className="size-4 rounded border border-input accent-primary"
                      />
                      <Label htmlFor={`explicit-${track.key}`} className="cursor-pointer">
                        Explicit content
                      </Label>
                    </div>
                  </div>
                ))
              )}
            </CardContent>
          </Card>

          {error ? <p className="text-sm text-destructive">{error}</p> : null}

          <div className="flex flex-wrap gap-2">
            <Button type="submit" disabled={submitting || tracks.length === 0}>
              {submitting ? "Creating & uploading…" : "Save draft & upload"}
            </Button>
            <Button
              type="button"
              variant="outline"
              disabled={submitting}
              render={<Link href={`/catalog/artists/${artistId}`} />}
            >
              Cancel
            </Button>
          </div>
        </form>
      ) : (
        <Card>
          <CardHeader>
            <CardTitle>Uploading tracks</CardTitle>
            <CardDescription>
              Keep this page open until uploads finish. Failed uploads can be retried.
            </CardDescription>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            {tracks.map((track) => {
              const upload = uploadByTrackKey[track.key];
              return (
                <div key={track.key} className="rounded-lg border p-4">
                  <p className="font-medium">
                    {track.trackNumber}. {track.title}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {upload?.status === "done"
                      ? "Uploaded"
                      : upload?.status === "error"
                        ? upload.error
                        : upload?.progressLabel ?? "Pending"}
                  </p>
                  {upload?.status === "error" ? (
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      className="mt-2"
                      onClick={() => void retryUpload(track.key)}
                    >
                      Retry upload
                    </Button>
                  ) : null}
                </div>
              );
            })}

            {allUploadsDone && createdReleaseId ? (
              <Button render={<Link href={`/catalog/releases/${createdReleaseId}`} />}>
                Continue to release
              </Button>
            ) : null}

            {hasUploadErrors && createdReleaseId ? (
              <Button
                variant="outline"
                render={<Link href={`/catalog/releases/${createdReleaseId}`} />}
              >
                Open release (retry remaining uploads there)
              </Button>
            ) : null}
          </CardContent>
        </Card>
      )}

      <FeaturingArtistsDialog
        open={featuringDialogOpen}
        onOpenChange={setFeaturingDialogOpen}
        artists={collaboratorOptions}
        primaryArtistName={artistName ?? "this artist"}
        selectedIds={collaboratorArtistIds}
        onSelectedIdsChange={setCollaboratorArtistIds}
      />
    </div>
  );
}
