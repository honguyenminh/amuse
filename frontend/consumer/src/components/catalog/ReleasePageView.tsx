"use client";

import { CollapsibleFormattedText } from "@/components/catalog/CollapsibleFormattedText";
import { UnverifiedSellerBadge } from "@/components/catalog/UnverifiedSellerBadge";
import { AddToPlaylistButton } from "@/components/discovery/AddToPlaylistButton";
import { SaveToLibraryButton } from "@/components/discovery/SaveToLibraryButton";
import { AppShell } from "@/components/ui/AppShell";
import { PageContent } from "@/components/ui/PageContent";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { IconButton } from "@/components/ui/IconButton";
import { OverflowMenuButton } from "@/components/ui/OverflowMenuButton";
import { PauseIcon, PlayIcon } from "@/components/ui/PlaybackIcons";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { getCatalogRelease, getCatalogReleaseBySlugs } from "@/lib/api/catalogClient";
import { TrackDownloadButton } from "@/components/finance/TrackDownloadButton";
import { acquireFree, checkReleaseOwnership, createCheckoutSession } from "@/lib/api/financeClient";
import type { GetReleaseDetailResponse, ReleaseType, TrackResponse } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/AuthProvider";
import {
  formatPricingLabel,
  isFreeEligible,
  isPaidOnly,
  defaultCheckoutAmountMinor,
} from "@/lib/finance/pricingDisplay";
import { useServerSyncedDetail } from "@/lib/react/useServerSyncedDetail";
import type { ColorSeed } from "@/theme/types";
import {
  catalogArtistPath,
  catalogReleaseGroupPath,
  catalogReleasePathFromEdition,
} from "@/lib/catalog/paths";
import { cn } from "@/lib/cn";
import { usePlayback } from "@/lib/playback/PlaybackContext";
import { formatDuration } from "@/lib/playback/formatDuration";
import { toPlaybackTrack, playableTracksFromRelease } from "@/lib/playback/toPlaybackTrack";
import {
  usePlayableClick,
  useReleasePlayableClick,
} from "@/lib/playback/useAltClickAddToQueue";
import {
  useReleaseContextMenu,
  useTrackContextMenu,
} from "@/lib/playback/usePlaybackContextMenuHandlers";
import { usePageSeed } from "@/theme/ThemeProvider";
import { useCoverArtSeed } from "@/theme/useCoverArtSeed";
import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";

const releaseTypeLabel: Record<ReleaseType, string> = {
  single: "Single",
  ep: "EP",
  album: "Album",
  compilation: "Compilation",
};

type ReleasePageViewProps = {
  artistKey?: string;
  releaseSlug?: string;
  releaseId?: string;
  initialRelease?: GetReleaseDetailResponse;
  initialColorSeed?: ColorSeed | null;
  /** From `?title=` while release detail is still loading on client navigation. */
  titleHint?: string;
};

