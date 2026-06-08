import { authFetch } from "@/lib/auth/authFetch";
import { ApiError } from "@/lib/api/types";

export type PayoutVerificationStatus =
  | "notStarted"
  | "submitted"
  | "underReview"
  | "verified"
  | "rejected";

export type LegalEntityType = "individual" | "company";
export type PayoutRail = "stripeGlobal" | "manualBank";

export type PayoutProfileResponse = {
  id: string;
  organizationId: string;
  legalEntityType: LegalEntityType;
  legalName: string;
  addressLine1: string;
  addressLine2: string | null;
  city: string;
  region: string | null;
  postalCode: string;
  countryCode: string;
  hasTaxId: boolean;
  representativeName: string | null;
  payoutRail: PayoutRail;
  bankAccountMasked: string | null;
  bankName: string | null;
  verificationStatus: PayoutVerificationStatus;
  documentObjectKeys: string[];
  createdAt: string;
  updatedAt: string;
  verifiedAt: string | null;
  rejectionReason: string | null;
  blocksWithdrawals: boolean;
};

export type UpsertPayoutProfileRequest = {
  legalEntityType: LegalEntityType;
  legalName: string;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  region?: string | null;
  postalCode: string;
  countryCode: string;
  taxId?: string | null;
  representativeName?: string | null;
  payoutRail: PayoutRail;
  bankAccountNumber?: string | null;
  bankName?: string | null;
  documentObjectKeys: string[];
};

export type CurrencyBalanceRow = {
  currency: string;
  pendingMinor: number;
  availableMinor: number;
  inPayoutMinor: number;
  receivableMinor: number;
  usdEquivalentMinor: number | null;
};

export type OrgBalanceResponse = {
  balances: CurrencyBalanceRow[];
  gateBVerified: boolean;
  blocksWithdrawals: boolean;
  cooldownEndsAt: string | null;
  hasOutstandingReceivable: boolean;
};

export type WithdrawalStatus =
  | "pendingApproval"
  | "approved"
  | "processing"
  | "completed"
  | "failed";

export type WithdrawalRow = {
  id: string;
  amountMinor: number;
  currency: string;
  status: WithdrawalStatus;
  transferReference: string | null;
  proofObjectKey: string | null;
  requestedAt: string;
  completedAt: string | null;
  failedAt: string | null;
};

export type StatementLineRow = {
  id: string;
  purchaseId: string;
  trackId: string;
  shareBps: number;
  amountMinor: number;
  currency: string;
  creditedAt: string;
};

