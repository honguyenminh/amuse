"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  createOrganization,
  type OrgClass,
  type OrganizationResponse,
} from "@/lib/api/tenancyClient";
import { ApiError } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { Building2, Mic2 } from "lucide-react";
import Link from "next/link";
import { useState } from "react";

const MAX_DISPLAY_NAME_LENGTH = 200;

type CreateOrganizationFormProps = {
  onCreated: (organization: OrganizationResponse) => Promise<void>;
  cancelHref?: string;
};

const orgClassOptions: {
  value: OrgClass;
  title: string;
  description: string;
  icon: typeof Mic2;
}[] = [
  {
    value: "indieGroup",
    title: "Indie group",
    description:
      "For solo artists or small collectives. Start uploading drafts immediately with discover-mode visibility rules.",
    icon: Mic2,
  },
  {
    value: "backingOrg",
    title: "Backing organization",
    description:
      "For labels or distributors. Your application is reviewed by platform operators before publish and payout features unlock.",
    icon: Building2,
  },
];

export function CreateOrganizationForm({
  onCreated,
  cancelHref,
}: CreateOrganizationFormProps) {
  const [displayName, setDisplayName] = useState("");
  const [orgClass, setOrgClass] = useState<OrgClass>("indieGroup");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    const trimmed = displayName.trim();
    if (!trimmed) {
      setError("Display name is required.");
      return;
    }
    if (trimmed.length > MAX_DISPLAY_NAME_LENGTH) {
      setError(`Display name must be at most ${MAX_DISPLAY_NAME_LENGTH} characters.`);
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      const organization = await createOrganization(trimmed, orgClass);
      await onCreated(organization);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError(
          err instanceof Error ? err.message : "Could not create organization.",
        );
      }
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="flex flex-col gap-6" onSubmit={onSubmit}>
      <div className="grid gap-2">
        <Label htmlFor="displayName">Organization name</Label>
        <Input
          id="displayName"
          value={displayName}
          onChange={(event) => setDisplayName(event.target.value)}
          placeholder="e.g. Northern Lights Collective"
          maxLength={MAX_DISPLAY_NAME_LENGTH}
          autoComplete="organization"
          required
        />
        <p className="text-xs text-muted-foreground">
          This is shown in the workspace switcher and organization profile.
        </p>
      </div>

      <fieldset className="flex flex-col gap-3">
        <legend className="text-sm font-medium">Organization type</legend>
        <div className="grid gap-3 sm:grid-cols-2">
          {orgClassOptions.map((option) => {
            const Icon = option.icon;
            const selected = orgClass === option.value;
            return (
              <button
                key={option.value}
                type="button"
                onClick={() => setOrgClass(option.value)}
                className={cn(
                  "flex flex-col items-start gap-2 rounded-lg border p-4 text-left transition-colors",
                  selected
                    ? "border-primary bg-primary/5 ring-1 ring-primary"
                    : "border-border hover:bg-muted/50",
                )}
              >
                <Icon className="size-4 text-primary" />
                <span className="font-medium">{option.title}</span>
                <span className="text-xs text-muted-foreground">
                  {option.description}
                </span>
              </button>
            );
          })}
        </div>
      </fieldset>

      {error ? <p className="text-sm text-destructive">{error}</p> : null}

      <div className="flex flex-wrap gap-2">
        <Button type="submit" disabled={submitting}>
          {submitting ? "Creating…" : "Create organization"}
        </Button>
        {cancelHref ? (
          <Button
            type="button"
            variant="outline"
            render={<Link href={cancelHref} />}
          >
            Cancel
          </Button>
        ) : null}
      </div>
    </form>
  );
}