export function ReleasePageView({
  artistKey,
  releaseSlug,
  releaseId,
  initialRelease,
  initialColorSeed = null,
  titleHint,
}: ReleasePageViewProps) {
  const loadKey = releaseId ?? `${artistKey}/${releaseSlug}`;
  const fetchRelease = useCallback(() => {
    if (releaseId) {
      return getCatalogRelease(releaseId);
    }
    return getCatalogReleaseBySlugs(artistKey!, releaseSlug!);
  }, [releaseId, artistKey, releaseSlug]);
  const { detail: release, pending, error } = useServerSyncedDetail({
    routeKey: loadKey,
    initialDetail: initialRelease,
    fetchDetail: fetchRelease,
  });
  const [ownsRelease, setOwnsRelease] = useState(false);
  const [acquiring, setAcquiring] = useState(false);
  const [checkingOut, setCheckingOut] = useState(false);
  const [acquireError, setAcquireError] = useState<string | null>(null);
  const { isAuthenticated, isReady: authReady } = useAuth();

  useEffect(() => {
    if (!authReady || !isAuthenticated || !release) {
      setOwnsRelease(false);
      return;
    }

    let cancelled = false;
    checkReleaseOwnership(release.id)
      .then((response) => {
        if (!cancelled) setOwnsRelease(response.ownsRelease);
      })
      .catch(() => {
        if (!cancelled) setOwnsRelease(false);
      });

    return () => {
      cancelled = true;
    };
  }, [authReady, isAuthenticated, release?.id]);

  const handleAcquireFree = useCallback(async () => {
    if (!release) return;
    setAcquireError(null);
    setAcquiring(true);
    try {
      await acquireFree({ releaseId: release.id });
      setOwnsRelease(true);
    } catch (err) {
      setAcquireError(err instanceof Error ? err.message : "Could not acquire release.");
    } finally {
      setAcquiring(false);
    }
  }, [release]);

  const handleBuyRelease = useCallback(async () => {
    if (!release?.pricing) return;
    const amountMinor = defaultCheckoutAmountMinor(release.pricing);
    if (amountMinor == null) return;

    setAcquireError(null);
    setCheckingOut(true);
    try {
      const session = await createCheckoutSession({ releaseId: release.id, amountMinor });
      window.location.assign(session.checkoutUrl);
    } catch (err) {
      setAcquireError(err instanceof Error ? err.message : "Could not start checkout.");
      setCheckingOut(false);
    }
  }, [release]);

  const { state, currentTrack, playQueue, toggle } = usePlayback();

  const seed = useCoverArtSeed(release?.coverArtUrl, { initialSeed: initialColorSeed });
  usePageSeed(seed);

  const playableTracks = useMemo(
    () => (release ? playableTracksFromRelease(release) : []),
    [release],
  );

  const onReleaseContextMenu = useReleaseContextMenu(release?.id ?? "");

  const playFromTrack = useCallback(
    (trackId: string) => {
      if (!release || playableTracks.length === 0) return;
      const idx = playableTracks.findIndex((t) => t.id === trackId);
      if (idx < 0) return;
      playQueue(playableTracks, idx);
    },
    [release, playableTracks, playQueue],
  );

  const playAll = useCallback(() => {
    if (playableTracks.length > 0) playQueue(playableTracks, 0);
  }, [playableTracks, playQueue]);

  const isPlayingThisRelease =
    currentTrack !== null && release !== null && currentTrack.releaseId === release.id;

  const { onClick: onReleaseAltClick, queueAddPulsing: releaseQueuePulsing } =
    useReleasePlayableClick({
      releaseId: release?.id ?? "",
      releaseTitle: release?.title ?? "",
      tracks: playableTracks,
    });
  const { onClick: onPlayAllClick } = usePlayableClick({
    tracks: playableTracks,
    hasAudio: playableTracks.length > 0,
    releaseTitle: release?.title,
    onDefaultClick: () => {
      if (isPlayingThisRelease) toggle();
      else playAll();
    },
  });

  const chromeTitle =
    !pending && release ? release.title : (titleHint ?? initialRelease?.title);

  return (
    <AppShell title={chromeTitle} activePath="/release">
      <PageContent gap="4">
        {error && (
          <Card>
            <Text variant="title-large">Could not load release</Text>
            <Text variant="label-medium">{error}</Text>
          </Card>
        )}
        {(pending || !release) && !error ? <ReleaseSkeleton /> : null}
        {!pending && release && (
          <>
            <Card onContextMenu={onReleaseContextMenu}>
              <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
                {release.coverArtUrl ? (
                  <div
                    onClick={onReleaseAltClick}
                    className={cn(
                      "relative shrink-0 rounded-md",
                      releaseQueuePulsing && "queue-add-pulse",
                    )}
                  >
                    {/* eslint-disable-next-line @next/next/no-img-element */}
                    <img
                      src={release.coverArtUrl}
                      alt={release.title}
                      className="aspect-square w-40 rounded-md object-cover"
                    />
                  </div>
                ) : null}
                <div className="flex flex-col gap-1">
                  <Text variant="label-medium" className="text-on-surface-variant">
                    {releaseTypeLabel[release.releaseType]}
                  </Text>
                  <div className="flex flex-wrap items-center gap-2">
                    <Text variant="headline-medium">{release.title}</Text>
                    <UnverifiedSellerBadge trustTier={release.trustTier} />
                  </div>
                  <Link href={catalogArtistPath(release.artistSlug)} className="underline">
                    <Text variant="title-medium">{release.artistName}</Text>
                  </Link>
                  <Text variant="label-medium">
                    {new Date(release.releaseDate).getFullYear()} ·{" "}
                    {release.tracks.length} track{release.tracks.length === 1 ? "" : "s"}
                  </Text>
                  {release.pricing?.isForSale ? (
                    <Text variant="label-medium" className="text-on-surface-variant">
                      {formatPricingLabel(release.pricing)}
                    </Text>
                  ) : null}
                  <div className="mt-2 flex flex-wrap gap-2">
                    <Button
                      type="button"
                      variant="primary"
                      onClick={onPlayAllClick}
                      disabled={playableTracks.length === 0}
                    >
                      {isPlayingThisRelease && state.isPlaying ? "Pause" : "Play"}
                    </Button>
                    <AddToPlaylistButton
                      trackIds={playableTracks.map((track) => track.id)}
                      disabled={playableTracks.length === 0}
                    />
                    <SaveToLibraryButton releaseId={release.id} />
                    {ownsRelease ? (
                      <Text variant="label-medium" className="self-center text-primary">
                        In your purchases
                      </Text>
                    ) : null}
                    {release.pricing?.isForSale ? (
                      ownsRelease ? null : isFreeEligible(release.pricing) ? (
                        <Button
                          type="button"
                          variant="primary"
                          disabled={!isAuthenticated || acquiring}
                          onClick={() => void handleAcquireFree()}
                        >
                          {acquiring ? "Adding…" : "Get for free"}
                        </Button>
                      ) : isPaidOnly(release.pricing) ? (
                        <Button
                          type="button"
                          variant="primary"
                          disabled={!isAuthenticated || checkingOut}
                          onClick={() => void handleBuyRelease()}
                        >
                          {checkingOut ? "Redirecting…" : "Buy"}
                        </Button>
                      ) : null
                    ) : null}
                  </div>
                  {!isAuthenticated && isFreeEligible(release.pricing) ? (
                    <Text variant="label-medium" className="text-on-surface-variant">
                      Sign in to add this release to your purchases.
                    </Text>
                  ) : null}
                  {acquireError ? (
                    <Text variant="label-medium" className="text-error">
                      {acquireError}
                    </Text>
                  ) : null}
                </div>
              </div>
            </Card>

            {release.description ? (
              <Card>
                <Text variant="title-large">About</Text>
                <CollapsibleFormattedText
                  text={release.description}
                  className="text-on-surface-variant"
                />
              </Card>
            ) : null}

            {release.otherEditions.length > 0 ? (
              <Card>
                <Text variant="title-large">
                  {release.releaseGroupTitle ? (
                    release.releaseGroupSlug ? (
                      <>
                        Other editions of{" "}
                        <Link
                          href={catalogReleaseGroupPath(
                            release.artistSlug,
                            release.releaseGroupSlug,
                          )}
                          className="underline"
                        >
                          {release.releaseGroupTitle}
                        </Link>
                      </>
                    ) : (
                      `Other editions of ${release.releaseGroupTitle}`
                    )
                  ) : (
                    "Other editions"
                  )}
                </Text>
                <ul className="mt-2 flex flex-col divide-y divide-outline/40">
                  {release.otherEditions.map((edition) => (
                    <li key={edition.id}>
                      <Link
                        href={catalogReleasePathFromEdition(release.artistSlug, edition)}
                        className="flex items-center gap-3 py-3 transition-colors hover:text-primary"
                      >
                        {edition.coverArtUrl ? (
                          // eslint-disable-next-line @next/next/no-img-element
                          <img
                            src={edition.coverArtUrl}
                            alt={edition.title}
                            className="size-12 rounded object-cover"
                          />
                        ) : (
                          <div className="size-12 rounded bg-surface-container-high" />
                        )}
                        <div className="min-w-0 flex-1">
                          <Text variant="body-medium" className="truncate">
                            {edition.title}
                          </Text>
                          <Text variant="label-medium" className="text-on-surface-variant">
                            {releaseTypeLabel[edition.releaseType]} ·{" "}
                            {new Date(edition.releaseDate).getFullYear()}
                          </Text>
                        </div>
                      </Link>
                    </li>
                  ))}
                </ul>
              </Card>
            ) : null}

            <Card>
              <Text variant="title-large">Tracks</Text>
              <ol className="mt-2 flex flex-col divide-y divide-outline/40">
                {release.tracks.map((track) => {
                  const isCurrent = currentTrack?.id === track.id;
                  return (
                    <TrackRow
                      key={track.id}
                      track={track}
                      release={release}
                      isCurrent={isCurrent}
                      isPlaying={state.isPlaying}
                      onPlay={() => playFromTrack(track.id)}
                      onToggle={toggle}
                      isAuthenticated={isAuthenticated}
                      ownsRelease={ownsRelease}
                    />
                  );
                })}
              </ol>
            </Card>
          </>
        )}
      </PageContent>
    </AppShell>
  );
}

