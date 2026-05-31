"use client";

import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  listClosedOrganizations,
  recoverOrganization,
  type OrganizationApplicationSummary,
} from "@/lib/api/platformClient";
import { Search } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

function formatTimestamp(value: string): string {
  return new Date(value).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

export function PlatformClosedOrganizationsCard() {
  const [organizations, setOrganizations] = useState<
    OrganizationApplicationSummary[]
  >([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [query, setQuery] = useState("");
  const [busyId, setBusyId] = useState<string | null>(null);
  const [recoverTarget, setRecoverTarget] =
    useState<OrganizationApplicationSummary | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setOrganizations(await listClosedOrganizations());
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Could not load closed organizations.",
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    if (!q) {
      return organizations;
    }
    return organizations.filter((item) =>
      [item.displayName, item.owner.email ?? "", item.organizationId]
        .join(" ")
        .toLowerCase()
        .includes(q),
    );
  }, [organizations, query]);

  async function onConfirmRecover() {
    if (!recoverTarget) {
      return;
    }
    setBusyId(recoverTarget.organizationId);
    setError(null);
    try {
      await recoverOrganization(recoverTarget.organizationId);
      setRecoverTarget(null);
      setOrganizations((items) =>
        items.filter((item) => item.organizationId !== recoverTarget.organizationId),
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Recovery failed.");
      throw err;
    } finally {
      setBusyId(null);
    }
  }

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>Closed organizations</CardTitle>
          <CardDescription>
            Soft-deleted organizations removed from member workspace lists. Recover
            when an owner deleted by mistake or support approves restoration.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="relative max-w-md">
            <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search name, email, organization id…"
              className="pl-9"
              aria-label="Search closed organizations"
            />
          </div>

          {loading ? (
            <div className="flex flex-col gap-2">
              <Skeleton className="h-24 w-full" />
              <Skeleton className="h-24 w-full" />
            </div>
          ) : null}

          {error ? <p className="text-sm text-destructive">{error}</p> : null}

          {!loading && filtered.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No closed organizations{query ? " match your search" : ""}.
            </p>
          ) : null}

          <ul className="flex flex-col gap-3">
            {filtered.map((item) => {
              const busy = busyId === item.organizationId;
              return (
                <li
                  key={item.organizationId}
                  className="flex flex-col gap-3 rounded-lg border bg-card p-4 sm:flex-row sm:items-center sm:justify-between"
                >
                  <div className="min-w-0 space-y-1">
                    <p className="font-semibold">{item.displayName}</p>
                    <p className="text-sm text-muted-foreground">
                      Closed · updated {formatTimestamp(item.updatedAt)}
                    </p>
                    <p className="break-all font-mono text-xs text-muted-foreground">
                      {item.organizationId}
                    </p>
                    {item.owner.email ? (
                      <p className="text-sm text-muted-foreground">{item.owner.email}</p>
                    ) : null}
                  </div>
                  <Button
                    size="sm"
                    variant="outline"
                    disabled={busy}
                    onClick={() => setRecoverTarget(item)}
                  >
                    Recover
                  </Button>
                </li>
              );
            })}
          </ul>
        </CardContent>
      </Card>

      <ConfirmDialog
        open={recoverTarget !== null}
        onOpenChange={(open) => {
          if (!open && !busyId) {
            setRecoverTarget(null);
          }
        }}
        title="Recover organization?"
        description={
          recoverTarget
            ? `${recoverTarget.displayName} will become active again for members who still have access.`
            : ""
        }
        confirmLabel="Recover organization"
        busy={busyId !== null}
        onConfirm={onConfirmRecover}
      />
    </>
  );
}
