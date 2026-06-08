"use client";

import { PlatformSortHeader } from "@/components/platform/PlatformSortHeader";
import type { PlatformTaxInvoiceRow } from "@/lib/api/financeClient";
import { formatTableDateTimeParts } from "@/lib/format/dateTime";
import { formatMinor } from "@/lib/finance/formatMoney";
import {
  type InvoiceSortKey,
  formatVatRateBps,
} from "@/lib/platform/accountingView";

type AccountingInvoicesTableProps = {
  rows: PlatformTaxInvoiceRow[];
  sortBy: InvoiceSortKey;
  sortDirection: "asc" | "desc";
  onSort: (column: InvoiceSortKey) => void;
};

function TableDateCell({ value }: { value: string }) {
  const parts = formatTableDateTimeParts(value);
  if (!parts) {
    return <span className="text-muted-foreground">—</span>;
  }

  return (
    <time
      dateTime={parts.iso}
      title={parts.title}
      className="block min-w-0 text-xs leading-snug text-muted-foreground"
    >
      <span className="block">{parts.date}</span>
      <span className="block tabular-nums">{parts.time}</span>
    </time>
  );
}

function MonoCell({ value }: { value: string }) {
  return (
    <span className="break-all font-mono text-xs text-muted-foreground" title={value}>
      {value}
    </span>
  );
}

export function AccountingInvoicesTable({
  rows,
  sortBy,
  sortDirection,
  onSort,
}: AccountingInvoicesTableProps) {
  if (rows.length === 0) {
    return (
      <p className="rounded-md border border-dashed bg-background px-4 py-12 text-center text-sm text-muted-foreground">
        No tax invoices match your filters.
      </p>
    );
  }

  return (
    <div className="overflow-x-auto rounded-md border bg-background">
      <table className="w-full min-w-[72rem] table-fixed border-collapse text-sm">
        <colgroup>
          <col className="w-[11rem]" />
          <col className="w-[7rem]" />
          <col className="w-[11rem]" />
          <col className="w-[11rem]" />
          <col className="w-[7rem]" />
          <col className="w-[7rem]" />
          <col className="w-[7rem]" />
          <col className="w-[4rem]" />
          <col className="w-[4rem]" />
        </colgroup>
        <thead>
          <tr className="border-b bg-muted/50 text-left text-muted-foreground">
            <th className="px-4 py-3">
              <PlatformSortHeader
                label="Invoice"
                column="invoiceNumber"
                sortBy={sortBy}
                sortDirection={sortDirection}
                onSort={onSort}
              />
            </th>
            <th className="px-4 py-3">
              <PlatformSortHeader
                label="Issued"
                column="issuedAt"
                sortBy={sortBy}
                sortDirection={sortDirection}
                onSort={onSort}
              />
            </th>
            <th className="px-4 py-3">
              <PlatformSortHeader
                label="Purchase"
                column="purchaseId"
                sortBy={sortBy}
                sortDirection={sortDirection}
                onSort={onSort}
              />
            </th>
            <th className="px-4 py-3">
              <PlatformSortHeader
                label="Buyer"
                column="buyerAccountId"
                sortBy={sortBy}
                sortDirection={sortDirection}
                onSort={onSort}
              />
            </th>
            <th className="px-4 py-3 text-right">
              <PlatformSortHeader
                label="Gross"
                column="grossMinor"
                sortBy={sortBy}
                sortDirection={sortDirection}
                onSort={onSort}
                align="right"
              />
            </th>
            <th className="px-4 py-3 text-right">
              <PlatformSortHeader
                label="VAT"
                column="vatMinor"
                sortBy={sortBy}
                sortDirection={sortDirection}
                onSort={onSort}
                align="right"
              />
            </th>
            <th className="px-4 py-3 text-right">
              <PlatformSortHeader
                label="Net ex VAT"
                column="netExVatMinor"
                sortBy={sortBy}
                sortDirection={sortDirection}
                onSort={onSort}
                align="right"
              />
            </th>
            <th className="px-4 py-3">
              <PlatformSortHeader
                label="CCY"
                column="currency"
                sortBy={sortBy}
                sortDirection={sortDirection}
                onSort={onSort}
              />
            </th>
            <th className="px-4 py-3 text-right">
              <PlatformSortHeader
                label="Rate"
                column="vatRateBps"
                sortBy={sortBy}
                sortDirection={sortDirection}
                onSort={onSort}
                align="right"
              />
            </th>
          </tr>
        </thead>
        <tbody>
          {rows.map((invoice) => (
            <tr key={invoice.id} className="border-b last:border-b-0 hover:bg-muted/30">
              <td className="px-4 py-3 align-top font-medium text-foreground">
                {invoice.invoiceNumber}
              </td>
              <td className="px-4 py-3 align-top">
                <TableDateCell value={invoice.issuedAt} />
              </td>
              <td className="px-4 py-3 align-top">
                <MonoCell value={invoice.purchaseId} />
              </td>
              <td className="px-4 py-3 align-top">
                <MonoCell value={invoice.buyerAccountId} />
              </td>
              <td className="px-4 py-3 align-top text-right tabular-nums">
                {formatMinor(invoice.grossMinor, invoice.currency)}
              </td>
              <td className="px-4 py-3 align-top text-right tabular-nums text-muted-foreground">
                {formatMinor(invoice.vatMinor, invoice.currency)}
              </td>
              <td className="px-4 py-3 align-top text-right tabular-nums">
                {formatMinor(invoice.netExVatMinor, invoice.currency)}
              </td>
              <td className="px-4 py-3 align-top font-medium">{invoice.currency}</td>
              <td className="px-4 py-3 align-top text-right tabular-nums text-muted-foreground">
                {formatVatRateBps(invoice.vatRateBps)}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
