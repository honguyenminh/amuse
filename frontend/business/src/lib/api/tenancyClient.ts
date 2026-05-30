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
