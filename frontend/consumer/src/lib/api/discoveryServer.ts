import "server-only";

import { cache } from "react";
import { PUBLIC_PAGE_REVALIDATE_SECONDS } from "./revalidate";
import { serverPublicFetch } from "./serverPublicFetch";
import type { PlaylistDetailDto } from "./types";

function enc(id: string): string {
  return encodeURIComponent(id);
}

export function fetchPlaylist(playlistId: string): Promise<PlaylistDetailDto> {
  return serverPublicFetch<PlaylistDetailDto>(
    `/api/v1/discovery/playlists/${enc(playlistId)}`,
    { method: "GET" },
    {
      revalidate: PUBLIC_PAGE_REVALIDATE_SECONDS,
      tags: [`playlist:${playlistId}`],
    },
  );
}

export const getCachedPlaylist = cache(fetchPlaylist);
