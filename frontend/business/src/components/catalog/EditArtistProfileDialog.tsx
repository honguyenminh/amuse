"use client";

import { CatalogTextEditor } from "@/components/catalog/CatalogTextEditor";
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
  updateArtist,
  type ManageArtistDetailResponse,
} from "@/lib/api/catalogClient";
import { useEffect, useState } from "react";

type EditArtistProfileDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  artist: ManageArtistDetailResponse;
  onUpdated: (artist: ManageArtistDetailResponse) => void;
};

export function EditArtistProfileDialog({
  open,
  onOpenChange,
  artist,
  onUpdated,
}: EditArtistProfileDialogProps) {
  const [name, setName] = useState(artist.name);
  const [bio, setBio] = useState(artist.bio ?? "");
  const [countryCode, setCountryCode] = useState(artist.countryCode ?? "");
  const [websiteUrl, setWebsiteUrl] = useState(artist.websiteUrl ?? "");
  const [aliases, setAliases] = useState(artist.aliases ?? "");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setName(artist.name);
      setBio(artist.bio ?? "");
      setCountryCode(artist.countryCode ?? "");
      setWebsiteUrl(artist.websiteUrl ?? "");
      setAliases(artist.aliases ?? "");
      setError(null);
    }
  }, [open, artist]);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    const trimmedName = name.trim();
    if (!trimmedName) {
      setError("Name is required.");
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      await updateArtist(artist.id, {
        name: trimmedName,
        bio: bio.trim() || null,
        countryCode: countryCode.trim() || null,
        websiteUrl: websiteUrl.trim() || null,
        aliases: aliases.trim() || null,
      });
      onUpdated({
        ...artist,
        name: trimmedName,
        bio: bio.trim() || null,
        countryCode: countryCode.trim() || null,
        websiteUrl: websiteUrl.trim() || null,
        aliases: aliases.trim() || null,
      });
      onOpenChange(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update artist.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Edit artist profile</DialogTitle>
          <DialogDescription>
            Slug /{artist.slug} cannot be changed here.
          </DialogDescription>
        </DialogHeader>
        <form className="flex flex-col" onSubmit={onSubmit}>
          <DialogBody>
          <div className="grid gap-2">
            <Label htmlFor="artist-name">Name</Label>
            <Input
              id="artist-name"
              value={name}
              onChange={(event) => setName(event.target.value)}
              disabled={submitting}
              required
            />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="artist-bio">Bio</Label>
            <CatalogTextEditor
              id="artist-bio"
              value={bio}
              onChange={setBio}
              disabled={submitting}
            />
          </div>
          <div className="grid gap-2 sm:grid-cols-2">
            <div className="grid gap-2">
              <Label htmlFor="artist-country">Country code</Label>
              <Input
                id="artist-country"
                value={countryCode}
                onChange={(event) => setCountryCode(event.target.value)}
                disabled={submitting}
                placeholder="US"
              />
            </div>
            <div className="grid gap-2">
              <Label htmlFor="artist-website">Website</Label>
              <Input
                id="artist-website"
                value={websiteUrl}
                onChange={(event) => setWebsiteUrl(event.target.value)}
                disabled={submitting}
                placeholder="https://"
              />
            </div>
          </div>
          <div className="grid gap-2">
            <Label htmlFor="artist-aliases">Aliases</Label>
            <Input
              id="artist-aliases"
              value={aliases}
              onChange={(event) => setAliases(event.target.value)}
              disabled={submitting}
              placeholder="Comma-separated alternate names"
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
