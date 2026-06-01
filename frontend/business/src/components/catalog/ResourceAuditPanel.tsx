"use client";

import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  listResourceAudits,
  type CatalogAuditEntryResponse,
  type CatalogAuditListResponse,
  type CatalogAuditTableName,
} from "@/lib/api/catalogClient";
import type { TenancyAuditListResponse } from "@/lib/api/tenancyClient";
import { ChevronDown, ChevronRight } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

type ResourceAuditPanelProps = {
  tableName?: CatalogAuditTableName;
  targetId: string;
  title?: string;
  loadAudits?: (targetId: string) => Promise<CatalogAuditListResponse | TenancyAuditListResponse>;
};

function formatTimestamp(iso: string): string {
  return new Date(iso).toLocaleString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    timeZoneName: "short",
  });
}

function formatAction(action: string): string {
  return action.replace(/_/g, " ").replace(/^./, (c) => c.toUpperCase());
}

function formatActor(actorAccountId: string | null): string {
  if (!actorAccountId) {
    return "System";
  }
  return actorAccountId.slice(0, 8);
}

function parseJson(value: string | null): unknown {
  if (!value) {
    return null;
  }
  try {
    return JSON.parse(value) as unknown;
  } catch {
    return value;
  }
}

function AuditEntryRow({ entry }: { entry: CatalogAuditEntryResponse }) {
  const [expanded, setExpanded] = useState(false);
  const hasDiff = entry.beforeJson || entry.afterJson;

  return (
    <li className="rounded border px-3 py-2">
      <button
        type="button"
        className="flex w-full items-start gap-2 text-left"
        disabled={!hasDiff}
        onClick={() => setExpanded((current) => !current)}
      >
        {hasDiff ? (
          expanded ? (
            <ChevronDown className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
          ) : (
            <ChevronRight className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
          )
        ) : (
          <span className="size-4 shrink-0" />
        )}
        <div className="min-w-0 flex-1">
          <p className="text-sm font-medium">{formatAction(entry.action)}</p>
          <p className="text-xs text-muted-foreground">
            {formatTimestamp(entry.changedAt)} · Actor {formatActor(entry.actorAccountId)}
          </p>
        </div>
      </button>
      {expanded && hasDiff ? (
        <div className="mt-3 grid gap-3 sm:grid-cols-2">
          <div>
            <p className="mb-1 text-xs font-medium text-muted-foreground">Before</p>
            <pre className="max-h-48 overflow-auto rounded bg-muted/50 p-2 text-xs">
              {JSON.stringify(parseJson(entry.beforeJson), null, 2) ?? "—"}
            </pre>
          </div>
          <div>
            <p className="mb-1 text-xs font-medium text-muted-foreground">After</p>
            <pre className="max-h-48 overflow-auto rounded bg-muted/50 p-2 text-xs">
              {JSON.stringify(parseJson(entry.afterJson), null, 2) ?? "—"}
            </pre>
          </div>
        </div>
      ) : null}
    </li>
  );
}

export function ResourceAuditPanel({
  tableName,
  targetId,
  title = "Change history",
  loadAudits,
}: ResourceAuditPanelProps) {
  const [items, setItems] = useState<CatalogAuditEntryResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!targetId) {
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const response = loadAudits
        ? await loadAudits(targetId)
        : await listResourceAudits(tableName!, targetId);
      setItems(response.items);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load audit history.");
    } finally {
      setLoading(false);
    }
  }, [tableName, targetId, loadAudits]);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-4">
        <div>
          <CardTitle>{title}</CardTitle>
          <CardDescription>
            {loading
              ? "Loading audit entries…"
              : `${items.length} entr${items.length === 1 ? "y" : "ies"} (most recent first)`}
          </CardDescription>
        </div>
        <button
          type="button"
          className="text-xs text-muted-foreground hover:text-foreground"
          onClick={() => void load()}
          disabled={loading}
        >
          Refresh
        </button>
      </CardHeader>
      <CardContent>
        {error ? <p className="text-sm text-destructive">{error}</p> : null}
        {!loading && items.length === 0 ? (
          <p className="text-sm text-muted-foreground">No audit entries yet.</p>
        ) : (
          <ul className="space-y-2">
            {items.map((entry) => (
              <AuditEntryRow key={entry.id} entry={entry} />
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
