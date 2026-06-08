"use client";

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
import type { ClaimPresetResponse } from "@/lib/api/tenancyClient";
import { summarizePermissionSelection, type PermissionSelection } from "@/lib/members/permissionSelection";
import { cn } from "@/lib/utils";
import { Shield } from "lucide-react";

type MembersInvitePanelProps = {
  open: boolean;
  presets: ClaimPresetResponse[];
  inviteEmail: string;
  permissionSelection: PermissionSelection;
  busy: boolean;
  lastInvitedEmail: string | null;
  onEmailChange: (value: string) => void;
  onChangePermissions: () => void;
  onSubmit: (event: React.FormEvent) => void;
};

export function MembersInvitePanel({
  open,
  presets,
  inviteEmail,
  permissionSelection,
  busy,
  lastInvitedEmail,
  onEmailChange,
  onChangePermissions,
  onSubmit,
}: MembersInvitePanelProps) {
  const summary = summarizePermissionSelection(presets, permissionSelection);

  return (
    <div
      className={cn(
        "grid transition-[grid-template-rows] duration-300 ease-out",
        open ? "grid-rows-[1fr]" : "grid-rows-[0fr]",
      )}
    >
      <div className="overflow-hidden">
        <Card className="mb-4 border-dashed shadow-sm">
          <CardHeader className="pb-3">
            <CardTitle className="text-base">Invite member</CardTitle>
            <CardDescription>
              Send an email invite. This panel stays open so you can add several people in a
              row.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form className="flex flex-col gap-4" onSubmit={onSubmit}>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="grid gap-2">
                  <Label htmlFor="invite-email">Email</Label>
                  <Input
                    id="invite-email"
                    type="email"
                    required
                    autoFocus={open}
                    placeholder="name@company.com"
                    value={inviteEmail}
                    onChange={(e) => onEmailChange(e.target.value)}
                  />
                </div>
                <div className="grid gap-2">
                  <Label>Permissions</Label>
                  <div className="flex min-h-9 flex-wrap items-center gap-2 rounded-md border bg-muted/20 px-3 py-2">
                    <Shield className="size-4 shrink-0 text-muted-foreground" />
                    <div className="min-w-0 flex-1">
                      <p className="text-sm font-medium text-foreground">{summary.title}</p>
                      {summary.detail ? (
                        <p className="text-xs text-muted-foreground">{summary.detail}</p>
                      ) : null}
                    </div>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      disabled={busy}
                      onClick={onChangePermissions}
                    >
                      Change permissions
                    </Button>
                  </div>
                </div>
              </div>
              <div className="flex flex-wrap items-center gap-3">
                <Button type="submit" disabled={busy}>
                  Send invite
                </Button>
                {lastInvitedEmail ? (
                  <p className="text-sm text-muted-foreground">
                    Last sent to <span className="font-medium text-foreground">{lastInvitedEmail}</span>
                  </p>
                ) : null}
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
