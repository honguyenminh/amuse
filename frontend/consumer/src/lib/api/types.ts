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
  email: string | null;
};

export type ListenerProfileResponse = {
  listenerId: string;
  displayName: string | null;
  avatarAccentSeed: number | null;
  avatarUrl: string | null;
  allowUnverifiedArtists: boolean | null;
  onboardingComplete: boolean;
  updatedAt: string;
};

export type UpdateListenerProfileRequest = {
  displayName?: string;
  avatarAccentSeed?: number | null;
  allowUnverifiedArtists?: boolean;
  clearAvatar?: boolean;
};

export type PresignListenerAvatarUploadResponse = {
  key: string;
  url: string;
  expiresAt: string;
  method: string;
};

export type CompleteListenerAvatarUploadResponse = {
  key: string;
  avatarUrl: string;
};

export type EnsureListenerProfileResponse = {
  listenerId: string;
  created: boolean;
};

export type ReleaseType = "single" | "ep" | "album" | "compilation";

export type OrganizationTrustTier = "unverified" | "identityVerified" | "platformVerified";

export type ArtistSummary = {
  id: string;
  slug: string;
  name: string;
  avatarUrl: string | null;
  coverUrl: string | null;
  trustTier: OrganizationTrustTier;
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
  trustTier: OrganizationTrustTier;
};

export type TrackResponse = {
  id: string;
  title: string;
  trackNumber: number;
  durationMs: number;
  hasAudio: boolean;
  pricing?: CatalogPricingResponse | null;
};

export type CatalogPricingResponse = {
  isForSale: boolean;
  priceFloorMinor: number;
  priceCeilingMinor: number | null;
  priceCurrency: string | null;
};

export type TrackStreamRenditionDto = {
  id: string;
  codec: string;
  bitrateKbps: number | null;
  bandwidth: number;
  sampleRateHz: number;
  adaptationSetId: string;
  representationId: string;
};

export type TrackStreamLoudness = {
  integratedLufs: number;
  truePeakDbtp: number;
  targetIntegratedLufs: number;
  targetTruePeakDbtp: number;
  linearGainLu: number;
};

export type TrackStreamInfoResponse = {
  trackId: string;
  url: string;
  contentType: string;
  durationMs: number;
  expiresAt: string;
  loudness: TrackStreamLoudness | null;
  renditions: TrackStreamRenditionDto[];
  isOwner: boolean;
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
  trustTier: OrganizationTrustTier;
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
  pricing?: CatalogPricingResponse | null;
  trustTier: OrganizationTrustTier;
};

export type GetReleaseGroupDetailResponse = {
  id: string;
  slug: string;
  title: string;
  description: string | null;
  artistId: string;
  artistName: string;
  artistSlug: string;
  releases: ReleaseEditionSummary[];
};

export type PlaylistOwnerDto = {
  listenerProfileId: string;
  displayName: string | null;
  avatarUrl: string | null;
};

export type PlaylistKind = "user" | "liked";

export type PlaylistSummaryDto = {
  id: string;
  title: string;
  kind: PlaylistKind;
  description: string | null;
  visibility: string;
  trackCount: number;
  updatedAt: string;
  owner: PlaylistOwnerDto | null;
  forkedFromPlaylistId: string | null;
  isOwned: boolean;
  isSaved: boolean;
  isFollowed: boolean;
  isDeletable: boolean;
  coverArtUrls: string[];
};

export type PlaylistItemDto = {
  itemId: string;
  trackId: string;
  position: number;
  title: string;
  durationMs: number;
  hasAudio: boolean;
  coverArtUrl: string | null;
  releaseId: string;
  releaseTitle: string;
  artistName: string;
};

export type PlaylistDetailDto = {
  id: string;
  title: string;
  kind: PlaylistKind;
  description: string | null;
  visibility: string;
  owner: PlaylistOwnerDto | null;
  forkedFromPlaylistId: string | null;
  items: PlaylistItemDto[];
  shareEmails: string[] | null;
  createdAt: string;
  updatedAt: string;
  isOwned: boolean;
  isSaved: boolean;
  isFollowed: boolean;
  isDeletable: boolean;
};

export type PlaylistListResponse = {
  playlists: PlaylistSummaryDto[];
};

export type SearchItemDto = {
  kind: string;
  id: string;
  title: string;
  subtitle: string | null;
  artistSlug: string | null;
  releaseSlug: string | null;
  artistId: string | null;
  releaseId: string | null;
  coverArtUrl: string | null;
  trustTier: OrganizationTrustTier;
  isVerified: boolean;
};

export type PublicPlaylistSearchCardDto = {
  id: string;
  title: string;
  description: string | null;
  trackCount: number;
  owner: PlaylistOwnerDto;
  updatedAt: string;
  coverArtUrls: string[];
};

export type SearchResponse = {
  verified: SearchItemDto[];
  unverified: SearchItemDto[];
  publicPlaylists: PublicPlaylistSearchCardDto[];
};

export type LikedTrackRowDto = {
  trackId: string;
  title: string;
  durationMs: number;
  hasAudio: boolean;
  coverArtUrl: string | null;
  releaseId: string;
  releaseTitle: string;
  artistName: string;
  likedAt: string;
};

export type LikedTracksResponse = {
  tracks: LikedTrackRowDto[];
};

export type SavedReleaseRowDto = {
  releaseId: string;
  title: string;
  artistName: string;
  artistSlug: string;
  releaseSlug: string;
  coverArtUrl: string | null;
  savedAt: string;
};

export type SavedReleasesResponse = {
  releases: SavedReleaseRowDto[];
};

export type PlayableTrackDto = {
  trackId: string;
  title: string;
  trackNumber: number;
  durationMs: number;
  hasAudio: boolean;
  coverArtUrl: string | null;
  releaseId: string;
  releaseTitle: string;
  artistName: string;
  artistSlug: string;
  releaseSlug: string;
};

export type PlayableTracksResponse = {
  tracks: PlayableTrackDto[];
};

export type CreatePlaylistRequest = {
  title: string;
  visibility: string;
  description?: string;
};

export type UpdatePlaylistRequest = {
  title?: string;
  description?: string;
  visibility?: string;
};

export type AddPlaylistItemRequest = {
  trackId: string;
};

export type ReorderPlaylistItemsRequest = {
  itemId: string;
  newPosition: number;
};

export type ReplacePlaylistSharesRequest = {
  emails: string[];
};

export type AddPlaylistItemResponse = {
  item: PlaylistItemDto;
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
