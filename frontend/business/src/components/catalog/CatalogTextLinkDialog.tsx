"use client";

import { isAllowedLinkUrl } from "@amuse/catalog-text";
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
import { useEffect, useState } from "react";

export type CatalogTextLinkDialogProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  initialUrl?: string;
  initialLabel?: string;
  hasExistingLink?: boolean;
  onApply: (url: string, label: string) => void;
  onRemove?: () => void;
};

export function CatalogTextLinkDialog({
  open,
  onOpenChange,
  initialUrl = "https://",
  initialLabel = "",
  hasExistingLink = false,
  onApply,
  onRemove,
}: CatalogTextLinkDialogProps) {
  const [url, setUrl] = useState(initialUrl);
  const [label, setLabel] = useState(initialLabel);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setUrl(initialUrl);
    setLabel(initialLabel);
    setError(null);
  }, [open, initialUrl, initialLabel]);

  function handleApply() {
    const trimmedUrl = url.trim();
    const trimmedLabel = label.trim();

    if (!trimmedUrl) {
      setError("URL is required.");
      return;
    }
    if (!isAllowedLinkUrl(trimmedUrl)) {
      setError("Only http and https links are allowed.");
      return;
    }
    if (!trimmedLabel) {
      setError("Link text is required.");
      return;
    }

    onApply(trimmedUrl, trimmedLabel);
    onOpenChange(false);
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>{hasExistingLink ? "Edit link" : "Insert link"}</DialogTitle>
          <DialogDescription>
            {hasExistingLink
              ? "Update the link text or destination URL."
              : "Add display text and a destination URL."}{" "}
            Only http and https are allowed. Stored as markdown{" "}
            <code className="rounded bg-muted px-1 py-0.5 font-mono text-xs">[text](url)</code>.
          </DialogDescription>
        </DialogHeader>
        <DialogBody className="gap-4">
          <div className="grid gap-2">
            <Label htmlFor="catalog-link-label">Link text</Label>
            <Input
              id="catalog-link-label"
              value={label}
              onChange={(event) => setLabel(event.target.value)}
              placeholder="Display text"
              autoFocus
            />
          </div>
          <div className="grid gap-2">
            <Label htmlFor="catalog-link-url">URL</Label>
            <Input
              id="catalog-link-url"
              value={url}
              onChange={(event) => setUrl(event.target.value)}
              placeholder="https://example.com"
            />
          </div>
          {error ? <p className="text-sm text-destructive">{error}</p> : null}
        </DialogBody>
        <DialogFooter>
          {hasExistingLink && onRemove ? (
            <Button type="button" variant="outline" onClick={onRemove}>
              Remove link
            </Button>
          ) : null}
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button type="button" onClick={handleApply}>
            {hasExistingLink ? "Save link" : "Insert link"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
