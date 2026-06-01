"use client";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  updateArtistReleaseGroup,
  type ManageReleaseGroupResponse,
} from "@/lib/api/catalogClient";
import { useEffect, useState } from "react";

type EditReleaseGroupDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  artistId: string;
  group: ManageReleaseGroupResponse;
  onUpdated: (group: ManageReleaseGroupResponse) => void;
};

export function EditReleaseGroupDialog({
  open,
  onOpenChange,
  artistId,
  group,
  onUpdated,
}: EditReleaseGroupDialogProps) {
  const [title, setTitle] = useState(group.title);
  const [description, setDescription] = useState(group.description ?? "");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setTitle(group.title);
      setDescription(group.description ?? "");
      setError(null);
    }
  }, [open, group]);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    const trimmedTitle = title.trim();
    if (!trimmedTitle) {
      setError("Title is required.");
      return;
    }

    setSubmitting(true);
    setError(null);
    try {
      const updated = await updateArtistReleaseGroup(artistId, group.id, {
        title: trimmedTitle,
        description: description.trim() || null,
      });
      onUpdated(updated);
      onOpenChange(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to update release group.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Edit release group</DialogTitle>
          <DialogDescription>Slug /{group.slug}</DialogDescription>
        </DialogHeader>
        <form className="flex flex-col gap-4" onSubmit={onSubmit}>
          <div className="grid gap-2">
            <Label htmlFor="group-title">Title</Label>
            <Input
              id="group-title"
              value={title}
              onChange={(event) => setTitle(event.target.value)}
              disabled={submitting}
              required
            />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="group-description">Description</Label>
            <textarea
              id="group-description"
              className="min-h-20 w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50"
              value={description}
              onChange={(event) => setDescription(event.target.value)}
              disabled={submitting}
            />
          </div>
          {error ? <p className="text-sm text-destructive">{error}</p> : null}
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
