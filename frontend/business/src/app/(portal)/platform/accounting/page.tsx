"use client";

import { AccountingInvoicesTable } from "@/components/platform/accounting/AccountingInvoicesTable";
import { AccountingVatSummaryTables } from "@/components/platform/accounting/AccountingVatSummaryTables";
import { PlatformPersonaGate } from "@/components/portal/PlatformPersonaGate";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import {
  getPlatformAccountingVatSummary,
  listPlatformAccountingInvoices,
  type PlatformTaxInvoiceRow,
  type PlatformVatSummaryResponse,
} from "@/lib/api/financeClient";
import { canReadPlatformAccounting } from "@/lib/auth/platformClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import { formatDateTime } from "@/lib/format/dateTime";
import {
  type InvoiceSortKey,
  dateInputToRangeEndIso,
  dateInputToRangeStartIso,
  defaultAccountingFromDate,
  defaultAccountingToDate,
  invoiceInDateRange,
  invoiceMatchesSearch,
  sortInvoices,
  toggleSort,
} from "@/lib/platform/accountingView";
import { RefreshCw, Search } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

type DatePreset = "30d" | "month" | "quarter";

function applyDatePreset(preset: DatePreset): { from: string; to: string } {
  const to = defaultAccountingToDate();
  const toDate = new Date(`${to}T00:00:00.000Z`);

  if (preset === "month") {
    const from = new Date(Date.UTC(toDate.getUTCFullYear(), toDate.getUTCMonth(), 1));
    return { from: from.toISOString().slice(0, 10), to };
  }

  if (preset === "quarter") {
    const from = new Date(toDate);
    from.setUTCMonth(from.getUTCMonth() - 3);
    return { from: from.toISOString().slice(0, 10), to };
  }

  return { from: defaultAccountingFromDate(), to };
}

export default function PlatformAccountingPage() {
  return (
    <PlatformPersonaGate>
      <AccountingContent />
    </PlatformPersonaGate>
  );
}

