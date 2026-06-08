"use client";

import { MemberRoleDialog } from "@/components/members/MemberRoleDialog";
import { MembersInvitePanel } from "@/components/members/MembersInvitePanel";
import { MembersTable, type MembersSortKey } from "@/components/members/MembersTable";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useAuth } from "@/lib/auth/AuthProvider";
import { hasClaim } from "@/lib/auth/jwtClaims";
import { getAccessToken } from "@/lib/auth/sessionStore";
import {
  createOrganizationInvite,
  getOrganization,
  listClaimPresets,
  listOrganizationMembers,
  removeOrganizationMember,
  transferOrganizationOwnership,
  updateOrganizationMember,
  type ClaimPresetResponse,
  type OrganizationCapabilities,
  type OrganizationMemberResponse,
} from "@/lib/api/tenancyClient";
import {
  buildPermissionPayload,
  defaultInviteSelection,
  type PermissionSelection,
} from "@/lib/members/permissionSelection";
import { ChevronRight, MailPlus, UserPlus } from "lucide-react";
import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";

const PAGE_SIZE = 50;

function toApiSortKey(key: MembersSortKey): "email" | "preset" | "lastlogin" | "lastactive" | "joined" {
  switch (key) {
    case "lastLogin":
      return "lastlogin";
    case "lastActive":
      return "lastactive";
    case "joined":
      return "joined";
    case "preset":
      return "preset";
    default:
      return "email";
  }
}