export type PagedStatementsResponse = {
  items: StatementLineRow[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type PlatformWithdrawalRow = {
  id: string;
  organizationId: string;
  amountMinor: number;
  currency: string;
  status: WithdrawalStatus;
  requestedAt: string;
  transferReference: string | null;
};

export async function getPayoutProfile(): Promise<PayoutProfileResponse | null> {
  try {
    return await authFetch<PayoutProfileResponse>("/api/v1/billing/payout-profile");
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      return null;
    }
    throw error;
  }
}

export function upsertPayoutProfile(
  body: UpsertPayoutProfileRequest,
): Promise<PayoutProfileResponse> {
  return authFetch<PayoutProfileResponse>("/api/v1/billing/payout-profile", {
    method: "PUT",
    body: JSON.stringify(body),
  });
}

export function submitPayoutProfile(): Promise<PayoutProfileResponse> {
  return authFetch<PayoutProfileResponse>("/api/v1/billing/payout-profile/submit", {
    method: "POST",
  });
}

export function getOrgBalance(): Promise<OrgBalanceResponse> {
  return authFetch<OrgBalanceResponse>("/api/v1/billing/balance");
}

export function listStatements(page = 1, pageSize = 25): Promise<PagedStatementsResponse> {
  const query = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  return authFetch<PagedStatementsResponse>(`/api/v1/billing/statements?${query}`);
}

export function listWithdrawals(): Promise<WithdrawalRow[]> {
  return authFetch<WithdrawalRow[]>("/api/v1/billing/withdrawals");
}

export function createWithdrawal(amountMinor: number, currency: string): Promise<WithdrawalRow> {
  return authFetch<WithdrawalRow>("/api/v1/billing/withdrawals", {
    method: "POST",
    body: JSON.stringify({ amountMinor, currency }),
  });
}

export function listPlatformWithdrawals(
  status: WithdrawalStatus = "pendingApproval",
): Promise<PlatformWithdrawalRow[]> {
  const query = new URLSearchParams({ status });
  return authFetch<PlatformWithdrawalRow[]>(
    `/api/v1/platform/withdrawals?${query}`,
  );
}

export function approvePlatformWithdrawal(withdrawalId: string): Promise<void> {
  return authFetch<void>(`/api/v1/platform/withdrawals/${withdrawalId}/approve`, {
    method: "POST",
  });
}

export function completePlatformWithdrawal(
  withdrawalId: string,
  transferReference: string,
  proofObjectKey?: string | null,
): Promise<void> {
  return authFetch<void>(`/api/v1/platform/withdrawals/${withdrawalId}/complete`, {
    method: "POST",
    body: JSON.stringify({ transferReference, proofObjectKey: proofObjectKey ?? null }),
  });
}

export function failPlatformWithdrawal(withdrawalId: string): Promise<void> {
  return authFetch<void>(`/api/v1/platform/withdrawals/${withdrawalId}/fail`, {
    method: "POST",
  });
}

export type PlatformPayoutProfileRow = {
  id: string;
  organizationId: string;
  legalEntityType: LegalEntityType;
  legalName: string;
  countryCode: string;
  payoutRail: PayoutRail;
  verificationStatus: PayoutVerificationStatus;
  bankAccountMasked: string | null;
  bankName: string | null;
  documentObjectKeys: string[];
  updatedAt: string;
};

export function listPlatformPayoutProfiles(
  status: PayoutVerificationStatus = "underReview",
): Promise<PlatformPayoutProfileRow[]> {
  const query = new URLSearchParams({ status });
  return authFetch<PlatformPayoutProfileRow[]>(
    `/api/v1/platform/payout-profiles?${query}`,
  );
}

export function approvePlatformPayoutProfile(organizationId: string): Promise<void> {
  return authFetch<void>(
    `/api/v1/platform/payout-profiles/${organizationId}/approve`,
    { method: "POST" },
  );
}

export function rejectPlatformPayoutProfile(
  organizationId: string,
  reason: string,
): Promise<void> {
  return authFetch<void>(
    `/api/v1/platform/payout-profiles/${organizationId}/reject`,
    {
      method: "POST",
      body: JSON.stringify({ reason }),
    },
  );
}

export type PlatformTaxInvoiceRow = {
  id: string;
  invoiceNumber: string;
  purchaseId: string;
  buyerAccountId: string;
  grossMinor: number;
  vatMinor: number;
  netExVatMinor: number;
  currency: string;
  vatRateBps: number;
  issuedAt: string;
};

export type CurrencyVatMovementRow = {
  currency: string;
  invoicedVatMinor: number;
  creditedVatMinor: number;
  netVatMinor: number;
};

export type CurrencyLedgerVatRow = {
  currency: string;
  creditedMinor: number;
  debitedMinor: number;
  netMovementMinor: number;
};

export type PlatformVatSummaryResponse = {
  from: string;
  to: string;
  invoiceMovements: CurrencyVatMovementRow[];
  ledgerMovements: CurrencyLedgerVatRow[];
};

export type PlatformPurchaseRow = {
  id: string;
  buyerAccountId: string;
  listingOrganizationId: string;
  purchasedUnit: string;
  trackId: string | null;
  releaseId: string | null;
  priceSnapshotMinor: number;
  currency: string;
  paymentStatus: string;
  entitlementStatus: string;
  purchasedAt: string;
  paidAt: string | null;
  refundReason: string | null;
  refundFeeBearer: string | null;
  refundedAt: string | null;
};

export type RefundPurchaseRequest = {
  reason: string;
  refundFeeBearer?: "platform" | "seller" | null;
};

export type RefundPurchaseResponse = {
  purchaseId: string;
  paymentStatus: string;
  refundedAt: string;
};

export function listPlatformAccountingInvoices(): Promise<PlatformTaxInvoiceRow[]> {
  return authFetch<PlatformTaxInvoiceRow[]>("/api/v1/platform/accounting/invoices");
}

export function getPlatformAccountingVatSummary(
  from?: string,
  to?: string,
): Promise<PlatformVatSummaryResponse> {
  const query = new URLSearchParams();
  if (from) query.set("from", from);
  if (to) query.set("to", to);
  const suffix = query.size > 0 ? `?${query}` : "";
  return authFetch<PlatformVatSummaryResponse>(
    `/api/v1/platform/accounting/vat-summary${suffix}`,
  );
}

export function listPlatformPurchases(
  queryText?: string,
  paymentStatus?: string,
  limit = 50,
): Promise<PlatformPurchaseRow[]> {
  const query = new URLSearchParams({ limit: String(limit) });
  if (queryText?.trim()) query.set("query", queryText.trim());
  if (paymentStatus?.trim()) query.set("paymentStatus", paymentStatus.trim());
  return authFetch<PlatformPurchaseRow[]>(`/api/v1/platform/purchases?${query}`);
}

export function refundPurchase(
  purchaseId: string,
  body: RefundPurchaseRequest,
): Promise<RefundPurchaseResponse> {
  return authFetch<RefundPurchaseResponse>(
    `/api/v1/billing/purchases/${purchaseId}/refund`,
    {
      method: "POST",
      body: JSON.stringify(body),
    },
  );
}