function TrackRow({
  track,
  release,
  isCurrent,
  isPlaying,
  onPlay,
  onToggle,
  isAuthenticated,
  ownsRelease,
}: {
  track: TrackResponse;
  release: GetReleaseDetailResponse;
  isCurrent: boolean;
  isPlaying: boolean;
  onPlay: () => void;
  onToggle: () => void;
  isAuthenticated: boolean;
  ownsRelease: boolean;
}) {
  const [acquiring, setAcquiring] = useState(false);
  const [owned, setOwned] = useState(false);
  const [acquireError, setAcquireError] = useState<string | null>(null);

  useEffect(() => {
    setOwned(ownsRelease);
  }, [ownsRelease]);

  const handleAcquireTrack = async () => {
    setAcquireError(null);
    setAcquiring(true);
    try {
      const response = await acquireFree({ trackId: track.id });
      setOwned(true);
      if (response.releaseEntitlementGranted) {
        // Parent will refresh ownership on next navigation; local track state is enough here.
      }
    } catch (err) {
      setAcquireError(err instanceof Error ? err.message : "Could not acquire track.");
    } finally {
      setAcquiring(false);
    }
  };

  const handleBuyTrack = async () => {
    const amountMinor = defaultCheckoutAmountMinor(track.pricing);
    if (amountMinor == null) return;

    setAcquireError(null);
    setAcquiring(true);
    try {
      const session = await createCheckoutSession({ trackId: track.id, amountMinor });
      window.location.assign(session.checkoutUrl);
    } catch (err) {
      setAcquireError(err instanceof Error ? err.message : "Could not start checkout.");
      setAcquiring(false);
    }
  };

  const showFreeAcquire =
    track.pricing?.isForSale && isFreeEligible(track.pricing) && !owned;
  const showPaidBuy = track.pricing?.isForSale && isPaidOnly(track.pricing) && !owned;
  const playbackTrack = toPlaybackTrack(track, release);
  const { onContextMenu, openMenuAt } = useTrackContextMenu(playbackTrack, track.hasAudio);
  const { onClick, queueAddPulsing } = usePlayableClick({
    tracks: [playbackTrack],
    hasAudio: track.hasAudio,
    onDefaultClick: onPlay,
  });

  return (
    <li
      className={cn(
        "group relative flex items-center justify-between gap-3 py-2",
        isCurrent && "text-primary",
        queueAddPulsing && "queue-add-pulse",
      )}
      onContextMenu={onContextMenu}
    >
      <div className="flex w-8 shrink-0 items-center justify-center">
        {isCurrent ? (
          <IconButton
            label={isPlaying ? "Pause" : "Play"}
            variant="outlined"
            size="sm"
            onClick={onToggle}
          >
            {isPlaying ? <PauseIcon /> : <PlayIcon />}
          </IconButton>
        ) : (
          <span className="w-full text-center tabular-nums opacity-70">
            {track.trackNumber}
          </span>
        )}
      </div>
      <button
        type="button"
        onClick={onClick}
        disabled={!track.hasAudio}
        className="flex min-w-0 flex-1 flex-col text-left disabled:cursor-not-allowed disabled:opacity-50"
      >
        <Text variant="body-medium" className="truncate">
          {track.title}
        </Text>
        <Text variant="label-medium" className="truncate text-on-surface-variant">
          {release.artistName}
        </Text>
      </button>
      <div className="flex items-center gap-2">
        {owned ? (
          <>
            <Text variant="label-medium" className="hidden text-primary sm:inline">
              Owned
            </Text>
            <TrackDownloadButton trackId={track.id} variant="text" />
          </>
        ) : null}
        {showFreeAcquire ? (
          <Button
            type="button"
            variant="outlined"
            disabled={!isAuthenticated || acquiring}
            onClick={(event) => {
              event.stopPropagation();
              void handleAcquireTrack();
            }}
          >
            {acquiring ? "…" : "Free"}
          </Button>
        ) : null}
        {showPaidBuy ? (
          <Button
            type="button"
            variant="outlined"
            disabled={!isAuthenticated || acquiring}
            onClick={(event) => {
              event.stopPropagation();
              void handleBuyTrack();
            }}
          >
            {acquiring ? "…" : "Buy"}
          </Button>
        ) : null}
        <OverflowMenuButton
          label="Track options"
          className="opacity-0 transition-opacity group-hover:opacity-100 group-focus-within:opacity-100"
          onClick={(event) => {
            event.preventDefault();
            event.stopPropagation();
            openMenuAt(event.clientX, event.clientY);
          }}
        />
        <Text variant="label-medium">{formatDuration(track.durationMs)}</Text>
      </div>
      {acquireError ? (
        <Text variant="label-medium" className="absolute right-0 bottom-0 text-error">
          {acquireError}
        </Text>
      ) : null}
    </li>
  );
}

function ReleaseSkeleton() {
  return (
    <div className="flex flex-col gap-4">
      <Card>
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end">
          <Skeleton className="aspect-square w-40 rounded-md" />
          <div className="flex flex-1 flex-col gap-2">
            <Skeleton className="h-4 w-16" />
            <Skeleton className="h-8 w-2/3" />
            <Skeleton className="h-5 w-1/2" />
            <Skeleton className="h-4 w-1/3" />
          </div>
        </div>
      </Card>
      <Card>
        <Skeleton className="mb-3 h-6 w-24" />
        <div className="flex flex-col gap-2">
          {Array.from({ length: 5 }, (_, i) => (
            <Skeleton key={i} className="h-6 w-full" />
          ))}
        </div>
      </Card>
    </div>
  );
}
