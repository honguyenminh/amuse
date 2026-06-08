export const SEARCH_KINDS = ["artist", "release", "track", "playlist"] as const;

export type SearchKind = (typeof SEARCH_KINDS)[number];

export const DEFAULT_SEARCH_KINDS: SearchKind[] = [...SEARCH_KINDS];

export const SEARCH_KIND_LABELS: Record<SearchKind, string> = {
  artist: "Artists",
  release: "Releases",
  track: "Tracks",
  playlist: "Playlists",
};

export function isAllSearchKindsSelected(kinds: readonly SearchKind[]): boolean {
  return SEARCH_KINDS.every((kind) => kinds.includes(kind));
}

export function toggleSearchKind(
  selected: readonly SearchKind[],
  kind: SearchKind,
): SearchKind[] {
  if (selected.includes(kind)) {
    const next = selected.filter((entry) => entry !== kind);
    return next.length === 0 ? [...DEFAULT_SEARCH_KINDS] : [...next];
  }

  return [...selected, kind];
}

export function searchKindsForRequest(kinds: readonly SearchKind[]): SearchKind[] | undefined {
  return isAllSearchKindsSelected(kinds) ? undefined : [...kinds];
}

export function appendSearchKindsParams(
  params: URLSearchParams,
  kinds: readonly SearchKind[] | undefined,
): void {
  if (!kinds) return;

  for (const kind of kinds) {
    params.append("kinds", kind);
  }
}

export function filteredKindsSummary(kinds: readonly SearchKind[]): string {
  return kinds.map((kind) => SEARCH_KIND_LABELS[kind]).join(", ");
}
