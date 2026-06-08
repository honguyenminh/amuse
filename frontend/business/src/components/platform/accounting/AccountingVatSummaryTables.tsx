"use client";

import { PlatformSortHeader } from "@/components/platform/PlatformSortHeader";
import type {
  CurrencyLedgerVatRow,
  CurrencyVatMovementRow,
} from "@/lib/api/financeClient";
import { formatMinor } from "@/lib/finance/formatMoney";
import {
  type LedgerVatSortKey,
  type VatMovementSortKey,
  toggleSort,
} from "@/lib/platform/accountingView";
import { cn } from "@/lib/utils";
import { useMemo, useState } from "react";

type AccountingVatSummaryTablesProps = {
  invoiceMovements: CurrencyVatMovementRow[];
  ledgerMovements: CurrencyLedgerVatRow[];
  currencyFilter: string;
};

function sortVatMovements(
  rows: CurrencyVatMovementRow[],
  sortBy: VatMovementSortKey,
  sortDirection: "asc" | "desc",
): CurrencyVatMovementRow[] {
  const direction = sortDirection === "asc" ? 1 : -1;
  return [...rows].sort((left, right) => {
    let result = 0;
    switch (sortBy) {
      case "currency":
        result = left.currency.localeCompare(right.currency);
        break;
      case "invoicedVatMinor":
        result = left.invoicedVatMinor - right.invoicedVatMinor;
        break;
      case "creditedVatMinor":
        result = left.creditedVatMinor - right.creditedVatMinor;
        break;
      case "netVatMinor":
        result = left.netVatMinor - right.netVatMinor;
        break;
    }
    return result * direction;
  });
}

function sortLedgerMovements(
  rows: CurrencyLedgerVatRow[],
  sortBy: LedgerVatSortKey,
  sortDirection: "asc" | "desc",
): CurrencyLedgerVatRow[] {
  const direction = sortDirection === "asc" ? 1 : -1;
  return [...rows].sort((left, right) => {
    let result = 0;
    switch (sortBy) {
      case "currency":
        result = left.currency.localeCompare(right.currency);
        break;
      case "creditedMinor":
        result = left.creditedMinor - right.creditedMinor;
        break;
      case "debitedMinor":
        result = left.debitedMinor - right.debitedMinor;
        break;
      case "netMovementMinor":
        result = left.netMovementMinor - right.netMovementMinor;
        break;
    }
    return result * direction;
  });
}

function EmptyRow({ colSpan, message }: { colSpan: number; message: string }) {
  return (
    <tr>
      <td
        colSpan={colSpan}
        className="px-4 py-10 text-center text-sm text-muted-foreground"
      >
        {message}
      </td>
    </tr>
  );
}

