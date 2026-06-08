"use client";

import { UserAvatar } from "@/components/account/UserAvatar";
import { MemberClaimsSheet } from "@/components/members/MemberClaimsSheet";
import { MemberRoleDialog } from "@/components/members/MemberRoleDialog";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type {
  ClaimPresetResponse,
  OrganizationCapabilities,
  OrganizationMemberResponse,
} from "@/lib/api/tenancyClient";
import {
  selectionFromMember,
  type PermissionSelection,
} from "@/lib/members/permissionSelection";
import { getPresetDisplayName } from "@/lib/members/presetDisplay";
import { formatTableDateTimeParts } from "@/lib/format/dateTime";
import { cn } from "@/lib/utils";
import { ArrowDown, ArrowUp, ArrowUpDown, MoreHorizontal, Shield } from "lucide-react";
import { useState } from "react";

export type MembersSortKey = "email" | "preset" | "lastLogin" | "lastActive" | "joined";

type PendingDestructiveAction =
  | { kind: "remove"; member: OrganizationMemberResponse }
  | { kind: "transfer"; member: OrganizationMemberResponse };

type MembersTableProps = {
  members: OrganizationMemberResponse[];
  presets: ClaimPresetResponse[];
  capabilities: OrganizationCapabilities | null;
  sortBy: MembersSortKey;
  sortDirection: "asc" | "desc";
  currentAccountId: string | null;
  canManage: boolean;
  canManagePermissions: boolean;
  canTransfer: boolean;
  busy: boolean;
  onSort: (column: MembersSortKey) => void;
  onApplyPermissions: (memberId: string, selection: PermissionSelection) => Promise<void>;
  onTransferOwnership: (memberId: string) => Promise<void>;
  onRemove: (memberId: string) => Promise<void>;
};

function SortHeader({
  label,
  column,
  sortBy,
  sortDirection,
  onSort,
}: {
  label: string;
  column: MembersSortKey;
  sortBy: MembersSortKey;
  sortDirection: "asc" | "desc";
  onSort: (column: MembersSortKey) => void;
}) {
  const active = sortBy === column;
  const Icon = !active ? ArrowUpDown : sortDirection === "asc" ? ArrowUp : ArrowDown;

  return (
    <button
      type="button"
      className="inline-flex items-center gap-1 font-medium hover:text-foreground"
      onClick={() => onSort(column)}
    >
      {label}
      <Icon className={cn("size-3.5", active ? "text-foreground" : "text-muted-foreground")} />
    </button>
  );
}

