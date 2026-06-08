import type { PlatformTaxInvoiceRow } from "@/lib/api/financeClient";

export type InvoiceSortKey =
  | "invoiceNumber"
  | "issuedAt"
  | "purchaseId"
  | "buyerAccountId"
  | "grossMinor"
  | "vatMinor"
  | "netExVatMinor"
  | "currency"
  | "vatRateBps";

export type VatMovementSortKey =
  | "currency"
  | "invoicedVatMinor"
  | "creditedVatMinor"
  | "netVatMinor";

export type LedgerVatSortKey =
  | "currency"
  | "creditedMinor"
  | "debitedMinor"
  | "netMovementMinor";

export function defaultAccountingFromDate(): string {
  const date = new Date();
  date.setUTCDate(date.getUTCDate() - 30);
  return date.toISOString().slice(0, 10);
}

export function defaultAccountingToDate(): string {
  return new Date().toISOString().slice(0, 10);
}

export function dateInputToRangeStartIso(date: string): string {
  return `${date}T00:00:00.000Z`;
}

export function dateInputToRangeEndIso(date: string): string {
  return `${date}T23:59:59.999Z`;
}

export function invoiceMatchesSearch(invoice: PlatformTaxInvoiceRow, query: string): boolean {
  const q = query.trim().toLowerCase();
  if (!q) {
    return true;
  }

  const haystack = [
    invoice.invoiceNumber,
    invoice.purchaseId,
    invoice.buyerAccountId,
    invoice.id,
    invoice.currency,
  ]
    .join(" ")
    .toLowerCase();

  return haystack.includes(q);
}

export function invoiceInDateRange(
  invoice: PlatformTaxInvoiceRow,
  fromDate: string,
  toDate: string,
): boolean {
  const issuedAt = new Date(invoice.issuedAt).getTime();
  if (Number.isNaN(issuedAt)) {
    return false;
  }

  const from = new Date(dateInputToRangeStartIso(fromDate)).getTime();
  const to = new Date(dateInputToRangeEndIso(toDate)).getTime();
  return issuedAt >= from && issuedAt <= to;
}

export function sortInvoices(
  rows: PlatformTaxInvoiceRow[],
  sortBy: InvoiceSortKey,
  sortDirection: "asc" | "desc",
): PlatformTaxInvoiceRow[] {
  const direction = sortDirection === "asc" ? 1 : -1;

  return [...rows].sort((left, right) => {
    const compareStrings = (a: string, b: string) => a.localeCompare(b, undefined, { sensitivity: "base" });
    const compareNumbers = (a: number, b: number) => a - b;

    let result = 0;
    switch (sortBy) {
      case "invoiceNumber":
        result = compareStrings(left.invoiceNumber, right.invoiceNumber);
        break;
      case "issuedAt":
        result = compareStrings(left.issuedAt, right.issuedAt);
        break;
      case "purchaseId":
        result = compareStrings(left.purchaseId, right.purchaseId);
        break;
      case "buyerAccountId":
        result = compareStrings(left.buyerAccountId, right.buyerAccountId);
        break;
      case "grossMinor":
        result = compareNumbers(left.grossMinor, right.grossMinor);
        break;
      case "vatMinor":
        result = compareNumbers(left.vatMinor, right.vatMinor);
        break;
      case "netExVatMinor":
        result = compareNumbers(left.netExVatMinor, right.netExVatMinor);
        break;
      case "currency":
        result = compareStrings(left.currency, right.currency);
        break;
      case "vatRateBps":
        result = compareNumbers(left.vatRateBps, right.vatRateBps);
        break;
    }

    return result * direction;
  });
}

export function toggleSort<T extends string>(
  column: T,
  sortBy: T,
  sortDirection: "asc" | "desc",
): { sortBy: T; sortDirection: "asc" | "desc" } {
  if (sortBy === column) {
    return { sortBy, sortDirection: sortDirection === "asc" ? "desc" : "asc" };
  }

  return { sortBy: column, sortDirection: "asc" };
}

export function formatVatRateBps(vatRateBps: number): string {
  return `${(vatRateBps / 100).toFixed(2)}%`;
}
