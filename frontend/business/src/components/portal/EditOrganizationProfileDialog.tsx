"use client";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogBody,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  updateOrganizationProfile,
  type OrganizationResponse,
} from "@/lib/api/tenancyClient";
import { useEffect, useState } from "react";

type EditOrganizationProfileDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  organization: OrganizationResponse;
  onUpdated: (organization: OrganizationResponse) => void;
};

export function EditOrganizationProfileDialog({
  open,
  onOpenChange,
  organization,
  onUpdated,
}: EditOrganizationProfileDialogProps) {
  const [description, setDescription] = useState(organization.description ?? "");
  const [websiteUrl, setWebsiteUrl] = useState(organization.websiteUrl ?? "");
  const [countryCode, setCountryCode] = useState(organization.countryCode ?? "");
  const [imprintName, setImprintName] = useState(organization.imprintName ?? "");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setDescription(organization.description ?? "");
      setWebsiteUrl(organization.websiteUrl ?? "");
      setCountryCode(organization.countryCode ?? "");
      setImprintName(organization.imprintName ?? "");
      setError(null);
    }
  }, [open, organization]);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      const updated = await updateOrganizationProfile(organization.id, {
        description: description.trim() || null,
        websiteUrl: websiteUrl.trim() || null,
        countryCode: countryCode.trim() || null,
        imprintName: imprintName.trim() || null,
      });
      onUpdated(updated);
      onOpenChange(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update organization profile.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Edit organization profile</DialogTitle>
          <DialogDescription>
            {organization.displayName} · display name and type cannot be changed here.
          </DialogDescription>
        </DialogHeader>
        <form className="flex flex-col" onSubmit={onSubmit}>
          <DialogBody>
          <div className="grid gap-2">
            <Label htmlFor="org-description">Description</Label>
            <textarea
              id="org-description"
              className="min-h-24 w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              disabled={submitting}
            />
          </div>
          <div className="grid gap-2 sm:grid-cols-2">
            <div className="grid gap-2">
              <Label htmlFor="org-country">Country code</Label>
              <Input
                id="org-country"
                value={countryCode}
                onChange={(event) => setCountryCode(event.target.value)}
                disabled={submitting}
                placeholder="US"
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="org-imprint">Imprint name</Label>
              <Input
                id="org-imprint"
                value={imprintName}
                onChange={(event) => setImprintName(event.target.value)}
                disabled={submitting}
              />
            </div>
          </div>
          <div className="grid gap-2">
            <Label htmlFor="org-website">Website</Label>
            <Input
              id="org-website"
              value={websiteUrl}
              onChange={(event) => setWebsiteUrl(event.target.value)}
              disabled={submitting}
              placeholder="https://"
            />
          </div>
          {error ? <p className="text-sm text-destructive">{error}</p> : null}
          </DialogBody>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Cancel
            </Button>
            <Button type="submit" disabled={submitting}>
              {submitting ? "Saving…" : "Save changes"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