function TableDateCell({ value }: { value: string | null | undefined }) {
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

function memberLabel(member: OrganizationMemberResponse): string {
  return member.displayName ?? member.email ?? member.accountId;
}

function memberPrimaryLine(member: OrganizationMemberResponse): string {
  return member.displayName ?? member.email ?? member.accountId;
}

export function MembersTable({
  members,
  presets,
  capabilities,
  sortBy,
  sortDirection,
  currentAccountId,
  canManage,
  canManagePermissions,
  canTransfer,
  busy,
  onSort,
  onApplyPermissions,
  onTransferOwnership,
  onRemove,
}: MembersTableProps) {
  const [claimsMember, setClaimsMember] = useState<OrganizationMemberResponse | null>(null);
  const [roleMember, setRoleMember] = useState<OrganizationMemberResponse | null>(null);
  const [pendingAction, setPendingAction] = useState<PendingDestructiveAction | null>(null);
  const [confirmBusy, setConfirmBusy] = useState(false);
  const showActions = canManage || canManagePermissions || canTransfer;
  const actionsDisabled = busy || confirmBusy;

  async function onConfirmDestructive() {
    if (!pendingAction) {
      return;
    }

    setConfirmBusy(true);
    try {
      if (pendingAction.kind === "remove") {
        await onRemove(pendingAction.member.id);
      } else {
        await onTransferOwnership(pendingAction.member.id);
      }
      setPendingAction(null);
    } catch {
      // Parent surfaces the error; keep the dialog open for retry or cancel.
    } finally {
      setConfirmBusy(false);
    }
  }

  if (members.length === 0) {
    return (
      <p className="rounded-md border border-dashed bg-background px-4 py-12 text-center text-sm text-muted-foreground">
        No members match your search.
      </p>
    );
  }

  return (
    <>
      <div className="overflow-x-auto rounded-md border bg-background">
        <table className="w-full min-w-[960px] table-fixed border-collapse text-sm">
          <colgroup>
            <col className="w-[30%]" />
            <col className="w-[14%]" />
            <col className="w-[14%]" />
            <col className="w-[11%]" />
            <col className="w-[11%]" />
            <col className="w-[11%]" />
            {showActions ? <col className="w-12" /> : null}
          </colgroup>
          <thead>
            <tr className="border-b bg-muted/50 text-left text-muted-foreground">
              <th className="px-4 py-3">
                <SortHeader
                  label="Member"
                  column="email"
                  sortBy={sortBy}
                  sortDirection={sortDirection}
                  onSort={onSort}
                />
              </th>
              <th className="px-4 py-3">
                <SortHeader
                  label="Role"
                  column="preset"
                  sortBy={sortBy}
                  sortDirection={sortDirection}
                  onSort={onSort}
                />
              </th>
              <th className="px-4 py-3">Claims</th>
              <th className="px-4 py-3">
                <SortHeader
                  label="Joined"
                  column="joined"
                  sortBy={sortBy}
                  sortDirection={sortDirection}
                  onSort={onSort}
                />
              </th>
              <th className="px-4 py-3">
                <SortHeader
                  label="Last login"
                  column="lastLogin"
                  sortBy={sortBy}
                  sortDirection={sortDirection}
                  onSort={onSort}
                />
              </th>
              <th className="px-4 py-3">
                <SortHeader
                  label="Last active"
                  column="lastActive"
                  sortBy={sortBy}
                  sortDirection={sortDirection}
                  onSort={onSort}
                />
              </th>
              {showActions ? <th className="px-4 py-3" /> : null}
            </tr>
          </thead>
          <tbody>
            {members.map((member) => {
              const isSelf =
                currentAccountId !== null && member.accountId === currentAccountId;
              const canRemoveMember = canManage && !member.isOwner && !isSelf;
              const showRowActions =
                !member.isOwner &&
                (canManagePermissions || canTransfer || canRemoveMember);

              return (
              <tr key={member.id} className="border-b last:border-b-0 hover:bg-muted/30">
                <td className="min-w-0 px-4 py-3 align-top">
                  <div className="flex items-start gap-3">
                    <UserAvatar
                      displayName={member.displayName}
                      email={member.email}
                      accentSeed={member.avatarAccentSeed}
                      avatarUrl={member.avatarUrl}
                      size="sm"
                    />
                    <div className="min-w-0">
                      <div className="break-all font-medium text-foreground">
                        {memberPrimaryLine(member)}
                        {isSelf ? (
                          <span className="ml-2 text-xs font-normal text-muted-foreground">
                            You
                          </span>
                        ) : null}
                      </div>
                      {member.displayName && member.email ? (
                        <div className="break-all text-xs text-muted-foreground">
                          {member.email}
                        </div>
                      ) : null}
                      {member.isOwner ? (
                        <span className="text-xs text-muted-foreground">Owner</span>
                      ) : null}
                    </div>
                  </div>
                </td>
                <td className="px-4 py-3 align-top text-muted-foreground">
                  {canManagePermissions && !member.isOwner ? (
                    <button
                      type="button"
                      className="break-words text-left hover:text-foreground hover:underline"
                      onClick={() => setRoleMember(member)}
                    >
                      {getPresetDisplayName(presets, member.presetRoleLabel)}
                    </button>
                  ) : (
                    <span className="break-words">
                      {getPresetDisplayName(presets, member.presetRoleLabel)}
                    </span>
                  )}
                </td>
                <td className="px-4 py-3 align-top">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="h-8 gap-1.5 text-xs"
                    onClick={() => setClaimsMember(member)}
                  >
                    <Shield className="size-3.5 shrink-0" />
                    {member.claims.length === 0
                      ? "No claims"
                      : `View ${member.claims.length} claim${member.claims.length === 1 ? "" : "s"}`}
                  </Button>
                </td>
                <td className="min-w-0 px-4 py-3 align-top">
                  <TableDateCell value={member.joinedAt} />
                </td>
                <td className="min-w-0 px-4 py-3 align-top">
                  <TableDateCell value={member.lastLoginAt} />
                </td>
                <td className="min-w-0 px-4 py-3 align-top">
                  <TableDateCell value={member.lastActiveAt} />
                </td>
                {showActions ? (
                  <td className="px-4 py-3 align-top">
                    {showRowActions ? (
                      <DropdownMenu>
                        <DropdownMenuTrigger
                          render={
                            <Button variant="ghost" size="icon-sm" disabled={actionsDisabled}>
                              <MoreHorizontal />
                              <span className="sr-only">Member actions</span>
                            </Button>
                          }
                        />
                        <DropdownMenuContent align="end">
                          {canManagePermissions ? (
                            <DropdownMenuItem onClick={() => setRoleMember(member)}>
                              Change role
                            </DropdownMenuItem>
                          ) : null}
                          {canTransfer ? (
                            <>
                              {canManagePermissions ? <DropdownMenuSeparator /> : null}
                              <DropdownMenuItem
                                onClick={() => setPendingAction({ kind: "transfer", member })}
                              >
                                Transfer ownership
                              </DropdownMenuItem>
                            </>
                          ) : null}
                          {canRemoveMember ? (
                            <>
                              {canManagePermissions || canTransfer ? (
                                <DropdownMenuSeparator />
                              ) : null}
                              <DropdownMenuItem
                                variant="destructive"
                                onClick={() => setPendingAction({ kind: "remove", member })}
                              >
                                Remove member
                              </DropdownMenuItem>
                            </>
                          ) : null}
                        </DropdownMenuContent>
                      </DropdownMenu>
                    ) : null}
                  </td>
                ) : null}
              </tr>
            );
            })}
          </tbody>
        </table>
      </div>

      <MemberClaimsSheet
        member={claimsMember}
        presets={presets}
        canManagePermissions={canManagePermissions}
        onEditRole={(member) => setRoleMember(member)}
        onOpenChange={(open) => {
          if (!open) {
            setClaimsMember(null);
          }
        }}
      />

      <MemberRoleDialog
        key={roleMember?.id ?? "closed"}
        mode="edit"
        open={roleMember !== null}
        member={roleMember}
        presets={presets}
        capabilities={capabilities}
        initialSelection={
          roleMember ? selectionFromMember(presets, roleMember) : { claims: [], presetLabel: null }
        }
        busy={busy}
        onOpenChange={(open) => {
          if (!open) {
            setRoleMember(null);
          }
        }}
        onConfirm={async (selection) => {
          if (!roleMember) {
            return;
          }
          await onApplyPermissions(roleMember.id, selection);
        }}
      />

      <ConfirmDialog
        open={pendingAction !== null}
        onOpenChange={(open) => {
          if (!open && !confirmBusy) {
            setPendingAction(null);
          }
        }}
        title={
          pendingAction?.kind === "transfer"
            ? "Transfer organization ownership?"
            : "Remove member?"
        }
        description={
          pendingAction === null
            ? ""
            : pendingAction.kind === "transfer"
              ? `${memberLabel(pendingAction.member)} will become the organization owner. You will lose owner privileges immediately.`
              : `${memberLabel(pendingAction.member)} will lose access to this organization.`
        }
        confirmLabel={
          pendingAction?.kind === "transfer" ? "Transfer ownership" : "Remove member"
        }
        destructive
        busy={confirmBusy}
        onConfirm={onConfirmDestructive}
      />
    </>
  );
}