export default function OrganizationMembersPage() {
  const auth = useAuth();
  const orgId = auth.activePersona?.type === "org" ? auth.activePersona.orgId : null;
  const token = getAccessToken();

  const canRead = hasClaim(token, "read:membership:all");
  const canManage = hasClaim(token, "manage:membership:all");
  const canManagePermissions = hasClaim(token, "manage:member_permissions:all");
  const canTransfer = hasClaim(token, "manage:org:all");

  const [members, setMembers] = useState<OrganizationMemberResponse[]>([]);
  const [presets, setPresets] = useState<ClaimPresetResponse[]>([]);
  const [capabilities, setCapabilities] = useState<OrganizationCapabilities | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [pendingInviteCount, setPendingInviteCount] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [sortBy, setSortBy] = useState<MembersSortKey>("email");
  const [sortDirection, setSortDirection] = useState<"asc" | "desc">("asc");
  const [invitePanelOpen, setInvitePanelOpen] = useState(false);
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteSelection, setInviteSelection] = useState<PermissionSelection>({
    claims: [],
    presetLabel: "member_manager",
  });
  const [invitePermissionsOpen, setInvitePermissionsOpen] = useState(false);
  const [lastInvitedEmail, setLastInvitedEmail] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(false);
  const loadMoreRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const timer = window.setTimeout(() => setDebouncedSearch(search.trim()), 300);
    return () => window.clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    setPage(1);
    setMembers([]);
  }, [debouncedSearch, sortBy, sortDirection, orgId]);

  const loadPage = useCallback(
    async (pageToLoad: number, append: boolean) => {
      if (!orgId || !canRead) {
        return;
      }

      setLoading(true);
      setError(null);
      try {
        const result = await listOrganizationMembers(orgId, {
          search: debouncedSearch || undefined,
          sortBy: toApiSortKey(sortBy),
          sortDirection,
          page: pageToLoad,
          pageSize: PAGE_SIZE,
        });

        setMembers((current) =>
          append ? [...current, ...result.items] : result.items,
        );
        setTotalCount(result.totalCount);
        setPendingInviteCount(result.pendingInviteCount);
      } catch (e) {
        setError(e instanceof Error ? e.message : "Failed to load members.");
      } finally {
        setLoading(false);
      }
    },
    [orgId, canRead, debouncedSearch, sortBy, sortDirection],
  );

  useEffect(() => {
    void listClaimPresets()
      .then((loadedPresets) => {
        setPresets(loadedPresets);
        setInviteSelection((current) =>
          current.claims.length > 0 ? current : defaultInviteSelection(loadedPresets),
        );
      })
      .catch(() => undefined);
  }, []);

  useEffect(() => {
    if (!orgId) {
      setCapabilities(null);
      return;
    }

    void getOrganization(orgId)
      .then((organization) => setCapabilities(organization.capabilities))
      .catch(() => setCapabilities(null));
  }, [orgId]);

  useEffect(() => {
    void loadPage(page, page > 1);
  }, [page, loadPage]);

  const hasMore = members.length < totalCount;

  useEffect(() => {
    const node = loadMoreRef.current;
    if (!node || !hasMore || loading) {
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0]?.isIntersecting) {
          setPage((current) => current + 1);
        }
      },
      { root: null, rootMargin: "200px", threshold: 0 },
    );

    observer.observe(node);
    return () => observer.disconnect();
  }, [hasMore, loading, members.length]);

  function onSort(column: MembersSortKey) {
    if (sortBy === column) {
      setSortDirection((current) => (current === "asc" ? "desc" : "asc"));
      return;
    }
    setSortBy(column);
    setSortDirection("asc");
  }

  async function onInvite(event: React.FormEvent) {
    event.preventDefault();
    if (!orgId || !canManage) {
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const email = inviteEmail.trim();
      await createOrganizationInvite(orgId, {
        email,
        ...buildPermissionPayload(inviteSelection),
      });
      setLastInvitedEmail(email);
      setInviteEmail("");
      setPage(1);
      setMembers([]);
      await loadPage(1, false);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to send invite.");
    } finally {
      setBusy(false);
    }
  }

  async function reload() {
    setPage(1);
    setMembers([]);
    await loadPage(1, false);
  }

  if (!orgId) {
    return (
      <p className="text-sm text-muted-foreground">
        Select an organization workspace to manage members.
      </p>
    );
  }

  if (!canRead) {
    return (
      <p className="text-sm text-muted-foreground">
        Your current workspace token does not include membership read permission. Refresh your
        org persona after an admin updates your claims.
      </p>
    );
  }

  return (
    <div className="flex w-full flex-col">
      <div className="sticky top-14 z-20 -mx-4 border-b bg-muted/95 px-4 py-4 backdrop-blur supports-backdrop-filter:bg-muted/80 md:-mx-6 md:px-6">
        <div className="flex flex-col gap-4">
          <p className="text-sm text-muted-foreground">
            {totalCount} member{totalCount === 1 ? "" : "s"}
            {pendingInviteCount > 0
              ? ` · ${pendingInviteCount} pending invite${pendingInviteCount === 1 ? "" : "s"}`
              : ""}
          </p>
          <div className="flex flex-col gap-3 lg:flex-row lg:items-center">
            <Input
              type="search"
              placeholder="Search by email, preset, or claim…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="bg-background lg:max-w-md"
            />
            <div className="flex flex-wrap gap-2 lg:ml-auto">
              {canManage ? (
                <>
                  <Button
                    variant={invitePanelOpen ? "secondary" : "default"}
                    onClick={() => setInvitePanelOpen((open) => !open)}
                  >
                    <UserPlus />
                    {invitePanelOpen ? "Hide invite form" : "Invite member"}
                  </Button>
                  <Button variant="outline" render={<Link href="/members/invites" />}>
                    <MailPlus />
                    Pending invites
                    {pendingInviteCount > 0 ? ` (${pendingInviteCount})` : ""}
                    <ChevronRight className="size-4" />
                  </Button>
                </>
              ) : null}
            </div>
          </div>
        </div>
      </div>

      <div className="flex flex-col pt-4">
        {error ? <p className="mb-4 text-sm text-destructive">{error}</p> : null}

        {canManage ? (
          <MembersInvitePanel
            open={invitePanelOpen}
            presets={presets}
            inviteEmail={inviteEmail}
            permissionSelection={inviteSelection}
            busy={busy}
            lastInvitedEmail={lastInvitedEmail}
            onEmailChange={setInviteEmail}
            onChangePermissions={() => setInvitePermissionsOpen(true)}
            onSubmit={onInvite}
          />
        ) : null}

        <MembersTable
          members={members}
          presets={presets}
          capabilities={capabilities}
          sortBy={sortBy}
          sortDirection={sortDirection}
          currentAccountId={auth.account?.accountId ?? null}
          canManage={canManage}
          canManagePermissions={canManagePermissions}
          canTransfer={canTransfer}
          busy={busy}
          onSort={onSort}
          onApplyPermissions={async (memberId, selection) => {
            if (!orgId) return;
            setBusy(true);
            try {
              await updateOrganizationMember(orgId, memberId, buildPermissionPayload(selection));
              await reload();
            } catch (e) {
              setError(e instanceof Error ? e.message : "Failed to update member.");
            } finally {
              setBusy(false);
            }
          }}
          onTransferOwnership={async (memberId) => {
            if (!orgId) return;
            setBusy(true);
            setError(null);
            try {
              await transferOrganizationOwnership(orgId, memberId);
              await reload();
            } catch (e) {
              const message =
                e instanceof Error ? e.message : "Failed to transfer ownership.";
              setError(message);
              throw e;
            } finally {
              setBusy(false);
            }
          }}
          onRemove={async (memberId) => {
            if (!orgId) return;
            setBusy(true);
            setError(null);
            try {
              await removeOrganizationMember(orgId, memberId);
              await reload();
            } catch (e) {
              const message = e instanceof Error ? e.message : "Failed to remove member.";
              setError(message);
              throw e;
            } finally {
              setBusy(false);
            }
          }}
        />

        <div ref={loadMoreRef} className="py-6 text-center text-sm text-muted-foreground">
          {loading
            ? "Loading…"
            : hasMore
              ? "Scroll for more members"
              : members.length > 0
                ? `Showing all ${members.length} of ${totalCount}`
                : null}
        </div>
      </div>

      <MemberRoleDialog
        mode="invite"
        open={invitePermissionsOpen}
        member={null}
        presets={presets}
        capabilities={capabilities}
        initialSelection={inviteSelection}
        busy={busy}
        onOpenChange={setInvitePermissionsOpen}
        onConfirm={async (selection) => {
          setInviteSelection(selection);
        }}
      />
    </div>
  );
}
