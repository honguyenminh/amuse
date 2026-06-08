"use client";

import { FormattedCatalogText } from "@amuse/catalog-text";
import { EditReleaseGroupDialog } from "@/components/catalog/EditReleaseGroupDialog";
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
  getArtistReleaseGroupDetail,
  type ManageReleaseGroupDetailResponse,
  type ManageReleaseGroupResponse,
} from "@/lib/api/catalogClient";
import { formatReleaseDateTime } from "@/lib/catalog/releaseDateTime";
import { useAuth } from "@/lib/auth/AuthProvider";
import { canReadCatalogResource, hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import { ChevronRight, Plus } from "lucide-react";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";

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

export default function ArtistReleaseGroupDetailPage() {
  const params = useParams<{ id: string; groupId: string }>();
  const artistId = params.id;
  const groupId = params.groupId;
  const auth = useAuth();
  const orgId = auth.activePersona?.type === "org" ? auth.activePersona.orgId : null;
  const token = getAccessToken();
  const canRead =
    groupId && artistId
      ? canReadCatalogResource(token, "release_group", groupId, { artistId })
      : false;
  const canWrite = hasClaim(token, "write_draft:catalog:all");

  const [group, setGroup] = useState<ManageReleaseGroupDetailResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editOpen, setEditOpen] = useState(false);

  const loadGroup = useCallback(() => {
    if (!artistId || !groupId || !canRead) {
      return;
    }

    setLoading(true);
    setError(null);
    getArtistReleaseGroupDetail(artistId, groupId)
      .then(setGroup)
      .catch((err) =>
        setError(err instanceof Error ? err.message : "Failed to load release group."),
      )
      .finally(() => setLoading(false));
  }, [artistId, groupId, canRead]);

  useEffect(() => {
    loadGroup();
  }, [loadGroup]);

  function onGroupUpdated(updated: ManageReleaseGroupResponse) {
    setGroup((current) =>
      current
        ? {
            ...current,
            title: updated.title,
            description: updated.description,
            updatedAt: updated.updatedAt,
          }
        : current,
    );
  }

  if (!orgId) {
    return (
      <p className="text-sm text-muted-foreground">
        Select an organization workspace to view release groups.
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
            href={`/catalog/artists/${artistId}`}
            className="text-sm text-muted-foreground hover:text-foreground"
          >
            ← {group?.artistName ?? "Artist"}
          </Link>
          <h1 className="mt-1 text-2xl font-semibold tracking-tight">
            {group?.title ?? (loading ? "Loading…" : "Release group")}
          </h1>
          {group ? (
            <p className="text-sm text-muted-foreground">/{group.slug}</p>
          ) : null}
        </div>
        {canWrite && group ? (
          <div className="flex flex-wrap gap-2">
            <Button variant="outline" onClick={() => setEditOpen(true)}>
              Edit group
            </Button>
            <Button
              render={
                <Link
                  href={`/catalog/artists/${artistId}/releases/new?releaseGroupId=${group.id}`}
                />
              }
            >
              <Plus />
              New edition
            </Button>
          </div>
        ) : null}
      </div>

      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      {group ? (
        <>
          <Card>
            <CardHeader>
              <CardTitle>About</CardTitle>
              <CardDescription>
                Updated {formatDate(group.updatedAt)} · {group.releases.length} edition
                {group.releases.length === 1 ? "" : "s"}
              </CardDescription>
            </CardHeader>
            <CardContent className="text-sm text-muted-foreground">
              {group.description ? (
                <FormattedCatalogText
                  text={group.description}
                  codeClassName="rounded bg-muted px-1 font-mono text-sm"
                  linkClassName="underline text-primary"
                  hashtagClassName="underline text-primary"
                />
              ) : (
                <p>No description.</p>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Editions</CardTitle>
              <CardDescription>
                Releases grouped as the same album, EP, or single across formats and years.
              </CardDescription>
            </CardHeader>
            <CardContent>
              {group.releases.length === 0 ? (
                <p className="text-sm text-muted-foreground">No releases in this group yet.</p>
              ) : (
                <ul className="divide-y">
                  {group.releases.map((release) => (
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

          <EditReleaseGroupDialog
            open={editOpen}
            onOpenChange={setEditOpen}
            artistId={artistId}
            group={group}
            onUpdated={onGroupUpdated}
          />

          <ResourceAuditPanel
            tableName={CATALOG_AUDIT_TABLES.releaseGroup}
            targetId={group.id}
          />
        </>
      ) : null}
    </div>
  );
}
