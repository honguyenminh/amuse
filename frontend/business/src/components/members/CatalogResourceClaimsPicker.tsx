"use client";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  buildCatalogReadClaim,
  catalogResourceKindLabel,
  extractCatalogResourceClaims,
  type CatalogResourceKind,
  type CatalogResourceRef,
} from "@/lib/members/catalogResourceClaims";
import {
  getRelease,
  listArtistReleaseGroups,
  listArtists,
  listReleases,
  type ManageArtistSummaryResponse,
  type ManageReleaseGroupResponse,
  type ManageReleaseSummaryResponse,
  type ManageTrackResponse,
} from "@/lib/api/catalogClient";
import { cn } from "@/lib/utils";
import { Check, Loader2, Search, X } from "lucide-react";
import { useEffect, useMemo, useState } from "react";

type CatalogResourceTab = CatalogResourceKind;

type CatalogResourceClaimsPickerProps = {
  disabled: boolean;
  selectedClaims: string[];
  onSelectedClaimsChange: (claims: string[]) => void;
};

type SelectableItem = {
  kind: CatalogResourceKind;
  id: string;
  label: string;
  subtitle?: string;
};

const TABS: { id: CatalogResourceTab; label: string }[] = [
  { id: "artist", label: "Artists" },
  { id: "release", label: "Releases" },
  { id: "track", label: "Tracks" },
  { id: "release_group", label: "Release groups" },
];

function claimKey(kind: CatalogResourceKind, id: string): string {
  return buildCatalogReadClaim(kind, id);
}

function matchesSearch(item: SelectableItem, query: string): boolean {
  const haystack = `${item.label} ${item.subtitle ?? ""}`.toLowerCase();
  return haystack.includes(query.toLowerCase());
}

