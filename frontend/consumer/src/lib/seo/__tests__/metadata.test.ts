import { describe, expect, it } from "vitest";
import type {
  GetArtistDetailResponse,
  GetReleaseDetailResponse,
  GetReleaseGroupDetailResponse,
  PlaylistDetailDto,
} from "@/lib/api/types";
import {
  artistMetadata,
  playlistMetadata,
  releaseGroupMetadata,
  releaseMetadata,
} from "@/lib/seo/metadata";

describe("artistMetadata", () => {
  it("builds title and canonical slug URL", () => {
    const artist: GetArtistDetailResponse = {
      id: "a",
      slug: "aurora-lights",
      name: "Aurora Lights",
      bio: "Bio text",
      avatarUrl: "https://cdn.example/avatar.jpg",
      coverUrl: null,
      releases: [],
    };

    const metadata = artistMetadata(artist);
    expect(metadata.title).toBe("Aurora Lights | Amuse");
    expect(metadata.alternates?.canonical).toContain("/artist/aurora-lights");
    expect(metadata.openGraph?.images).toEqual([
      { url: "https://cdn.example/avatar.jpg", alt: "" },
    ]);
  });
});

describe("releaseMetadata", () => {
  it("includes artist in title and release cover", () => {
    const release: GetReleaseDetailResponse = {
      id: "r",
      slug: "dawn-anatomy",
      title: "Dawn Anatomy",
      artistId: "a",
      artistName: "Aurora Lights",
      artistSlug: "aurora-lights",
      releaseType: "album",
      releaseDate: "2024-01-01T00:00:00Z",
      releaseGroupId: null,
      releaseGroupTitle: null,
      releaseGroupSlug: null,
      description: "A long description for the release.",
      upc: null,
      primaryGenre: null,
      tags: null,
      languageCode: null,
      labelName: null,
      coverArtUrl: "https://cdn.example/cover.jpg",
      tracks: [{ id: "t", title: "Track", trackNumber: 1, durationMs: 1000, hasAudio: true }],
      otherEditions: [],
    };

    const metadata = releaseMetadata(release);
    expect(metadata.title).toBe("Dawn Anatomy — Aurora Lights | Amuse");
    expect(metadata.alternates?.canonical).toContain(
      "/artist/aurora-lights/release/dawn-anatomy",
    );
  });
});

describe("releaseGroupMetadata", () => {
  it("uses first edition cover for open graph", () => {
    const group: GetReleaseGroupDetailResponse = {
      id: "g",
      slug: "dawn-anatomy",
      title: "Dawn Anatomy",
      description: null,
      artistId: "a",
      artistName: "Aurora Lights",
      artistSlug: "aurora-lights",
      releases: [
        {
          id: "r",
          slug: "dawn-anatomy",
          title: "Dawn Anatomy",
          releaseType: "album",
          releaseDate: "2024-01-01T00:00:00Z",
          coverArtUrl: "https://cdn.example/cover.jpg",
        },
      ],
    };

    const metadata = releaseGroupMetadata(group);
    expect(metadata.title).toBe("Dawn Anatomy — Aurora Lights | Amuse");
    expect(metadata.openGraph?.images).toEqual([
      { url: "https://cdn.example/cover.jpg", alt: "" },
    ]);
  });
});

describe("playlistMetadata", () => {
  it("noindexes private playlists", () => {
    const playlist: PlaylistDetailDto = {
      id: "p",
      title: "Private Mix",
      kind: "user",
      description: null,
      visibility: "private",
      owner: null,
      forkedFromPlaylistId: null,
      items: [],
      shareEmails: null,
      createdAt: "2024-01-01T00:00:00Z",
      updatedAt: "2024-01-01T00:00:00Z",
      isOwned: false,
      isSaved: false,
      isFollowed: false,
      isDeletable: false,
    };

    const metadata = playlistMetadata(playlist);
    expect(metadata.robots).toEqual({ index: false, follow: false });
    expect(metadata.alternates?.canonical).toBeUndefined();
  });

  it("indexes public playlists", () => {
    const playlist: PlaylistDetailDto = {
      id: "p",
      title: "Public Mix",
      kind: "user",
      description: "Summer tracks",
      visibility: "public",
      owner: null,
      forkedFromPlaylistId: null,
      items: [],
      shareEmails: null,
      createdAt: "2024-01-01T00:00:00Z",
      updatedAt: "2024-01-01T00:00:00Z",
      isOwned: false,
      isSaved: false,
      isFollowed: false,
      isDeletable: false,
    };

    const metadata = playlistMetadata(playlist);
    expect(metadata.robots).toEqual({ index: true, follow: true });
    expect(metadata.alternates?.canonical).toContain("/playlist/p");
  });
});

describe("excerpt truncation", () => {
  it("truncates long descriptions via release metadata", () => {
    const release: GetReleaseDetailResponse = {
      id: "r",
      slug: "x",
      title: "Title",
      artistId: "a",
      artistName: "Artist",
      artistSlug: "artist",
      releaseType: "single",
      releaseDate: "2024-01-01T00:00:00Z",
      releaseGroupId: null,
      releaseGroupTitle: null,
      releaseGroupSlug: null,
      description: "word ".repeat(40).trim(),
      upc: null,
      primaryGenre: null,
      tags: null,
      languageCode: null,
      labelName: null,
      coverArtUrl: null,
      tracks: [],
      otherEditions: [],
    };

    const metadata = releaseMetadata(release);
    expect(metadata.description?.length).toBeLessThanOrEqual(160);
    expect(metadata.description?.endsWith("…")).toBe(true);
  });
});
