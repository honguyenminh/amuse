export type PersonaContextType = "org" | "listener" | "platform";

export type PersonaContextRequest = {
  type: PersonaContextType;
  orgId: string | null;
  listenerId: string | null;
};

export type AuthTokenResponse = {
  accessToken: string;
  accessExpiresAt: string;
  refreshToken: string | null;
  refreshExpiresAt: string;
};

export type AvailablePersona = {
  type: string;
  orgId: string | null;
  listenerId: string | null;
  label: string | null;
};

export type CurrentAccountResponse = {
  accountId: string;
  idpIssuer: string;
  idpSubject: string;
  status: string;
};

export type EnsureListenerProfileResponse = {
  listenerId: string;
  created: boolean;
};

export type ReleaseType = "single" | "ep" | "album" | "compilation";

export type ArtistSummary = {
  id: string;
  slug: string;
  name: string;
  avatarUrl: string | null;
  coverUrl: string | null;
};

export type ReleaseSummary = {
  id: string;
  slug: string;
  title: string;
  artistId: string;
  artistName: string;
  artistSlug: string;
  releaseType: ReleaseType;
  releaseDate: string;
  coverArtUrl: string | null;
};

export type TrackResponse = {
  id: string;
  title: string;
  trackNumber: number;
  durationMs: number;
  hasAudio: boolean;
};

export type TrackStreamInfoResponse = {
  trackId: string;
  url: string;
  contentType: string;
  durationMs: number;
  expiresAt: string;
};

export type BrowseHomeResponse = {
  recentReleases: ReleaseSummary[];
  featuredArtists: ArtistSummary[];
};

export type GetArtistDetailResponse = {
  id: string;
  slug: string;
  name: string;
  bio: string | null;
  avatarUrl: string | null;
  coverUrl: string | null;
  releases: ReleaseSummary[];
};

export type ReleaseEditionSummary = {
  id: string;
  slug: string;
  title: string;
  releaseType: ReleaseType;
  releaseDate: string;
  coverArtUrl: string | null;
};

export type GetReleaseDetailResponse = {
  id: string;
  slug: string;
  title: string;
  artistId: string;
  artistName: string;
  artistSlug: string;
  releaseType: ReleaseType;
  releaseDate: string;
  releaseGroupId: string | null;
  releaseGroupTitle: string | null;
  releaseGroupSlug: string | null;
  description: string | null;
  upc: string | null;
  primaryGenre: string | null;
  tags: string | null;
  languageCode: string | null;
  labelName: string | null;
  coverArtUrl: string | null;
  tracks: TrackResponse[];
  otherEditions: ReleaseEditionSummary[];
};

export type ApiProblem = {
  title?: string;
  detail?: string;
  code?: string;
  status?: number;
};

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly code?: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}
