import { authFetch } from "@/lib/auth/authFetch";

export type ArtistVisibilityTier = "unverified" | "platformVerified";

export type ReleaseType = "single" | "ep" | "album" | "compilation";

export type ReleaseLifecycleStatus =
  | "draft"
  | "processing"
  | "ready"
  | "scheduled"
  | "published"
  | "hidden"
  | "archived";

export type TrackLifecycleStatus =
  | "draft"
  | "processing"
  | "ready"
  | "published"
  | "hidden";

export type AudioTranscodeJobStatus = "queued" | "processing" | "succeeded" | "failed";

export type ManageArtistSummaryResponse = {
  id: string;
  slug: string;
  name: string;
  visibilityTier: ArtistVisibilityTier;
  createdAt: string;
};

export type ManageArtistListResponse = {
  items: ManageArtistSummaryResponse[];
};

export type ManageArtistReleaseSummary = {
  id: string;
  slug: string;
  title: string;
  releaseType: ReleaseType;
  lifecycleStatus: ReleaseLifecycleStatus;
  releaseDate: string;
  coverArtUrl: string | null;
};

export type ManageArtistTrackSummary = {
  id: string;
  title: string;
  trackNumber: number;
  durationMs: number;
  explicitFlag: boolean;
  lifecycleStatus: TrackLifecycleStatus;
};

export type ManageArtistDetailResponse = {
  id: string;
  slug: string;
  name: string;
  bio: string | null;
  countryCode: string | null;
  websiteUrl: string | null;
  aliases: string | null;
  avatarUrl: string | null;
  coverUrl: string | null;
  visibilityTier: ArtistVisibilityTier;
  createdAt: string;
  releases: ManageArtistReleaseSummary[];
  tracks: ManageArtistTrackSummary[];
  releaseGroups: ManageReleaseGroupSummaryResponse[];
};

export type ManageReleaseGroupResponse = {
  id: string;
  slug: string;
  title: string;
  description: string | null;
  artistId: string;
  createdAt: string;
  updatedAt: string;
};

export type ManageReleaseGroupSummaryResponse = {
  id: string;
  slug: string;
  title: string;
  description: string | null;
  releaseCount: number;
  updatedAt: string;
};

export type ManageReleaseGroupMemberResponse = {
  id: string;
  slug: string;
  title: string;
  releaseType: ReleaseType;
  lifecycleStatus: ReleaseLifecycleStatus;
  releaseDate: string;
  coverArtUrl: string | null;
};

export type ManageReleaseGroupDetailResponse = {
  id: string;
  slug: string;
  title: string;
  description: string | null;
  artistId: string;
  artistName: string;
  createdAt: string;
  updatedAt: string;
  releases: ManageReleaseGroupMemberResponse[];
};

export type ManageReleaseGroupListResponse = {
  items: ManageReleaseGroupResponse[];
};

export type ManageReleaseSummaryResponse = {
  id: string;
  slug: string;
  title: string;
  artistId: string;
  artistName: string;
  releaseType: ReleaseType;
  lifecycleStatus: ReleaseLifecycleStatus;
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
  pLine: string | null;
  cLine: string | null;
  originalReleaseDate: string | null;
  metadataComplete: boolean;
  coverArtUrl: string | null;
  createdAt: string;
  updatedAt: string;
};

export type ManageReleaseListResponse = {
  items: ManageReleaseSummaryResponse[];
};

export type ManageTrackResponse = {
  id: string;
  title: string;
  trackNumber: number;
  durationMs: number;
  explicitFlag: boolean;
  isrc: string | null;
  lyrics: string | null;
  languageCode: string | null;
  versionTitle: string | null;
  composerCredits: string | null;
  lifecycleStatus: TrackLifecycleStatus;
  hasAudioMaster: boolean;
  hasAudioStream: boolean;
};

export type ReleaseCollaboratorRole = "featured";

export type ManageReleaseCollaboratorResponse = {
  artistId: string;
  artistName: string;
  role: ReleaseCollaboratorRole;
  displayOrder: number;
};

export type ManageReleaseDetailResponse = {
  id: string;
  slug: string;
  title: string;
  artistId: string;
  artistName: string;
  releaseType: ReleaseType;
  lifecycleStatus: ReleaseLifecycleStatus;
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
  pLine: string | null;
  cLine: string | null;
  originalReleaseDate: string | null;
  metadataComplete: boolean;
  coverArtUrl: string | null;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string;
  collaborators: ManageReleaseCollaboratorResponse[];
  tracks: ManageTrackResponse[];
};

