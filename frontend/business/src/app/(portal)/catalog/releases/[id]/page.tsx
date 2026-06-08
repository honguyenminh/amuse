"use client";

import { FormattedCatalogText } from "@amuse/catalog-text";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { EditReleaseMetadataDialog } from "@/components/catalog/EditReleaseMetadataDialog";
import { EditTrackDialog } from "@/components/catalog/EditTrackDialog";
import { formatTrackCollaborators } from "@/components/catalog/TrackCollaboratorsEditor";
import { formatPricingSummary } from "@/components/catalog/ReleasePricingPanel";
import { ReleaseCoverArtPanel } from "@/components/catalog/ReleaseCoverArtPanel";
import { ResourceAuditPanel } from "@/components/catalog/ResourceAuditPanel";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  CATALOG_AUDIT_TABLES,
  deleteRelease,
  deleteTrack,
  getRelease,
  getTrackIngestion,
  retryTrackTranscode,
  scheduleRelease,
  cancelScheduledRelease,
  publishRelease,
  type ManageReleaseDetailResponse,
  type ManageTrackResponse,
  type TrackIngestionResponse,
  type TrackLifecycleStatus,
} from "@/lib/api/catalogClient";
import {
  extractEmbeddedCoverArt,
  uploadReleaseCoverArt,
} from "@/lib/catalog/coverArt";
import {
  formatDurationMs,
  formatUploadError,
  formatUploadProgress,
  uploadTrackAudioMaster,
} from "@/lib/catalog/audioUpload";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { ChevronRight } from "lucide-react";
import {
  formatReleaseDateTime,
  isReleaseDateInFuture,
} from "@/lib/catalog/releaseDateTime";
import { useCallback, useEffect, useMemo, useState } from "react";

function formatDuration(ms: number): string {
  return formatDurationMs(ms);
}

function formatLifecycle(status: string): string {
  return status.replace(/([A-Z])/g, " $1").replace(/^./, (c) => c.toUpperCase());
}

function isTrackReady(status: TrackLifecycleStatus): boolean {
  return status === "ready" || status === "published";
}

function isTrackProcessing(status: TrackLifecycleStatus): boolean {
  return status === "processing";
}

function isReleaseDeletable(status: ManageReleaseDetailResponse["lifecycleStatus"]): boolean {
  return (
    status === "draft" ||
    status === "processing" ||
    status === "ready" ||
    status === "scheduled"
  );
}

function formatReleasePricingOverview(release: ManageReleaseDetailResponse): string {
  const tracksForSale = release.tracks.filter((track) => track.pricing.isForSale).length;
  const trackSummary =
    tracksForSale === 0
      ? "No individual track sales"
      : `${tracksForSale} track${tracksForSale === 1 ? "" : "s"} for sale individually`;
  return `${formatPricingSummary(release.pricing)} · ${trackSummary}`;
}

type TrackUploadState = {
  uploading: boolean;
  progressLabel: string | null;
  error: string | null;
};

type TrackRetryState = {
  retrying: boolean;
  error: string | null;
};