export function AccountingVatSummaryTables({
  invoiceMovements,
  ledgerMovements,
  currencyFilter,
}: AccountingVatSummaryTablesProps) {
  const [invoiceSortBy, setInvoiceSortBy] = useState<VatMovementSortKey>("currency");
  const [invoiceSortDirection, setInvoiceSortDirection] = useState<"asc" | "desc">("asc");
  const [ledgerSortBy, setLedgerSortBy] = useState<LedgerVatSortKey>("currency");
  const [ledgerSortDirection, setLedgerSortDirection] = useState<"asc" | "desc">("asc");

  const filteredInvoiceMovements = useMemo(() => {
    const rows =
      currencyFilter === "all"
        ? invoiceMovements
        : invoiceMovements.filter((row) => row.currency === currencyFilter);
    return sortVatMovements(rows, invoiceSortBy, invoiceSortDirection);
  }, [currencyFilter, invoiceMovements, invoiceSortBy, invoiceSortDirection]);

  const filteredLedgerMovements = useMemo(() => {
    const rows =
      currencyFilter === "all"
        ? ledgerMovements
        : ledgerMovements.filter((row) => row.currency === currencyFilter);
    return sortLedgerMovements(rows, ledgerSortBy, ledgerSortDirection);
  }, [currencyFilter, ledgerMovements, ledgerSortBy, ledgerSortDirection]);

  function onInvoiceSort(column: VatMovementSortKey) {
    const next = toggleSort(column, invoiceSortBy, invoiceSortDirection);
    setInvoiceSortBy(next.sortBy);
    setInvoiceSortDirection(next.sortDirection);
  }

  function onLedgerSort(column: LedgerVatSortKey) {
    const next = toggleSort(column, ledgerSortBy, ledgerSortDirection);
    setLedgerSortBy(next.sortBy);
    setLedgerSortDirection(next.sortDirection);
  }

  return (
    <div className="grid gap-6 xl:grid-cols-2">
      <div className="space-y-2">
        <h3 className="text-sm font-medium text-foreground">Invoiced vs credited VAT</h3>
        <div className="overflow-x-auto rounded-md border bg-background">
          <table className="w-full min-w-[32rem] border-collapse text-sm">
            <thead>
              <tr className="border-b bg-muted/50 text-left text-muted-foreground">
                <th className="px-4 py-3">
                  <PlatformSortHeader
                    label="Currency"
                    column="currency"
                    sortBy={invoiceSortBy}
                    sortDirection={invoiceSortDirection}
                    onSort={onInvoiceSort}
                  />
                </th>
                <th className="px-4 py-3 text-right">
                  <PlatformSortHeader
                    label="Invoiced VAT"
                    column="invoicedVatMinor"
                    sortBy={invoiceSortBy}
                    sortDirection={invoiceSortDirection}
                    onSort={onInvoiceSort}
                    align="right"
                  />
                </th>
                <th className="px-4 py-3 text-right">
                  <PlatformSortHeader
                    label="Credited VAT"
                    column="creditedVatMinor"
                    sortBy={invoiceSortBy}
                    sortDirection={invoiceSortDirection}
                    onSort={onInvoiceSort}
                    align="right"
                  />
                </th>
                <th className="px-4 py-3 text-right">
                  <PlatformSortHeader
                    label="Net VAT"
                    column="netVatMinor"
                    sortBy={invoiceSortBy}
                    sortDirection={invoiceSortDirection}
                    onSort={onInvoiceSort}
                    align="right"
                  />
                </th>
              </tr>
            </thead>
            <tbody>
              {filteredInvoiceMovements.length === 0 ? (
                <EmptyRow colSpan={4} message="No VAT movements in this period." />
              ) : (
                filteredInvoiceMovements.map((row) => (
                  <tr key={row.currency} className="border-b last:border-b-0 hover:bg-muted/30">
                    <td className="px-4 py-3 font-medium">{row.currency}</td>
                    <td className="px-4 py-3 text-right tabular-nums text-muted-foreground">
                      {formatMinor(row.invoicedVatMinor, row.currency)}
                    </td>
                    <td className="px-4 py-3 text-right tabular-nums text-muted-foreground">
                      {formatMinor(row.creditedVatMinor, row.currency)}
                    </td>
                    <td className="px-4 py-3 text-right tabular-nums font-medium">
                      {formatMinor(row.netVatMinor, row.currency)}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      <div className="space-y-2">
        <h3 className="text-sm font-medium text-foreground">Ledger VatPayable movement</h3>
        <div className="overflow-x-auto rounded-md border bg-background">
          <table className="w-full min-w-[32rem] border-collapse text-sm">
            <thead>
              <tr className="border-b bg-muted/50 text-left text-muted-foreground">
                <th className="px-4 py-3">
                  <PlatformSortHeader
                    label="Currency"
                    column="currency"
                    sortBy={ledgerSortBy}
                    sortDirection={ledgerSortDirection}
                    onSort={onLedgerSort}
                  />
                </th>
                <th className="px-4 py-3 text-right">
                  <PlatformSortHeader
                    label="Credits"
                    column="creditedMinor"
                    sortBy={ledgerSortBy}
                    sortDirection={ledgerSortDirection}
                    onSort={onLedgerSort}
                    align="right"
                  />
                </th>
                <th className="px-4 py-3 text-right">
                  <PlatformSortHeader
                    label="Debits"
                    column="debitedMinor"
                    sortBy={ledgerSortBy}
                    sortDirection={ledgerSortDirection}
                    onSort={onLedgerSort}
                    align="right"
                  />
                </th>
                <th className="px-4 py-3 text-right">
                  <PlatformSortHeader
                    label="Net"
                    column="netMovementMinor"
                    sortBy={ledgerSortBy}
                    sortDirection={ledgerSortDirection}
                    onSort={onLedgerSort}
                    align="right"
                  />
                </th>
              </tr>
            </thead>
            <tbody>
              {filteredLedgerMovements.length === 0 ? (
                <EmptyRow colSpan={4} message="No ledger VAT movement in this period." />
              ) : (
                filteredLedgerMovements.map((row) => (
                  <tr key={row.currency} className="border-b last:border-b-0 hover:bg-muted/30">
                    <td className="px-4 py-3 font-medium">{row.currency}</td>
                    <td className="px-4 py-3 text-right tabular-nums text-muted-foreground">
                      {formatMinor(row.creditedMinor, row.currency)}
                    </td>
                    <td className="px-4 py-3 text-right tabular-nums text-muted-foreground">
                      {formatMinor(row.debitedMinor, row.currency)}
                    </td>
                    <td
                      className={cn(
                        "px-4 py-3 text-right tabular-nums font-medium",
                        row.netMovementMinor < 0 && "text-destructive",
                      )}
                    >
                      {formatMinor(row.netMovementMinor, row.currency)}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