export type TrackIngestionResponse = {
  trackId: string;
  lifecycleStatus: TrackLifecycleStatus;
  audioMasterKey: string | null;
  audioStreamKey: string | null;
  latestJobId: string | null;
  jobStatus: AudioTranscodeJobStatus | null;
  jobLastError: string | null;
  jobUpdatedAt: string | null;
};

export type PresignAudioMasterUploadResponse = {
  trackId: string;
  key: string;
  url: string;
  expiresAt: string;
  method: string;
};

export type CompleteAudioMasterUploadResponse = {
  trackId: string;
  masterKey: string;
  streamKey: string;
  jobId: string;
};

export type RetryTrackTranscodeResponse = {
  trackId: string;
  jobId: string;
  masterKey: string;
  streamKey: string;
  jobStatus: AudioTranscodeJobStatus;
  attemptCount: number;
  reusedInflightJob: boolean;
};

export type PresignReleaseCoverUploadResponse = {
  releaseId: string;
  key: string;
  url: string;
  expiresAt: string;
  method: string;
};

export type CompleteReleaseCoverUploadResponse = {
  releaseId: string;
  coverArtKey: string;
  coverArtUrl: string | null;
};

export type PresignArtistAvatarUploadResponse = {
  artistId: string;
  key: string;
  url: string;
  expiresAt: string;
  method: string;
};

export type CompleteArtistAvatarUploadResponse = {
  artistId: string;
  avatarKey: string;
  avatarUrl: string | null;
};

export type PresignArtistCoverUploadResponse = {
  artistId: string;
  key: string;
  url: string;
  expiresAt: string;
  method: string;
};

export type CompleteArtistCoverUploadResponse = {
  artistId: string;
  coverKey: string;
  coverUrl: string | null;
};

export const CATALOG_AUDIT_TABLES = {
  artist: "catalog.artist",
  release: "catalog.release",
  track: "catalog.track",
  releaseGroup: "catalog.release_group",
} as const;

export type CatalogAuditTableName =
  (typeof CATALOG_AUDIT_TABLES)[keyof typeof CATALOG_AUDIT_TABLES];

export type CatalogAuditEntryResponse = {
  id: string;
  action: string;
  tableName: string;
  targetId: string;
  beforeJson: string | null;
  afterJson: string | null;
  changedAt: string;
  actorAccountId: string | null;
};

export type CatalogAuditListResponse = {
  items: CatalogAuditEntryResponse[];
};

export type ArtistSlugAvailabilityResponse = {
  normalizedSlug: string;
  isValid: boolean;
  isAvailable: boolean;
};

export type ReleaseSlugAvailabilityResponse = {
  normalizedSlug: string;
  isValid: boolean;
  isAvailable: boolean;
};

export function checkArtistSlugAvailability(
  slug: string,
): Promise<ArtistSlugAvailabilityResponse> {
  const query = new URLSearchParams({ slug });
  return authFetch<ArtistSlugAvailabilityResponse>(
    `/api/v1/catalog/manage/artists/slug-availability?${query.toString()}`,
  );
}

export function checkReleaseSlugAvailability(
  artistId: string,
  slug: string,
  excludingReleaseId?: string,
): Promise<ReleaseSlugAvailabilityResponse> {
  const query = new URLSearchParams({ slug });
  if (excludingReleaseId) {
    query.set("excludingReleaseId", excludingReleaseId);
  }
  return authFetch<ReleaseSlugAvailabilityResponse>(
    `/api/v1/catalog/artists/${artistId}/releases/slug-availability?${query.toString()}`,
  );
}

export function listArtists(): Promise<ManageArtistListResponse> {
  return authFetch<ManageArtistListResponse>("/api/v1/catalog/manage/artists");
}