export function CatalogResourceClaimsPicker({
  disabled,
  selectedClaims,
  onSelectedClaimsChange,
}: CatalogResourceClaimsPickerProps) {
  const [activeTab, setActiveTab] = useState<CatalogResourceTab>("artist");
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [artists, setArtists] = useState<ManageArtistSummaryResponse[]>([]);
  const [releases, setReleases] = useState<ManageReleaseSummaryResponse[]>([]);
  const [tracksByRelease, setTracksByRelease] = useState<Record<string, ManageTrackResponse[]>>({});
  const [loadingReleaseId, setLoadingReleaseId] = useState<string | null>(null);
  const [releaseGroupsArtistId, setReleaseGroupsArtistId] = useState<string | null>(null);
  const [releaseGroups, setReleaseGroups] = useState<ManageReleaseGroupResponse[]>([]);
  const [labelByKey, setLabelByKey] = useState<Record<string, string>>({});

  const selectedResources = useMemo(
    () => extractCatalogResourceClaims(selectedClaims),
    [selectedClaims],
  );

  const selectedKeys = useMemo(
    () => new Set(selectedResources.map((resource) => `${resource.kind}:${resource.id}`)),
    [selectedResources],
  );

  useEffect(() => {
    if (disabled) {
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    void Promise.all([listArtists(), listReleases()])
      .then(([artistResponse, releaseResponse]) => {
        if (cancelled) {
          return;
        }

        setArtists(artistResponse.items);
        setReleases(releaseResponse.items);
        setReleaseGroupsArtistId((current) => current ?? artistResponse.items[0]?.id ?? null);
      })
      .catch((err) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "Failed to load catalog resources.");
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
  }, [disabled]);

  useEffect(() => {
    if (disabled || !releaseGroupsArtistId) {
      setReleaseGroups([]);
      return;
    }

    let cancelled = false;
    void listArtistReleaseGroups(releaseGroupsArtistId)
      .then((response) => {
        if (!cancelled) {
          setReleaseGroups(response.items);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setReleaseGroups([]);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [disabled, releaseGroupsArtistId]);

  const artistItems: SelectableItem[] = useMemo(
    () =>
      artists.map((artist) => ({
        kind: "artist" as const,
        id: artist.id,
        label: artist.name,
        subtitle: artist.slug,
      })),
    [artists],
  );

  const releaseItems: SelectableItem[] = useMemo(
    () =>
      releases.map((release) => ({
        kind: "release" as const,
        id: release.id,
        label: release.title,
        subtitle: release.artistName,
      })),
    [releases],
  );

  const trackItems: SelectableItem[] = useMemo(
    () =>
      Object.entries(tracksByRelease).flatMap(([releaseId, tracks]) => {
        const release = releases.find((entry) => entry.id === releaseId);
        return tracks.map((track) => ({
          kind: "track" as const,
          id: track.id,
          label: track.title,
          subtitle: release?.title,
        }));
      }),
    [releases, tracksByRelease],
  );

  const releaseGroupItems: SelectableItem[] = useMemo(
    () =>
      releaseGroups.map((group) => ({
        kind: "release_group" as const,
        id: group.id,
        label: group.title,
        subtitle: artists.find((artist) => artist.id === group.artistId)?.name,
      })),
    [artists, releaseGroups],
  );

  const visibleItems = useMemo(() => {
    const query = search.trim();
    const source =
      activeTab === "artist"
        ? artistItems
        : activeTab === "release"
          ? releaseItems
          : activeTab === "track"
            ? trackItems
            : releaseGroupItems;

    if (!query) {
      return source;
    }

    return source.filter((item) => matchesSearch(item, query));
  }, [activeTab, artistItems, releaseGroupItems, releaseItems, search, trackItems]);

  function toggleItem(item: SelectableItem) {
    const key = `${item.kind}:${item.id}`;
    const claim = claimKey(item.kind, item.id);
    setLabelByKey((current) => ({ ...current, [key]: item.label }));

    if (selectedKeys.has(key)) {
      onSelectedClaimsChange(selectedClaims.filter((entry) => entry !== claim));
      return;
    }

    onSelectedClaimsChange([...selectedClaims, claim]);
  }

  async function loadTracksForRelease(releaseId: string) {
    if (tracksByRelease[releaseId]) {
      return;
    }

    setLoadingReleaseId(releaseId);
    setError(null);
    try {
      const release = await getRelease(releaseId);
      setTracksByRelease((current) => ({
        ...current,
        [releaseId]: release.tracks,
      }));
      const labels: Record<string, string> = {};
      for (const track of release.tracks) {
        labels[`track:${track.id}`] = track.title;
      }
      setLabelByKey((current) => ({ ...current, ...labels }));
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load tracks.");
    } finally {
      setLoadingReleaseId(null);
    }
  }

  const selectedChips: CatalogResourceRef[] = selectedResources.map((resource) => {
    const key = `${resource.kind}:${resource.id}`;
    return {
      kind: resource.kind,
      id: resource.id,
      label: labelByKey[key] ?? `${catalogResourceKindLabel(resource.kind)} ${resource.id.slice(0, 8)}…`,
    };
  });

  if (disabled) {
    return (
      <p className="text-sm text-muted-foreground">
        This member has full catalog read access. Remove the &quot;View catalog&quot; permission
        above to assign individual artists, releases, tracks, or release groups.
      </p>
    );
  }

  return (
    <div className="space-y-4">
      {selectedChips.length > 0 ? (
        <div className="flex flex-wrap gap-2">
          {selectedChips.map((chip) => (
            <button
              key={`${chip.kind}:${chip.id}`}
              type="button"
              className="inline-flex items-center gap-1 rounded-full border bg-muted/40 px-2.5 py-1 text-xs"
              onClick={() =>
                onSelectedClaimsChange(
                  selectedClaims.filter(
                    (claim) => claim !== buildCatalogReadClaim(chip.kind, chip.id),
                  ),
                )
              }
            >
              <span>
                {catalogResourceKindLabel(chip.kind)}: {chip.label}
              </span>
              <X className="size-3" />
            </button>
          ))}
        </div>
      ) : null}

      <div className="flex flex-wrap gap-2">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            type="button"
            className={cn(
              "rounded-md border px-3 py-1.5 text-xs font-medium",
              activeTab === tab.id
                ? "border-primary bg-primary/10 text-primary"
                : "border-border text-muted-foreground hover:bg-muted/40",
            )}
            onClick={() => {
              setActiveTab(tab.id);
              setSearch("");
            }}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {activeTab === "release_group" ? (
        <div className="grid gap-2">
          <Label htmlFor="release-group-artist">Artist</Label>
          <select
            id="release-group-artist"
            className="border-input bg-background h-9 w-full rounded-md border px-3 text-sm"
            value={releaseGroupsArtistId ?? ""}
            onChange={(event) => setReleaseGroupsArtistId(event.target.value || null)}
          >
            {artists.map((artist) => (
              <option key={artist.id} value={artist.id}>
                {artist.name}
              </option>
            ))}
          </select>
        </div>
      ) : null}

      <div className="relative">
        <Search className="pointer-events-none absolute top-2.5 left-3 size-4 text-muted-foreground" />
        <Input
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder={`Search ${TABS.find((tab) => tab.id === activeTab)?.label.toLowerCase() ?? "resources"}…`}
          className="pl-9"
        />
      </div>

      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      {loading ? (
        <div className="flex items-center gap-2 py-6 text-sm text-muted-foreground">
          <Loader2 className="size-4 animate-spin" />
          Loading catalog…
        </div>
      ) : (
        <div className="max-h-56 space-y-1 overflow-y-auto rounded-md border p-1">
          {activeTab === "track" && releaseItems.length > 0 ? (
            <div className="space-y-2 border-b px-2 py-2">
              <p className="text-xs text-muted-foreground">Load tracks from a release</p>
              <div className="flex flex-wrap gap-2">
                {releaseItems.slice(0, 8).map((release) => (
                  <button
                    key={release.id}
                    type="button"
                    className="rounded-md border px-2 py-1 text-xs hover:bg-muted/40"
                    disabled={loadingReleaseId === release.id}
                    onClick={() => void loadTracksForRelease(release.id)}
                  >
                    {loadingReleaseId === release.id ? "Loading…" : release.label}
                  </button>
                ))}
              </div>
            </div>
          ) : null}

          {visibleItems.length === 0 ? (
            <p className="px-3 py-6 text-center text-sm text-muted-foreground">
              {activeTab === "track"
                ? "Load tracks from a release to select them."
                : "No matching resources."}
            </p>
          ) : (
            visibleItems.map((item) => {
              const key = `${item.kind}:${item.id}`;
              const selected = selectedKeys.has(key);
              return (
                <button
                  key={key}
                  type="button"
                  className={cn(
                    "flex w-full items-start gap-2 rounded-md px-3 py-2 text-left text-sm hover:bg-muted/40",
                    selected && "bg-primary/5",
                  )}
                  onClick={() => toggleItem(item)}
                >
                  <span
                    className={cn(
                      "mt-0.5 flex size-4 shrink-0 items-center justify-center rounded border",
                      selected ? "border-primary bg-primary text-primary-foreground" : "border-border",
                    )}
                  >
                    {selected ? <Check className="size-3" /> : null}
                  </span>
                  <span className="min-w-0">
                    <span className="block font-medium text-foreground">{item.label}</span>
                    {item.subtitle ? (
                      <span className="block truncate text-xs text-muted-foreground">
                        {item.subtitle}
                      </span>
                    ) : null}
                  </span>
                </button>
              );
            })
          )}
        </div>
      )}
    </div>
  );
}
