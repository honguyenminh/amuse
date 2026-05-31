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
import { cn } from "@/lib/utils";

type MembersInvitePanelProps = {
  open: boolean;
  presets: ClaimPresetResponse[];
  inviteEmail: string;
  invitePreset: string;
  busy: boolean;
  lastInvitedEmail: string | null;
  onEmailChange: (value: string) => void;
  onPresetChange: (value: string) => void;
  onSubmit: (event: React.FormEvent) => void;
};

export function MembersInvitePanel({
  open,
  presets,
  inviteEmail,
  invitePreset,
  busy,
  lastInvitedEmail,
  onEmailChange,
  onPresetChange,
  onSubmit,
}: MembersInvitePanelProps) {
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
                  <Label htmlFor="invite-preset">Permission preset</Label>
                  <select
                    id="invite-preset"
                    className="border-input bg-background h-9 w-full rounded-md border px-3 text-sm"
                    value={invitePreset}
                    onChange={(e) => onPresetChange(e.target.value)}
                  >
                    {presets.map((preset) => (
                      <option key={preset.label} value={preset.label}>
                        {preset.displayName} — {preset.description}
                      </option>
                    ))}
                  </select>
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