export function createArtist(body: {
  name: string;
  slug: string;
  bio?: string | null;
  countryCode?: string | null;
  websiteUrl?: string | null;
  aliases?: string | null;
}): Promise<ManageArtistSummaryResponse> {
  return authFetch<ManageArtistSummaryResponse>("/api/v1/catalog/artists", {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function updateArtist(
  artistId: string,
  body: {
    name: string;
    bio?: string | null;
    countryCode?: string | null;
    websiteUrl?: string | null;
    aliases?: string | null;
  },
): Promise<ManageArtistSummaryResponse> {
  return authFetch<ManageArtistSummaryResponse>(`/api/v1/catalog/artists/${artistId}`, {
    method: "PATCH",
    body: JSON.stringify(body),
  });
}

export function listArtistReleaseGroups(
  artistId: string,
): Promise<ManageReleaseGroupListResponse> {
  return authFetch<ManageReleaseGroupListResponse>(
    `/api/v1/catalog/artists/${artistId}/release-groups`,
  );
}

export function createArtistReleaseGroup(
  artistId: string,
  body: { title: string; description?: string | null },
): Promise<ManageReleaseGroupResponse> {
  return authFetch<ManageReleaseGroupResponse>(
    `/api/v1/catalog/artists/${artistId}/release-groups`,
    {
      method: "POST",
      body: JSON.stringify(body),
    },
  );
}

export function getArtistReleaseGroupDetail(
  artistId: string,
  releaseGroupId: string,
): Promise<ManageReleaseGroupDetailResponse> {
  return authFetch<ManageReleaseGroupDetailResponse>(
    `/api/v1/catalog/artists/${artistId}/release-groups/${releaseGroupId}`,
  );
}

export function updateArtistReleaseGroup(
  artistId: string,
  releaseGroupId: string,
  body: { title: string; description?: string | null },
): Promise<ManageReleaseGroupResponse> {
  return authFetch<ManageReleaseGroupResponse>(
    `/api/v1/catalog/artists/${artistId}/release-groups/${releaseGroupId}`,
    {
      method: "PATCH",
      body: JSON.stringify(body),
    },
  );
}

export function getArtist(artistId: string): Promise<ManageArtistDetailResponse> {
  return authFetch<ManageArtistDetailResponse>(
    `/api/v1/catalog/manage/artists/${artistId}`,
  );
}

export function listReleases(
  status?: ReleaseLifecycleStatus,
): Promise<ManageReleaseListResponse> {
  const query = status ? `?status=${encodeURIComponent(status)}` : "";
  return authFetch<ManageReleaseListResponse>(`/api/v1/catalog/manage/releases${query}`);
}

export function createRelease(
  artistId: string,
  body: {
    title: string;
    slug?: string;
    releaseType: ReleaseType;
    releaseDate: string;
    releaseGroupId?: string | null;
    description?: string | null;
    upc?: string | null;
    primaryGenre?: string | null;
    tags?: string | null;
    languageCode?: string | null;
    labelName?: string | null;
    pLine?: string | null;
    cLine?: string | null;
    originalReleaseDate?: string | null;
    metadataComplete?: boolean;
    collaboratorArtistIds?: string[];
  },
): Promise<ManageReleaseDetailResponse> {
  return authFetch<ManageReleaseDetailResponse>(
    `/api/v1/catalog/artists/${artistId}/releases`,
    {
      method: "POST",
      body: JSON.stringify(body),
    },
  );
}

export function getRelease(releaseId: string): Promise<ManageReleaseDetailResponse> {
  return authFetch<ManageReleaseDetailResponse>(
    `/api/v1/catalog/manage/releases/${releaseId}`,
  );
}

export function updateRelease(
  releaseId: string,
  body: {
    title: string;
    slug?: string;
    releaseType: ReleaseType;
    releaseDate: string;
    releaseGroupId?: string | null;
    description?: string | null;
    upc?: string | null;
    primaryGenre?: string | null;
    tags?: string | null;
    languageCode?: string | null;
    labelName?: string | null;
    pLine?: string | null;
    cLine?: string | null;
    originalReleaseDate?: string | null;
    metadataComplete?: boolean;
    collaboratorArtistIds?: string[];
  },
): Promise<ManageReleaseDetailResponse> {
  return authFetch<ManageReleaseDetailResponse>(
    `/api/v1/catalog/releases/${releaseId}`,
    {
      method: "PATCH",
      body: JSON.stringify(body),
    },
  );
}

export function publishRelease(
  releaseId: string,
): Promise<ManageReleaseDetailResponse> {
  return authFetch<ManageReleaseDetailResponse>(
    `/api/v1/catalog/releases/${releaseId}/publish`,
    { method: "POST" },
  );
}

export function scheduleRelease(
  releaseId: string,
): Promise<ManageReleaseDetailResponse> {
  return authFetch<ManageReleaseDetailResponse>(
    `/api/v1/catalog/releases/${releaseId}/schedule`,
    { method: "POST" },
  );
}

export function cancelScheduledRelease(
  releaseId: string,
): Promise<ManageReleaseDetailResponse> {
  return authFetch<ManageReleaseDetailResponse>(
    `/api/v1/catalog/releases/${releaseId}/cancel-schedule`,
    { method: "POST" },
  );
}

export function createTrack(
  releaseId: string,
  body: {
    title: string;
    trackNumber: number;
    durationMs: number;
    explicitFlag: boolean;
    isrc?: string | null;
    lyrics?: string | null;
    languageCode?: string | null;
    versionTitle?: string | null;
    composerCredits?: string | null;
  },
): Promise<ManageTrackResponse> {
  return authFetch<ManageTrackResponse>(
    `/api/v1/catalog/releases/${releaseId}/tracks`,
    {
      method: "POST",
      body: JSON.stringify(body),
    },
  );
}

export function updateTrack(
  trackId: string,
  body: {
    title: string;
    trackNumber: number;
    explicitFlag: boolean;
    isrc?: string | null;
    lyrics?: string | null;
    languageCode?: string | null;
    versionTitle?: string | null;
    composerCredits?: string | null;
  },
): Promise<ManageTrackResponse> {
  return authFetch<ManageTrackResponse>(`/api/v1/catalog/tracks/${trackId}`, {
    method: "PATCH",
    body: JSON.stringify(body),
  });
}

export function deleteTrack(trackId: string): Promise<void> {
  return authFetch(`/api/v1/catalog/tracks/${trackId}`, { method: "DELETE" });
}

export function deleteRelease(releaseId: string): Promise<void> {
  return authFetch(`/api/v1/catalog/releases/${releaseId}`, { method: "DELETE" });
}

export function listResourceAudits(
  tableName: CatalogAuditTableName,
  targetId: string,
): Promise<CatalogAuditListResponse> {
  const query = new URLSearchParams({ tableName, targetId });
  return authFetch<CatalogAuditListResponse>(
    `/api/v1/catalog/manage/audit?${query.toString()}`,
  );
}

export function presignReleaseCoverUpload(
  releaseId: string,
  body: { fileName: string; contentType: string },
): Promise<PresignReleaseCoverUploadResponse> {
  return authFetch<PresignReleaseCoverUploadResponse>(
    `/api/v1/catalog/releases/${releaseId}/cover/presign-upload`,
    {
      method: "POST",
      body: JSON.stringify(body),
    },
  );
}

export function completeReleaseCoverUpload(
  releaseId: string,
  body: { key: string },
): Promise<CompleteReleaseCoverUploadResponse> {
  return authFetch<CompleteReleaseCoverUploadResponse>(
    `/api/v1/catalog/releases/${releaseId}/cover/complete`,
    {
      method: "POST",
      body: JSON.stringify(body),
    },
  );
}

export function presignArtistAvatarUpload(
  artistId: string,
  body: { fileName: string; contentType: string },
): Promise<PresignArtistAvatarUploadResponse> {
  return authFetch<PresignArtistAvatarUploadResponse>(
    `/api/v1/catalog/artists/${artistId}/avatar/presign-upload`,
    {
      method: "POST",
      body: JSON.stringify(body),
    },
  );
}

export function completeArtistAvatarUpload(
  artistId: string,
  body: { key: string },
): Promise<CompleteArtistAvatarUploadResponse> {
  return authFetch<CompleteArtistAvatarUploadResponse>(
    `/api/v1/catalog/artists/${artistId}/avatar/complete`,
    {
      method: "POST",
      body: JSON.stringify(body),
    },
  );
}

export function presignArtistCoverUpload(
  artistId: string,
  body: { fileName: string; contentType: string },
): Promise<PresignArtistCoverUploadResponse> {
  return authFetch<PresignArtistCoverUploadResponse>(
    `/api/v1/catalog/artists/${artistId}/cover/presign-upload`,
    {
      method: "POST",
      body: JSON.stringify(body),
    },
  );
}

export function completeArtistCoverUpload(
  artistId: string,
  body: { key: string },
): Promise<CompleteArtistCoverUploadResponse> {
  return authFetch<CompleteArtistCoverUploadResponse>(
    `/api/v1/catalog/artists/${artistId}/cover/complete`,
    {
      method: "POST",
      body: JSON.stringify(body),
    },
  );
}

export function getTrackIngestion(trackId: string): Promise<TrackIngestionResponse> {
  return authFetch<TrackIngestionResponse>(
    `/api/v1/catalog/tracks/${trackId}/ingestion`,
  );
}

export function retryTrackTranscode(trackId: string): Promise<RetryTrackTranscodeResponse> {
  return authFetch<RetryTrackTranscodeResponse>(
    `/api/v1/catalog/tracks/${trackId}/ingestion/retry-transcode`,
    { method: "POST" },
  );
}

export function presignAudioUpload(
  trackId: string,
  body: { fileName: string; contentType: string },
): Promise<PresignAudioMasterUploadResponse> {
  return authFetch<PresignAudioMasterUploadResponse>(
    `/api/v1/catalog/tracks/${trackId}/audio-master/presign-upload`,
    {
      method: "POST",
      body: JSON.stringify(body),
    },
  );
}

export function completeAudioUpload(
  trackId: string,
  body: { key: string },
): Promise<CompleteAudioMasterUploadResponse> {
  return authFetch<CompleteAudioMasterUploadResponse>(
    `/api/v1/catalog/tracks/${trackId}/audio-master/complete`,
    {
      method: "POST",
      body: JSON.stringify(body),
    },
  );
}
