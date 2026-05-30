"use client";

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
import { Skeleton } from "@/components/ui/skeleton";
import {
  approveOrganization,
  listOrganizationApplications,
  rejectOrganization,
  type OrganizationApplicationSummary,
} from "@/lib/api/platformClient";
import { Search } from "lucide-react";
import { useCallback, useEffect, useMemo, useState } from "react";

function formatOrgClass(orgClass: string): string {
  if (orgClass === "backingOrg") {
    return "Backing organization";
  }
  if (orgClass === "indieGroup") {
    return "Indie group";
  }
  return orgClass;
}

function formatTimestamp(value: string): string {
  return new Date(value).toLocaleString(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

export default function PlatformApplicationsPage() {
  return (
    <PlatformPersonaGate>
      <ApplicationsContent />
    </PlatformPersonaGate>
  );
}

function ApplicationsContent() {
  const [applications, setApplications] = useState<
    OrganizationApplicationSummary[]
  >([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [query, setQuery] = useState("");
  const [busyId, setBusyId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const items = await listOrganizationApplications("pendingReview");
      setApplications(items);
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Could not load applications.",
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
      return applications;
    }
    return applications.filter((item) => {
      const haystack = [
        item.displayName,
        item.owner.email ?? "",
        item.owner.idpIssuer,
        item.owner.idpSubject,
        item.owner.accountId,
        item.organizationId,
      ]
        .join(" ")
        .toLowerCase();
      return haystack.includes(q);
    });
  }, [applications, query]);

  async function onApprove(organizationId: string) {
    setBusyId(organizationId);
    try {
      await approveOrganization(organizationId);
      setApplications((items) =>
        items.filter((item) => item.organizationId !== organizationId),
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Approval failed.");
    } finally {
      setBusyId(null);
    }
  }

  async function onReject(organizationId: string) {
    const reason = window.prompt("Rejection reason (required):");
    if (!reason?.trim()) {
      return;
    }
    setBusyId(organizationId);
    try {
      await rejectOrganization(organizationId, reason.trim());
      setApplications((items) =>
        items.filter((item) => item.organizationId !== organizationId),
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Rejection failed.");
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="mx-auto flex w-full max-w-4xl flex-col gap-4">
      <Card>
        <CardHeader>
          <CardTitle>Organization applications</CardTitle>
          <CardDescription>
            Review backing organizations awaiting platform approval. Use owner
            contact details below for off-platform follow-up before you approve
            or reject.
          </CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="relative max-w-md">
            <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search name, email, account id…"
              className="pl-9"
              aria-label="Search applications"
            />
          </div>

          {loading ? (
            <div className="flex flex-col gap-2">
              <Skeleton className="h-32 w-full" />
              <Skeleton className="h-32 w-full" />
            </div>
          ) : null}

          {error ? <p className="text-sm text-destructive">{error}</p> : null}

          {!loading && filtered.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No pending applications
              {query ? " match your search" : ""}.
            </p>
          ) : null}

          <ul className="flex flex-col gap-4">
            {filtered.map((item) => {
              const busy = busyId === item.organizationId;
              const owner = item.owner;
              return (
                <li
                  key={item.organizationId}
                  className="flex flex-col gap-4 rounded-lg border bg-card p-4"
                >
                  <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                    <div className="min-w-0 flex-1 space-y-1">
                      <p className="text-lg font-semibold leading-tight">
                        {item.displayName}
                      </p>
                      <p className="text-sm text-muted-foreground">
                        {formatOrgClass(item.orgClass)} · Pending approval ·{" "}
                        {item.trustTier} trust
                      </p>
                    </div>
                    <div className="flex shrink-0 gap-2">
                      <Button
                        size="sm"
                        disabled={busy}
                        onClick={() => void onApprove(item.organizationId)}
                      >
                        Approve
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={busy}
                        onClick={() => void onReject(item.organizationId)}
                      >
                        Reject
                      </Button>
                    </div>
                  </div>

                  <dl className="grid gap-3 text-sm sm:grid-cols-2">
                    <div className="space-y-1">
                      <dt className="font-medium text-muted-foreground">
                        Owner contact
                      </dt>
                      <dd>
                        {owner.email ? (
                          <a
                            href={`mailto:${owner.email}`}
                            className="text-foreground underline-offset-4 hover:underline"
                          >
                            {owner.email}
                          </a>
                        ) : (
                          <span className="text-muted-foreground">
                            No local email on file
                          </span>
                        )}
                      </dd>
                    </div>
                    <div className="space-y-1">
                      <dt className="font-medium text-muted-foreground">
                        Identity provider
                      </dt>
                      <dd className="break-all">
                        {owner.idpIssuer}
                        <span className="text-muted-foreground">
                          {" "}
                          · subject {owner.idpSubject}
                        </span>
                      </dd>
                    </div>
                    <div className="space-y-1">
                      <dt className="font-medium text-muted-foreground">
                        Account
                      </dt>
                      <dd className="break-all font-mono text-xs">
                        {owner.accountId}
                        <span className="block font-sans text-sm text-muted-foreground">
                          Status: {owner.accountStatus}
                        </span>
                      </dd>
                    </div>
                    <div className="space-y-1">
                      <dt className="font-medium text-muted-foreground">
                        Organization
                      </dt>
                      <dd className="break-all font-mono text-xs">
                        {item.organizationId}
                      </dd>
                    </div>
                    <div className="space-y-1">
                      <dt className="font-medium text-muted-foreground">
                        Submitted
                      </dt>
                      <dd>{formatTimestamp(item.createdAt)}</dd>
                    </div>
                    <div className="space-y-1">
                      <dt className="font-medium text-muted-foreground">
                        Last updated
                      </dt>
                      <dd>{formatTimestamp(item.updatedAt)}</dd>
                    </div>
                  </dl>
                </li>
              );
            })}
          </ul>
        </CardContent>
      </Card>
    </div>
  );
}
