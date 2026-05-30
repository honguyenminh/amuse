import { authFetch } from "@/lib/auth/authFetch";

export type OrganizationApplicationOwner = {
  accountId: string;
  email: string | null;
  idpIssuer: string;
  idpSubject: string;
  accountStatus: string;
};

export type OrganizationApplicationSummary = {
  organizationId: string;
  displayName: string;
  orgClass: string;
  onboardingStatus: string;
  trustTier: string;
  createdAt: string;
  updatedAt: string;
  owner: OrganizationApplicationOwner;
};

export function listOrganizationApplications(
  status = "pendingReview",
): Promise<OrganizationApplicationSummary[]> {
  const query = new URLSearchParams({ status });
  return authFetch<OrganizationApplicationSummary[]>(
    `/api/v1/platform/organizations/applications?${query}`,
  );
}

export function approveOrganization(organizationId: string): Promise<void> {
  return authFetch<void>(
    `/api/v1/platform/organizations/${organizationId}/approve`,
    { method: "POST" },
  );
}

export function rejectOrganization(
  organizationId: string,
  reason: string,
): Promise<void> {
  return authFetch<void>(
    `/api/v1/platform/organizations/${organizationId}/reject`,
    {
      method: "POST",
      body: JSON.stringify({ reason }),
    },
  );
}