export default function ReleaseDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const releaseId = params.id;
  const auth = useAuth();
  const orgId = auth.activePersona?.type === "org" ? auth.activePersona.orgId : null;
  const token = getAccessToken();
  const canRead = hasClaim(token, "read:catalog:all");
  const canWrite = hasClaim(token, "write_draft:catalog:all");
  const canUpload = hasClaim(token, "upload:catalog:all");
  const canPublish = hasClaim(token, "publish_public:catalog:all");
  const canManagePricing = hasClaim(token, "manage:catalog:pricing:all");

  const [release, setRelease] = useState<ManageReleaseDetailResponse | null>(null);
  const [ingestionByTrack, setIngestionByTrack] = useState<
    Record<string, TrackIngestionResponse>
  >({});
  const [uploadState, setUploadState] = useState<Record<string, TrackUploadState>>({});
  const [retryState, setRetryState] = useState<Record<string, TrackRetryState>>({});
  const [loading, setLoading] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [scheduling, setScheduling] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editReleaseOpen, setEditReleaseOpen] = useState(false);
  const [editingTrack, setEditingTrack] = useState<ManageTrackResponse | null>(null);
  const [auditTrackId, setAuditTrackId] = useState<string | null>(null);
  const [deleteTrackTarget, setDeleteTrackTarget] = useState<ManageTrackResponse | null>(null);
  const [deleteReleaseOpen, setDeleteReleaseOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);

  const loadRelease = useCallback(async () => {
    if (!releaseId) {
      return null;
    }
    const data = await getRelease(releaseId);
    setRelease(data);
    return data;
  }, [releaseId]);

  const loadIngestion = useCallback(async (tracks: ManageTrackResponse[]) => {
    const results = await Promise.all(
      tracks.map(async (track) => {
        try {
          const ingestion = await getTrackIngestion(track.id);
          return [track.id, ingestion] as const;
        } catch {
          return null;
        }
      }),
    );

    const next: Record<string, TrackIngestionResponse> = {};
    for (const entry of results) {
      if (entry) {
        next[entry[0]] = entry[1];
      }
    }
    setIngestionByTrack(next);
    return next;
  }, []);

  useEffect(() => {
    if (!orgId || !canRead || !releaseId) {
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    loadRelease()
      .then(async (data) => {
        if (cancelled || !data) {
          return;
        }
        await loadIngestion(data.tracks);
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load release.");
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [orgId, canRead, releaseId, loadRelease, loadIngestion]);

  const shouldPoll = useMemo(() => {
    if (!release) {
      return false;
    }
    return release.tracks.some((track) => {
      const ingestion = ingestionByTrack[track.id];
      const status = ingestion?.lifecycleStatus ?? track.lifecycleStatus;
      return isTrackProcessing(status);
    });
  }, [release, ingestionByTrack]);

  useEffect(() => {
    if (!shouldPoll || !release) {
      return;
    }

    const interval = window.setInterval(() => {
      void loadIngestion(release.tracks).then(async () => {
        const refreshed = await getRelease(releaseId);
        setRelease(refreshed);
      });
    }, 2000);

    return () => window.clearInterval(interval);
  }, [shouldPoll, release, releaseId, loadIngestion]);

  const allTracksReady = useMemo(() => {
    if (!release || release.tracks.length === 0) {
      return false;
    }
    return release.tracks.every((track) => {
      const ingestion = ingestionByTrack[track.id];
      const status = ingestion?.lifecycleStatus ?? track.lifecycleStatus;
      return isTrackReady(status);
    });
  }, [release, ingestionByTrack]);

  async function onUpload(trackId: string, file: File) {
    setUploadState((current) => ({
      ...current,
      [trackId]: { uploading: true, progressLabel: "Starting…", error: null },
    }));
    setError(null);

    try {
      await uploadTrackAudioMaster(trackId, file, {
        onProgress: (progress) => {
          setUploadState((current) => ({
            ...current,
            [trackId]: {
              uploading: true,
              progressLabel: formatUploadProgress(progress),
              error: null,
            },
          }));
        },
      });

      const refreshed = await loadRelease();
      if (refreshed) {
        await loadIngestion(refreshed.tracks);
        if (!refreshed.coverArtUrl && releaseId) {
          const embeddedCover = await extractEmbeddedCoverArt(file);
          if (embeddedCover) {
            try {
              const coverResult = await uploadReleaseCoverArt(releaseId, embeddedCover);
              setRelease((current) =>
                current
                  ? {
                      ...current,
                      coverArtUrl: coverResult.coverArtUrl,
                    }
                  : current,
              );
            } catch {
              // Embedded cover upload is best-effort after a successful track upload.
            }
          }
        }
      }
    } catch (err) {
      setUploadState((current) => ({
        ...current,
        [trackId]: {
          uploading: false,
          progressLabel: null,
          error: formatUploadError(err),
        },
      }));
      return;
    }

    setUploadState((current) => ({
      ...current,
      [trackId]: { uploading: false, progressLabel: null, error: null },
    }));
  }

  async function onPublish() {
    if (!releaseId) {
      return;
    }

    setPublishing(true);
    setError(null);
    try {
      const published = await publishRelease(releaseId);
      setRelease(published);
      await loadIngestion(published.tracks);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to publish release.");
    } finally {
      setPublishing(false);
    }
  }

  async function onRetryTranscode(trackId: string) {
    setRetryState((current) => ({
      ...current,
      [trackId]: { retrying: true, error: null },
    }));
    setError(null);

    try {
      await retryTrackTranscode(trackId);
      const refreshed = await loadRelease();
      if (refreshed) {
        await loadIngestion(refreshed.tracks);
      }
      setRetryState((current) => ({
        ...current,
        [trackId]: { retrying: false, error: null },
      }));
    } catch (err) {
      setRetryState((current) => ({
        ...current,
        [trackId]: {
          retrying: false,
          error: err instanceof Error ? err.message : "Failed to retry transcode job.",
        },
      }));
    }
  }

  async function onSchedule() {
    if (!releaseId) {
      return;
    }

    setScheduling(true);
    setError(null);
    try {
      const scheduled = await scheduleRelease(releaseId);
      setRelease(scheduled);
      await loadIngestion(scheduled.tracks);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to schedule release.");
    } finally {
      setScheduling(false);
    }
  }

  async function onCancelSchedule() {
    if (!releaseId) {
      return;
    }

    setCancelling(true);
    setError(null);
    try {
      const draft = await cancelScheduledRelease(releaseId);
      setRelease(draft);
      await loadIngestion(draft.tracks);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to cancel schedule.");
    } finally {
      setCancelling(false);
    }
  }

  async function onConfirmDeleteTrack() {
    if (!deleteTrackTarget) {
      return;
    }

    setDeleting(true);
    setError(null);
    try {
      await deleteTrack(deleteTrackTarget.id);
      setDeleteTrackTarget(null);
      const refreshed = await loadRelease();
      if (refreshed) {
        await loadIngestion(refreshed.tracks);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete track.");
    } finally {
      setDeleting(false);
    }
  }

  async function onConfirmDeleteRelease() {
    if (!release) {
      return;
    }

    setDeleting(true);
    setError(null);
    try {
      await deleteRelease(release.id);
      setDeleteReleaseOpen(false);
      router.push(`/catalog/artists/${release.artistId}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to delete release.");
      setDeleting(false);
    }
  }

  if (!orgId) {
    return (
      <p className="text-sm text-muted-foreground">
        Select an organization workspace to view release details.
      </p>
    );
  }

  if (!canRead) {
    return (
      <p className="text-sm text-muted-foreground">
        Your current workspace token does not include catalog read permission.
      </p>
    );
  }

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          {release ? (
            <Link
              href={`/catalog/artists/${release.artistId}`}
              className="text-sm text-muted-foreground hover:text-foreground"
            >
              ← {release.artistName}
            </Link>
          ) : null}
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">
            {release?.title ?? (loading ? "Loading…" : "Release")}
          </h1>
          {release ? (
            <p className="text-sm text-muted-foreground">
              {formatLifecycle(release.releaseType)} ·{" "}
              {formatLifecycle(release.lifecycleStatus)} ·{" "}
              {formatReleaseDateTime(release.releaseDate)}
            </p>
          ) : null}
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {canPublish && release && allTracksReady && release.lifecycleStatus === "draft" ? (
            <>
              <Button onClick={() => void onPublish()} disabled={publishing || scheduling}>
                {publishing ? "Publishing…" : "Publish now"}
              </Button>
              <Button
                variant="outline"
                onClick={() => void onSchedule()}
                disabled={
                  scheduling ||
                  publishing ||
                  !isReleaseDateInFuture(release.releaseDate)
                }
                title={
                  !isReleaseDateInFuture(release.releaseDate)
                    ? "Set a future release date (Edit metadata) to schedule automatic publishing."
                    : undefined
                }
              >
                {scheduling ? "Scheduling…" : "Schedule for release date"}
              </Button>
            </>
          ) : null}
          {canWrite && release && isReleaseDeletable(release.lifecycleStatus) ? (
            <Button
              variant="destructive"
              onClick={() => setDeleteReleaseOpen(true)}
              disabled={deleting}
            >
              Delete release
            </Button>
          ) : null}
        </div>
      </div>

      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      {release?.lifecycleStatus === "draft" ? (
        <Card className="border-dashed">
          <CardHeader>
            <CardTitle className="text-base">Draft</CardTitle>
            <CardDescription>
              This release is not public. Upload and process all tracks, then publish
              when you are ready.
            </CardDescription>
          </CardHeader>
        </Card>
      ) : null}

      {release?.lifecycleStatus === "scheduled" ? (
        <Card className="border-dashed">
          <CardHeader className="flex flex-row items-start justify-between gap-4">
            <div>
              <CardTitle className="text-base">Scheduled</CardTitle>
              <CardDescription>
                Goes public on {formatReleaseDateTime(release.releaseDate)}.{" "}
                {!isReleaseDateInFuture(release.releaseDate)
                  ? "Release date has passed — will publish automatically when all tracks are ready."
                  : ""}
              </CardDescription>
            </div>
            {canPublish ? (
              <Button
                variant="outline"
                onClick={() => void onCancelSchedule()}
                disabled={cancelling}
              >
                {cancelling ? "Cancelling…" : "Cancel schedule"}
              </Button>
            ) : null}
          </CardHeader>
        </Card>
      ) : null}

      {release ? (
        <ReleaseCoverArtPanel
          releaseId={release.id}
          coverArtUrl={release.coverArtUrl}
          canUpload={canWrite && release.lifecycleStatus === "draft"}
          onCoverUpdated={(coverArtUrl) =>
            setRelease((current) => (current ? { ...current, coverArtUrl } : current))
          }
        />
      ) : null}

      <Card>
        <CardHeader className="flex flex-row items-start justify-between gap-4">
          <CardTitle>Metadata</CardTitle>
          {canWrite && release?.lifecycleStatus === "draft" ? (
            <Button variant="outline" size="sm" onClick={() => setEditReleaseOpen(true)}>
              Edit
            </Button>
          ) : null}
        </CardHeader>
        <CardContent className="space-y-2 text-sm text-muted-foreground">
          <p>
            Release group:{" "}
            {release?.releaseGroupId && release.releaseGroupTitle ? (
              <Link
                href={`/catalog/artists/${release.artistId}/release-groups/${release.releaseGroupId}`}
                className="text-primary underline-offset-4 hover:underline"
              >
                {release.releaseGroupTitle} (/{release.releaseGroupSlug})
              </Link>
            ) : (
              "None"
            )}
          </p>
          <p>UPC: {release?.upc ?? "—"}</p>
          <p>Genre: {release?.primaryGenre ?? "—"}</p>
          <p>Tags: {release?.tags ?? "—"}</p>
          <p>Language: {release?.languageCode ?? "—"}</p>
          <p>Label: {release?.labelName ?? "—"}</p>
          <p>P-line: {release?.pLine ?? "—"}</p>
          <p>C-line: {release?.cLine ?? "—"}</p>
          <p>
            Original release date:{" "}
            {release?.originalReleaseDate
              ? formatReleaseDateTime(release.originalReleaseDate)
              : "—"}
          </p>
          <p>Metadata complete: {release?.metadataComplete ? "Yes" : "No"}</p>
          {release?.description ? (
            <FormattedCatalogText
              text={release.description}
              codeClassName="rounded bg-muted px-1 font-mono text-sm"
              linkClassName="underline text-primary"
              hashtagClassName="underline text-primary"
            />
          ) : null}
        </CardContent>
      </Card>

      {canManagePricing && release ? (
        <Card>
          <Link
            href={`/catalog/releases/${release.id}/pricing`}
            className="flex items-center justify-between gap-4 px-6 py-4 transition-colors hover:text-primary"
          >
            <div>
              <p className="font-medium">Sales & pricing</p>
              <p className="text-sm text-muted-foreground">
                {formatReleasePricingOverview(release)}
              </p>
            </div>
            <ChevronRight className="size-4 shrink-0 text-muted-foreground" />
          </Link>
        </Card>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>Tracks</CardTitle>
          <CardDescription>
            {release
              ? `${release.tracks.length} track${release.tracks.length === 1 ? "" : "s"}`
              : "—"}
            {shouldPoll ? " · Refreshing ingestion status…" : ""}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {!release || release.tracks.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              {loading ? "Loading tracks…" : "No tracks on this release."}
            </p>
          ) : (
            <ul className="divide-y">
              {release.tracks.map((track) => {
                const ingestion = ingestionByTrack[track.id];
                const lifecycleStatus =
                  ingestion?.lifecycleStatus ?? track.lifecycleStatus;
                const upload = uploadState[track.id];
                const retry = retryState[track.id];
                const jobStatus = ingestion?.jobStatus;
                const jobError = ingestion?.jobLastError;
                const featuring = formatTrackCollaborators(track.collaborators);
                const canRetryTranscode =
                  canUpload &&
                  release.lifecycleStatus === "draft" &&
                  jobStatus === "failed" &&
                  (lifecycleStatus === "processing" || !isTrackReady(lifecycleStatus));

                return (
                  <li key={track.id} className="flex flex-col gap-3 py-4">
                    <div className="flex flex-wrap items-start justify-between gap-2">
                      <div>
                        <p className="font-medium">
                          {track.trackNumber}. {track.title}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {formatDuration(track.durationMs)}
                          {track.explicitFlag ? " · Explicit" : ""} ·{" "}
                          {formatLifecycle(lifecycleStatus)}
                          {featuring ? ` · feat. ${featuring}` : ""}
                          {jobStatus ? ` · Job ${formatLifecycle(jobStatus)}` : ""}
                          {track.isrc ? ` · ISRC ${track.isrc}` : ""}
                          {track.versionTitle ? ` · ${track.versionTitle}` : ""}
                        </p>
                        {track.lyrics ? (
                          <p className="mt-1 line-clamp-2 text-xs text-muted-foreground">
                            {track.lyrics}
                          </p>
                        ) : null}
                        {jobError ? (
                          <p className="mt-1 text-xs text-destructive">{jobError}</p>
                        ) : null}
                        {upload?.error ? (
                          <p className="mt-1 text-xs text-destructive">{upload.error}</p>
                        ) : null}
                        {retry?.error ? (
                          <p className="mt-1 text-xs text-destructive">{retry.error}</p>
                        ) : null}
                        {upload?.progressLabel ? (
                          <p className="mt-1 text-xs text-muted-foreground">
                            {upload.progressLabel}
                          </p>
                        ) : null}
                      </div>

                      <div className="flex flex-wrap gap-2">
                        {canWrite && release.lifecycleStatus === "draft" ? (
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            onClick={() => setEditingTrack(track)}
                          >
                            Edit track
                          </Button>
                        ) : null}
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() =>
                            setAuditTrackId((current) =>
                              current === track.id ? null : track.id,
                            )
                          }
                        >
                          {auditTrackId === track.id ? "Hide audit" : "Audit"}
                        </Button>

                      {canUpload &&
                      release.lifecycleStatus === "draft" &&
                      !isTrackReady(lifecycleStatus) &&
                      !isTrackProcessing(lifecycleStatus) ? (
                        <label className="inline-flex cursor-pointer items-center gap-2">
                          <input
                            type="file"
                            accept="audio/*"
                            className="hidden"
                            disabled={upload?.uploading}
                            onChange={(event) => {
                              const file = event.target.files?.[0];
                              if (file) {
                                void onUpload(track.id, file);
                              }
                              event.target.value = "";
                            }}
                          />
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            disabled={upload?.uploading}
                            onClick={(event) => {
                              const input = event.currentTarget
                                .previousElementSibling as HTMLInputElement | null;
                              input?.click();
                            }}
                          >
                            {upload?.uploading
                              ? upload.progressLabel ?? "Uploading…"
                              : upload?.error
                                ? "Retry upload"
                                : "Upload audio"}
                          </Button>
                        </label>
                      ) : null}

                        {canRetryTranscode ? (
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          disabled={retry?.retrying}
                          onClick={() => void onRetryTranscode(track.id)}
                        >
                          {retry?.retrying ? "Retrying transcode…" : "Retry transcode"}
                        </Button>
                      ) : null}
                        {canWrite && isReleaseDeletable(release.lifecycleStatus) ? (
                          <Button
                            type="button"
                            variant="destructive"
                            size="sm"
                            disabled={deleting}
                            onClick={() => setDeleteTrackTarget(track)}
                          >
                            Delete track
                          </Button>
                        ) : null}
                      </div>
                    </div>
                    {auditTrackId === track.id ? (
                      <ResourceAuditPanel
                        tableName={CATALOG_AUDIT_TABLES.track}
                        targetId={track.id}
                        title={`Track ${track.trackNumber} audit`}
                      />
                    ) : null}
                  </li>
                );
              })}
            </ul>
          )}
        </CardContent>
      </Card>

      {canPublish &&
      release &&
      release.lifecycleStatus === "draft" &&
      !allTracksReady &&
      release.tracks.length > 0 ? (
        <p className="text-sm text-muted-foreground">
          Upload and process all track audio before publishing.
        </p>
      ) : null}

      {canPublish &&
      release &&
      release.lifecycleStatus === "draft" &&
      allTracksReady &&
      !isReleaseDateInFuture(release.releaseDate) ? (
        <p className="text-sm text-muted-foreground">
          To schedule automatic publishing, set a release date in the future using Edit
          metadata (times use your local timezone).
        </p>
      ) : null}

      {release ? (
        <>
          <ResourceAuditPanel
            tableName={CATALOG_AUDIT_TABLES.release}
            targetId={release.id}
          />
          <EditReleaseMetadataDialog
            open={editReleaseOpen}
            onOpenChange={setEditReleaseOpen}
            release={release}
            onUpdated={setRelease}
          />
          {editingTrack ? (
            <EditTrackDialog
              open={Boolean(editingTrack)}
              onOpenChange={(open) => {
                if (!open) {
                  setEditingTrack(null);
                }
              }}
              track={editingTrack}
              primaryArtistId={release.artistId}
              releaseLifecycleStatus={release.lifecycleStatus}
              onUpdated={(updated) => {
                setRelease((current) =>
                  current
                    ? {
                        ...current,
                        tracks: current.tracks
                          .map((entry) => (entry.id === updated.id ? updated : entry))
                          .sort((a, b) => a.trackNumber - b.trackNumber),
                      }
                    : current,
                );
                setEditingTrack(null);
              }}
            />
          ) : null}
          <ConfirmDialog
            open={Boolean(deleteTrackTarget)}
            onOpenChange={(open) => {
              if (!open && !deleting) {
                setDeleteTrackTarget(null);
              }
            }}
            title="Delete track?"
            description={
              deleteTrackTarget
                ? `Remove "${deleteTrackTarget.title}" from this release. Uploaded audio and transcode artifacts will be permanently deleted.`
                : ""
            }
            confirmLabel="Delete track"
            destructive
            busy={deleting}
            onConfirm={() => void onConfirmDeleteTrack()}
          />
          <ConfirmDialog
            open={deleteReleaseOpen}
            onOpenChange={(open) => {
              if (!open && !deleting) {
                setDeleteReleaseOpen(open);
              }
            }}
            title="Delete release?"
            description={
              release
                ? `Permanently delete "${release.title}" and all of its tracks, cover art, and media files. This cannot be undone.`
                : ""
            }
            confirmLabel="Delete release"
            destructive
            busy={deleting}
            onConfirm={() => void onConfirmDeleteRelease()}
          />
        </>
      ) : null}
    </div>
  );
}
