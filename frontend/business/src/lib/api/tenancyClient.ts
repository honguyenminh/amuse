import { authFetch } from "@/lib/auth/authFetch";
import type { AvailablePersona } from "@/lib/api/types";

export type OrgClass = "indieGroup" | "backingOrg";

export type OrganizationCapabilities = {
  canReadOrg: boolean;
  canReadMembership: boolean;
  canUpload: boolean;
  canWriteDraft: boolean;
  canPublishPublic: boolean;
  canReadPayout: boolean;
  claimStrings: string[];
};

export type OrganizationResponse = {
  id: string;
  displayName: string;
  orgClass: string;
  lifecycleStatus: string;
  onboardingStatus: string;
  trustTier: string;
  approvedAt: string | null;
  rejectionReason: string | null;
  createdAt: string;
  updatedAt: string;
  capabilities: OrganizationCapabilities;
  isOwner: boolean;
};

export type OrganizationSummary = Pick<
  OrganizationResponse,
  | "id"
  | "displayName"
  | "orgClass"
  | "lifecycleStatus"
  | "onboardingStatus"
  | "trustTier"
  | "isOwner"
>;

export type OrganizationMemberResponse = {
  id: string;
  accountId: string;
  email: string | null;
  status: string;
  presetRoleLabel: string | null;
  claims: string[];
  isOwner: boolean;
  joinedAt: string | null;
  lastLoginAt: string | null;
  lastActiveAt: string | null;
};

export type OrganizationMemberListResponse = {
  items: OrganizationMemberResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
  pendingInviteCount: number;
};

export type ListOrganizationMembersParams = {
  search?: string;
  sortBy?: "email" | "preset" | "lastlogin" | "lastactive" | "joined";
  sortDirection?: "asc" | "desc";
  page?: number;
  pageSize?: number;
};

export type OrganizationInviteResponse = {
  id: string;
  email: string;
  presetRoleLabel: string | null;
  claims: string[];
  status: string;
  expiresAt: string;
  createdAt: string;
};

export type ClaimPresetResponse = {
  label: string;
  displayName: string;
  description: string;
  icon: string;
  claims: string[];
};

export type InvitePreviewResponse = {
  organizationId: string;
  organizationDisplayName: string;
  email: string;
  status: string;
  expiresAt: string;
};

export type AcceptInviteResponse = {
  organizationId: string;
  memberId: string;
};

export function organizationToPersona(
  organization: OrganizationResponse,
): AvailablePersona {
  return {
    type: "org",
    orgId: organization.id,
    listenerId: null,
    label: organization.displayName,
    orgClass: organization.orgClass,
    onboardingStatus: organization.onboardingStatus,
  };
}

export function listMyOrganizations(): Promise<OrganizationSummary[]> {
  return authFetch<OrganizationSummary[]>("/api/v1/tenancy/organizations");
}

export function getOrganization(id: string): Promise<OrganizationResponse> {
  return authFetch<OrganizationResponse>(`/api/v1/tenancy/organizations/${id}`);
}

export function createOrganization(
  displayName: string,
  orgClass: OrgClass,
): Promise<OrganizationResponse> {
  return authFetch<OrganizationResponse>("/api/v1/tenancy/organizations", {
    method: "POST",
    body: JSON.stringify({ displayName, orgClass }),
  });
}

export async function listClaimPresets(): Promise<ClaimPresetResponse[]> {
  const { getApiBaseUrl } = await import("@/lib/api/config");
  const response = await fetch(`${getApiBaseUrl()}/api/v1/tenancy/claim-presets`);
  if (!response.ok) {
    throw new Error("Failed to load claim presets.");
  }
  return (await response.json()) as ClaimPresetResponse[];
}

export function listOrganizationMembers(
  organizationId: string,
  params: ListOrganizationMembersParams = {},
): Promise<OrganizationMemberListResponse> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.sortBy) query.set("sortBy", params.sortBy);
  if (params.sortDirection) query.set("sortDirection", params.sortDirection);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const suffix = query.size > 0 ? `?${query.toString()}` : "";
  return authFetch<OrganizationMemberListResponse>(
    `/api/v1/tenancy/organizations/${organizationId}/members${suffix}`,
  );
}

export function listOrganizationInvites(
  organizationId: string,
): Promise<OrganizationInviteResponse[]> {
  return authFetch<OrganizationInviteResponse[]>(
    `/api/v1/tenancy/organizations/${organizationId}/members/invites`,
  );
}

export function createOrganizationInvite(
  organizationId: string,
  body: { email: string; presetRoleLabel?: string | null; claims?: string[] | null },
): Promise<{ inviteId: string; expiresAt: string }> {
  return authFetch(`/api/v1/tenancy/organizations/${organizationId}/members/invites`, {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export function revokeOrganizationInvite(
  organizationId: string,
  inviteId: string,
): Promise<void> {
  return authFetch(
    `/api/v1/tenancy/organizations/${organizationId}/members/invites/${inviteId}`,
    { method: "DELETE" },
  );
}

export function updateOrganizationMember(
  organizationId: string,
  memberId: string,
  body: { presetRoleLabel?: string | null; claims?: string[] | null },
): Promise<OrganizationMemberResponse> {
  return authFetch(
    `/api/v1/tenancy/organizations/${organizationId}/members/${memberId}`,
    {
      method: "PATCH",
      body: JSON.stringify(body),
    },
  );
}

export function removeOrganizationMember(
  organizationId: string,
  memberId: string,
): Promise<void> {
  return authFetch(
    `/api/v1/tenancy/organizations/${organizationId}/members/${memberId}`,
    { method: "DELETE" },
  );
}

export function leaveOrganization(organizationId: string): Promise<void> {
  return authFetch(
    `/api/v1/tenancy/organizations/${organizationId}/membership/leave`,
    { method: "POST" },
  );
}

export function transferOrganizationOwnership(
  organizationId: string,
  targetMemberId: string,
): Promise<void> {
  return authFetch(
    `/api/v1/tenancy/organizations/${organizationId}/ownership/transfer`,
    {
      method: "POST",
      body: JSON.stringify({ targetMemberId }),
    },
  );
}

export function deleteOrganization(organizationId: string): Promise<void> {
  return authFetch(`/api/v1/tenancy/organizations/${organizationId}`, {
    method: "DELETE",
  });
}

export async function getInvitePreview(token: string): Promise<InvitePreviewResponse> {
  const { getApiBaseUrl } = await import("@/lib/api/config");
  const response = await fetch(
    `${getApiBaseUrl()}/api/v1/tenancy/invites/${encodeURIComponent(token)}`,
  );
  if (!response.ok) {
    throw new Error("Invite not found or expired.");
  }
  return (await response.json()) as InvitePreviewResponse;
}

export function acceptOrganizationInvite(token: string): Promise<AcceptInviteResponse> {
  return authFetch(`/api/v1/tenancy/invites/${encodeURIComponent(token)}/accept`, {
    method: "POST",
  });
}

export function declineOrganizationInvite(token: string): Promise<void> {
  return authFetch(`/api/v1/tenancy/invites/${encodeURIComponent(token)}/decline`, {
    method: "POST",
  });
}
