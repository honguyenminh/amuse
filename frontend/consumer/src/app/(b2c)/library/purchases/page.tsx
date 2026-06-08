"use client";

import { TrackDownloadButton } from "@/components/finance/TrackDownloadButton";
import { ReleaseTile } from "@/components/playback/ReleaseTile";
import { Card } from "@/components/ui/Card";
import { LibraryCardGrid, LibraryCardGridItem } from "@/components/ui/LibraryCardGrid";
import { Skeleton } from "@/components/ui/Skeleton";
import { Text } from "@/components/ui/Text";
import { listMyPurchases } from "@/lib/api/financeClient";
import type { PurchasedReleaseRow, PurchasedTrackRow } from "@/lib/api/financeTypes";
import { catalogReleasePath } from "@/lib/catalog/paths";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";

export default function LibraryPurchasesPage() {
  const searchParams = useSearchParams();
  const checkoutReturn = searchParams.get("checkout");
  const [tracks, setTracks] = useState<PurchasedTrackRow[]>([]);
  const [releases, setReleases] = useState<PurchasedReleaseRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [checkoutNotice, setCheckoutNotice] = useState<string | null>(null);

  const loadPurchases = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await listMyPurchases();
      setTracks(response.tracks);
      setReleases(response.releases);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load purchases.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadPurchases();
  }, [loadPurchases]);

  useEffect(() => {
    if (checkoutReturn !== "success") {
      if (checkoutReturn === "cancelled") {
        setCheckoutNotice("Checkout was cancelled.");
      }
      return;
    }

    setCheckoutNotice("Payment received — updating your library…");
    let attempts = 0;
    const interval = window.setInterval(() => {
      attempts += 1;
      void listMyPurchases()
        .then((response) => {
          setTracks(response.tracks);
          setReleases(response.releases);
          if (response.tracks.length > 0 || response.releases.length > 0 || attempts >= 8) {
            setCheckoutNotice(
              response.tracks.length > 0 || response.releases.length > 0
                ? "Purchase added to your library."
                : "Payment is processing — refresh shortly if your purchase is missing.",
            );
            window.clearInterval(interval);
          }
        })
        .catch(() => {
          if (attempts >= 8) window.clearInterval(interval);
        });
    }, 2000);

    return () => window.clearInterval(interval);
  }, [checkoutReturn]);

  const empty = !loading && !error && tracks.length === 0 && releases.length === 0;

  return (
    <section className="flex flex-col gap-6">
      <Text variant="title-large">Purchases</Text>

      {checkoutNotice ? (
        <Card>
          <Text variant="body-medium">{checkoutNotice}</Text>
        </Card>
      ) : null}

      {loading ? (
        <LibraryCardGrid>
          {Array.from({ length: 6 }, (_, i) => (
            <LibraryCardGridItem key={i}>
              <Skeleton className="aspect-square w-full rounded-md" />
            </LibraryCardGridItem>
          ))}
        </LibraryCardGrid>
      ) : null}

      {error ? (
        <Card>
          <Text variant="label-medium">{error}</Text>
        </Card>
      ) : null}

      {empty ? (
        <Card>
          <Text variant="body-medium" className="text-on-surface-variant">
            Tracks and releases you buy will appear here forever.
          </Text>
        </Card>
      ) : null}

      {!loading && releases.length > 0 ? (
        <div className="flex flex-col gap-3">
          <Text variant="title-medium">Releases</Text>
          <LibraryCardGrid>
            {releases.map((release) => (
              <LibraryCardGridItem key={release.purchaseId}>
                <ReleaseTile
                  release={{
                    id: release.releaseId,
                    slug: release.releaseSlug,
                    title: release.releaseTitle,
                    artistSlug: release.artistSlug,
                    coverArtUrl: release.coverArtUrl,
                  }}
                  subtitle={release.artistName}
                />
              </LibraryCardGridItem>
            ))}
          </LibraryCardGrid>
        </div>
      ) : null}

      {!loading && tracks.length > 0 ? (
        <div className="flex flex-col gap-3">
          <Text variant="title-medium">Tracks</Text>
          <Card>
            <ul className="flex flex-col divide-y divide-outline/40">
              {tracks.map((track) => (
                <li
                  key={track.purchaseId}
                  className="flex flex-wrap items-center justify-between gap-3 py-3"
                >
                  <Link
                    href={catalogReleasePath(track.artistSlug, track.releaseSlug)}
                    className="flex min-w-0 flex-1 flex-col gap-0.5 transition-colors hover:text-primary"
                  >
                    <Text variant="body-medium">{track.trackTitle}</Text>
                    <Text variant="label-medium" className="text-on-surface-variant">
                      {track.artistName} · {track.releaseTitle}
                    </Text>
                  </Link>
                  <TrackDownloadButton trackId={track.trackId} />
                </li>
              ))}
            </ul>
          </Card>
        </div>
      ) : null}
    </section>
  );
}