function AccountingContent() {
  const token = getAccessToken();
  const canRead = canReadPlatformAccounting(token);

  const [invoices, setInvoices] = useState<PlatformTaxInvoiceRow[]>([]);
  const [vatSummary, setVatSummary] = useState<PlatformVatSummaryResponse | null>(null);
  const [loadingInvoices, setLoadingInvoices] = useState(true);
  const [loadingSummary, setLoadingSummary] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [fromDate, setFromDate] = useState(defaultAccountingFromDate);
  const [toDate, setToDate] = useState(defaultAccountingToDate);
  const [appliedFromDate, setAppliedFromDate] = useState(defaultAccountingFromDate);
  const [appliedToDate, setAppliedToDate] = useState(defaultAccountingToDate);
  const [currencyFilter, setCurrencyFilter] = useState("all");

  const [invoiceSortBy, setInvoiceSortBy] = useState<InvoiceSortKey>("issuedAt");
  const [invoiceSortDirection, setInvoiceSortDirection] = useState<"asc" | "desc">("desc");

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedSearch(search.trim()), 250);
    return () => window.clearTimeout(timer);
  }, [search]);

  const loadInvoices = useCallback(async () => {
    setLoadingInvoices(true);
    setError(null);
    try {
      const rows = await listPlatformAccountingInvoices();
      setInvoices(rows);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load tax invoices.");
    } finally {
      setLoadingInvoices(false);
    }
  }, []);

  const loadVatSummary = useCallback(async (from: string, to: string) => {
    setLoadingSummary(true);
    setError(null);
    try {
      const summary = await getPlatformAccountingVatSummary(
        dateInputToRangeStartIso(from),
        dateInputToRangeEndIso(to),
      );
      setVatSummary(summary);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not load VAT summary.");
    } finally {
      setLoadingSummary(false);
    }
  }, []);

  useEffect(() => {
    if (!canRead) {
      return;
    }
    void loadInvoices();
    void loadVatSummary(appliedFromDate, appliedToDate);
  }, [canRead, loadInvoices, loadVatSummary, appliedFromDate, appliedToDate]);

  const currencyOptions = useMemo(() => {
    const values = new Set<string>();
    for (const invoice of invoices) {
      values.add(invoice.currency);
    }
    for (const row of vatSummary?.invoiceMovements ?? []) {
      values.add(row.currency);
    }
    return [...values].sort((left, right) => left.localeCompare(right));
  }, [invoices, vatSummary]);

  const filteredInvoices = useMemo(() => {
    const filtered = invoices.filter(
      (invoice) =>
        invoiceInDateRange(invoice, appliedFromDate, appliedToDate) &&
        invoiceMatchesSearch(invoice, debouncedSearch) &&
        (currencyFilter === "all" || invoice.currency === currencyFilter),
    );
    return sortInvoices(filtered, invoiceSortBy, invoiceSortDirection);
  }, [
    appliedFromDate,
    appliedToDate,
    currencyFilter,
    debouncedSearch,
    invoiceSortBy,
    invoiceSortDirection,
    invoices,
  ]);

  function onInvoiceSort(column: InvoiceSortKey) {
    const next = toggleSort(column, invoiceSortBy, invoiceSortDirection);
    setInvoiceSortBy(next.sortBy);
    setInvoiceSortDirection(next.sortDirection);
  }

  function applyFilters() {
    setAppliedFromDate(fromDate);
    setAppliedToDate(toDate);
  }

  function applyPreset(preset: DatePreset) {
    const range = applyDatePreset(preset);
    setFromDate(range.from);
    setToDate(range.to);
    setAppliedFromDate(range.from);
    setAppliedToDate(range.to);
  }

  async function refreshAll() {
    await Promise.all([
      loadInvoices(),
      loadVatSummary(appliedFromDate, appliedToDate),
    ]);
  }

  if (!canRead) {
    return (
      <p className="text-sm text-muted-foreground">
        Your platform token does not include accounting read permissions.
      </p>
    );
  }

  const loading = loadingInvoices || loadingSummary;

  return (
    <div className="mx-auto flex w-full max-w-6xl flex-col gap-6">
      <div className="space-y-1">
        <h1 className="text-2xl font-semibold tracking-tight">Accounting</h1>
        <p className="text-sm text-muted-foreground">
          Tax invoices, VAT movement, and ledger reconciliation for platform finance ops.
        </p>
      </div>

      <Card className="gap-3">
        <CardHeader className="space-y-0 pb-0">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
            <div className="space-y-1">
              <CardTitle>Filters</CardTitle>
              <CardDescription>
                Period drives VAT summary from the server. Invoice search and currency filter apply
                to the loaded invoice list (recent 500).
              </CardDescription>
            </div>
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="shrink-0 gap-2"
              disabled={loading}
              onClick={() => void refreshAll()}
            >
              <RefreshCw className="size-4" />
              Refresh
            </Button>
          </div>
        </CardHeader>
        <CardContent className="flex flex-col gap-4 pt-0">
          <div className="relative max-w-md">
            <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search invoice #, purchase id, buyer id…"
              className="pl-9"
              aria-label="Search tax invoices"
            />
          </div>

          <div className="flex flex-wrap items-end gap-3">
            <div className="space-y-1">
              <Label htmlFor="accounting-from">From</Label>
              <Input
                id="accounting-from"
                type="date"
                value={fromDate}
                onChange={(event) => setFromDate(event.target.value)}
                className="w-[11rem]"
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor="accounting-to">To</Label>
              <Input
                id="accounting-to"
                type="date"
                value={toDate}
                onChange={(event) => setToDate(event.target.value)}
                className="w-[11rem]"
              />
            </div>
            <div className="space-y-1">
              <Label htmlFor="accounting-currency">Currency</Label>
              <select
                id="accounting-currency"
                className="h-9 w-[8rem] rounded-md border border-input bg-background px-3 text-sm"
                value={currencyFilter}
                onChange={(event) => setCurrencyFilter(event.target.value)}
              >
                <option value="all">All</option>
                {currencyOptions.map((currency) => (
                  <option key={currency} value={currency}>
                    {currency}
                  </option>
                ))}
              </select>
            </div>
            <Button type="button" onClick={applyFilters} disabled={loadingSummary}>
              Apply period
            </Button>
          </div>

          <div className="flex flex-wrap gap-2">
            <Button type="button" size="sm" variant="secondary" onClick={() => applyPreset("30d")}>
              Last 30 days
            </Button>
            <Button
              type="button"
              size="sm"
              variant="secondary"
              onClick={() => applyPreset("month")}
            >
              This month
            </Button>
            <Button
              type="button"
              size="sm"
              variant="secondary"
              onClick={() => applyPreset("quarter")}
            >
              Last 3 months
            </Button>
          </div>

          {error ? <p className="text-sm text-destructive">{error}</p> : null}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>VAT summary</CardTitle>
          <CardDescription>
            {vatSummary
              ? `Period ${formatDateTime(vatSummary.from)} – ${formatDateTime(vatSummary.to)}`
              : "Invoiced vs credited VAT and ledger VatPayable movement."}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {loadingSummary ? (
            <div className="grid gap-4 xl:grid-cols-2">
              <Skeleton className="h-40 w-full" />
              <Skeleton className="h-40 w-full" />
            </div>
          ) : vatSummary ? (
            <AccountingVatSummaryTables
              invoiceMovements={vatSummary.invoiceMovements}
              ledgerMovements={vatSummary.ledgerMovements}
              currencyFilter={currencyFilter}
            />
          ) : null}
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
          <div className="space-y-1">
            <CardTitle>Tax invoices</CardTitle>
            <CardDescription>
              Buyer tax invoices sorted and filtered for the selected period.
            </CardDescription>
          </div>
          <p className="text-sm text-muted-foreground">
            Showing {filteredInvoices.length} of {invoices.length}
          </p>
        </CardHeader>
        <CardContent>
          {loadingInvoices ? (
            <div className="flex flex-col gap-2">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
            </div>
          ) : (
            <AccountingInvoicesTable
              rows={filteredInvoices}
              sortBy={invoiceSortBy}
              sortDirection={invoiceSortDirection}
              onSort={onInvoiceSort}
            />
          )}
        </CardContent>
      </Card>
    </div>
  );
}
