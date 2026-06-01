"use client";

import { EditArtistProfileDialog } from "@/components/catalog/EditArtistProfileDialog";
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
  getArtist,
  type ManageArtistDetailResponse,
} from "@/lib/api/catalogClient";
import { formatReleaseDateTime } from "@/lib/catalog/releaseDateTime";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import { ChevronRight, Plus } from "lucide-react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useEffect, useState } from "react";

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

function formatLifecycle(status: string): string {
  return status.replace(/([A-Z])/g, " $1").replace(/^./, (c) => c.toUpperCase());
}

export default function ArtistDetailPage() {
  const params = useParams<{ id: string }>();
  const artistId = params.id;
  const auth = useAuth();
  const orgId = auth.activePersona?.type === "org" ? auth.activePersona.orgId : null;
  const token = getAccessToken();
  const canRead = hasClaim(token, "read:catalog:all");
  const canWrite = hasClaim(token, "write_draft:catalog:all");

  const [artist, setArtist] = useState<ManageArtistDetailResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editOpen, setEditOpen] = useState(false);

  useEffect(() => {
    if (!orgId || !canRead || !artistId) {
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    getArtist(artistId)
      .then((response) => {
        if (!cancelled) {
          setArtist(response);
        }
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load artist.");
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
  }, [orgId, canRead, artistId]);

  if (!orgId) {
    return (
      <p className="text-sm text-muted-foreground">
        Select an organization workspace to view artist details.
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
          <Link
            href="/catalog"
            className="text-sm text-muted-foreground hover:text-foreground"
          >
            ← Catalog
          </Link>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">
            {artist?.name ?? (loading ? "Loading…" : "Artist")}
          </h1>
          {artist ? (
            <p className="text-sm text-muted-foreground">/{artist.slug}</p>
          ) : null}
        </div>
        {canWrite && artist ? (
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" onClick={() => setEditOpen(true)}>
              Edit profile
            </Button>
            <Button render={<Link href={`/catalog/artists/${artist.id}/releases/new`} />}>
              <Plus />
              New release
            </Button>
          </div>
        ) : null}
      </div>

      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      {artist ? (
        <Card>
          <CardHeader>
            <CardTitle>Profile</CardTitle>
            <CardDescription>
              Added {formatDate(artist.createdAt)} ·{" "}
              {formatLifecycle(artist.visibilityTier)}
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-2 text-sm text-muted-foreground">
            {artist.bio ? <p>{artist.bio}</p> : <p>No bio yet.</p>}
            <p>Country: {artist.countryCode ?? "—"}</p>
            <p>Website: {artist.websiteUrl ?? "—"}</p>
            <p>Aliases: {artist.aliases ?? "—"}</p>
          </CardContent>
        </Card>
      ) : null}

      {artist ? (
        <>
          <EditArtistProfileDialog
            open={editOpen}
            onOpenChange={setEditOpen}
            artist={artist}
            onUpdated={setArtist}
          />
          <ResourceAuditPanel
            tableName={CATALOG_AUDIT_TABLES.artist}
            targetId={artist.id}
          />
        </>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>Release groups</CardTitle>
          <CardDescription>
            {artist
              ? `${artist.releaseGroups.length} group${artist.releaseGroups.length === 1 ? "" : "s"} · editions of the same album or EP`
              : "—"}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {!artist || artist.releaseGroups.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              {loading
                ? "Loading release groups…"
                : "No release groups yet. Groups are created automatically when you add releases."}
            </p>
          ) : (
            <ul className="divide-y">
              {artist.releaseGroups.map((group) => (
                <li key={group.id}>
                  <Link
                    href={`/catalog/artists/${artist.id}/release-groups/${group.id}`}
                    className="flex items-center justify-between gap-4 py-3 transition-colors hover:text-primary"
                  >
                    <div>
                      <p className="font-medium">{group.title}</p>
                      <p className="text-xs text-muted-foreground">
                        /{group.slug} · {group.releaseCount} edition
                        {group.releaseCount === 1 ? "" : "s"}
                      </p>
                    </div>
                    <ChevronRight className="size-4 shrink-0 text-muted-foreground" />
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Releases</CardTitle>
          <CardDescription>
            {artist
              ? `${artist.releases.length} release${artist.releases.length === 1 ? "" : "s"}`
              : "—"}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {!artist || artist.releases.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              {loading ? "Loading releases…" : "No releases yet."}
            </p>
          ) : (
            <ul className="divide-y">
              {artist.releases.map((release) => (
                <li key={release.id}>
                  <Link
                    href={`/catalog/releases/${release.id}`}
                    className="flex items-center justify-between gap-4 py-3 transition-colors hover:text-primary"
                  >
                    <div>
                      <p className="font-medium">{release.title}</p>
                      <p className="text-xs text-muted-foreground">
                        {formatLifecycle(release.releaseType)} ·{" "}
                        {formatLifecycle(release.lifecycleStatus)} ·{" "}
                        {formatReleaseDateTime(release.releaseDate)}
                      </p>
                    </div>
                    <ChevronRight className="size-4 shrink-0 text-muted-foreground" />
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
